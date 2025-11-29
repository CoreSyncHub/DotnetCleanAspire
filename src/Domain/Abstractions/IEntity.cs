namespace Domain.Abstractions;

/// <summary>
/// Represents an entity with a unique identifier.
/// </summary>
public interface IEntity
{
   /// <summary>
   /// The unique identifier of the entity.
   /// </summary>
   Id Id { get; }
}
