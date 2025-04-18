using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using IntexBackend.Data;
using IntexBackend.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace IntexBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IConfiguration _configuration;
        private readonly ApplicationDbContext _context;

        public AuthController(
            UserManager<IdentityUser> userManager,
            SignInManager<IdentityUser> signInManager,
            RoleManager<IdentityRole> roleManager,
            IConfiguration configuration,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _configuration = configuration;
            _context = context;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var user = new IdentityUser
            {
                UserName = model.Email,
                Email = model.Email,
                EmailConfirmed = true // For simplicity, we're auto-confirming emails
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                return BadRequest(ModelState);
            }

            // Add user to the User role
            await _userManager.AddToRoleAsync(user, "User");

            // Create a MovieUser record with the additional information
            try
            {
                // Parse age and zip to integers, defaulting to 0 if parsing fails
                int age = 0;
                if (!string.IsNullOrEmpty(model.Age))
                {
                    int.TryParse(model.Age, out age);
                }

                int zip = 0;
                if (!string.IsNullOrEmpty(model.Zip))
                {
                    int.TryParse(model.Zip, out zip);
                }

                var movieUser = new MovieUser
                {
                    Name = model.FullName,
                    Phone = model.Phone,
                    Email = model.Email,
                    Age = age,
                    Gender = model.Gender,
                    City = model.City,
                    State = model.State,
                    Zip = zip,
                // Set streaming service flags - convert any string values to integers
                Netflix = ConvertServiceFlagToInt(model.Services, "Netflix"),
                AmazonPrime = ConvertServiceFlagToInt(model.Services, "Amazon Prime"),
                DisneyPlus = ConvertServiceFlagToInt(model.Services, "Disney+"),
                ParamountPlus = ConvertServiceFlagToInt(model.Services, "Paramount+"),
                Max = ConvertServiceFlagToInt(model.Services, "Max"),
                Hulu = ConvertServiceFlagToInt(model.Services, "Hulu"),
                AppleTVPlus = ConvertServiceFlagToInt(model.Services, "Apple TV+"),
                Peacock = ConvertServiceFlagToInt(model.Services, "Peacock")
                };

                _context.MovieUsers.Add(movieUser);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                // Log the error and delete the identity user to ensure consistency
                Console.WriteLine($"Error creating MovieUser record: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                
                // Delete the identity user to maintain consistency
                await _userManager.DeleteAsync(user);
                
                // Return a more specific error message
                ModelState.AddModelError(string.Empty, "Failed to create user profile. Please try again.");
                return BadRequest(ModelState);
            }

            // Generate JWT token for the newly registered user
            var token = await GenerateJwtToken(user);
            var roles = await _userManager.GetRolesAsync(user);

            return Ok(new
            {
                message = "User registered successfully",
                token,
                user = new
                {
                    id = user.Id,
                    email = user.Email,
                    roles
                }
            });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                return Unauthorized(new { message = "Invalid email or password" });
            }

            var result = await _signInManager.CheckPasswordSignInAsync(user, model.Password, false);
            if (!result.Succeeded)
            {
                return Unauthorized(new { message = "Invalid email or password" });
            }

            var token = await GenerateJwtToken(user);
            var roles = await _userManager.GetRolesAsync(user);

            return Ok(new
            {
                token,
                user = new
                {
                    id = user.Id,
                    email = user.Email,
                    roles
                }
            });
        }

        [HttpGet("ping")]
        public IActionResult Ping()
        {
            return Ok(new { message = "Auth service is running" });
        }

        [HttpGet("pingauth")]
        public IActionResult PingAuth()
        {
            return Ok(new { message = "Auth service is running (no auth required)" });
        }

        [HttpGet("user-info")]
        [Microsoft.AspNetCore.Authorization.Authorize]
        public async Task<IActionResult> GetUserInfo()
        {
            // Get the user's email from the claims
            var email = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
            
            if (string.IsNullOrEmpty(email))
            {
                return Unauthorized(new { message = "User not authenticated" });
            }

            // Find the MovieUser by email
            var movieUser = await _context.MovieUsers.FirstOrDefaultAsync(u => u.Email == email);
            
            if (movieUser == null)
            {
                return NotFound(new { message = "User profile not found" });
            }

            // Check if user is under 16 for Kids Mode enforcement
            bool enforceKidsMode = movieUser.Age < 16;

            // Convert streaming service flags to a list of service names
            var services = new List<string>();
            if (movieUser.Netflix == 1) services.Add("Netflix");
            if (movieUser.AmazonPrime == 1) services.Add("AmazonPrime");
            if (movieUser.DisneyPlus == 1) services.Add("DisneyPlus");
            if (movieUser.Hulu == 1) services.Add("Hulu");
            if (movieUser.Max == 1) services.Add("Max");
            if (movieUser.AppleTVPlus == 1) services.Add("AppleTVPlus");
            if (movieUser.ParamountPlus == 1) services.Add("ParamountPlus");
            if (movieUser.Peacock == 1) services.Add("Peacock");

            // Split the name into first and last name if available
            string firstName = null;
            string lastName = null;
            if (!string.IsNullOrEmpty(movieUser.Name))
            {
                var nameParts = movieUser.Name.Split(' ', 2);
                firstName = nameParts[0];
                if (nameParts.Length > 1)
                {
                    lastName = nameParts[1];
                }
            }

            return Ok(new
            {
                email = movieUser.Email,
                firstName = firstName,
                lastName = lastName,
                phone = movieUser.Phone,
                age = movieUser.Age.ToString(),
                gender = movieUser.Gender,
                city = movieUser.City,
                state = movieUser.State,
                zip = movieUser.Zip.ToString(),
                services = services,
                enforceKidsMode = enforceKidsMode
            });
        }

        [HttpPut("update")]
        [Microsoft.AspNetCore.Authorization.Authorize]
        public async Task<IActionResult> UpdateUser([FromBody] UpdateUserModel model)
        {
            // Get the user's email from the claims
            var email = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
            
            if (string.IsNullOrEmpty(email))
            {
                return Unauthorized(new { message = "User not authenticated" });
            }

            // Find the MovieUser by email
            var movieUser = await _context.MovieUsers.FirstOrDefaultAsync(u => u.Email == email);
            
            if (movieUser == null)
            {
                return NotFound(new { message = "User profile not found" });
            }

            // Update the user's information
            // Combine first and last name into the Name field
            if (!string.IsNullOrEmpty(model.FirstName))
            {
                movieUser.Name = !string.IsNullOrEmpty(model.LastName) 
                    ? $"{model.FirstName} {model.LastName}" 
                    : model.FirstName;
            }

            // Update other fields if provided
            if (!string.IsNullOrEmpty(model.Phone))
            {
                movieUser.Phone = model.Phone;
            }

            if (!string.IsNullOrEmpty(model.Age))
            {
                if (int.TryParse(model.Age, out int age))
                {
                    movieUser.Age = age;
                }
            }

            if (!string.IsNullOrEmpty(model.Gender))
            {
                movieUser.Gender = model.Gender;
            }

            if (!string.IsNullOrEmpty(model.City))
            {
                movieUser.City = model.City;
            }

            if (!string.IsNullOrEmpty(model.State))
            {
                movieUser.State = model.State;
            }

            if (!string.IsNullOrEmpty(model.Zip))
            {
                if (int.TryParse(model.Zip, out int zip))
                {
                    movieUser.Zip = zip;
                }
            }

            // Update streaming services
            if (model.Services != null)
            {
                movieUser.Netflix = model.Services.Contains("Netflix") ? 1 : 0;
                movieUser.AmazonPrime = model.Services.Contains("AmazonPrime") ? 1 : 0;
                movieUser.DisneyPlus = model.Services.Contains("DisneyPlus") ? 1 : 0;
                movieUser.Hulu = model.Services.Contains("Hulu") ? 1 : 0;
                movieUser.Max = model.Services.Contains("Max") ? 1 : 0;
                movieUser.AppleTVPlus = model.Services.Contains("AppleTVPlus") ? 1 : 0;
                movieUser.ParamountPlus = model.Services.Contains("ParamountPlus") ? 1 : 0;
                movieUser.Peacock = model.Services.Contains("Peacock") ? 1 : 0;
            }

            // Save changes to the database
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = $"Failed to update user profile: {ex.Message}" });
            }

            // Return the updated user information
            // Convert streaming service flags to a list of service names for the response
            var services = new List<string>();
            if (movieUser.Netflix == 1) services.Add("Netflix");
            if (movieUser.AmazonPrime == 1) services.Add("AmazonPrime");
            if (movieUser.DisneyPlus == 1) services.Add("DisneyPlus");
            if (movieUser.Hulu == 1) services.Add("Hulu");
            if (movieUser.Max == 1) services.Add("Max");
            if (movieUser.AppleTVPlus == 1) services.Add("AppleTVPlus");
            if (movieUser.ParamountPlus == 1) services.Add("ParamountPlus");
            if (movieUser.Peacock == 1) services.Add("Peacock");

            // Split the name into first and last name if available
            string firstName = null;
            string lastName = null;
            if (!string.IsNullOrEmpty(movieUser.Name))
            {
                var nameParts = movieUser.Name.Split(' ', 2);
                firstName = nameParts[0];
                if (nameParts.Length > 1)
                {
                    lastName = nameParts[1];
                }
            }

            return Ok(new
            {
                email = movieUser.Email,
                firstName = firstName,
                lastName = lastName,
                phone = movieUser.Phone,
                age = movieUser.Age.ToString(),
                gender = movieUser.Gender,
                city = movieUser.City,
                state = movieUser.State,
                zip = movieUser.Zip.ToString(),
                services = services
            });
        }

        // Helper method to convert service flag to integer
        private int ConvertServiceFlagToInt(List<string>? services, string serviceName)
        {
            if (services == null)
                return 0;
                
            // Check if the service exists in the list (case-insensitive)
            bool hasService = services.Any(s => string.Equals(s, serviceName, StringComparison.OrdinalIgnoreCase));
            
            // Handle both string and integer values
            foreach (var service in services)
            {
                // If the service is already a "1" or "0" string with the service name
                if (service == $"{serviceName}:1" || service == $"{serviceName}=1")
                    return 1;
                    
                if (service == $"{serviceName}:0" || service == $"{serviceName}=0")
                    return 0;
            }
            
            // Default behavior: return 1 if service is in the list, 0 otherwise
            return hasService ? 1 : 0;
        }

        private async Task<string> GenerateJwtToken(IdentityUser user)
        {
            var jwtSettings = _configuration.GetSection("JwtSettings");
            var key = Encoding.ASCII.GetBytes(jwtSettings["Secret"]);
            var roles = await _userManager.GetRolesAsync(user);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            // Add roles as claims
            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMinutes(Convert.ToDouble(jwtSettings["ExpiryMinutes"])),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature),
                Issuer = jwtSettings["Issuer"],
                Audience = jwtSettings["Audience"]
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);

            return tokenHandler.WriteToken(token);
        }
    }

    public class RegisterModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [StringLength(100, MinimumLength = 10)]
        public string Password { get; set; }

        [Required]
        [Compare("Password")]
        public string ConfirmPassword { get; set; }
        
        // Additional user information
        public string? FullName { get; set; }
        public string? Phone { get; set; }
        public string? Username { get; set; }
        public string? Age { get; set; }
        public string? Gender { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? Zip { get; set; }
        public List<string>? Services { get; set; }
    }

    public class LoginModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        public string Password { get; set; }
    }

    public class UpdateUserModel
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Phone { get; set; }
        public string? Age { get; set; }
        public string? Gender { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? Zip { get; set; }
        public List<string>? Services { get; set; }
    }
}
