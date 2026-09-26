namespace BookingApp.Application.Exceptions.Auth;

public class InvalidRefreshTokenHashGenerationException : InvalidOperationException
{
    public InvalidRefreshTokenHashGenerationException()
        : base("Could not generate hash for refresh token")
    {
    }
    
    public InvalidRefreshTokenHashGenerationException(string message)
        : base(message)
    {
    }

    public InvalidRefreshTokenHashGenerationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}