using Application.Abstractions.Caching;
using Application.Abstractions.Persistence;
using Infrastructure.Caching;
using Infrastructure.Persistence;
using Infrastructure.Persistence.Interceptors;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure;

/// <summary>
/// Extension methods for registering Infrastructure layer services.
/// </summary>
public static class InfrastructureDependencyInjection
{
   /// <summary>
   /// Adds Infrastructure layer services to the service collection.
   /// </summary>
   /// <param name="services">The service collection.</param>
   /// <param name="configuration">The configuration.</param>
   /// <returns>The service collection for chaining.</returns>
   public static IServiceCollection AddInfrastructure(
       this IServiceCollection services,
       IConfiguration configuration)
   {
      // Interceptors
      services.AddScoped<AuditableEntityInterceptor>();

      // DbContext (can be overridden by Aspire)
      services.AddDbContext<ApplicationDbContext>((sp, options) =>
      {
         string? connectionString = configuration.GetConnectionString("DefaultConnection");
         if (!string.IsNullOrEmpty(connectionString))
         {
            options.UseNpgsql(connectionString);
         }

         options.AddInterceptors(sp.GetRequiredService<AuditableEntityInterceptor>());
      });

      // Register as IApplicationDbContext
      services.AddScoped<IApplicationDbContext>(sp =>
          sp.GetRequiredService<ApplicationDbContext>());

      // Caching
      services.AddScoped<ICacheService, DistributedCacheService>();

      return services;
   }
}
