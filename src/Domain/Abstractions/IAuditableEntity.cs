namespace Domain.Abstractions;

/// <summary>
/// Represents an auditable entity with creation and modification tracking.
/// </summary>
public interface IAuditableEntity
{
   /// <summary>
   /// Gets the date and time when the entity was created.
   /// </summary>
   DateTimeOffset Created { get; }

   /// <summary>
   /// Gets the identifier of the user who created the entity.
   /// </summary>
   string? CreatedBy { get; }

   /// <summary>
   /// Gets the date and time when the entity was last modified.
   /// </summary>
   DateTimeOffset? LastModified { get; }

   /// <summary>
   /// Gets the identifier of the user who last modified the entity.
   /// </summary>
   string? LastModifiedBy { get; }

   /// <summary>
   /// Gets the version number of the entity for concurrency control.
   /// </summary>
   uint Version { get; }

   /// <summary>
   /// Updates the last modified information.
   /// </summary>
   /// <param name="user">The identifier of the user who modified the entity.</param>
   void Touch(string? user);
}
