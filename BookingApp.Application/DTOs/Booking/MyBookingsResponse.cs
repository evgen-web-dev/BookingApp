namespace BookingApp.Application.DTOs.Booking;

public record MyBookingsResponse
{
    public List<BookingResponse> Bookings { get; init; } = [];
}