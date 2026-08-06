namespace BookingApp.Domain.Entities;

public class Session
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = default!;
    public DateTime CreatedAt { get; set; }
    public DateTime AbsoluteExpiresAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    public RevocationReason? RevokedReason { get; set; }
    public List<RefreshToken> RefreshTokens { get; set; } = [];
}