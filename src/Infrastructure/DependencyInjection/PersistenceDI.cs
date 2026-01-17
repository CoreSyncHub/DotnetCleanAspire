using Application.Abstractions.Persistence;
using Infrastructure.Persistence;
using Infrastructure.Persistence.Interceptors;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

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

            // DbContext with Aspire PostgreSQL integration
            builder.AddNpgsqlDbContext<ApplicationDbContext>("cleanaspire-db");

            // Register as IApplicationDbContext
            builder.Services.AddScoped<IApplicationDbContext>(sp =>
                sp.GetRequiredService<ApplicationDbContext>());

            return builder;
        }
    }
}
