using BookingApp.Application.DTOs;
using BookingApp.Application.DTOs.Auth;
using BookingApp.Domain;

namespace BookingApp.Application.Interfaces;

public interface IUserRepository
{
    Task<OperationResult<CreateUserResult>> CreateAsync(User user, string password);
    Task<OperationResult> AddToRoleAsync(User user, string role);
}