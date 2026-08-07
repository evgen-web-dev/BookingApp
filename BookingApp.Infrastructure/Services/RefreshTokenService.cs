using System.Buffers.Text;
using System.Security.Cryptography;
using BookingApp.Application.Interfaces;

namespace BookingApp.Infrastructure.Services;

public class RefreshTokenService : IRefreshTokenService
{
    private const int TokenSizeBytes = 32;
    
    public string GenerateRefreshToken()
    {
        return Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(TokenSizeBytes));
    }

    public bool TryHashRefreshToken(string rawToken, out string hash)
    {
        hash = string.Empty;
        
        Span<byte> buffer = stackalloc byte[TokenSizeBytes];

        if (!Base64Url.TryDecodeFromChars(rawToken, buffer, out var bytesWritten) || bytesWritten != TokenSizeBytes)
        {
            return false;
        }
        
        hash = Convert.ToHexString(SHA256.HashData(buffer));
        
        return true;
    }
}