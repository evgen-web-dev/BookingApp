namespace BookingApp.Domain.Entities;

public class RefreshToken
{
    public int Id { get; set; }
    public int TokenFamilyId { get; set; }
    public TokenFamily TokenFamily { get; set; } = default!;
    public string TokenHash { get; set; } = default!;
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime? RevokedAt { get; set; }
}