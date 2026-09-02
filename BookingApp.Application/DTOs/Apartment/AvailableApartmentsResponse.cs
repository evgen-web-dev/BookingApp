namespace BookingApp.Application.DTOs.Apartment;

public record AvailableApartmentsResponse
{
    public List<ApartmentDetailsResponse> Apartments { get; set; } = [];
}