namespace BookingApp.Domain.Entities;

public class RefreshToken
{
    public int Id { get; set; }
    public int SessionId { get; set; }
    public required Session Session { get; set; }
    public required string TokenHash { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime? RevokedAt { get; set; }
}