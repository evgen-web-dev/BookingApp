using BookingApp.Application.DTOs;
using BookingApp.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace BookingApp.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StatusController : ControllerBase
{
    private readonly IAppStatusService _appStatusService;
    
    public StatusController(IAppStatusService appStatusService)
    {
        _appStatusService = appStatusService;
    }
    
    [HttpGet]
    public async Task<ActionResult<AppStatusDto>> GetStatus()
    {
        return await _appStatusService.GetStatusAsync();
    }
}