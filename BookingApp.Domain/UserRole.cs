namespace BookingApp.Domain;

public class UserRole
{
    public User User { get; set; }
    public int UserId { get; set; }
    public Role Role { get; set; }
}