using Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Extensions;

public static class EfExtensions
{
   extension(IServiceProvider sp)
   {
      public async Task MigrateAsync(bool seed, CancellationToken ct)
      {
         using IServiceScope scope = sp.CreateScope();
         ApplicationDbContext dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
         await dbContext.Database.MigrateAsync(ct);
         if (seed)
         {
            await SeedAsync(sp, ct);
         }
      }
   }

   private static async Task SeedAsync(IServiceProvider sp, CancellationToken ct)
   {
      using IServiceScope scope = sp.CreateScope();
      ApplicationDbContext dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
      await ApplicationDbContextSeed.SeedAsync(dbContext, ct);
   }
}
