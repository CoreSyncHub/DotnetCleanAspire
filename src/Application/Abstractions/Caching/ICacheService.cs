namespace Application.Abstractions.Caching;

/// <summary>
/// Abstraction for cache operations.
/// Implementation should use IDistributedCache for Redis compatibility.
/// </summary>
public interface ICacheService
{
   /// <summary>
   /// Gets a cached entry by key.
   /// Always returns a CacheEntry to distinguish between cache miss and stored null values.
   /// </summary>
   /// <typeparam name="T">The type of the cached value.</typeparam>
   /// <param name="key">The cache key.</param>
   /// <param name="version">Optional version prefix for the key.</param>
   /// <param name="cancellationToken">Cancellation token.</param>
   /// <returns>A CacheEntry, or null if the key doesn't exist in cache (cache miss).</returns>
   Task<CacheEntry<T>?> GetAsync<T>(ICacheKey key, string? version = null, CancellationToken cancellationToken = default);

   /// <summary>
   /// Sets a cache entry in the cache.
   /// </summary>
   /// <typeparam name="T">The type of the value.</typeparam>
   /// <param name="key">The cache key.</param>
   /// <param name="entry">The cache entry to store.</param>
   /// <param name="expiration">The expiration time. If null, uses global CacheOptions.DefaultCacheDuration.</param>
   /// <param name="version">Optional version prefix for the key.</param>
   /// <param name="useCompression">Whether to use compression for this entry. If null, uses global CacheOptions.EnableCompression.</param>
   /// <param name="cancellationToken">Cancellation token.</param>
   Task SetAsync<T>(ICacheKey key, CacheEntry<T> entry, TimeSpan? expiration = null, string? version = null, bool? useCompression = null, CancellationToken cancellationToken = default);

   /// <summary>
   /// Removes a specific cache entry.
   /// </summary>
   /// <param name="key">The cache key.</param>
   /// <param name="version">Optional version prefix for the key.</param>
   /// <param name="cancellationToken">Cancellation token.</param>
   Task RemoveAsync(ICacheKey key, string? version = null, CancellationToken cancellationToken = default);

   /// <summary>
   /// Removes all cache entries for a specific feature using O(1) tag-based invalidation.
   /// </summary>
   /// <param name="feature">The feature name (e.g., "todos").</param>
   /// <param name="cancellationToken">Cancellation token.</param>
   Task RemoveByFeatureAsync(string feature, CancellationToken cancellationToken = default);
}
