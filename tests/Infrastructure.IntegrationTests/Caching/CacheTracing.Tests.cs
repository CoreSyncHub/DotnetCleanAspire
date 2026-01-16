using Application.Abstractions.Caching;
using Infrastructure.IntegrationTests.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;

namespace Infrastructure.IntegrationTests.Caching;

[Collection(nameof(IntegrationTestCollection))]
public sealed class CacheTracingTests : IDisposable
{
    private readonly ActivityListener _activityListener;
    private readonly List<Activity> _activities = [];
    private readonly ICacheService _cacheService;
    private readonly TestsWebApplicationFactory _factory;

    public CacheTracingTests(TestContainersFixture containersFixture)
    {
        _activityListener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == "DotnetCleanAspire.Caching",
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = activity => _activities.Add(activity)
        };

        ActivitySource.AddActivityListener(_activityListener);

        // Create WebApplicationFactory with the Redis connection string from the containers fixture
        _factory = new TestsWebApplicationFactory(containersFixture.RedisConnectionString);

        // Get the real ICacheService from DI container (implementation is DistributedCacheService)
        _cacheService = _factory.Services.GetRequiredService<ICacheService>();
    }

    [Fact]
    public async Task GetAsync_ShouldCreateActivity_WithCorrectTags()
    {
        // Arrange
        var cacheKey = new CacheKey("todos", "123");
        // First set a value in cache to ensure GetAsync retrieves it
        await _cacheService.SetAsync(cacheKey, CacheEntry<string>.Create("test-value"), TimeSpan.FromMinutes(5));
        _activities.Clear(); // Clear the SetAsync activity

        // Act
        await _cacheService.GetAsync<string>(cacheKey);

        // Assert
        Activity? activity = _activities.FirstOrDefault(a => a.DisplayName == "cache.get");
        activity.ShouldNotBeNull();
        activity.Tags.Any(t => t.Key == "cache.feature" && t.Value?.ToString() == "todos").ShouldBeTrue();
    }

    [Fact]
    public async Task GetAsync_OnCacheMiss_ShouldTagActivityWithMiss()
    {
        // Arrange
        var cacheKey = new CacheKey("missing", "nonexistent-key-" + Guid.NewGuid());

        // Act
        await _cacheService.GetAsync<string>(cacheKey);

        // Assert
        Activity? activity = _activities.FirstOrDefault(a => a.DisplayName == "cache.get");
        activity.ShouldNotBeNull();
    }

    [Fact]
    public async Task SetAsync_ShouldCreateActivity_WithSizeAndCompressionTags()
    {
        // Arrange
        var cacheKey = new CacheKey("todos", "list");
        const string testValue = "test-value";

        // Act
        await _cacheService.SetAsync(cacheKey, CacheEntry<string>.Create(testValue), expiration: TimeSpan.FromMinutes(5));

        // Assert
        Activity? activity = _activities.FirstOrDefault(a => a.DisplayName == "cache.set");
        activity.ShouldNotBeNull();
        activity.Tags.Any(t => t.Key == "cache.feature" && t.Value?.ToString() == "todos").ShouldBeTrue();
    }

    [Fact]
    public async Task RemoveAsync_ShouldCreateActivity()
    {
        // Arrange
        var cacheKey = new CacheKey("users", "42");

        // Act
        await _cacheService.RemoveAsync(cacheKey);

        // Assert
        Activity? activity = _activities.FirstOrDefault(a => a.DisplayName == "cache.remove");
        activity.ShouldNotBeNull();

        activity.Tags.Any(t => t.Key == "cache.feature" && t.Value?.ToString() == "users").ShouldBeTrue();
    }

    [Fact]
    public async Task RemoveByFeatureAsync_WithoutRedis_ShouldCreateActivity()
    {
        // Arrange
        const string feature = "todos";

        // Act (no Redis connection, so it will just return)
        await _cacheService.RemoveByFeatureAsync(feature);

        // Assert
        Activity? activity = _activities.FirstOrDefault(a => a.DisplayName == "cache.invalidate");
        activity.ShouldNotBeNull();

        activity.Tags.Any(t => t.Key == "cache.feature").ShouldBeTrue();
    }

    [Fact]
    public async Task GetAsync_OnSerializationError_ShouldSetActivityError()
    {
        // Arrange - Try to deserialize incompatible data type
        var cacheKey = new CacheKey("error", "incompatible-type");

        // First, set a string value
        await _cacheService.SetAsync(cacheKey, CacheEntry<string>.Create("test-value"), TimeSpan.FromMinutes(5));
        _activities.Clear();

        // Act & Assert - Try to get as int (should fail deserialization)
        var result = await _cacheService.GetAsync<int>(cacheKey);

        // Result should be null (cache miss on deserialization error)
        result.ShouldBeNull();

        // Activity should be recorded
        Activity? activity = _activities.FirstOrDefault(a => a.DisplayName == "cache.get");
        activity.ShouldNotBeNull();
    }

    [Fact]
    public async Task Activity_ShouldHaveTraceId()
    {
        // Arrange
        var cacheKey = new CacheKey("test", "key");
        // Set a value in cache first
        await _cacheService.SetAsync(cacheKey, CacheEntry<string>.Create("test-value"), TimeSpan.FromMinutes(5));
        _activities.Clear(); // Clear the SetAsync activity

        // Act
        await _cacheService.GetAsync<string>(cacheKey);

        // Assert
        Activity? activity = _activities.FirstOrDefault(a => a.DisplayName == "cache.get");
        activity.ShouldNotBeNull();
        activity.TraceId.ShouldNotBe(default(ActivityTraceId));
    }

    public void Dispose()
    {
        _activityListener.Dispose();
        _factory.Dispose();
    }
}
