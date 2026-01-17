using Application.Abstractions.Caching;
using Application.DependencyInjection.Options;
using Infrastructure.Caching;
using Infrastructure.Caching.Serializers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Infrastructure.DependencyInjection;

internal static partial class InfrastructureDependencyInjection
{
    extension(IHostApplicationBuilder builder)
    {
        /// <summary>
        /// Adds Infrastructure layer services with caching configuration.
        /// </summary>
        /// <returns>The host application builder for chaining.</returns>
        public IHostApplicationBuilder AddCaching()
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

            // Configure CacheOptions
            builder.Services.Configure<CacheOptions>(builder.Configuration.GetSection(CacheOptions.SectionName));

            return builder;
        }
    }
}
