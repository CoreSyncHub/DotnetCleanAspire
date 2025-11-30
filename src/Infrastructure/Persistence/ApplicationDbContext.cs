using Application.Abstractions.Messaging;
using Application.Abstractions.Persistence;
using Domain.Todos.Entities;
using Infrastructure.Extensions;
using Infrastructure.Persistence.Interceptors;

namespace Infrastructure.Persistence;

public sealed class ApplicationDbContext(
    DbContextOptions<ApplicationDbContext> options,
    IDispatcher dispatcher,
    AuditableEntityInterceptor auditableEntityInterceptor) : DbContext(options), IApplicationDbContext
{
   public DbSet<Todo> Todos => Set<Todo>();

   public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
   {
      // Collect domain events before saving
      List<AggregateRoot> aggregateRoots = ChangeTracker.Entries<IAggregateRoot>()
          .Select(e => e.Entity)
          .OfType<AggregateRoot>()
          .ToList();

      List<IDomainEvent> domainEvents = aggregateRoots
          .SelectMany(r => r.DomainEvents)
          .ToList();

      // Save changes
      int result = await base.SaveChangesAsync(cancellationToken);

      // Dispatch domain events after successful commit
      foreach (IDomainEvent domainEvent in domainEvents)
      {
         await dispatcher.Publish(domainEvent, cancellationToken);
      }

      // Clear events from aggregate roots
      foreach (AggregateRoot root in aggregateRoots)
      {
         root.ClearEvents();
      }

      return result;
   }

   protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
   {
      base.OnConfiguring(optionsBuilder);
      optionsBuilder.AddInterceptors(auditableEntityInterceptor);
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
