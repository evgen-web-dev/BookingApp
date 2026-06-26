using BookingApp.Application.DTOs;
using BookingApp.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace BookingApp.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;
    
    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpPost]
    public async Task<ActionResult<UserDto>> CreateUser([FromBody] CreateUserDto userDto, CancellationToken cancellationToken)
    {
        var newUserDto = await _userService.CreateUser(userDto, cancellationToken);
        
        return CreatedAtAction(
            actionName: nameof(GetUserById),
            routeValues: new { id = newUserDto.Id },
            value: newUserDto
        );
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<UserDto>> GetUserById(int id, CancellationToken cancellationToken)
    {
        var userDto = await _userService.GetUserById(id, cancellationToken);
        
        if (userDto == null)
        {
            return NotFound($"User with id = {id} not found");
        }
        
        return Ok(userDto);
    }
}