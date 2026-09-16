using BookingApp.Application.DTOs.Common;
using BookingApp.Domain.Entities;

namespace BookingApp.Application.Interfaces;

public interface IBookingRepository
{
    void Add(Booking booking);
    Task<Booking?> FindByIdAsync(int id, CancellationToken cancellationToken);
    Task<PagedResult<Booking>> FindByClientIdAsync(int clientId, PageQueryParams pageQueryParams, CancellationToken cancellationToken);
    Task<bool> HasOverlappingBookingAsync(int apartmentId, DateTime checkInDate, DateTime checkOutDate, CancellationToken cancellationToken);
}