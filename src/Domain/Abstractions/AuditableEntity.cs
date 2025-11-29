namespace Domain.Abstractions;

public abstract class AuditableEntity : Entity, IAuditableEntity
{
   protected AuditableEntity() : base()
   {
   }

   protected AuditableEntity(Id id) : base(id)
   {
   }

   /// <inheritdoc/>
   public DateTimeOffset Created { get; init; } = DateTimeOffset.UtcNow;

   /// <inheritdoc/>
   public string? CreatedBy { get; init; }

   /// <inheritdoc/>
   public DateTimeOffset? LastModified { get; private set; }

   /// <inheritdoc/>
   public string? LastModifiedBy { get; private set; }

   /// <inheritdoc/>
   public uint Version { get; set; }

   /// <inheritdoc/>
   public void Touch(string? user)
   {
      LastModified = DateTimeOffset.UtcNow;
      LastModifiedBy = user;
   }
}
