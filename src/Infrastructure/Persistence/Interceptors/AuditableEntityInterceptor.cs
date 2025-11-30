using Application.Abstractions.Helpers;
using Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Infrastructure.Persistence.Interceptors;

public sealed class AuditableEntityInterceptor(IUser user) : SaveChangesInterceptor
{
   public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
   {
      UpdateEntities(eventData.Context);

      return base.SavingChanges(eventData, result);
   }

   public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
   {
      UpdateEntities(eventData.Context);

      return base.SavingChangesAsync(eventData, result, cancellationToken);
   }

   private void UpdateEntities(DbContext? context)
   {
      if (context is null)
         return;

      DateTimeOffset utcNow = DateTimeOffset.UtcNow;

      foreach (EntityEntry<IAuditableEntity> entry in context.ChangeTracker.Entries<IAuditableEntity>())
      {
         if (entry.State is not (EntityState.Added or EntityState.Modified) && !entry.HasChangedOwnedEntities())
            continue;

         if (entry.State == EntityState.Added)
         {
            entry.Property(nameof(IAuditableEntity.Created)).CurrentValue = utcNow;
            entry.Property(nameof(IAuditableEntity.CreatedBy)).CurrentValue = user.Id;
         }

         entry.Property(nameof(IAuditableEntity.LastModified)).CurrentValue = utcNow;
         entry.Property(nameof(IAuditableEntity.LastModifiedBy)).CurrentValue = user.Id;
      }
   }
}
