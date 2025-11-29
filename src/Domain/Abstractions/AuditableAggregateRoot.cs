namespace Domain.Abstractions;

public abstract class AuditableAggregateRoot : AuditableEntity, IAggregateRoot
{
   protected AuditableAggregateRoot() : base()
   {
   }

   protected AuditableAggregateRoot(Id id) : base(id)
   {
   }

   private readonly List<IDomainEvent> _domainEvents = [];

   /// <inheritdoc/>
   public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

   /// <summary>
   /// Raises a domain event.
   /// </summary>
   /// <param name="event">The domain event to raise.</param>
   protected void Raise(IDomainEvent @event) => _domainEvents.Add(@event);

   /// <inheritdoc/>
   public void ClearEvents() => _domainEvents.Clear();
}
