namespace BDFR.Database.Models;

public sealed class Session
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public required string RefreshTokenHash { get; set; }
    public string? DeviceName { get; set; }
    public string? Platform { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }
    public User User { get; set; } = null!;
}
