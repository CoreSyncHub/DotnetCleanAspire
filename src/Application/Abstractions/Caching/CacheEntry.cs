using System.Text.Json.Serialization;

namespace Application.Abstractions.Caching;

/// <summary>
/// Wrapper class for cached values to properly handle struct types (like Result&lt;T&gt;).
/// Distinguishes between null/default values and actual cache misses.
/// </summary>
/// <typeparam name="T">The type of value being cached.</typeparam>
public sealed class CacheEntry<T>
{
    /// <summary>
    /// Gets the cached value.
    /// </summary>
    [JsonInclude]
    public T? Value { get; init; }

    /// <summary>
    /// Gets a value indicating whether this entry contains a valid cached value.
    /// </summary>
    [JsonInclude]
    public bool HasValue { get; init; }

    /// <summary>
    /// Creates a cache entry with a value.
    /// </summary>
    public static CacheEntry<T> Create(T value) => new()
    {
        Value = value,
        HasValue = true
    };

    /// <summary>
    /// Creates an empty cache entry (cache miss).
    /// </summary>
    public static CacheEntry<T> Empty() => new()
    {
        Value = default,
        HasValue = false
    };
}
