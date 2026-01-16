namespace Domain.Todos.Events;

public sealed record TodoCompletedEvent(Id TodoId) : DomainEventBase;
