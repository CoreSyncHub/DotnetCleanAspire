using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace Presentation.DependencyInjection;

internal static partial class PresentationDependencyInjection
{
    extension(IHostApplicationBuilder builder)
    {
        public IHostApplicationBuilder AddObservability()
        {
            // Add Aspire service defaults (telemetry, health checks, etc.)
            builder.AddServiceDefaults();
            builder.Services.AddTelemetryServices();

            return builder;
        }
    }

    extension(IServiceCollection services)
    {
        public IServiceCollection AddTelemetryServices()
        {
            // Configure OpenTelemetry for cache observability
            services.ConfigureOpenTelemetryMeterProvider(metrics =>
            {
                metrics.AddMeter("DotnetCleanAspire.Caching");
            });

            services.ConfigureOpenTelemetryTracerProvider(tracing =>
            {
                tracing.AddSource("DotnetCleanAspire.Caching");
            });

            return services;
        }
    }
}
