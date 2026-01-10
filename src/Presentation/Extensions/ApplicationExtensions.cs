using Infrastructure.Extensions;

namespace Presentation.Extensions;

/// <summary>
/// Extensions for configuring the application middleware pipeline.
/// </summary>
internal static class ApplicationExtensions
{
   /// <summary>
   /// Configures the middleware pipeline with exception handling, security, and API documentation.
   /// </summary>
   /// <param name="app">The web application.</param>
   /// <returns>The web application for chaining.</returns>
   public static WebApplication ConfigurePipeline(this WebApplication app)
   {
      // Exception handling
      app.UseExceptionHandler();

      // Security
      app.UseHttpsRedirection();

      // CORS (configure policies in services)
      app.UseCors();

      // Authentication & Authorization
      app.UseAuthentication();
      app.UseAuthorization();

      return app;
   }

   public static async Task MigrateDatabase(this WebApplication app)
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

   /// <summary>
   /// Configures OpenAPI/Swagger documentation endpoints.
   /// </summary>
   /// <param name="app">The web application.</param>
   /// <returns>The web application for chaining.</returns>
   public static WebApplication UseOpenApiDocumentation(this WebApplication app)
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
   /// <param name="app">The web application.</param>
   /// <returns>The web application for chaining.</returns>
   public static WebApplication MapHealthCheckEndpoints(this WebApplication app)
   {
      app.MapHealthChecks("/health");
      app.MapHealthChecks("/alive", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
      {
         Predicate = _ => false
      });

      return app;
   }
}
