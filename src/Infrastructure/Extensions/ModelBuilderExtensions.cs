using Microsoft.EntityFrameworkCore.Metadata;

namespace Infrastructure.Extensions;

internal static class ModelBuilderExtensions
{
   extension(ModelBuilder modelBuilder)
   {
      public void ConfigureBaseAbstractions()
      {
         modelBuilder.ConfigureId();
         modelBuilder.ConfigureAuditableEntity();
         modelBuilder.ConfigureAggregateRoot();
      }

      private void ConfigureId()
      {
         foreach (IMutableEntityType entityType in modelBuilder.Model.GetEntityTypes().Where(e => typeof(IEntity).IsAssignableFrom(e.ClrType)))
         {
            IMutableProperty? id = entityType.FindProperty(nameof(IEntity.Id));
            id?.ValueGenerated = ValueGenerated.Never;
         }
      }

      private void ConfigureAuditableEntity()
      {
         foreach (IMutableEntityType entityType in modelBuilder.Model.GetEntityTypes().Where(e => typeof(IAuditableEntity).IsAssignableFrom(e.ClrType)))
         {
            modelBuilder.Entity(entityType.ClrType, eb =>
            {
               eb.Property(nameof(IAuditableEntity.Created)).HasColumnName("created_utc").IsRequired();
               eb.Property(nameof(IAuditableEntity.CreatedBy)).HasColumnName("created_by").HasMaxLength(64);
               eb.Property(nameof(IAuditableEntity.LastModified)).HasColumnName("last_modified_utc");
               eb.Property(nameof(IAuditableEntity.LastModifiedBy)).HasColumnName("last_modified_by").HasMaxLength(64);
               eb.Property(nameof(IAuditableEntity.Version)).IsRowVersion();
            });
         }
      }

      private void ConfigureAggregateRoot()
      {
         foreach (IMutableEntityType entityType in modelBuilder.Model.GetEntityTypes().Where(e => typeof(IAggregateRoot).IsAssignableFrom(e.ClrType)))
         {
            modelBuilder.Entity(entityType.ClrType).Ignore(nameof(IAggregateRoot.DomainEvents));
         }
      }
   }
}
