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

        /*
        Base64Url.TryDecodeFromChars may throw when passing some invalid string.
        For example, a URL-encoded Base64 value may contain percent-encoded characters like %2F, %2B or %3D 
        which are non-valid Base64Uel characters
        Base64Url.IsValid before Base64Url.TryDecodeFromChars prevents case like that
        */
        if (!Base64Url.IsValid(rawToken))
        {
            return false;
        }
        
        Span<byte> buffer = stackalloc byte[TokenSizeBytes];

        if (!Base64Url.TryDecodeFromChars(rawToken, buffer, out var bytesWritten) || bytesWritten != TokenSizeBytes)
        {
            return false;
        }
        
        hash = Convert.ToHexString(SHA256.HashData(buffer));
        
        return true;
    }
}