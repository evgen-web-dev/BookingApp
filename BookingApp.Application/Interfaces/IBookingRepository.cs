using BookingApp.Domain.Entities;

namespace BookingApp.Application.Interfaces;

public interface IBookingRepository
{
    void Add(Booking booking);
    Task<Booking?> FindByIdAsync(int id, CancellationToken cancellationToken);
    Task<List<Booking>> FindByClientIdAsync(int clientId, CancellationToken cancellationToken);
    Task<bool> HasOverlappingBookingAsync(int apartmentId, DateTime checkInDate, DateTime checkOutDate, CancellationToken cancellationToken);
}