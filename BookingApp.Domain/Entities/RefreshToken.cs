namespace BookingApp.Domain.Entities;

public class RefreshToken
{
    public int Id { get; set; }
    public int SessionId { get; set; }
    public Session Session { get; set; } = default!;
    public string TokenHash { get; set; } = default!;
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime? RevokedAt { get; set; }
}