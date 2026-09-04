using BookingApp.Application.DTOs.Apartment;
using BookingApp.Application.DTOs.Common;
using BookingApp.Domain.Entities;

namespace BookingApp.Application.Interfaces;

public interface IApartmentRepository
{
    Task<PagedResult<Apartment>> FindAvailableAsync(DateTime? startDate, DateTime? endDate, PageQueryParams pageQueryParams, CancellationToken cancellationToken);
    Task<bool> ExistsAsync(int apartmentId, CancellationToken cancellationToken);
}