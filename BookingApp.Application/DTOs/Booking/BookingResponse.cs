namespace BookingApp.Application.DTOs.Booking;

public record BookingResponse
{
    public int Id { get; init; }
    public int ApartmentId { get; init; }
    public DateTime CheckIn { get; init; }
    public DateTime CheckOut { get; init; }
    public DateTime CreatedAt { get; init; }
}