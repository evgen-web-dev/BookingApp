using BookingApp.Application.DTOs.Auth;

namespace BookingApp.Application.Interfaces;

public interface ITokenService
{
    string GenerateAccessToken(AuthenticatedUserResult user);
}