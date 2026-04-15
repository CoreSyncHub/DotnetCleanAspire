namespace Application.IntegrationTests.Fakes;

/// <summary>
/// No-op implementation of ICacheService for integration tests.
/// Allows CachingBehavior and CacheInvalidationBehavior to run in the pipeline
/// without any actual Redis operations.
/// </summary>
internal sealed class NullCacheService : ICacheService
{
    public Task<CacheEntry<T>?> GetAsync<T>(
        ICacheKey key,
        string? version = null,
        CancellationToken cancellationToken = default)
        => Task.FromResult<CacheEntry<T>?>(null);

    public Task SetAsync<T>(
        ICacheKey key,
        CacheEntry<T> entry,
        TimeSpan? expiration = null,
        string? version = null,
        bool? useCompression = null,
        CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task RemoveAsync(
        ICacheKey key,
        string? version = null,
        CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task RemoveByFeatureAsync(
        string feature,
        CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}
