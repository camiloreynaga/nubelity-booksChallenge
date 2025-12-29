using Application.DTOs.Auth;
using Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IConfiguration _configuration;

    public AuthController(IAuthService authService, IConfiguration configuration)
    {
        _authService = authService;
        _configuration = configuration;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
    {
        if (string.IsNullOrWhiteSpace(loginDto.Username) || string.IsNullOrWhiteSpace(loginDto.Password))
        {
            return BadRequest(new { message = "Username and password are required" });
        }

        var token = await _authService.LoginAsync(loginDto.Username, loginDto.Password);

        if (token == null)
        {
            return Unauthorized(new { message = "Invalid credentials" });
        }

        var expirationMinutes = int.Parse(_configuration["Jwt:ExpirationMinutes"] ?? "60");
        var expiresIn = expirationMinutes * 60; // Convert to seconds

        return Ok(new LoginResponseDto
        {
            Token = token,
            ExpiresIn = expiresIn
        });
    }
}

