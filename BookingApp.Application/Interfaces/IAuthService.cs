using BookingApp.Application.DTOs;
using BookingApp.Application.DTOs.Auth;

namespace BookingApp.Application.Interfaces;

public interface IAuthService
{
    Task<OperationResult<RegisterResponse>> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);
    Task<OperationResult<LoginResult>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
    Task<OperationResult<RefreshResult>> RefreshAsync(string refreshToken, CancellationToken cancellationToken = default);
}