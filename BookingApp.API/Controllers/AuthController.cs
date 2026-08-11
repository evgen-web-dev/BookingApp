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
    private readonly IOptions<TokenFamilyOptions> _tokenFamilyOptions;
    private const string RefreshTokenCookieName = "refreshToken";
    private const string RefreshTokenCookiePath = "/api/auth";
    
    public AuthController(IAuthService authService, IOptions<TokenFamilyOptions> tokenFamilyOptions)
    {
        _authService = authService;
        _tokenFamilyOptions = tokenFamilyOptions;
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
        var loginIssuedTokensResult = await _authService.LoginAsync(request);

        if (!loginIssuedTokensResult.Succeeded)
        {
            return BadRequest(new ErrorResponse(loginIssuedTokensResult.Errors.ToList()));
        }

        AppendRefreshTokenCookie(loginIssuedTokensResult.Value.RefreshToken, _tokenFamilyOptions.Value.TokenFamilyAbsoluteLifeTimeDays);
        
        return Ok(new LoginResponse(loginIssuedTokensResult.Value.AccessToken));
    }

    [HttpPost("refresh")]
    public async Task<ActionResult<RefreshResponse>> RefreshAsync(CancellationToken cancellationToken)
    {
        var refreshToken = Request.Cookies[RefreshTokenCookieName];

        if (string.IsNullOrEmpty(refreshToken))
        {
            return BadRequest(new ErrorResponse([AuthErrorCodes.InvalidRefreshToken]));
        }
        
        var issuedTokensResult = await _authService.RefreshAsync(refreshToken, cancellationToken);

        if (!issuedTokensResult.Succeeded)
        {
            return BadRequest(new ErrorResponse(issuedTokensResult.Errors.ToList()));
        }
        
        AppendRefreshTokenCookie(issuedTokensResult.Value.RefreshToken, _tokenFamilyOptions.Value.TokenFamilyAbsoluteLifeTimeDays);
        
        return Ok(new RefreshResponse(issuedTokensResult.Value.AccessToken));
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