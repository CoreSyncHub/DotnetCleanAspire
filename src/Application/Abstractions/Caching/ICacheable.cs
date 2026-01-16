namespace Application.Abstractions.Caching;

/// <summary>
/// Marker interface for cacheable queries.
/// Implement this interface on queries that should be cached.
/// </summary>
public interface ICacheable
{
   /// <summary>
   /// Gets the cache key for this request.
   /// This key will be prefixed with the version automatically.
   /// </summary>
   /// <remarks>
   /// Use a structured ICacheKey implementation to avoid key collisions and ensure consistency.
   /// And note that pattern-based invalidation (wildcards) is not supported for performance and complexity reasons.
   /// </remarks>
   ICacheKey CacheKey { get; }

   /// <summary>
   /// Gets the cache duration.
   /// If null, uses the global CacheOptions.DefaultCacheDuration.
   /// Use this to override duration for specific queries.
   /// </summary>
   TimeSpan? CacheDuration => null;

   /// <summary>
   /// Gets the cache version for this request.
   /// If null, uses the global or feature-specific version from CacheOptions.
   /// Use this to override versioning for specific queries.
   /// </summary>
   string? Version => null;

   /// <summary>
   /// Gets whether compression should be used for this cache entry.
   /// If null, uses the global CacheOptions.EnableCompression.
   /// Use this to override compression for specific queries.
   /// </summary>
   bool? UseCompression => null;
}
