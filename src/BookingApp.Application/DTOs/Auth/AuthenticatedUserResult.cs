namespace BookingApp.Application.DTOs.Auth;

public record AuthenticatedUserResult
{
    public int Id { get; init; }
    public string Email { get; init; }
    public List<string> Roles { get; init; }
}