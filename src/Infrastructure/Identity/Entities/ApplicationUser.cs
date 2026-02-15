using Microsoft.AspNetCore.Identity;

namespace Infrastructure.Identity.Entities;

public sealed class ApplicationUser : IdentityUser<Id>
{
    public ApplicationUser() : base()
    {
        Id = Id.New();
    }

    public string? ExternalProvider { get; set; }

    public string? ExternalProviderKey { get; set; }

    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? LastLoginAt { get; set; }

    public ICollection<RefreshTokenEntity> RefreshTokens { get; init; } = [];
}
