using System.Diagnostics.Metrics;

namespace Infrastructure.Caching.Observability;

/// <summary>
/// Provides OpenTelemetry metrics for cache operations.
/// </summary>
internal sealed class CacheMetrics : IDisposable
{
    private const string MeterName = "DotnetCleanAspire.Caching";
    private readonly Meter _meter;

    // Counters
    private readonly Counter<long> _cacheHits;
    private readonly Counter<long> _cacheMisses;
    private readonly Counter<long> _cacheInvalidations;

    // Histograms
    private readonly Histogram<double> _operationDuration;
    private readonly Histogram<long> _entrySize;
    private readonly Histogram<double> _serializationDuration;
    private readonly Histogram<double> _compressionRatio;

    public CacheMetrics(IMeterFactory meterFactory)
    {
        _meter = meterFactory.Create(MeterName);

        // Initialize counters
        _cacheHits = _meter.CreateCounter<long>(
            name: "cache.hits",
            unit: "{operations}",
            description: "Total number of cache hits");

        _cacheMisses = _meter.CreateCounter<long>(
            name: "cache.misses",
            unit: "{operations}",
            description: "Total number of cache misses");

        _cacheInvalidations = _meter.CreateCounter<long>(
            name: "cache.invalidations",
            unit: "{operations}",
            description: "Total number of cache invalidations");

        // Initialize histograms
        _operationDuration = _meter.CreateHistogram<double>(
            name: "cache.operation.duration",
            unit: "ms",
            description: "Duration of cache operations in milliseconds");

        _entrySize = _meter.CreateHistogram<long>(
            name: "cache.entry.size",
            unit: "bytes",
            description: "Size of cached entries in bytes");

        _serializationDuration = _meter.CreateHistogram<double>(
            name: "cache.serialization.duration",
            unit: "ms",
            description: "Duration of serialization/deserialization operations in milliseconds");

        _compressionRatio = _meter.CreateHistogram<double>(
            name: "cache.compression.ratio",
            unit: "{ratio}",
            description: "Compression ratio (original size / compressed size)");
    }

    /// <summary>
    /// Records a cache hit.
    /// </summary>
    /// <param name="feature">The feature name (e.g., "todos").</param>
    public void RecordHit(string feature)
    {
        _cacheHits.Add(1, new KeyValuePair<string, object?>("cache.key", feature));
    }

    /// <summary>
    /// Records a cache miss.
    /// </summary>
    /// <param name="feature">The feature name (e.g., "todos").</param>
    public void RecordMiss(string feature)
    {
        _cacheMisses.Add(1, new KeyValuePair<string, object?>("cache.key", feature));
    }

    /// <summary>
    /// Records a cache invalidation.
    /// </summary>
    /// <param name="pattern">The invalidation pattern used.</param>
    /// <param name="count">The number of entries invalidated.</param>
    public void RecordInvalidation(string pattern, int count = 1)
    {
        _cacheInvalidations.Add(count, new KeyValuePair<string, object?>("cache.pattern", pattern));
    }

    /// <summary>
    /// Records the duration of a cache operation.
    /// </summary>
    /// <param name="operationType">The type of operation (get, set, remove).</param>
    /// <param name="durationMs">The duration in milliseconds.</param>
    /// <param name="success">Whether the operation succeeded.</param>
    public void RecordOperationDuration(string operationType, double durationMs, bool success = true)
    {
        _operationDuration.Record(durationMs,
            new KeyValuePair<string, object?>("cache.operation", operationType),
            new KeyValuePair<string, object?>("cache.success", success));
    }

    /// <summary>
    /// Records the size of a cached entry.
    /// </summary>
    /// <param name="sizeBytes">The size in bytes.</param>
    /// <param name="compressed">Whether the entry is compressed.</param>
    public void RecordEntrySize(long sizeBytes, bool compressed = false)
    {
        _entrySize.Record(sizeBytes,
            new KeyValuePair<string, object?>("cache.compressed", compressed));
    }

    /// <summary>
    /// Records the duration of a serialization or deserialization operation.
    /// </summary>
    /// <param name="operationType">The type of operation ("serialize" or "deserialize").</param>
    /// <param name="durationMs">The duration in milliseconds.</param>
    /// <param name="serializerName">The name of the serializer used (e.g., "json", "messagepack").</param>
    public void RecordSerializationDuration(string operationType, double durationMs, string serializerName)
    {
        _serializationDuration.Record(durationMs,
            new KeyValuePair<string, object?>("cache.serialization.operation", operationType),
            new KeyValuePair<string, object?>("cache.serializer", serializerName));
    }

    /// <summary>
    /// Records the compression ratio achieved during serialization.
    /// </summary>
    /// <param name="originalSizeBytes">The original uncompressed size in bytes.</param>
    /// <param name="compressedSizeBytes">The compressed size in bytes.</param>
    /// <param name="serializerName">The name of the serializer used.</param>
    public void RecordCompressionRatio(long originalSizeBytes, long compressedSizeBytes, string serializerName)
    {
        if (originalSizeBytes <= 0) return;

        // Calculate compression ratio (higher is better)
        // Example: 1000 bytes -> 250 bytes = 4.0 ratio (75% reduction)
        double ratio = (double)originalSizeBytes / compressedSizeBytes;

        _compressionRatio.Record(ratio,
            new KeyValuePair<string, object?>("cache.serializer", serializerName));
    }

    public void Dispose()
    {
        _meter.Dispose();
    }
}
