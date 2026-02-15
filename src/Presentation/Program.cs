using Application.DependencyInjection;
using Infrastructure.DependencyInjection;
using Presentation.DependencyInjection;
using Presentation.Extensions;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Load appsettings.Development.json in Development environment
Console.WriteLine($"Environment: {builder.Environment.EnvironmentName}");
if (builder.Environment.IsDevelopment())
{
    builder.Configuration.AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: true);
}

builder
    .AddInfrastructure()
    .AddApplication()
    .AddPresentation();

WebApplication app = builder.Build();

await app.LaunchAsync();
