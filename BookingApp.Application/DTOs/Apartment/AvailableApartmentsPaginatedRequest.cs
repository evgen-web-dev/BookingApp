using BookingApp.Application.DTOs.Common;

namespace BookingApp.Application.DTOs.Apartment;

public record AvailableApartmentsPaginatedRequest : PaginatedRequest
{
    public DateTime? AvailableFrom { get; set; }
    public DateTime? AvailableTo { get; set; }
}