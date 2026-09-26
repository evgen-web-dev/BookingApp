namespace BookingApp.Application.DTOs.Auth;

public record LoginRequest
{
    public string Email { get; init; } = default;
    public string Password { get; init; } = default;
}