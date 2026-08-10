using BookingApp.Application.DTOs.Auth;
using BookingApp.Application.Errors;
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
    private const string RefreshTokenCookieName = "refreshToken";
    private const string RefreshTokenCookiePath = "/api/auth";
    
    public AuthController(IAuthService authService, IOptions<UserSessionOptions> userSessionOptions)
    {
        _authService = authService;
        _userSessionOptions = userSessionOptions;
    }

    private void AppendRefreshTokenCookie(string refreshToken, int maxAgeDays)
    {
        Response.Cookies.Append(RefreshTokenCookieName, refreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = true, // requires https:// for cookie with "Secure = true" to be stored correctly 
            SameSite = SameSiteMode.Lax,
            MaxAge = TimeSpan.FromDays(maxAgeDays),
            Path = RefreshTokenCookiePath
        });
    }

    private void DeleteRefreshTokenCookie()
    {
        Response.Cookies.Delete(RefreshTokenCookieName, new CookieOptions { Path = RefreshTokenCookiePath });
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

        AppendRefreshTokenCookie(loginResult.Value.RefreshToken, _userSessionOptions.Value.AbsoluteLifeTimeDays);
        
        return Ok(loginResult.Value.LoginResponse);
    }

    // TODO - remove after authorization is fully completed (access-tokens + refresh-tokens)
    [HttpGet("test/protected")]
    [Authorize(Roles = Roles.Client)]
    public IActionResult TestProtected() => Ok("Access allowed");

    [HttpPost("refresh")]
    public async Task<ActionResult<RefreshResponse>> RefreshAsync(CancellationToken cancellationToken)
    {
        var refreshToken = Request.Cookies[RefreshTokenCookieName] ?? null;

        if (refreshToken is null)
        {
            return BadRequest(new ErrorResponse([AuthErrorCodes.InvalidRefreshToken]));
        }
        
        var refreshResult = await _authService.RefreshAsync(refreshToken, cancellationToken);

        if (!refreshResult.Succeeded)
        {
            return BadRequest(new ErrorResponse(refreshResult.Errors.ToList()));
        }
        
        AppendRefreshTokenCookie(refreshResult.Value.RefreshToken, _userSessionOptions.Value.AbsoluteLifeTimeDays);
        
        return Ok(refreshResult.Value.RefreshResponse);
    }
    
    [HttpPost("logout")]
    public async Task<ActionResult> LogoutAsync(CancellationToken cancellationToken)
    {
        var refreshToken = Request.Cookies[RefreshTokenCookieName] ?? null;

        if (refreshToken is null)
        {
            return BadRequest(new ErrorResponse([AuthErrorCodes.InvalidRefreshToken]));
        }

        DeleteRefreshTokenCookie();
        
        var logoutResult = await _authService.LogoutAsync(refreshToken, cancellationToken);

        if (!logoutResult.Succeeded)
        {
            return BadRequest(new ErrorResponse(logoutResult.Errors.ToList()));
        }
        
        return Ok();
    }
}