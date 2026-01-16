namespace Application.Abstractions.Caching;

/// <summary>
/// Marker interface for commands that should invalidate cache entries.
/// Implement this interface on commands whose execution should trigger cache removal.
/// </summary>
public interface ICacheInvalidating
{
    /// <summary>
    /// Gets specific cache keys to invalidate after successful command execution.
    /// These are individual entries that will be removed.
    /// </summary>
    /// <remarks>
    /// Pattern-based invalidation (wildcards) is not supported for performance and complexity reasons.
    /// Use feature-level invalidation for broader cache clears.
    /// </remarks>
    IReadOnlyCollection<ICacheKey>? KeysToInvalidate => null;

    /// <summary>
    /// Gets feature names to invalidate entirely (all keys in that feature).
    /// Uses O(1) tag-based invalidation to remove all entries for each feature.
    /// Example: ["todos", "users"] will invalidate all todo and user cache entries.
    /// </summary>
    IReadOnlyCollection<string>? FeaturesToInvalidate => null;
}
