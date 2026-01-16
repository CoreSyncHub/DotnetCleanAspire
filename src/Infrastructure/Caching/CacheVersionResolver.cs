using Application.Abstractions.Caching;
using Microsoft.Extensions.Options;

namespace Infrastructure.Caching;

/// <summary>
/// Implementation of version resolution for cache operations.
/// Ensures consistent version resolution between caching and invalidation.
/// </summary>
public sealed class CacheVersionResolver(IOptions<CacheOptions> cacheOptions) : ICacheVersionResolver
{
    private readonly CacheOptions _cacheOptions = cacheOptions.Value;

    /// <inheritdoc />
    public string Resolve(ICacheable cacheable)
    {
        // Use explicit version if provided
        if (!string.IsNullOrEmpty(cacheable.Version))
        {
            return cacheable.Version;
        }

        // Try to get feature-specific version
        if (_cacheOptions.FeatureVersions.TryGetValue(cacheable.CacheKey.Feature, out string? featureVersion))
        {
            return featureVersion;
        }

        // Fallback to global version
        return _cacheOptions.GlobalVersion;
    }

    /// <inheritdoc />
    public string Resolve(ICacheKey cacheKey)
    {
        // Try to get feature-specific version
        if (_cacheOptions.FeatureVersions.TryGetValue(cacheKey.Feature, out string? featureVersion))
        {
            return featureVersion;
        }

        // Fallback to global version
        return _cacheOptions.GlobalVersion;
    }
}
