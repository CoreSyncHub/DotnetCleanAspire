namespace Application.Abstractions.Caching;

/// <summary>
/// Marker interface for cacheable queries.
/// Implement this interface on queries that should be cached.
/// </summary>
public interface ICacheable
{
   /// <summary>
   /// Gets the cache key for this request.
   /// </summary>
   string CacheKey { get; }

   /// <summary>
   /// Gets the cache duration.
   /// </summary>
   TimeSpan CacheDuration { get; }
}
