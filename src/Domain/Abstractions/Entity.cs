namespace Domain.Abstractions;

public abstract class Entity : IEntity, IEquatable<Entity>
{
   /// <inheritdoc/>
   public Id Id { get; protected set; }

   /// <summary>
   /// Initializes a new instance of the <see cref="Entity"/> class with a new unique identifier.
   /// </summary>
   protected Entity()
   {
      Id = Id.New();
   }

   /// <summary>
   /// Initializes a new instance of the <see cref="Entity"/> class with the specified identifier.
   /// </summary>
   /// <param name="id">The unique identifier for the entity.</param>
   protected Entity(Id id)
   {
      Id = id;
   }

   public sealed override bool Equals(object? obj) =>
       obj is Entity other && GetType() == other.GetType() && Id == other.Id;

   public sealed override int GetHashCode() => HashCode.Combine(GetType(), Id);

   public bool Equals(Entity? other) => Equals((object?)other);

   public static bool operator ==(Entity? left, Entity? right)
      => left?.Equals(right) ?? (right is null);

   public static bool operator !=(Entity? left, Entity? right)
      => !(left == right);
}
