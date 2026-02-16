using Application.Abstractions.Caching;
using Microsoft.AspNetCore.OpenApi;
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
                options.AddDocumentTransformer(TransformDocument);
                options.AddOperationTransformer(TransformOperation);
                options.AddSchemaTransformer(TransformSchema);
            });

            services.AddEndpointsApiExplorer();

            return services;
        }
    }

    private static Task TransformDocument(OpenApiDocument document, OpenApiDocumentTransformerContext _, CancellationToken __)
    {
        document.Info.Title = "Clean Aspire API";
        document.Info.Version = "v1";
        document.Info.Description = "A Clean Architecture template with .NET Aspire";

        ReplaceVersionPlaceholdersInPaths(document);

        return Task.CompletedTask;
    }

    private static void ReplaceVersionPlaceholdersInPaths(OpenApiDocument document)
    {
        if (document.Paths is null)
        {
            return;
        }

        var pathsToUpdate = document.Paths.ToList();
        document.Paths.Clear();

        foreach (KeyValuePair<string, IOpenApiPathItem> pathItem in pathsToUpdate)
        {
            string updatedPath = pathItem.Key
                .Replace("{version:apiVersion}", "1", StringComparison.Ordinal)
                .Replace("{version}", "1", StringComparison.Ordinal);
            document.Paths.Add(updatedPath, pathItem.Value);
        }
    }

    private static Task TransformOperation(OpenApiOperation operation, OpenApiOperationTransformerContext _, CancellationToken __)
    {
        RemoveVersionParameter(operation);

        return Task.CompletedTask;
    }

    private static void RemoveVersionParameter(OpenApiOperation operation)
    {
        if (operation.Parameters is null)
        {
            return;
        }

        IOpenApiParameter? versionParam = operation.Parameters.FirstOrDefault(p => p.Name is "version");
        if (versionParam is not null)
        {
            operation.Parameters.Remove(versionParam);
        }
    }

    private static Task TransformSchema(OpenApiSchema schema, OpenApiSchemaTransformerContext context, CancellationToken _)
    {
        if (!ShouldProcessSchema(schema, context))
        {
            return Task.CompletedTask;
        }

        RemoveCacheProperties(schema);

        return Task.CompletedTask;
    }

    private static bool ShouldProcessSchema(OpenApiSchema schema, OpenApiSchemaTransformerContext context)
    {
        if (schema.Properties is null || schema.Properties.Count == 0)
        {
            return false;
        }

        Type type = context.JsonTypeInfo.Type;

        return typeof(ICacheable).IsAssignableFrom(type)
            || typeof(ICacheInvalidating).IsAssignableFrom(type);
    }

    private static void RemoveCacheProperties(OpenApiSchema schema)
    {
        if (schema.Properties is null)
        {
            return;
        }

        foreach (string propName in CachePropertiesToExclude)
        {
            string camelCaseName = char.ToLowerInvariant(propName[0]) + propName[1..];
            schema.Properties.Remove(camelCaseName);
        }
    }
}
