using BookingApp.Application.DTOs;

namespace BookingApp.Application.Interfaces;

public interface IRoleRepository
{
    Task<bool> ExistsAsync(string name);
}