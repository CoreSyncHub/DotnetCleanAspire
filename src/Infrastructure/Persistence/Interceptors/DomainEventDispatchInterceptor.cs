using Application.Abstractions.Messaging;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Infrastructure.Persistence.Interceptors;

/// <summary>
/// Interceptor that dispatches domain events after SaveChanges completes successfully.
/// </summary>
public sealed class DomainEventDispatchInterceptor(IDispatcher dispatcher) : SaveChangesInterceptor
{
   public override async ValueTask<int> SavedChangesAsync(
       SaveChangesCompletedEventData eventData,
       int result,
       CancellationToken cancellationToken = default)
   {
      if (eventData.Context is not null)
      {
         await DispatchDomainEvents(eventData.Context, cancellationToken);
      }

      return await base.SavedChangesAsync(eventData, result, cancellationToken);
   }

   public override int SavedChanges(
       SaveChangesCompletedEventData eventData,
       int result)
   {
      if (eventData.Context is not null)
      {
         DispatchDomainEvents(eventData.Context, CancellationToken.None).GetAwaiter().GetResult();
      }

      return base.SavedChanges(eventData, result);
   }

   private async Task DispatchDomainEvents(DbContext context, CancellationToken cancellationToken)
   {
      // Collect domain events from aggregate roots
      var aggregateRoots = context.ChangeTracker
          .Entries<IAggregateRoot>()
          .Select(e => e.Entity)
          .OfType<AggregateRoot>()
          .Where(r => r.DomainEvents.Count != 0)
          .ToList();

      var domainEvents = aggregateRoots
          .SelectMany(r => r.DomainEvents)
          .ToList();

      // Clear events before dispatching to prevent duplicate dispatch
      foreach (AggregateRoot root in aggregateRoots)
      {
         root.ClearEvents();
      }

      // Dispatch domain events
      foreach (IDomainEvent domainEvent in domainEvents)
      {
         await dispatcher.Publish(domainEvent, cancellationToken);
      }
   }
}
