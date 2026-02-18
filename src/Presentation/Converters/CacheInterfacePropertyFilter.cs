using Application.Abstractions.Caching;
using System.Text.Json.Serialization.Metadata;

namespace Presentation.Converters;

/// <summary>
/// JSON type info modifier that excludes properties from caching interfaces
/// (ICacheable, ICacheInvalidating) from serialization and OpenAPI documentation.
/// </summary>
internal static class CacheInterfacePropertyFilter
{
    private static readonly HashSet<string> CacheableProperties =
    [
        nameof(ICacheable.CacheKey),
        nameof(ICacheable.CacheDuration),
        nameof(ICacheable.Version),
        nameof(ICacheable.UseCompression)
    ];

    private static readonly HashSet<string> CacheInvalidatingProperties =
    [
        nameof(ICacheInvalidating.KeysToInvalidate),
        nameof(ICacheInvalidating.FeaturesToInvalidate)
    ];

    public static void ExcludeCacheProperties(JsonTypeInfo typeInfo)
    {
        if (typeInfo.Kind != JsonTypeInfoKind.Object)
        {
            return;
        }

        bool implementsCacheable = typeof(ICacheable).IsAssignableFrom(typeInfo.Type);
        bool implementsCacheInvalidating = typeof(ICacheInvalidating).IsAssignableFrom(typeInfo.Type);

        if (!implementsCacheable && !implementsCacheInvalidating)
        {
            return;
        }

        foreach (JsonPropertyInfo property in typeInfo.Properties)
        {
            if ((implementsCacheable && CacheableProperties.Contains(property.Name))
                || (implementsCacheInvalidating && CacheInvalidatingProperties.Contains(property.Name)))
            {
                property.ShouldSerialize = (_, _) => false;
            }
        }
    }
}
