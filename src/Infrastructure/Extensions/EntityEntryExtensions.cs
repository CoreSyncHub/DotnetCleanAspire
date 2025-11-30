using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Infrastructure.Extensions;

internal static class EntityEntryExtensions
{
   extension(EntityEntry entry)
   {
      public bool HasChangedOwnedEntities()
      {
         bool refs = entry.References.Any(r =>
             r.TargetEntry is { } t
             && t.Metadata.IsOwned()
             && t.State is EntityState.Added or EntityState.Modified or EntityState.Deleted);

         bool cols = entry.Collections.Any(c =>
             c.Metadata.TargetEntityType.IsOwned()
             && c.IsLoaded
             && c.CurrentValue!.Cast<object>().Any(i =>
             {
                EntityEntry e = entry.Context.Entry(i);
                return e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted;
             }));

         return refs || cols;
      }
   }
}
