using Infrastructure.Extensions;

namespace Presentation.Extensions;

/// <summary>
/// Extensions for configuring the application middleware pipeline.
/// </summary>
internal static class WebApplicationExtensions
{
   extension(WebApplication app)
   {
      public async Task LaunchAsync()
      {
         ApiVersionSet versions = app.CreateApiVersionSet();

         app
            .ConfigureMiddlewarePipeline()
            .UseOpenApiDocumentation()
            .MapHealthCheckEndpoints()
            .MapEndpoints(versions)
            .MapObservabilityEndpoints();

         await app.ExecuteDatabaseMigrations();
         await app.RunAsync();
      }

      private ApiVersionSet CreateApiVersionSet()
      {
         return app.NewApiVersionSet()
            .HasApiVersion(new ApiVersion(1, 0))
            .ReportApiVersions()
            .Build();
      }

      /// <summary>
      /// Configures the middleware pipeline for the web application.
      /// </summary>
      private WebApplication ConfigureMiddlewarePipeline()
      {
         // Forwarded headers (when behind a proxy)
         app.UseForwardedHeaders();

         // Exception handling
         app.UseExceptionHandler();

         // Security
         app.UseHttpsRedirection();

         // CORS (configure policies in services)
         app.UseCors();

         // Rate limiting
         app.UseRateLimiter();

         // Authentication & Authorization
         app.UseAuthentication();
         app.UseAuthorization();

         return app;
      }

      /// <summary>
      /// Configures OpenAPI/Swagger documentation endpoints.
      /// </summary>
      private WebApplication UseOpenApiDocumentation()
      {
         if (app.Environment.IsDevelopment())
         {
            app.MapOpenApi();
            app.UseSwaggerUI(options =>
            {
               // Configure Swagger UI to use the OpenAPI document
               options.SwaggerEndpoint("/openapi/v1.json", "API v1");
               options.RoutePrefix = "swagger";
            });
         }

         return app;
      }

      /// <summary>
      /// Maps health check endpoints.
      /// </summary>
      private WebApplication MapHealthCheckEndpoints()
      {
         app.MapHealthChecks("/health");
         app.MapHealthChecks("/alive", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
         {
            Predicate = _ => false
         });

         return app;
      }

      private WebApplication MapObservabilityEndpoints()
      {
         if (!app.Environment.IsEnvironment("Testing"))
         {
            app.MapPrometheusScrapingEndpoint();
         }

         return app;
      }

      /// <summary>
      /// Migrates the database on application startup if configured to do so.
      /// </summary>
      private async Task ExecuteDatabaseMigrations()
      {
         if (app.Configuration.GetValue<bool>("Ef:MigrateOnStartup"))
         {
            ILogger logger = app.Services.GetRequiredService<ILogger<Program>>();
            logger.LogInformation("Starting database migration...");
            await app.Services.MigrateAsync(
               seed: app.Configuration.GetValue<bool>("Ef:SeedOnStartup"),
               app.Lifetime.ApplicationStopping
            );
            logger.LogInformation("Database migration completed");
         }
      }
   }
}
