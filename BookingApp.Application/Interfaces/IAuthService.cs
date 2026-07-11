using BookingApp.Application.DTOs;

namespace BookingApp.Application.Interfaces;

public interface IAuthService
{
    Task<AuthResult<RegisterResponse>> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);
}