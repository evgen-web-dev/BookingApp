namespace BookingApp.Application.DTOs.Auth;

public record AuthenticatedUserResult(int Id, string Email, List<string> Roles);