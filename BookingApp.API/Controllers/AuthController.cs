using BookingApp.Application.DTOs;
using BookingApp.Application.Interfaces;
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
        var registerAuthResult = await _authService.RegisterAsync(request, cancellationToken);

        if (registerAuthResult.Succeeded)
        {
            // TODO - update later to CreatedAtAction / CreatedAtRoute - when implement UsersController
            return Ok(registerAuthResult.Response);
        }

        return BadRequest(new ErrorResponse(registerAuthResult.Errors ?? []));
    }
}