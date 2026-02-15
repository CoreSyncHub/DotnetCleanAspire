using Application.Abstractions.Persistence;
using Infrastructure.Persistence;
using Infrastructure.Persistence.Interceptors;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Npgsql;

namespace Infrastructure.DependencyInjection;

internal static partial class InfrastructureDependencyInjection
{
    extension(IHostApplicationBuilder builder)
    {
        public IHostApplicationBuilder AddPersistence()
        {
            // Interceptors (registered as scoped so they're injected into DbContext)
            builder.Services.AddScoped<AuditableEntityInterceptor>();
            builder.Services.AddScoped<DomainEventDispatchInterceptor>();

            // Register Npgsql data source via Aspire (handles connection string, health checks, telemetry)
            builder.AddNpgsqlDataSource("cleanaspire-db");

            // Register DbContext without pooling to support scoped interceptors
            builder.Services.AddDbContext<ApplicationDbContext>((sp, options) =>
            {
                NpgsqlDataSource dataSource = sp.GetRequiredService<NpgsqlDataSource>();
                options.UseNpgsql(dataSource);
            });

            // Register as IApplicationDbContext
            builder.Services.AddScoped<IApplicationDbContext>(sp =>
                sp.GetRequiredService<ApplicationDbContext>());

            return builder;
        }
    }
}
