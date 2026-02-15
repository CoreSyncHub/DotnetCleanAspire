using Application.Abstractions.Caching;
using Microsoft.OpenApi;

namespace Presentation.DependencyInjection;

internal static partial class PresentationDependencyInjection
{
    // Properties from ICacheable and ICacheInvalidating to exclude from OpenAPI schemas
    private static readonly HashSet<string> CachePropertiesToExclude =
    [
        nameof(ICacheable.CacheKey),
        nameof(ICacheable.CacheDuration),
        nameof(ICacheable.Version),
        nameof(ICacheable.UseCompression),
        nameof(ICacheInvalidating.KeysToInvalidate),
        nameof(ICacheInvalidating.FeaturesToInvalidate)
    ];

    extension(IServiceCollection services)
    {
        public IServiceCollection AddOpenApiDocumentation()
        {
            services.AddOpenApi(options =>
            {
                options.AddDocumentTransformer((document, context, cancellationToken) =>
               {
                   document.Info.Title = "Clean Aspire API";
                   document.Info.Version = "v1";
                   document.Info.Description = "A Clean Architecture template with .NET Aspire";

                   // Replace version placeholder with actual version in all paths
                   if (document.Paths is not null)
                   {
                       var pathsToUpdate = document.Paths.ToList();
                       document.Paths.Clear();

                       foreach (KeyValuePair<string, IOpenApiPathItem> pathItem in pathsToUpdate)
                       {
                           // Handle both {version} and {version:apiVersion} patterns
                           string updatedPath = pathItem.Key
                               .Replace("{version:apiVersion}", "1", StringComparison.Ordinal)
                               .Replace("{version}", "1", StringComparison.Ordinal);
                           document.Paths.Add(updatedPath, pathItem.Value);
                       }
                   }

                   return Task.CompletedTask;
               });

                // Add operation transformer to remove version parameter from operations
                options.AddOperationTransformer((operation, context, cancellationToken) =>
               {
                   // Remove version parameter from operations
                   if (operation.Parameters is not null)
                   {
                       IOpenApiParameter? versionParam = operation.Parameters.FirstOrDefault(p => p.Name is "version");
                       if (versionParam is not null)
                       {
                           operation.Parameters.Remove(versionParam);
                       }
                   }

                   return Task.CompletedTask;
               });

                // Add schema transformer to exclude cache interface properties
                options.AddSchemaTransformer((schema, context, cancellationToken) =>
                {
                    if (schema.Properties is null || schema.Properties.Count == 0)
                    {
                        return Task.CompletedTask;
                    }

                    Type type = context.JsonTypeInfo.Type;
                    bool implementsCaching = typeof(ICacheable).IsAssignableFrom(type)
                        || typeof(ICacheInvalidating).IsAssignableFrom(type);

                    if (!implementsCaching)
                    {
                        return Task.CompletedTask;
                    }

                    // Remove cache-related properties from schema (use camelCase as that's the JSON naming policy)
                    foreach (string propName in CachePropertiesToExclude)
                    {
                        string camelCaseName = char.ToLowerInvariant(propName[0]) + propName[1..];
                        schema.Properties.Remove(camelCaseName);
                    }

                    return Task.CompletedTask;
                });
            });

            services.AddEndpointsApiExplorer();

            return services;
        }
    }
}
