namespace BookingApp.Application.Options.Auth;

public sealed class JwtOptions
{
    public static readonly string SectionName = "Jwt";
    public string Issuer { get; set; } = null!;
    public string Audience { get; set; } = null!;
    public string SigningKey { get; set; } = null!;
    public int AccessTokenLifetimeMinutes { get; set; }
}