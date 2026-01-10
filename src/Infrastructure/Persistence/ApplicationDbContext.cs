using Application.Abstractions.Messaging;
using Application.Abstractions.Persistence;
using Domain.Todos.Entities;
using Infrastructure.Extensions;
using Infrastructure.Persistence.Interceptors;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Persistence;

public sealed class ApplicationDbContext(
    DbContextOptions<ApplicationDbContext> options,
    AuditableEntityInterceptor auditableEntityInterceptor,
    DomainEventDispatchInterceptor domainEventDispatchInterceptor) : DbContext(options), IApplicationDbContext
{
   public DbSet<Todo> Todos => Set<Todo>();

   protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
   {
      base.OnConfiguring(optionsBuilder);
      optionsBuilder.AddInterceptors(auditableEntityInterceptor, domainEventDispatchInterceptor);
   }

   protected override void OnModelCreating(ModelBuilder modelBuilder)
   {
      base.OnModelCreating(modelBuilder);

      // Apply all configurations from this assembly
      modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

      // Configure base abstractions (Id, Auditable, AggregateRoot)
      modelBuilder.ConfigureBaseAbstractions();
   }

   protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
   {
      base.ConfigureConventions(configurationBuilder);

      // Configure Id type conventions
      configurationBuilder.ConfigureIdConventions();
   }
}
