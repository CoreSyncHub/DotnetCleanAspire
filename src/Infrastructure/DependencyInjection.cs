using Application.Abstractions.Caching;
using Application.Abstractions.Helpers;
using Application.Abstractions.Persistence;
using Infrastructure.Caching;
using Infrastructure.Caching.Serializers;
using Infrastructure.Identity;
using Infrastructure.Persistence;
using Infrastructure.Persistence.Interceptors;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;

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
         builder.AddAuthenticationConfiguration();

         // Persistence
         builder.AddPersistence();

         // Caching
         builder.AddCaching();

         return builder;
      }

      private void AddPersistence()
      {
         // Interceptors (registered as scoped so they're injected into DbContext)
         builder.Services.AddScoped<AuditableEntityInterceptor>();
         builder.Services.AddScoped<DomainEventDispatchInterceptor>();

         // DbContext with Aspire PostgreSQL integration
         builder.AddNpgsqlDbContext<ApplicationDbContext>("cleanaspire-db");

         // Register as IApplicationDbContext
         builder.Services.AddScoped<IApplicationDbContext>(sp =>
             sp.GetRequiredService<ApplicationDbContext>());
      }

      private void AddCaching()
      {
         // Redis caching with Aspire integration
         builder.AddRedisDistributedCache("redis");

         // Register cache serializer (default: JSON, can be switched to MessagePack for performance)
         builder.Services.AddSingleton<ICacheSerializer, JsonCacheSerializer>();
         // Alternative: builder.Services.AddSingleton<ICacheSerializer, MessagePackCacheSerializer>();

         // Register cache metrics (singleton for performance)
         builder.Services.AddSingleton<Caching.Observability.CacheMetrics>();

         // Register cache version resolver (transverse responsibility for caching and invalidation)
         builder.Services.AddSingleton<ICacheVersionResolver, CacheVersionResolver>();

         builder.Services.AddScoped<ICacheService, DistributedCacheService>();
      }

      private void AddAuthenticationConfiguration()
      {
         IConfigurationSection jwtConfig = builder.Configuration.GetSection("Jwt");

         builder.Services.AddAuthentication(options =>
         {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
         })
         .AddJwtBearer(options =>
         {
            options.TokenValidationParameters = new TokenValidationParameters
            {
               ValidateIssuer = true,
               ValidateAudience = true,
               ValidateLifetime = true,
               ValidateIssuerSigningKey = true,
               ClockSkew = TimeSpan.Zero,
               ValidIssuer = jwtConfig["Issuer"],
               ValidAudience = jwtConfig["Audience"],
               IssuerSigningKey = new SymmetricSecurityKey(
                  Encoding.UTF8.GetBytes(jwtConfig["Key"] ?? throw new InvalidOperationException("JWT Key is not configured")))
            };

            options.Events = new JwtBearerEvents
            {
               OnAuthenticationFailed = context =>
               {
                  if (context.Exception.GetType() == typeof(SecurityTokenExpiredException))
                  {
                     context.Response.Headers.Append("Token-Expired", "true");
                  }

                  return Task.CompletedTask;
               }
            };
         });

         builder.Services.AddAuthorization();
         builder.Services.AddHttpContextAccessor();
         builder.Services.AddScoped<IUser, CurrentUser>();
      }
   }
}
