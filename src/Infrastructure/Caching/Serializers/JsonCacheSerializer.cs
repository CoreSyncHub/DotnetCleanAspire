using System.IO.Compression;
using System.Text.Json;

namespace Infrastructure.Caching.Serializers;

/// <summary>
/// JSON-based cache serializer with optional Brotli compression.
/// This is the default serializer, balancing compatibility and performance.
/// </summary>
internal sealed class JsonCacheSerializer : ICacheSerializer
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    // Compression marker: first byte is 0x01 for compressed data, 0x00 for uncompressed
    private const byte CompressionMarker = 0x01;
    private const byte NoCompressionMarker = 0x00;

    public string Name => "JSON";

    public byte[] Serialize<T>(T value, bool useCompression = false, int compressionThreshold = 1024)
    {
        byte[] jsonBytes = JsonSerializer.SerializeToUtf8Bytes(value, JsonOptions);

        // Check if compression should be applied
        if (useCompression && jsonBytes.Length >= compressionThreshold)
        {
            return CompressWithMarker(jsonBytes);
        }

        // Return uncompressed with marker
        return PrependMarker(jsonBytes, NoCompressionMarker);
    }

    public SerializationResult SerializeWithMetrics<T>(T value, bool useCompression = false, int compressionThreshold = 1024)
    {
        byte[] jsonBytes = JsonSerializer.SerializeToUtf8Bytes(value, JsonOptions);
        long originalSize = jsonBytes.Length;

        // Check if compression should be applied
        if (useCompression && jsonBytes.Length >= compressionThreshold)
        {
            byte[] compressedBytes = CompressWithMarker(jsonBytes);
            return new SerializationResult
            {
                Bytes = compressedBytes,
                OriginalSize = originalSize,
                FinalSize = compressedBytes.Length - 1, // -1 for compression marker
                IsCompressed = true
            };
        }

        // Return uncompressed with marker
        byte[] uncompressedBytes = PrependMarker(jsonBytes, NoCompressionMarker);
        return new SerializationResult
        {
            Bytes = uncompressedBytes,
            OriginalSize = originalSize,
            FinalSize = originalSize,
            IsCompressed = false
        };
    }

    public T? Deserialize<T>(byte[]? bytes)
    {
        if (bytes is null || bytes.Length is 0)
        {
            return default;
        }

        // Read compression marker
        byte marker = bytes[0];
        byte[] data = bytes[1..];

        if (marker is CompressionMarker)
        {
            // Decompress
            data = Decompress(data);
        }

        return JsonSerializer.Deserialize<T>(data, JsonOptions);
    }

    private static byte[] CompressWithMarker(byte[] data)
    {
        using var outputStream = new MemoryStream();

        // Write compression marker
        outputStream.WriteByte(CompressionMarker);

        // Compress data
        using (var brotliStream = new BrotliStream(outputStream, CompressionLevel.Fastest, leaveOpen: true))
        {
            brotliStream.Write(data);
        }

        return outputStream.ToArray();
    }

    private static byte[] Decompress(byte[] compressedData)
    {
        using var inputStream = new MemoryStream(compressedData);
        using var brotliStream = new BrotliStream(inputStream, CompressionMode.Decompress);
        using var outputStream = new MemoryStream();

        brotliStream.CopyTo(outputStream);
        return outputStream.ToArray();
    }

    private static byte[] PrependMarker(byte[] data, byte marker)
    {
        byte[] result = new byte[data.Length + 1];
        result[0] = marker;
        Array.Copy(data, 0, result, 1, data.Length);
        return result;
    }
}
