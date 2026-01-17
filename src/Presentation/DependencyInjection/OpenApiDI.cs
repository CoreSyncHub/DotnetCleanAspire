using Microsoft.OpenApi;

namespace Presentation.DependencyInjection;

internal static partial class PresentationDependencyInjection
{
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

                   // Replace {version} placeholder with actual version in all paths
                   if (document.Paths is not null)
                   {
                       var pathsToUpdate = document.Paths.ToList();
                       document.Paths.Clear();

                       foreach (KeyValuePair<string, IOpenApiPathItem> pathItem in pathsToUpdate)
                       {
                           string updatedPath = pathItem.Key.Replace("{version}", "1", StringComparison.Ordinal);
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
            });

            services.AddEndpointsApiExplorer();

            return services;
        }
    }
}
