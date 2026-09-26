namespace BookingApp.Domain.Entities;

public class Apartment
{
    public int Id { get; set; }
    public string Location { get; set; } = default!;
    public string Title { get; set; } = default!;
    public string Description { get; set; } = default!;
    public decimal Price { get; set; }
    public int Capacity { get; set; }
    public int OwnerId { get; set; }
    public User Owner { get; set; } = default!;
    public List<Booking> Bookings { get; set; } = [];
}