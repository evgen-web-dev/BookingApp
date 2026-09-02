using BookingApp.Application.DTOs.Apartment;
using BookingApp.Application.Interfaces;
using BookingApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BookingApp.Infrastructure.Persistence.Repositories;

public class ApartmentRepository : IApartmentRepository
{
    private readonly AppDbContext _dbContext;

    public ApartmentRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }
    
    public async Task<List<Apartment>> FindAvailableAsync(DateTime? startDate, DateTime? endDate)
    {
        return await _dbContext.Set<Apartment>()
            .AsNoTracking()
            .Where(apartment => !apartment.Bookings.Any(booking => booking.CheckIn < endDate && booking.CheckOut > startDate))
            .ToListAsync();
    }
}