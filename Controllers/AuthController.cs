




using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ECommerce.Api.Domain;
using ECommerce.Api.Application.Dtos.Auth;
using ECommerce.Api.Application.Dtos.User;
using ECommerce.Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace ECommerce.Api.Controllers;

[ApiController]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<AuthController> _logger;

    private readonly IPasswordHasher<User> _passwordHasher;

    private readonly IConfiguration _configuration;

    public AuthController(AppDbContext dbContext, ILogger<AuthController> logger, IPasswordHasher<User> passwordHasher, IConfiguration configuration)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
        _logger = logger;
        _configuration = configuration;
    }



    [HttpPost("api/signup")]
    public async Task<ActionResult<UserDto>> SignUp([FromBody] CreateUserRequest request)
    {
        var emailExists = await _dbContext.Users.AnyAsync(u => u.Email == request.Email);
        if (emailExists)
        {
            return BadRequest(new { error = "Email already exists" });
        }

        var user = new User
        {
            Name = request.Name,
            Email = request.Email,
            Role = UserRole.Customer
        };

        user.PasswordHash = _passwordHasher.HashPassword(user, request.Password);

        await _dbContext.Users.AddAsync(user);
        await _dbContext.SaveChangesAsync();

        return Ok(new UserDto
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email,
            Role = user.Role.ToString()
        });
    }


    [HttpPost("api/login")]
    public async Task<ActionResult<UserDto>> Login([FromBody] LoginRequest request)
    {

        if (string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Password))
        {
            return BadRequest(new { error = "Email and password are required" });
        }

        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
        if (user == null || user.PasswordHash == null)
        {
            return BadRequest(new { error = "Invalid email or password" });
        }

        var passwordValid = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password) == PasswordVerificationResult.Success;
        if (!passwordValid)
        {
            return BadRequest(new { error = "Invalid email or password" });
        }

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role.ToString())
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: creds
        );

        return Ok(new
        {
            token = new JwtSecurityTokenHandler().WriteToken(token),
            user = new UserDto
            { Id = user.Id, Name = user.Name, Email = user.Email, Role = user.Role.ToString() }
        });
    }
}