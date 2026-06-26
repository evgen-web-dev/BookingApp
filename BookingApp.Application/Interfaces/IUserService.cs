using BookingApp.Application.DTOs;

namespace BookingApp.Application.Interfaces;

public interface IUserService
{
    Task<UserDto> CreateUser(CreateUserDto createUserDto, CancellationToken cancellationToken = default);
    Task<UserDto?> GetUserById(int userId, CancellationToken cancellationToken = default);
}