namespace BookingApp.Application.Options.Auth;

public class UserSessionOptions
{
    public const string SectionName = "Session";
    public int RefreshTokenLifeTimeDays { get; set; }
    public int AbsoluteLifeTimeDays { get; set; }
}