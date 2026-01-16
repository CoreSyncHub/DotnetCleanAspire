namespace Domain.Todos.Events;

public sealed record TodoTitleUpdatedEvent(Id TodoId, string OldTitle, string NewTitle) : DomainEventBase;