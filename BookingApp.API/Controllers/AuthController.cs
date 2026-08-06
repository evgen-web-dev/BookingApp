using BookingApp.Application.DTOs.Auth;
using BookingApp.Application.Interfaces;
using BookingApp.Application.Options.Auth;
using BookingApp.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace BookingApp.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IOptions<UserSessionOptions> _userSessionOptions;
    
    public AuthController(IAuthService authService, IOptions<UserSessionOptions> userSessionOptions)
    {
        _authService = authService;
        _userSessionOptions = userSessionOptions;
    }
    
    [HttpPost("register")]
    public async Task<ActionResult<RegisterResponse>> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken)
    {
        var registerResult = await _authService.RegisterAsync(request, cancellationToken);

        if (registerResult.Succeeded)
        {
            // TODO - update later to CreatedAtAction / CreatedAtRoute - when implement UsersController
            return Ok(registerResult.Value);
        }

        return BadRequest(new ErrorResponse(registerResult.Errors.ToList()));
    }
    
    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
    {
        var loginResult = await _authService.LoginAsync(request);

        if (!loginResult.Succeeded)
        {
            return BadRequest(new ErrorResponse(loginResult.Errors.ToList()));
        }

        Response.Cookies.Append("refreshToken", loginResult.Value.RefreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Lax,
            MaxAge = TimeSpan.FromDays(_userSessionOptions.Value.AbsoluteLifeTimeDays),
            Path = "/api/auth/refresh"
        });
        
        return Ok(loginResult.Value.LoginResponse);
    }

    // TODO - remove after JWT authorization is fully completed (access-tokens + refresh-tokens)
    [HttpGet("test/protected")]
    [Authorize(Roles = Roles.Client)]
    public IActionResult TestProtected() => Ok("Access allowed");
}