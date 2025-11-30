namespace Application.Abstractions.Caching;

/// <summary>
/// Abstraction for cache operations.
/// Implementation should use IDistributedCache for Redis compatibility.
/// </summary>
public interface ICacheService
{
   /// <summary>
   /// Gets a cached value by key.
   /// </summary>
   /// <typeparam name="T">The type of the cached value.</typeparam>
   /// <param name="key">The cache key.</param>
   /// <param name="cancellationToken">Cancellation token.</param>
   /// <returns>The cached value, or null if not found.</returns>
   Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default);

   /// <summary>
   /// Sets a value in the cache.
   /// </summary>
   /// <typeparam name="T">The type of the value.</typeparam>
   /// <param name="key">The cache key.</param>
   /// <param name="value">The value to cache.</param>
   /// <param name="expiration">The expiration time.</param>
   /// <param name="cancellationToken">Cancellation token.</param>
   Task SetAsync<T>(string key, T value, TimeSpan expiration, CancellationToken cancellationToken = default);

   /// <summary>
   /// Removes a value from the cache.
   /// </summary>
   /// <param name="key">The cache key.</param>
   /// <param name="cancellationToken">Cancellation token.</param>
   Task RemoveAsync(string key, CancellationToken cancellationToken = default);

   /// <summary>
   /// Removes all values matching a pattern from the cache.
   /// </summary>
   /// <param name="pattern">The key pattern (e.g., "todos:*").</param>
   /// <param name="cancellationToken">Cancellation token.</param>
   Task RemoveByPatternAsync(string pattern, CancellationToken cancellationToken = default);
}
