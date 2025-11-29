using Domain.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Infrastructure.Extensions;

internal static class ModelBuilderExtensions
{
   extension(ModelBuilder modelBuilder)
   {
      public void ConfigureBaseAbstractions()
      {
         modelBuilder.ConfigureId();
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

      private void ConfigureAggregateRoot()
      {
         foreach (IMutableEntityType entityType in modelBuilder.Model.GetEntityTypes().Where(e => typeof(IAggregateRoot).IsAssignableFrom(e.ClrType)))
         {
            modelBuilder.Entity(entityType.ClrType).Ignore(nameof(IAggregateRoot.DomainEvents));
         }
      }
   }
}
