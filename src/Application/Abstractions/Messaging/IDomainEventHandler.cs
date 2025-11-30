namespace Application.Abstractions.Messaging;

/// <summary>
/// Defines a handler for a domain event.
/// </summary>
/// <typeparam name="TEvent">The type of domain event being handled.</typeparam>
public interface IDomainEventHandler<in TEvent>
    where TEvent : IDomainEvent
{
   /// <summary>
   /// Handles a domain event.
   /// </summary>
   /// <param name="domainEvent">The domain event to handle.</param>
   /// <param name="cancellationToken">Cancellation token.</param>
   Task Handle(TEvent domainEvent, CancellationToken cancellationToken = default);
}
