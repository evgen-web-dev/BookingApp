using System.Security.Claims;
using BookingApp.Application.DTOs.Auth;
using BookingApp.Application.Interfaces;
using BookingApp.Application.Options.Auth;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace BookingApp.Infrastructure.Auth;

public class JsonWebTokenService : ITokenService
{
    private readonly IOptions<JwtOptions> _jwtOptions;

    public JsonWebTokenService(IOptions<JwtOptions> jwtOptions)
    {
        _jwtOptions = jwtOptions;
    }
    
    public string GenerateAccessToken(AuthenticatedUserResult user)
    {
        var claims = user.Roles.Select(role => new Claim(ClaimTypes.Role, role)).ToList();
        claims.AddRange([
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),    
            new Claim(ClaimTypes.Email, user.Email)
        ]);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Issuer = _jwtOptions.Value.Issuer,
            Audience = _jwtOptions.Value.Audience,
            Expires = DateTime.UtcNow.AddMinutes(_jwtOptions.Value.AccessTokenLifetimeMinutes),
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Convert.FromBase64String(_jwtOptions.Value.SigningKey)),
                SecurityAlgorithms.HmacSha256
            )
        };
        
        var tokenHandler = new JsonWebTokenHandler();
        
        return tokenHandler.CreateToken(tokenDescriptor);
    }
}