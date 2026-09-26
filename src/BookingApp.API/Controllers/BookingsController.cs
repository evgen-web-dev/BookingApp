using System.Security.Claims;
using BookingApp.Application.DTOs.Booking;
using BookingApp.Application.DTOs.Common;
using BookingApp.Application.Interfaces;
using BookingApp.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookingApp.API.Controllers;

[ApiController]
[Route("api/bookings")]
public class BookingsController : ControllerBase
{
    private readonly IBookingService _bookingService;
    
    public BookingsController(IBookingService bookingService)
    {
        _bookingService = bookingService;
    }

    [Authorize]
    [HttpGet("{id:int}")]
    public async Task<ActionResult<BookingResponse>> GetBookingById([FromRoute] int id, CancellationToken cancellationToken)
    {
        var userId = GetUserIdFromClaim();

        var getBookingByIdResult = await _bookingService.GetBookingAsync(id, userId, cancellationToken);
        if (!getBookingByIdResult.Succeeded)
        {
            return getBookingByIdResult.ToProblemDetailsResult(Request.Path);
        }
        
        return Ok(getBookingByIdResult.Value);
    }
    
    [Authorize]
    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<BookingResponse>>> GetBookingsAsync([FromQuery] MyBookingsPaginatedRequest request, CancellationToken cancellationToken)
    {
        var userId = GetUserIdFromClaim();

        var getMyBookingsResult = await _bookingService.GetMyBookingsAsync(request, userId, cancellationToken);
        if (!getMyBookingsResult.Succeeded)
        {
            return getMyBookingsResult.ToProblemDetailsResult(Request.Path);
        }
        
        return Ok(getMyBookingsResult.Value);
    }
    
    [Authorize(Roles = Roles.Client)]
    [HttpPost]
    public async Task<ActionResult<BookingResponse>> CreateBooking([FromBody] CreateBookingRequest request, CancellationToken cancellationToken)
    {
        var userId = GetUserIdFromClaim();
        var createBookingResult = await _bookingService.CreateBookingAsync(request, userId, cancellationToken);
        
        if (!createBookingResult.Succeeded)
        {
            return createBookingResult.ToProblemDetailsResult(Request.Path);
        }
        
        return CreatedAtAction(
            actionName: nameof(GetBookingById),
            routeValues: new { id = createBookingResult.Value.Id },
            value: createBookingResult.Value);
    }

    private int GetUserIdFromClaim()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(userIdClaim, out var userId)
            ? userId
            : throw new InvalidOperationException("Authenticated request missing a valid NameIdentifier claim");
    }
}