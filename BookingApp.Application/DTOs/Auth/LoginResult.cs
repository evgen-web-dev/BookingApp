namespace BookingApp.Application.DTOs.Auth;

public record LoginResult(string RefreshToken, LoginResponse LoginResponse);