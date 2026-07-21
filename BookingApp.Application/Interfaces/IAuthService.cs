using BookingApp.Application.DTOs;
using BookingApp.Application.DTOs.Auth;

namespace BookingApp.Application.Interfaces;

public interface IAuthService
{
    Task<OperationResult<RegisterResponse>> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);
    Task<OperationResult<LoginResponse>> LoginAsync(LoginRequest request);
}