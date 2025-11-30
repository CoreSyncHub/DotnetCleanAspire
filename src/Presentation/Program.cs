using Application;
using Infrastructure;
using Presentation.Extensions;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Add Aspire service defaults (telemetry, health checks, etc.)
builder.AddServiceDefaults();

// Add application layers
builder.Services.AddApplication();
builder.AddInfrastructure();
builder.Services.AddPresentation();

WebApplication app = builder.Build();

// Create API version set
ApiVersionSet versions = app.NewApiVersionSet()
    .HasApiVersion(new ApiVersion(1, 0))
    .ReportApiVersions()
    .Build();

// Configure middleware pipeline
app.ConfigurePipeline();

// Map endpoints
app.UseOpenApiDocumentation();
app.MapHealthCheckEndpoints();
app.MapEndpoints(versions);

await app.RunAsync();
