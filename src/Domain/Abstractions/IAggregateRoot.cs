namespace Domain.Abstractions;

/// <summary>
/// Represents an aggregate root in the domain.
/// </summary>
public interface IAggregateRoot
{
   /// <summary>
   /// Gets the domain events associated with this aggregate root.
   /// </summary>
   IReadOnlyCollection<IDomainEvent> DomainEvents { get; }

   /// <summary>
   /// Clears all domain events.
   /// </summary>
   void ClearEvents();
}
