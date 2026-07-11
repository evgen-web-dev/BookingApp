namespace BookingApp.Application.DTOs.Auth;

public record AuthResult<TResponse>(bool Succeeded, List<string>? Errors = null, TResponse? Response = default);