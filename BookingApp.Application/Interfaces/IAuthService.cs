using BookingApp.Application.DTOs;
using BookingApp.Application.DTOs.Auth;

namespace BookingApp.Application.Interfaces;

public interface IAuthService
{
    Task<OperationResult<RegisterResponse>> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);
    Task<OperationResult<IssuedTokens>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
    Task<OperationResult<IssuedTokens>> RefreshAsync(string refreshToken, CancellationToken cancellationToken = default);
    Task<OperationResult> LogoutAsync(string refreshToken, CancellationToken cancellationToken = default);
}