namespace BookingApp.Domain.Entities;

public class Booking
{
    public int Id { get; set; }
    public DateTime CheckIn { get; set; }
    public DateTime CheckOut { get; set; }
    public int ApartmentId { get; set; }
    public Apartment Apartment { get; set; } = default!;
    public int ClientId { get; set; }
    public User Client { get; set; } = default!;
    public DateTime CreatedAt { get; set; }
}