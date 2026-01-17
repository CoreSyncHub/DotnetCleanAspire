using Infrastructure.Extensions;

namespace Presentation.Extensions;

/// <summary>
/// Extensions for configuring the application middleware pipeline.
/// </summary>
internal static class WebApplicationExtensions
{
   extension(WebApplication app)
   {
      /// <summary>
      /// Configures the middleware pipeline for the web application.
      /// </summary>
      /// <returns></returns>
      public WebApplication ConfigureMiddlewarePipeline()
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

      /// <summary>
      /// Migrates the database on application startup if configured to do so.
      /// </summary>
      public async Task MigrateDatabase()
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
      public WebApplication UseOpenApiDocumentation()
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
      public WebApplication MapHealthCheckEndpoints()
      {
         app.MapHealthChecks("/health");
         app.MapHealthChecks("/alive", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
         {
            Predicate = _ => false
         });

         return app;
      }
   }
}
