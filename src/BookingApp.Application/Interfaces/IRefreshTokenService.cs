namespace BookingApp.Application.Interfaces;

public interface IRefreshTokenService
{
    string GenerateRefreshToken();
    bool TryHashRefreshToken(string rawToken, out string hash);
}