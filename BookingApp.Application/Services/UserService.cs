using BookingApp.Application.DTOs;
using BookingApp.Application.Interfaces;
using BookingApp.Domain;
using BookingApp.Domain.Interfaces;
using Microsoft.AspNetCore.Identity;

namespace BookingApp.Application.Services;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher<User> _passwordHasher;

    public UserService(IUserRepository repository, IPasswordHasher<User> passwordHasher)
    {
        _userRepository = repository;
        _passwordHasher = passwordHasher;
    }
    
    public async Task<UserDto> CreateUser(CreateUserDto createUserDto, CancellationToken cancellationToken)
    {
        var newUser = new User
        {
            FirstName = createUserDto.FirstName,
            LastName = createUserDto.LastName,
            MiddleName = createUserDto.MiddleName,
            Email = createUserDto.Email,
            DateOfBirth = createUserDto.BirthDate,
            UserRoles = new List<UserRole> { new UserRole { Role = createUserDto.Role } }
        };
        
        newUser.PasswordHash = _passwordHasher.HashPassword(newUser, createUserDto.Password);

        var addedUser = await _userRepository.CreateUser(newUser, cancellationToken);

        return MapToDto(addedUser);
    }

    public async Task<UserDto?> GetUserById(int userId, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetUserById(userId, cancellationToken);

        if (user == null)
        {
            return null;
        }
        
        return MapToDto(user);
    }

    private UserDto MapToDto(User user)
    {
        return new UserDto
        {
            Id = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            MiddleName = user.MiddleName,
            Email = user.Email,
            DateOfBirth = user.DateOfBirth,
            UserRoles = user.UserRoles.Select(userRole => userRole.Role).ToList()
        };
    }
}