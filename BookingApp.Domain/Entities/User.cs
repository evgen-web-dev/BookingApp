using Microsoft.AspNetCore.Identity;

namespace BookingApp.Domain.Entities;

public class User : IdentityUser<int>
{
    public required string FirstName { get; set; }
    public string? MiddleName { get; set; }
    public required string LastName { get; set; }
    public required DateOnly DateOfBirth { get; set; }
}