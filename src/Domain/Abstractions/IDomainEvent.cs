namespace Domain.Abstractions;

/// <summary>
/// Represents a domain event.
/// </summary>
public interface IDomainEvent
{
   /// <summary>
   /// Gets the date and time when the event occurred.
   /// </summary>
   DateTimeOffset OccurredOn { get; }
}

/// <summary>
/// Base implementation of a domain event.
/// </summary>
public abstract record DomainEventBase : IDomainEvent
{
   /// <inheritdoc/>
   public DateTimeOffset OccurredOn { get; init; } = DateTimeOffset.UtcNow;
}
