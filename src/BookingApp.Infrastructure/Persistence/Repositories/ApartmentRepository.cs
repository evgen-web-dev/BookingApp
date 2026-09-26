using BookingApp.Application.DTOs.Apartment;
using BookingApp.Application.DTOs.Common;
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
    
    public async Task<PagedResult<Apartment>> FindAvailableAsync(DateTime? startDate, DateTime? endDate, PageQueryParams pageQueryParams, CancellationToken cancellationToken)
    {
        var query = _dbContext.Set<Apartment>()
            .AsNoTracking()
            .Where(apartment => !apartment.Bookings.Any(booking => booking.CheckIn < endDate && booking.CheckOut > startDate));
            
        var totalCount = await query.CountAsync(cancellationToken);
        
        var items = await query
            .OrderBy(apartment => apartment.Id)
            .Skip((pageQueryParams.PageNumber - 1) * pageQueryParams.PageSize)
            .Take(pageQueryParams.PageSize)
            .ToListAsync(cancellationToken);
            
        return new PagedResult<Apartment>
        {
            Items = items,
            TotalCount = totalCount,
        };
    }

    public async Task<bool> ExistsAsync(int apartmentId, CancellationToken cancellationToken)
    {
        return await _dbContext.Set<Apartment>()
            .AnyAsync(apartment => apartment.Id == apartmentId, cancellationToken);
    }
}