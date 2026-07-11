namespace BookingApp.Application.DTOs;

public record AuthResult<TResponse>(bool Succeeded, List<string>? Errors = null, TResponse? Response = default);