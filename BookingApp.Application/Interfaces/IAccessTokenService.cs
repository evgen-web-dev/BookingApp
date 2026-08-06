using BookingApp.Application.DTOs.Auth;

namespace BookingApp.Application.Interfaces;

public interface IAccessTokenService
{
    string GenerateAccessToken(AuthenticatedUserResult user);
}