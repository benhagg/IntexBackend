using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using IntexBackend.Data;
using IntexBackend.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
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
                    // Set streaming service flags
                    Netflix = model.Services?.Contains("Netflix") == true ? 1 : 0,
                    AmazonPrime = model.Services?.Contains("Amazon Prime") == true ? 1 : 0,
                    DisneyPlus = model.Services?.Contains("Disney+") == true ? 1 : 0,
                    ParamountPlus = model.Services?.Contains("Paramount+") == true ? 1 : 0,
                    Max = model.Services?.Contains("Max") == true ? 1 : 0,
                    Hulu = model.Services?.Contains("Hulu") == true ? 1 : 0,
                    AppleTVPlus = model.Services?.Contains("Apple TV+") == true ? 1 : 0,
                    Peacock = model.Services?.Contains("Peacock") == true ? 1 : 0
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

            return Ok(new { message = "User registered successfully" });
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
}
