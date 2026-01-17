namespace Application.Abstractions.Caching;

/// <summary>
/// Represents a cache key with a feature and value component.
/// </summary>
public interface ICacheKey
{
    /// <summary>
    /// Domain feature associated with the cache key (e.g., "todos").
    /// Used for feature-level invalidation via tag sets.
    /// </summary>
    string Feature { get; }

    /// <summary>
    /// Logical key used for caching (e.g., "42").
    /// Use feature-level invalidation for O(1) removal of multiple entries.
    /// </summary>
    string Value { get; }
}
