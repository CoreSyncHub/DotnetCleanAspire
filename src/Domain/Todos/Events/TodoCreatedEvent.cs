namespace Domain.Todos.Events;

public sealed record TodoCreatedEvent(Id TodoId, string Title) : DomainEventBase;
