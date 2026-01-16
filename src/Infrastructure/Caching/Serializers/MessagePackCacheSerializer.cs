using MessagePack;
using MessagePack.Resolvers;
using System.IO.Compression;

namespace Infrastructure.Caching.Serializers;

/// <summary>
/// MessagePack-based cache serializer with optional Brotli compression.
/// More performant than JSON but requires types to be MessagePack-compatible.
/// Use this for high-performance scenarios.
/// </summary>
internal sealed class MessagePackCacheSerializer : ICacheSerializer
{
    private static readonly MessagePackSerializerOptions Options = MessagePackSerializerOptions.Standard
        .WithResolver(ContractlessStandardResolver.Instance);

    // Compression marker: first byte is 0x01 for compressed data, 0x00 for uncompressed
    private const byte CompressionMarker = 0x01;
    private const byte NoCompressionMarker = 0x00;

    public string Name => "MessagePack";

    public byte[] Serialize<T>(T value, bool useCompression = false, int compressionThreshold = 1024)
    {
        byte[] messagePackBytes = MessagePackSerializer.Serialize(value, Options);

        // Check if compression should be applied
        if (useCompression && messagePackBytes.Length >= compressionThreshold)
        {
            return CompressWithMarker(messagePackBytes);
        }

        // Return uncompressed with marker
        return PrependMarker(messagePackBytes, NoCompressionMarker);
    }

    public SerializationResult SerializeWithMetrics<T>(T value, bool useCompression = false, int compressionThreshold = 1024)
    {
        byte[] messagePackBytes = MessagePackSerializer.Serialize(value, Options);
        long originalSize = messagePackBytes.Length;

        // Check if compression should be applied
        if (useCompression && messagePackBytes.Length >= compressionThreshold)
        {
            byte[] compressedBytes = CompressWithMarker(messagePackBytes);
            return new SerializationResult
            {
                Bytes = compressedBytes,
                OriginalSize = originalSize,
                FinalSize = compressedBytes.Length - 1, // -1 for compression marker
                IsCompressed = true
            };
        }

        // Return uncompressed with marker
        byte[] uncompressedBytes = PrependMarker(messagePackBytes, NoCompressionMarker);
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

        return MessagePackSerializer.Deserialize<T>(data, Options);
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
