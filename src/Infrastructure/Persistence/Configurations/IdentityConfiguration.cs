using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

internal sealed class IdentityUserRoleConfiguration : IEntityTypeConfiguration<IdentityUserRole<Id>>
{
    public void Configure(EntityTypeBuilder<IdentityUserRole<Id>> builder)
    {
        builder.ToTable("user_roles");
    }
}

internal sealed class IdentityUserClaimConfiguration : IEntityTypeConfiguration<IdentityUserClaim<Id>>
{
    public void Configure(EntityTypeBuilder<IdentityUserClaim<Id>> builder)
    {
        builder.ToTable("user_claims");
    }
}

internal sealed class IdentityUserLoginConfiguration : IEntityTypeConfiguration<IdentityUserLogin<Id>>
{
    public void Configure(EntityTypeBuilder<IdentityUserLogin<Id>> builder)
    {
        builder.ToTable("user_logins");
    }
}

internal sealed class IdentityRoleClaimConfiguration : IEntityTypeConfiguration<IdentityRoleClaim<Id>>
{
    public void Configure(EntityTypeBuilder<IdentityRoleClaim<Id>> builder)
    {
        builder.ToTable("role_claims");
    }
}

internal sealed class IdentityUserTokenConfiguration : IEntityTypeConfiguration<IdentityUserToken<Id>>
{
    public void Configure(EntityTypeBuilder<IdentityUserToken<Id>> builder)
    {
        builder.ToTable("user_tokens");
    }
}
