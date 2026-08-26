namespace BookingApp.Application.DTOs.Auth;

public record RegisterRequest
{
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public string? MiddleName { get; init; }
    public DateOnly DateOfBirth { get; init; }
    public string Email { get; init; } = null!;
    public string Password { get; init; } = null!;
    public string Role { get; init; } = null!;
}