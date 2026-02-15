using Application.Abstractions.Persistence;
using Domain.Todos.Entities;
using Infrastructure.Extensions;
using Infrastructure.Identity.Entities;
using Infrastructure.Persistence.Interceptors;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

namespace Infrastructure.Persistence;

public sealed class ApplicationDbContext(
    DbContextOptions<ApplicationDbContext> options,
    AuditableEntityInterceptor auditableEntityInterceptor,
    DomainEventDispatchInterceptor domainEventDispatchInterceptor)
    : IdentityDbContext<ApplicationUser, ApplicationRole, Id>(options), IApplicationDbContext
{
    public DbSet<Todo> Todos => Set<Todo>();

    public DbSet<RefreshTokenEntity> RefreshTokens => Set<RefreshTokenEntity>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);
        optionsBuilder.AddInterceptors(auditableEntityInterceptor, domainEventDispatchInterceptor);
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
        builder.ConfigureBaseAbstractions();
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        base.ConfigureConventions(configurationBuilder);
        configurationBuilder.ConfigureIdConventions();
    }
}
