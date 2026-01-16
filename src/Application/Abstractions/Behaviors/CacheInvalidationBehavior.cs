using Microsoft.Extensions.Logging;

namespace Application.Abstractions.Behaviors;

/// <summary>
/// Pipeline behavior that invalidates cache entries after successful command execution.
/// Only applies to commands that implement <see cref="ICacheInvalidating"/>.
/// </summary>
/// <typeparam name="TResponse">The type of response.</typeparam>
public sealed class CacheInvalidationBehavior<TResponse>(
    ICacheService cacheService,
    ICacheVersionResolver versionResolver,
    ILogger<CacheInvalidationBehavior<TResponse>> logger) : IPipelineBehavior<TResponse>
{
    private readonly ICacheService _cacheService = cacheService;
    private readonly ICacheVersionResolver _versionResolver = versionResolver;
    private readonly ILogger<CacheInvalidationBehavior<TResponse>> _logger = logger;

    public async Task<TResponse> Handle(
        object request,
        RequestHandlerDelegate<TResponse> nextHandler,
        CancellationToken cancellationToken)
    {
        // Execute the handler first
        TResponse response = await nextHandler();

        // Only invalidate if request implements ICacheInvalidating
        if (request is not ICacheInvalidating invalidating)
        {
            return response;
        }

        // Only invalidate if the response indicates success
        bool isSuccess = response is IResult { IsSuccess: true };
        if (!isSuccess)
        {
            _logger.LogDebug("Skipping cache invalidation due to failed command");
            return response;
        }

        // Invalidate specific cache keys
        if (invalidating.KeysToInvalidate is not null)
        {
            foreach (ICacheKey key in invalidating.KeysToInvalidate)
            {
                try
                {
                    string version = _versionResolver.Resolve(key);
                    await _cacheService.RemoveAsync(key, version, cancellationToken);
                    _logger.LogDebug("Invalidated cache key - Feature: {Feature}, Value: {Value}, Version: {Version}", key.Feature, key.Value, version);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to invalidate cache key - Feature: {Feature}, Value: {Value}", key.Feature, key.Value);
                }
            }
        }

        // Invalidate entire features (all keys in the feature)
        if (invalidating.FeaturesToInvalidate is not null)
        {
            foreach (string feature in invalidating.FeaturesToInvalidate)
            {
                try
                {
                    await _cacheService.RemoveByFeatureAsync(feature, cancellationToken);
                    _logger.LogDebug("Invalidated entire feature: {Feature}", feature);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to invalidate feature: {Feature}", feature);
                }
            }
        }

        return response;
    }
}
