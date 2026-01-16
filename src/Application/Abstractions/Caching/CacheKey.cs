namespace Application.Abstractions.Caching;

/// <summary>
/// Represents a cache key composed of a feature and a logical value.
/// Used for both caching queries and invalidating cache entries.
/// </summary>
/// <param name="Feature">Domain feature associated with the cache key (e.g., "todos").</param>
/// <param name="Value">Logical key used for caching (e.g., entity ID or pattern).</param>
public sealed record CacheKey(string Feature, string Value) : ICacheKey
{
    /// <summary>
    /// Returns a string representation of the cache key in the format "{Feature}:{Value}".
    /// </summary>
    public override string ToString() => $"{Feature}:{Value}";

    /// <summary>
    /// Creates a wildcard cache key for a given feature (Value = "*").
    /// </summary>
    public static CacheKey ForFeature(string feature) => new(feature, "*");

    /// <summary>
    /// Creates a new CacheKey with the same feature but a different value.
    /// </summary>
    /// <param name="value">New logical value for the cache key.</param>
    public CacheKey WithValue(string value) => this with { Value = value };
}
