using BookingApp.Application.DTOs.Auth;
using BookingApp.Application.Interfaces;
using BookingApp.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookingApp.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    
    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }
    
    [HttpPost("register")]
    public async Task<ActionResult<RegisterResponse>> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken)
    {
        var registerResult = await _authService.RegisterAsync(request, cancellationToken);

        if (registerResult.Succeeded)
        {
            // TODO - update later to CreatedAtAction / CreatedAtRoute - when implement UsersController
            return Ok(registerResult.GetValueOrThrow());
        }

        return BadRequest(new ErrorResponse(registerResult.Errors.ToList()));
    }
    
    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
    {
        var loginResult = await _authService.LoginAsync(request);

        if (loginResult.Succeeded)
        {
            return Ok(loginResult.GetValueOrThrow());
        }

        return BadRequest(new ErrorResponse(loginResult.Errors.ToList()));
    }

    // TODO - remove after JWT authorization is fully completed (access-tokens + refresh-tokens)
    [HttpGet("test/protected")]
    [Authorize(Roles = Roles.Client)]
    public IActionResult TestProtected() => Ok("Access allowed");
}