namespace Infrastructure.Caching.Serializers;

/// <summary>
/// Interface for cache serialization strategies.
/// Allows pluggable serialization (JSON, MessagePack, etc.) and optional compression.
/// </summary>
public interface ICacheSerializer
{
    /// <summary>
    /// Gets the name of this serializer for diagnostics and logging.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Serializes an object to bytes.
    /// </summary>
    /// <typeparam name="T">The type to serialize.</typeparam>
    /// <param name="value">The value to serialize.</param>
    /// <param name="useCompression">Whether to compress the serialized data.</param>
    /// <param name="compressionThreshold">Minimum size in bytes before compression is applied.</param>
    /// <returns>The serialized bytes.</returns>
    byte[] Serialize<T>(T value, bool useCompression = false, int compressionThreshold = 1024);

    /// <summary>
    /// Serializes an object to bytes with detailed metrics.
    /// </summary>
    /// <typeparam name="T">The type to serialize.</typeparam>
    /// <param name="value">The value to serialize.</param>
    /// <param name="useCompression">Whether to compress the serialized data.</param>
    /// <param name="compressionThreshold">Minimum size in bytes before compression is applied.</param>
    /// <returns>The serialization result with metrics.</returns>
    SerializationResult SerializeWithMetrics<T>(T value, bool useCompression = false, int compressionThreshold = 1024);

    /// <summary>
    /// Deserializes bytes to an object.
    /// Automatically detects and decompresses if the data was compressed.
    /// </summary>
    /// <typeparam name="T">The type to deserialize to.</typeparam>
    /// <param name="bytes">The bytes to deserialize.</param>
    /// <returns>The deserialized object, or default if bytes are null/empty.</returns>
    T? Deserialize<T>(byte[]? bytes);
}
