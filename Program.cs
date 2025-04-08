using System.Text;
using IntexBackend.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);


// Add services to the container.
builder.Services.AddControllers();

// Configure database
// .GetValue gets the string from Azure environment variables
// Get connection string from Azure environment variables
var azureConnectionString = Environment.GetEnvironmentVariable("AZURE_SQL_CONNECTIONSTRING");
// for local development, use the connection string from appsettings.json
var localConnectionString = builder.Configuration.GetConnectionString("DefaultConnection");

if (azureConnectionString != null)
{
    // Use Azure SQL if available
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlServer(azureConnectionString));
    Console.WriteLine("Using Azure SQL Database");
}
else if (localConnectionString != null)
{
    // Fall back to SQLite if Azure SQL is not available
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlite(localConnectionString));
    Console.WriteLine("Using local SQLite Database");
}
else
{
    throw new InvalidOperationException("No database connection string is available");
}

// Configure Identity
builder.Services.AddIdentity<IdentityUser, IdentityRole>(options => {
    // Password settings as per requirements
    options.Password.RequiredLength = 15;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// Configure JWT Authentication
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var key = Encoding.ASCII.GetBytes(jwtSettings["Secret"] ?? "YourSuperSecretKeyHereThatIsAtLeast32BytesLong");

builder.Services.AddAuthentication(options => {
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options => {
    options.RequireHttpsMetadata = true;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        ClockSkew = TimeSpan.Zero
    };
});

// Configure CORS
builder.Services.AddCors(options => {
    options.AddPolicy("AllowReactApp", policy => {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Configure Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c => {
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Intex Movie API", Version = "v1" });
    
    // Add JWT Authentication to Swagger
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme {
        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token in the text input below.",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    
    c.AddSecurityRequirement(new OpenApiSecurityRequirement {
        {
            new OpenApiSecurityScheme {
                Reference = new OpenApiReference {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Always redirect to HTTPS
app.UseHttpsRedirection();

// Use CORS
app.UseCors("AllowReactApp");

// Add Content Security Policy
app.Use(async (context, next) => {
    context.Response.Headers.Append(
        "Content-Security-Policy",
        "default-src 'self'; " +
        "script-src 'self' 'unsafe-inline'; " +
        "style-src 'self' 'unsafe-inline'; " +
        "img-src 'self' data:; " +
        "font-src 'self'; " +
        "connect-src 'self' http://:5173 http://localhost:5174 http://localhost:5175"
    );
    
    // Add HSTS header
    context.Response.Headers.Append(
        "Strict-Transport-Security",
        "max-age=31536000; includeSubDomains"
    );
    
    await next();
});

// Authentication & Authorization
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Create roles and admin user if they don't exist
using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
    
    // Create roles if they don't exist
    string[] roleNames = { "Admin", "User" };
    foreach (var roleName in roleNames)
    {
        if (!await roleManager.RoleExistsAsync(roleName))
        {
            await roleManager.CreateAsync(new IdentityRole(roleName));
        }
    }
    
    // Create admin user if it doesn't exist
    var adminEmail = "admin@movies.com";
    var adminUser = await userManager.FindByEmailAsync(adminEmail);
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    
    if (adminUser == null)
    {
        adminUser = new IdentityUser
        {
            UserName = adminEmail,
            Email = adminEmail,
            EmailConfirmed = true
        };
        
        var result = await userManager.CreateAsync(adminUser, "Admin@123456");
        
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(adminUser, "Admin");
            
            // Create MovieUser record for admin
            var adminMovieUser = new IntexBackend.Models.MovieUser
            {
                Name = "Admin User",
                Email = adminEmail,
                Age = 30,
                Gender = "Other",
                City = "Movie City",
                State = "CA",
                Zip = 12345
            };
            
            dbContext.MovieUsers.Add(adminMovieUser);
            await dbContext.SaveChangesAsync();
        }
    }
    else
    {
        // Check if MovieUser record exists for admin
        var adminMovieUser = await dbContext.MovieUsers.FirstOrDefaultAsync(u => u.Email == adminEmail);
        if (adminMovieUser == null)
        {
            // Create MovieUser record for existing admin
            adminMovieUser = new IntexBackend.Models.MovieUser
            {
                Name = "Admin User",
                Email = adminEmail,
                Age = 30,
                Gender = "Other",
                City = "Movie City",
                State = "CA",
                Zip = 12345
            };
            
            dbContext.MovieUsers.Add(adminMovieUser);
            await dbContext.SaveChangesAsync();
        }
    }
    
    // Create regular user if it doesn't exist
    var userEmail = "user@example.com";
    var regularUser = await userManager.FindByEmailAsync(userEmail);
    
    if (regularUser == null)
    {
        regularUser = new IdentityUser
        {
            UserName = userEmail,
            Email = userEmail,
            EmailConfirmed = true
        };
        
        var result = await userManager.CreateAsync(regularUser, "User@123456");
        
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(regularUser, "User");
            
            // Create MovieUser record for regular user
            var regularMovieUser = new IntexBackend.Models.MovieUser
            {
                Name = "Regular User",
                Email = userEmail,
                Age = 25,
                Gender = "Other",
                City = "User City",
                State = "NY",
                Zip = 54321
            };
            
            dbContext.MovieUsers.Add(regularMovieUser);
            await dbContext.SaveChangesAsync();
        }
    }
    else
    {
        // Check if MovieUser record exists for regular user
        var regularMovieUser = await dbContext.MovieUsers.FirstOrDefaultAsync(u => u.Email == userEmail);
        if (regularMovieUser == null)
        {
            // Create MovieUser record for existing regular user
            regularMovieUser = new IntexBackend.Models.MovieUser
            {
                Name = "Regular User",
                Email = userEmail,
                Age = 25,
                Gender = "Other",
                City = "User City",
                State = "NY",
                Zip = 54321
            };
            
            dbContext.MovieUsers.Add(regularMovieUser);
            await dbContext.SaveChangesAsync();
        }
    }
}

app.Run();
