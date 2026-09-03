using BookingApp.Application.DTOs;
using BookingApp.Application.DTOs.Booking;

namespace BookingApp.Application.Interfaces;

public interface IBookingService
{
    Task<OperationResult<BookingResponse>> CreateBookingAsync(CreateBookingRequest request, int clientId, CancellationToken cancellationToken);
    Task<OperationResult<BookingResponse>> GetBookingAsync(int bookingId, int callerId, CancellationToken cancellationToken);
}