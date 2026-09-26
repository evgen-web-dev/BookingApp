using BookingApp.Application.DTOs;
using BookingApp.Application.DTOs.Booking;
using BookingApp.Application.DTOs.Common;

namespace BookingApp.Application.Interfaces;

public interface IBookingService
{
    Task<OperationResult<BookingResponse>> CreateBookingAsync(CreateBookingRequest request, int clientId, CancellationToken cancellationToken);
    Task<OperationResult<BookingResponse>> GetBookingAsync(int bookingId, int callerId, CancellationToken cancellationToken);
    Task<OperationResult<PaginatedResponse<BookingResponse>>> GetMyBookingsAsync(MyBookingsPaginatedRequest request, int callerId, CancellationToken cancellationToken);
}