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

   public bool Equals(Entity? other) =>
       other is not null && GetType() == other.GetType() && Id == other.Id;

   /// <inheritdoc/>
   public override bool Equals(object? obj) => Equals(obj as Entity);

   public override int GetHashCode() => HashCode.Combine(GetType(), Id);
}
