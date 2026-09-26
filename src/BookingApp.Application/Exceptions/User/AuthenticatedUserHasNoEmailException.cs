namespace BookingApp.Application.Exceptions.User;

public class AuthenticatedUserHasNoEmailException : InvalidOperationException
{
    public AuthenticatedUserHasNoEmailException() 
        : base("Authenticated user has no email")
    {
    }

    public AuthenticatedUserHasNoEmailException(string message)
        : base(message)
    {
    }

    public AuthenticatedUserHasNoEmailException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}