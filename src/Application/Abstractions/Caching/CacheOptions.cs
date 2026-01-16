namespace Application.Abstractions.Caching;

/// <summary>
/// Configuration options for the caching system.
/// </summary>
/// <remarks>
/// Inject via IOptions&lt;CacheOptions&gt; using configuration section "Caching".
/// </remarks>
public sealed class CacheOptions
{
    /// <summary>
    /// Configuration section name in appsettings.json.
    /// </summary>
    public const string SectionName = "Caching";

    /// <summary>
    /// Gets or sets the global cache version.
    /// When changed, all cache entries with the old version become inaccessible.
    /// Default: "v1"
    /// </summary>
    public string GlobalVersion { get; set; } = "v1";

    /// <summary>
    /// Gets or sets feature-specific cache versions.
    /// Allows versioning per feature/domain area.
    /// Example: { "todos": "v2", "users": "v1" }
    /// </summary>
    public Dictionary<string, string> FeatureVersions { get; init; } = [];

    /// <summary>
    /// Gets or sets whether compression is enabled globally.
    /// Individual queries can override this via ICacheable.UseCompression.
    /// Default: false
    /// </summary>
    public bool EnableCompression { get; set; }

    /// <summary>
    /// Gets or sets the minimum size in bytes before compression is applied.
    /// Only used when compression is enabled.
    /// Default: 1024 bytes (1 KB)
    /// </summary>
    public int CompressionThresholdBytes { get; set; } = 1024;

    /// <summary>
    /// Gets or sets the default cache duration for cacheable queries.
    /// Individual queries can override this via ICacheable.CacheDuration.
    /// Default: 5 minutes
    /// </summary>
    public TimeSpan DefaultCacheDuration { get; set; } = TimeSpan.FromMinutes(5);
}
