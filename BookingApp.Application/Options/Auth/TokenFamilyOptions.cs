namespace BookingApp.Application.Options.Auth;

public class TokenFamilyOptions
{
    public const string SectionName = "TokenFamily";
    public int RefreshTokenLifeTimeDays { get; set; }
    public int TokenFamilyAbsoluteLifeTimeDays { get; set; }
}