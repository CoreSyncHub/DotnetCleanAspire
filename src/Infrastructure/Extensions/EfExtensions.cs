using Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

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
      }
   }
}
