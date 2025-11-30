using Microsoft.Extensions.Logging;

namespace Application.Abstractions.Behaviors;

/// <summary>
/// Pipeline behavior that caches query results.
/// Only applies to queries that implement <see cref="ICacheable"/>.
/// </summary>
/// <typeparam name="TResponse">The type of response.</typeparam>
public sealed class CachingBehavior<TResponse>(ICacheService cacheService, ILogger<CachingBehavior<TResponse>> logger) : IPipelineBehavior<TResponse>
{
   private readonly ICacheService _cacheService = cacheService;
   private readonly ILogger<CachingBehavior<TResponse>> _logger = logger;

   public async Task<TResponse> Handle(
       object request,
       RequestHandlerDelegate<TResponse> nextHandler,
       CancellationToken cancellationToken)
   {
      // Only cache if request implements ICacheable
      if (request is not ICacheable cacheable)
      {
         return await nextHandler();
      }

      string cacheKey = cacheable.CacheKey;

      // Try to get from cache
      TResponse? cachedValue = await _cacheService.GetAsync<TResponse>(cacheKey, cancellationToken);

      if (cachedValue is not null)
      {
         _logger.LogDebug("Cache hit for {CacheKey}", cacheKey);
         return cachedValue;
      }

      _logger.LogDebug("Cache miss for {CacheKey}", cacheKey);

      // Execute handler
      TResponse? response = await nextHandler();

      // Cache the result if successful
      if (response is Result<object> { IsSuccess: true })
      {
         await _cacheService.SetAsync(
             cacheKey,
             response,
             cacheable.CacheDuration,
             cancellationToken);

         _logger.LogDebug("Cached {CacheKey} for {Duration}", cacheKey, cacheable.CacheDuration);
      }

      return response;
   }
}
