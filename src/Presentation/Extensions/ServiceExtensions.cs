using Presentation.Converters;
using Presentation.Middleware;

namespace Presentation.Extensions;

/// <summary>
/// Extensions for configuring presentation layer services.
/// </summary>
internal static class ServiceExtensions
{
   /// <summary>
   /// Adds presentation layer services including OpenAPI, versioning, and CORS.
   /// </summary>
   /// <param name="services">The service collection.</param>
   /// <returns>The service collection for chaining.</returns>
   public static IServiceCollection AddPresentation(this IServiceCollection services)
   {
      services.AddOpenApiDocumentation();
      services.AddApiVersioningConfiguration();
      services.AddCorsConfiguration();
      services.AddExceptionHandling();
      services.ConfigureJsonOptions();

      return services;
   }

   private static IServiceCollection AddOpenApiDocumentation(this IServiceCollection services)
   {
      services.AddOpenApi(options =>
      {
         options.AddDocumentTransformer((document, context, cancellationToken) =>
           {
             document.Info.Title = "Clean Aspire API";
             document.Info.Version = "v1";
             document.Info.Description = "A Clean Architecture template with .NET Aspire";
             return Task.CompletedTask;
          });
      });

      services.AddEndpointsApiExplorer();

      return services;
   }

   private static IServiceCollection AddApiVersioningConfiguration(this IServiceCollection services)
   {
      services.AddApiVersioning(options =>
      {
         options.DefaultApiVersion = new ApiVersion(1, 0);
         options.AssumeDefaultVersionWhenUnspecified = true;
         options.ReportApiVersions = true;
         options.ApiVersionReader = ApiVersionReader.Combine(
               new UrlSegmentApiVersionReader(),
               new HeaderApiVersionReader("X-Api-Version"));
      });

      return services;
   }

   private static IServiceCollection AddCorsConfiguration(this IServiceCollection services)
   {
      services.AddCors(options =>
      {
         options.AddDefaultPolicy(policy =>
           {
              // Configure as needed - this is a permissive default for development
             policy.AllowAnyOrigin()
                     .AllowAnyMethod()
                     .AllowAnyHeader();
          });

         // Named policy for more restrictive scenarios
         options.AddPolicy("Strict", policy =>
           {
             policy.SetIsOriginAllowedToAllowWildcardSubdomains()
                     .AllowAnyMethod()
                     .AllowAnyHeader()
                     .AllowCredentials();
          });
      });

      return services;
   }

   private static IServiceCollection AddExceptionHandling(this IServiceCollection services)
   {
      services.AddExceptionHandler<GlobalExceptionHandler>();
      services.AddProblemDetails(options =>
      {
         options.CustomizeProblemDetails = context =>
           {
             context.ProblemDetails.Extensions["traceId"] = context.HttpContext.TraceIdentifier;
          };
      });

      return services;
   }

   private static IServiceCollection ConfigureJsonOptions(this IServiceCollection services)
   {
      services.ConfigureHttpJsonOptions(options =>
      {
         options.SerializerOptions.Converters.Add(new IdJsonConverter());
         options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
         options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
      });

      return services;
   }
}
