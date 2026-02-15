namespace Infrastructure.Identity.Entities;

public sealed class RefreshTokenEntity
{
    public Id Id { get; init; } = Id.New();

    public required string TokenHash { get; init; }

    public required Id UserId { get; init; }

    public required DateTimeOffset ExpiresAt { get; init; }

    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? RevokedAt { get; set; }

    public string? ReplacedByTokenHash { get; set; }

    public string? CreatedByIp { get; set; }

    public bool IsExpired => DateTimeOffset.UtcNow >= ExpiresAt;

    public bool IsRevoked => RevokedAt.HasValue;

    public bool IsActive => !IsRevoked && !IsExpired;

    public ApplicationUser User { get; init; } = null!;
}
