using BookingApp.Application.DTOs;
using BookingApp.Application.DTOs.Auth;

namespace BookingApp.Application.Interfaces;

public interface IAuthService
{
    Task<AuthResult<RegisterResponse>> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);
    Task<AuthResult<LoginResponse>> LoginAsync(LoginRequest request);
}