using BookingApp.Application.Interfaces;
using BookingApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BookingApp.Infrastructure.Persistence.Repositories;

public class BookingRepository : IBookingRepository
{
    private readonly AppDbContext _dbContext;
    
    public BookingRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }
    
    public void Add(Booking booking)
    {
        _dbContext.Set<Booking>().Add(booking);
    }

    public async Task<Booking?> FindByIdAsync(int id, CancellationToken cancellationToken)
    {
        return await _dbContext.Set<Booking>()
            .AsNoTracking()
            .FirstOrDefaultAsync(booking => booking.Id == id, cancellationToken);
    }

    public async Task<List<Booking>> FindByClientIdAsync(int clientId, CancellationToken cancellationToken)
    {
        return await _dbContext.Set<Booking>()
            .AsNoTracking()
            .Where(booking => booking.ClientId == clientId)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> HasOverlappingBookingAsync(int apartmentId, DateTime checkInDate, DateTime checkOutDate, CancellationToken cancellationToken)
    {
        return await _dbContext.Set<Booking>()
            .AnyAsync(
                booking => booking.ApartmentId == apartmentId &&
                           booking.CheckIn < checkOutDate && booking.CheckOut > checkInDate, 
                cancellationToken);
    }
}