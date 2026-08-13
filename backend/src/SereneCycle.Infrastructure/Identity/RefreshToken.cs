namespace SereneCycle.Infrastructure.Identity;

/// <summary>
/// Refresh token kaydı. Token'ın kendisi değil hash'i saklanır ve
/// kullanıldığında döndürülür (rotation): çalınan bir token yeniden
/// kullanılmaya çalışılırsa <see cref="RevokedAt"/> dolu olduğu için reddedilir.
/// </summary>
public class RefreshToken
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid UserId { get; set; }
    public AppUser? User { get; set; }

    public required string TokenHash { get; set; }

    public DateTimeOffset ExpiresAt { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? RevokedAt { get; set; }

    public bool IsActive => RevokedAt is null && DateTimeOffset.UtcNow < ExpiresAt;
}
