namespace Application.Abstractions.Messaging;

/// <summary>
/// Dispatches requests (commands and queries) to their handlers.
/// </summary>
public interface IDispatcher
{
    /// <summary>
    /// Sends a request to be handled by the appropriate handler.
    /// </summary>
    /// <typeparam name="TResponse">The type of response.</typeparam>
    /// <param name="request">The request to send.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The response from the handler.</returns>
    /// <exception cref="InvalidOperationException">>No handler is registered for the request type.</exception>
    Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Publishes a domain event to all registered handlers.
    /// </summary>
    /// <typeparam name="TEvent">The type of domain event.</typeparam>
    /// <param name="domainEvent">The domain event to publish.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task Publish<TEvent>(TEvent domainEvent, CancellationToken cancellationToken = default)
        where TEvent : IDomainEvent;
}
