using BookingApp.Application.DTOs.Apartment;
using BookingApp.Domain.Entities;

namespace BookingApp.Application.Interfaces;

public interface IApartmentRepository
{
    Task<List<Apartment>> FindAvailableAsync(DateTime? startDate, DateTime? endDate);
}