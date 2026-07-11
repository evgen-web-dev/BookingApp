namespace BookingApp.Application.DTOs;

public record RegisterRequest(
    string? FirstName,
    string? LastName,
    string? MiddleName,
    DateOnly DateOfBirth,
    string Email,
    string Password,
    string Role
);