namespace Application.Abstractions.Caching;

/// <summary>
/// Resolves cache version for a cacheable item based on version precedence rules.
/// This is a transverse responsibility shared between caching and cache invalidation.
/// </summary>
public interface ICacheVersionResolver
{
    /// <summary>
    /// Resolves the version to use for caching or invalidation.
    /// Resolution order:
    /// 1. Explicit version from the cacheable item (if provided)
    /// 2. Feature-specific version from configuration (if exists for the feature)
    /// 3. Global version from configuration
    /// </summary>
    /// <param name="cacheable">The cacheable item containing version and cache key information.</param>
    /// <returns>The resolved version string.</returns>
    string Resolve(ICacheable cacheable);

    /// <summary>
    /// Resolves the version to use for a cache key during invalidation.
    /// Uses the same resolution logic as caching to ensure consistency.
    /// Resolution order:
    /// 1. Feature-specific version from configuration (if exists for the feature)
    /// 2. Global version from configuration
    /// </summary>
    /// <param name="cacheKey">The cache key to resolve version for.</param>
    /// <returns>The resolved version string.</returns>
    string Resolve(ICacheKey cacheKey);
}
