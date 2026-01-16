namespace Application.Features.Todos.Cache;

/// <summary>
/// Centralized cache key builder for Todo feature.
/// Prevents magic strings and typos in cache key construction.
///
/// Supported invalidation strategies:
/// 1. Exact keys: Use KeysToInvalidate with ById() or List() to invalidate specific entries
/// 2. Feature-level: Use FeaturesToInvalidate with FeatureTag to invalidate all todo cache entries
///
/// Note: Pattern-based invalidation (wildcards) is not supported for performance and complexity reasons.
/// </summary>
public static class TodoCacheKeys
{
    /// <summary>
    /// Feature prefix for all todo-related cache keys.
    /// </summary>
    private const string Feature = "todos";

    #region Exact Keys

    /// <summary>
    /// Cache key for a specific todo by ID.
    /// </summary>
    /// <param name="id">The todo ID.</param>
    /// <returns>Cache key in format "todos:{id}"</returns>
    public static ICacheKey ById(Id id) => new CacheKey(Feature, id.ToString());

    /// <summary>
    /// Cache key for todo list queries (can be extended with filters).
    /// </summary>
    /// <returns>Cache key "todos:list"</returns>
    public static ICacheKey List() => new CacheKey(Feature, "list");

    #endregion

    #region Feature-level invalidation

    /// <summary>
    /// Feature tag for invalidating all todo cache entries.
    /// Use this in FeaturesToInvalidate to clear all todos-related cache.
    /// Uses O(1) tag-based invalidation via Redis Sets.
    /// </summary>
    /// <example>
    /// FeaturesToInvalidate => [TodoCacheKeys.FeatureTag]
    /// </example>
    public static string FeatureTag => Feature;

    #endregion

}
