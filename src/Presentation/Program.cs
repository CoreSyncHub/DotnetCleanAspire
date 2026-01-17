using Application;
using Infrastructure;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Presentation.Extensions;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Add Aspire service defaults (telemetry, health checks, etc.)
builder.AddServiceDefaults();

// Configure OpenTelemetry for cache observability
builder.Services.ConfigureOpenTelemetryMeterProvider(metrics =>
{
    metrics.AddMeter("DotnetCleanAspire.Caching");
});

builder.Services.ConfigureOpenTelemetryTracerProvider(tracing =>
{
    tracing.AddSource("DotnetCleanAspire.Caching");
});

// Add application layers
builder.Services.AddApplication(builder.Configuration);
builder.AddInfrastructure();
builder.Services.AddPresentation();

WebApplication app = builder.Build();

// Create API version set
ApiVersionSet versions = app.NewApiVersionSet()
    .HasApiVersion(new ApiVersion(1, 0))
    .ReportApiVersions()
    .Build();

// Configure middleware pipeline
app.ConfigureMiddlewarePipeline();

// Map endpoints
app.UseOpenApiDocumentation();
app.MapHealthCheckEndpoints();

// Only map Prometheus endpoint if not in Testing environment
if (!app.Environment.IsEnvironment("Testing"))
{
    app.MapPrometheusScrapingEndpoint();
}

app.MapEndpoints(versions);

await app.MigrateDatabase();

await app.RunAsync();
