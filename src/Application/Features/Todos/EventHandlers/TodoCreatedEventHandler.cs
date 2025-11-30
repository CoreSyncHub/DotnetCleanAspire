using Domain.Todos.Events;
using Microsoft.Extensions.Logging;

namespace Application.Features.Todos.EventHandlers;

/// <summary>
/// Handles the TodoCreatedEvent.
/// This handler is called after SaveChangesAsync successfully commits the transaction.
/// </summary>
internal sealed class TodoCreatedEventHandler(ILogger<TodoCreatedEventHandler> logger) : IDomainEventHandler<TodoCreatedEvent>
{

   public Task Handle(TodoCreatedEvent @event, CancellationToken cancellationToken = default)
   {
      logger.LogInformation(
          "Todo created: {TodoId} - {Title} at {OccurredOn}",
          @event.TodoId,
          @event.Title,
          @event.OccurredOn);

      // Here you could:
      // - Send a notification
      // - Update a read model
      // - Publish to a message queue
      // - etc.

      return Task.CompletedTask;
   }
}
