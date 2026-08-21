namespace BookingApp.Application.Errors;

public static class AuthErrorCodes
{
    public const string InvalidEmailOrPassword = "InvalidEmailOrPassword";
    public const string InvalidEmailOrUserName = "InvalidEmailOrUserName";
    public const string UserNotFound = "UserNotFound";
    public const string InvalidRefreshToken = "InvalidRefreshToken";
}