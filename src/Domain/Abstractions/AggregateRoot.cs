namespace Domain.Abstractions;

public abstract class AggregateRoot : Entity, IAggregateRoot
{
   private readonly List<IDomainEvent> _domainEvents = [];

   /// <inheritdoc/>
   public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

   /// <summary>
   /// Initializes a new instance of the <see cref="AggregateRoot"/> class with a new unique identifier.
   /// </summary>
   protected AggregateRoot() : base()
   {
   }

   /// <summary>
   /// Initializes a new instance of the <see cref="AggregateRoot"/> class with the specified identifier.
   /// </summary>
   /// <param name="id">The unique identifier for the aggregate root.</param>
   protected AggregateRoot(Id id) : base(id)
   {
   }

   /// <summary>
   /// Raises a domain event.
   /// </summary>
   /// <param name="event">The domain event to raise.</param>
   protected void Raise(IDomainEvent @event) => _domainEvents.Add(@event);

   /// <inheritdoc/>
   public void ClearEvents() => _domainEvents.Clear();
}
