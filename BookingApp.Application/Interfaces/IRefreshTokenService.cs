namespace BookingApp.Application.Interfaces;

public interface IRefreshTokenService
{
    string GenerateRefreshToken();
    string HashRefreshToken(string rawToken);
}