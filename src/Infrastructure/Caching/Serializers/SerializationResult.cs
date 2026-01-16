namespace Infrastructure.Caching.Serializers;

/// <summary>
/// Result of a serialization operation, containing both the serialized bytes and metrics.
/// </summary>
public sealed class SerializationResult
{
    /// <summary>
    /// Gets the serialized bytes (including compression marker if applicable).
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1819:Properties should not return arrays", Justification = "Byte array is the native serialization format and is not modified after creation")]
    public required byte[] Bytes { get; init; }

    /// <summary>
    /// Gets the original size before compression (in bytes).
    /// If compression was not applied, this equals the final size minus the marker byte.
    /// </summary>
    public required long OriginalSize { get; init; }

    /// <summary>
    /// Gets the final size after compression (in bytes), excluding the compression marker.
    /// </summary>
    public required long FinalSize { get; init; }

    /// <summary>
    /// Gets whether compression was applied.
    /// </summary>
    public required bool IsCompressed { get; init; }

    /// <summary>
    /// Gets the compression ratio (original size / final size).
    /// Only meaningful if IsCompressed is true. Returns 1.0 if not compressed.
    /// </summary>
    public double CompressionRatio => IsCompressed && FinalSize > 0
        ? (double)OriginalSize / FinalSize
        : 1.0;
}
