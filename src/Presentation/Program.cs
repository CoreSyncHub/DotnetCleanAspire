using Application.DependencyInjection;
using Infrastructure.DependencyInjection;
using Presentation.DependencyInjection;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder
    .AddInfrastructure()
    .AddApplication()
    .AddPresentation();

WebApplication app = builder.Build();

await app.RunAsync();
