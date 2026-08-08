using BookingApp.Application.DTOs;

namespace BookingApp.Application.Interfaces;

public interface IRoleIdentityService
{
    Task<bool> ExistsAsync(string roleName);
    Task<OperationResult> CreateAsync(string roleName);
}