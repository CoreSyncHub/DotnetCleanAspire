using Application.Abstractions.Caching;
using Application.Abstractions.Helpers;
using Application.Abstractions.Persistence;
using Infrastructure.Caching;
using Infrastructure.Identity;
using Infrastructure.Persistence;
using Infrastructure.Persistence.Interceptors;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Infrastructure;

/// <summary>
/// Extension methods for registering Infrastructure layer services.
/// </summary>
public static class InfrastructureDependencyInjection
{
   extension(IHostApplicationBuilder builder)
   {
      /// <summary>
      /// Adds Infrastructure layer services with Aspire integration.
      /// </summary>
      /// <returns>The host application builder for chaining.</returns>
      public IHostApplicationBuilder AddInfrastructure()
      {
         // Identity
         builder.Services.AddHttpContextAccessor();
         builder.Services.AddScoped<IUser, CurrentUser>();

         // Interceptors
         builder.Services.AddSingleton<AuditableEntityInterceptor>();

         // DbContext with Aspire PostgreSQL integration
         builder.AddNpgsqlDbContext<ApplicationDbContext>("cleanaspire-db");

         // Register as IApplicationDbContext
         builder.Services.AddScoped<IApplicationDbContext>(sp =>
             sp.GetRequiredService<ApplicationDbContext>());

         // Redis caching with Aspire integration
         builder.AddRedisDistributedCache("redis");
         builder.Services.AddScoped<ICacheService, DistributedCacheService>();

         return builder;
      }
   }
}
