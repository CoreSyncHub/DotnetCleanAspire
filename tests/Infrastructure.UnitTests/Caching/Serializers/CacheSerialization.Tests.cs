using Application.Abstractions.Caching;
using Infrastructure.Caching.Serializers;

namespace Infrastructure.UnitTests.Caching.Serializers;

public class CacheSerializationTests
{
    [Fact]
    public void JsonSerializer_SerializeDeserialize_WithoutCompression_ShouldWorkCorrectly()
    {
        // Arrange
        var serializer = new JsonCacheSerializer();
        var testData = new TestData { Id = 42, Name = "Test", Values = [1, 2, 3] };

        // Act
        byte[] serialized = serializer.Serialize(testData, useCompression: false);
        TestData? deserialized = serializer.Deserialize<TestData>(serialized);

        // Assert
        deserialized.ShouldNotBeNull();
        deserialized.Id.ShouldBe(42);
        deserialized.Name.ShouldBe("Test");
        deserialized.Values.ShouldBe([1, 2, 3]);
    }

    [Fact]
    public void JsonSerializer_SerializeDeserialize_WithCompression_ShouldWorkCorrectly()
    {
        // Arrange
        var serializer = new JsonCacheSerializer();
        // Create data larger than compression threshold
        var testData = new TestData
        {
            Id = 123,
            Name = new string('A', 2000), // 2KB of 'A's
            Values = [.. Enumerable.Range(1, 1000)]
        };

        // Act
        byte[] serialized = serializer.Serialize(testData, useCompression: true, compressionThreshold: 1024);
        TestData? deserialized = serializer.Deserialize<TestData>(serialized);

        // Assert
        deserialized.ShouldNotBeNull();
        deserialized.Id.ShouldBe(123);
        deserialized.Name.Length.ShouldBe(2000);
        deserialized.Values.Count.ShouldBe(1000);
    }

    [Fact]
    public void JsonSerializer_Compression_ShouldReduceSize()
    {
        // Arrange
        var serializer = new JsonCacheSerializer();
        // Highly compressible data (repetitive)
        var testData = new TestData
        {
            Id = 1,
            Name = new string('X', 5000), // Very compressible
            Values = [.. Enumerable.Repeat(42, 1000)]
        };

        // Act
        byte[] uncompressed = serializer.Serialize(testData, useCompression: false);
        byte[] compressed = serializer.Serialize(testData, useCompression: true, compressionThreshold: 100);

        // Assert
        compressed.Length.ShouldBeLessThan(uncompressed.Length);

        // Verify both deserialize to the same data
        TestData? uncompressedData = serializer.Deserialize<TestData>(uncompressed);
        TestData? compressedData = serializer.Deserialize<TestData>(compressed);

        uncompressedData.ShouldNotBeNull();
        compressedData.ShouldNotBeNull();
        compressedData.Id.ShouldBe(uncompressedData.Id);
        compressedData.Name.ShouldBe(uncompressedData.Name);
    }

    [Fact]
    public void JsonSerializer_SmallData_ShouldNotCompress()
    {
        // Arrange
        var serializer = new JsonCacheSerializer();
        var testData = new TestData { Id = 1, Name = "Small", Values = [1, 2] };

        // Act
        byte[] result = serializer.Serialize(testData, useCompression: true, compressionThreshold: 1024);

        // Assert - first byte should be 0x00 (no compression marker)
        result[0].ShouldBe((byte)0x00);
    }

    [Fact]
    public void MessagePackSerializer_SerializeDeserialize_WithoutCompression_ShouldWorkCorrectly()
    {
        // Arrange
        var serializer = new MessagePackCacheSerializer();
        var testData = new TestData { Id = 99, Name = "MessagePack", Values = [5, 10, 15] };

        // Act
        byte[] serialized = serializer.Serialize(testData, useCompression: false);
        TestData? deserialized = serializer.Deserialize<TestData>(serialized);

        // Assert
        deserialized.ShouldNotBeNull();
        deserialized.Id.ShouldBe(99);
        deserialized.Name.ShouldBe("MessagePack");
        deserialized.Values.ShouldBe([5, 10, 15]);
    }

    [Fact]
    public void MessagePackSerializer_SerializeDeserialize_WithCompression_ShouldWorkCorrectly()
    {
        // Arrange
        var serializer = new MessagePackCacheSerializer();
        var testData = new TestData
        {
            Id = 456,
            Name = new string('B', 3000),
            Values = [.. Enumerable.Range(100, 500)]
        };

        // Act
        byte[] serialized = serializer.Serialize(testData, useCompression: true, compressionThreshold: 1024);
        TestData? deserialized = serializer.Deserialize<TestData>(serialized);

        // Assert
        deserialized.ShouldNotBeNull();
        deserialized.Id.ShouldBe(456);
        deserialized.Name.Length.ShouldBe(3000);
        deserialized.Values.Count.ShouldBe(500);
    }

    [Fact]
    public void MessagePackSerializer_ShouldBeMoreCompact_ThanJson()
    {
        // Arrange
        var jsonSerializer = new JsonCacheSerializer();
        var msgPackSerializer = new MessagePackCacheSerializer();
        var testData = new TestData
        {
            Id = 1,
            Name = "Comparison Test",
            Values = [.. Enumerable.Range(1, 100)]
        };

        // Act
        byte[] jsonBytes = jsonSerializer.Serialize(testData, useCompression: false);
        byte[] msgPackBytes = msgPackSerializer.Serialize(testData, useCompression: false);

        // Assert - MessagePack should typically be more compact than JSON
        msgPackBytes.Length.ShouldBeLessThan(jsonBytes.Length);
    }

    [Fact]
    public void Serializer_DeserializeNull_ShouldReturnDefault()
    {
        // Arrange
        var serializer = new JsonCacheSerializer();

        // Act
        TestData? result = serializer.Deserialize<TestData>(null);

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public void Serializer_DeserializeEmpty_ShouldReturnDefault()
    {
        // Arrange
        var serializer = new JsonCacheSerializer();

        // Act
        TestData? result = serializer.Deserialize<TestData>([]);

        // Assert
        result.ShouldBeNull();
    }

    // Test helper class
    public sealed class TestData
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public IReadOnlyCollection<int> Values { get; set; } = [];
    }
}
