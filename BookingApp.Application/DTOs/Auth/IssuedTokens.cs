namespace BookingApp.Application.DTOs.Auth;

public record IssuedTokens(string RefreshToken, string AccessToken);