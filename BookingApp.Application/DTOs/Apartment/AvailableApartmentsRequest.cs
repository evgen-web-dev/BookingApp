namespace BookingApp.Application.DTOs.Apartment;

public record AvailableApartmentsRequest
{
    public DateTime? AvailableFrom { get; set; }
    public DateTime? AvailableTo { get; set; }
}