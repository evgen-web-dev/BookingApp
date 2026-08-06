using System.Buffers.Text;
using System.Security.Cryptography;
using BookingApp.Application.Interfaces;

namespace BookingApp.Infrastructure.Services;

public class RefreshTokenService : IRefreshTokenService
{
    public string GenerateRefreshToken()
    {
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
    }

    public string HashRefreshToken(string rawToken)
    {
        return Convert.ToHexString(SHA256.HashData(Convert.FromBase64String(rawToken)));
    }
}