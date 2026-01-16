using Infrastructure.Caching.Observability;
using System.Diagnostics.Metrics;

namespace Infrastructure.UnitTests.Caching.Observability;

public sealed class CacheMetricsTests : IDisposable
{
    private readonly MeterListener _meterListener;
    private readonly CacheMetrics _metrics;
    private readonly List<MeasurementSnapshot<long>> _longMeasurements = [];
    private readonly List<MeasurementSnapshot<double>> _doubleMeasurements = [];

    public CacheMetricsTests()
    {
        var meterFactory = new TestMeterFactory();
        _metrics = new CacheMetrics(meterFactory);

        _meterListener = new MeterListener
        {
            InstrumentPublished = (instrument, listener) =>
            {
                if (instrument.Meter.Name == "DotnetCleanAspire.Caching")
                {
                    listener.EnableMeasurementEvents(instrument);
                }
            }
        };

        _meterListener.SetMeasurementEventCallback<long>((instrument, measurement, tags, state) =>
        {
            _longMeasurements.Add(new MeasurementSnapshot<long>(measurement, tags.ToArray()));
        });

        _meterListener.SetMeasurementEventCallback<double>((instrument, measurement, tags, state) =>
        {
            _doubleMeasurements.Add(new MeasurementSnapshot<double>(measurement, tags.ToArray()));
        });

        _meterListener.Start();
    }

    [Fact]
    public void RecordHit_ShouldIncrementCacheHitsCounter()
    {
        // Arrange
        const string feature = "todos";

        // Act
        _metrics.RecordHit(feature);

        // Assert
        var measurement = _longMeasurements.FirstOrDefault(m => m.Value == 1);
        measurement.ShouldNotBeNull();
        measurement.Tags.Any(t => t.Key == "cache.key" && t.Value?.ToString() == "todos").ShouldBeTrue();
    }

    [Fact]
    public void RecordMiss_ShouldIncrementCacheMissesCounter()
    {
        // Arrange
        const string feature = "users";

        // Act
        _metrics.RecordMiss(feature);

        // Assert
        var measurement = _longMeasurements.FirstOrDefault(m => m.Value == 1);
        measurement.ShouldNotBeNull();
        measurement.Tags.Any(t => t.Key == "cache.key" && t.Value?.ToString() == "users").ShouldBeTrue();
    }

    [Fact]
    public void RecordInvalidation_ShouldIncrementInvalidationsCounter()
    {
        // Arrange
        const string pattern = "todos:*";
        const int count = 5;

        // Act
        _metrics.RecordInvalidation(pattern, count);

        // Assert
        var measurement = _longMeasurements.FirstOrDefault(m => m.Value == count);
        measurement.ShouldNotBeNull();
        measurement.Tags.Any(t => t.Key == "cache.pattern" && t.Value?.ToString() == pattern).ShouldBeTrue();
    }

    [Fact]
    public void RecordOperationDuration_ShouldRecordHistogram()
    {
        // Arrange
        const string operationType = "get";
        const double durationMs = 42.5;

        // Act
        _metrics.RecordOperationDuration(operationType, durationMs, success: true);

        // Assert
        var measurement = _doubleMeasurements.FirstOrDefault(m => m.Value == durationMs);
        measurement.ShouldNotBeNull();
        measurement.Tags.Any(t => t.Key == "cache.operation" && t.Value?.ToString() == operationType).ShouldBeTrue();
        measurement.Tags.Any(t => t.Key == "cache.success" && t.Value is true).ShouldBeTrue();
    }

    [Fact]
    public void RecordOperationDuration_WithFailure_ShouldRecordWithFailureTag()
    {
        // Arrange
        const string operationType = "set";
        const double durationMs = 100.0;

        // Act
        _metrics.RecordOperationDuration(operationType, durationMs, success: false);

        // Assert
        var measurement = _doubleMeasurements.FirstOrDefault(m => m.Value == durationMs);
        measurement.ShouldNotBeNull();
        measurement.Tags.Any(t => t.Key == "cache.success" && t.Value is false).ShouldBeTrue();
    }

    [Fact]
    public void RecordEntrySize_ShouldRecordHistogram()
    {
        // Arrange
        const long sizeBytes = 1024;

        // Act
        _metrics.RecordEntrySize(sizeBytes, compressed: false);

        // Assert
        var measurement = _longMeasurements.FirstOrDefault(m => m.Value == sizeBytes);
        measurement.ShouldNotBeNull();
        measurement.Tags.Any(t => t.Key == "cache.compressed" && t.Value is false).ShouldBeTrue();
    }

    [Fact]
    public void RecordEntrySize_WithCompression_ShouldRecordWithCompressionTag()
    {
        // Arrange
        const long sizeBytes = 512;

        // Act
        _metrics.RecordEntrySize(sizeBytes, compressed: true);

        // Assert
        var measurement = _longMeasurements.FirstOrDefault(m => m.Value == sizeBytes);
        measurement.ShouldNotBeNull();
        measurement.Tags.Any(t => t.Key == "cache.compressed" && t.Value is true).ShouldBeTrue();
    }

    [Fact]
    public void RecordHit_WithFeatureName_ShouldTagCorrectly()
    {
        // Arrange
        const string feature = "products";

        // Act
        _metrics.RecordHit(feature);

        // Assert
        var measurement = _longMeasurements.FirstOrDefault(m => m.Value == 1);
        measurement.ShouldNotBeNull();
        measurement.Tags.Any(t => t.Key == "cache.key" && t.Value?.ToString() == "products").ShouldBeTrue();
    }

    [Fact]
    public void MultipleRecords_ShouldAccumulateMetrics()
    {
        // Arrange & Act
        _metrics.RecordHit("todos");
        _metrics.RecordHit("todos");
        _metrics.RecordMiss("users");
        _metrics.RecordInvalidation("todos:*", 3);

        // Assert
        _longMeasurements.Count.ShouldBeGreaterThan(0);
    }

    public void Dispose()
    {
        _meterListener.Dispose();
        _metrics.Dispose();
    }

    // Helper class to snapshot measurements
    private sealed record MeasurementSnapshot<T>(T Value, KeyValuePair<string, object?>[] Tags) where T : struct;

    // Test helper class
    private sealed class TestMeterFactory : IMeterFactory
    {
        public Meter Create(MeterOptions options)
        {
            return new Meter(options);
        }

        public void Dispose()
        {
        }
    }
}
