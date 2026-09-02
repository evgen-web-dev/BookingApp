namespace BookingApp.Application.DTOs.Apartment;

public record ApartmentDetailsResponse
{
    public int Id { get; init; }
    public string Title { get; init; } = default!;
    public string Description { get; init; } = default!;
    public string Location { get; init; } = default!;
    public decimal Price { get; init; }
    public int Capacity { get; init; }
}