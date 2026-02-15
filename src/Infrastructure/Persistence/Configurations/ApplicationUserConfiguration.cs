using Infrastructure.Identity.Entities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

internal sealed class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        builder.ToTable("users");

        builder.Property(u => u.ExternalProvider)
               .HasColumnName("external_provider")
               .HasMaxLength(50);

        builder.Property(u => u.ExternalProviderKey)
               .HasColumnName("external_provider_key")
               .HasMaxLength(256);

        builder.Property(u => u.CreatedAt)
               .HasColumnName("created_at");

        builder.Property(u => u.LastLoginAt)
               .HasColumnName("last_login_at");

        builder.HasIndex(u => new { u.ExternalProvider, u.ExternalProviderKey })
               .HasDatabaseName("ix_users_external_provider")
               .IsUnique()
               .HasFilter("external_provider IS NOT NULL");
    }
}
