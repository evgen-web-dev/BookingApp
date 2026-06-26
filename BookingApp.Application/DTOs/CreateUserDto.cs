using BookingApp.Domain;

namespace BookingApp.Application.DTOs;

public class CreateUserDto
{
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public string? MiddleName { get; set; }
    public required DateOnly BirthDate { get; set; }
    public required string Email { get; set; }
    public required Role Role { get; set; }
    public required string Password { get; set; }
}