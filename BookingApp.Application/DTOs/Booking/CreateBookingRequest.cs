namespace BookingApp.Application.DTOs.Booking;

public record CreateBookingRequest
{
    public int ApartmentId { get; init; }
    public DateTime CheckIn { get; init; }
    public DateTime CheckOut { get; init; }
}