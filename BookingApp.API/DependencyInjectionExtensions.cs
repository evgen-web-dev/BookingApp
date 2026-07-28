using BookingApp.Application.Options.Auth;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace BookingApp.API;

public static class DependencyInjectionExtensions
{
    public static void AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .Validate(options => !string.IsNullOrEmpty(options.Issuer), 
                $"{JwtOptions.SectionName}:{nameof(JwtOptions.Issuer)} is  missing in configuration")
            .Validate(options => !string.IsNullOrEmpty(options.Audience), 
                $"{JwtOptions.SectionName}:{nameof(JwtOptions.Audience)} missing in configuration")
            .Validate(options => !string.IsNullOrEmpty(options.SigningKey), 
                $"{JwtOptions.SectionName}:{nameof(JwtOptions.SigningKey)} missing in configuration")
            .Validate(options => options.AccessTokenLifetimeMinutes > 0, 
                $"{JwtOptions.SectionName}:{nameof(JwtOptions.AccessTokenLifetimeMinutes)} has invalid value")
            .ValidateOnStart();

        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtOptions>>((bearerOptions, jwtOptions) =>
            {
                var jwt =  jwtOptions.Value;
                
                bearerOptions.MapInboundClaims = false;
                bearerOptions.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwt.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwt.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Convert.FromBase64String(jwt.SigningKey)
                    ),
                    ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromMinutes(3),
                };
            });

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer();
    }
}