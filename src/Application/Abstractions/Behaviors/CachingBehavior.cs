using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace Application.Abstractions.Behaviors;

/// <summary>
/// Pipeline behavior that caches query results.
/// Only applies to queries that implement <see cref="ICacheable"/>.
/// </summary>
/// <typeparam name="TResponse">The type of response.</typeparam>
public sealed class CachingBehavior<TResponse>(
    ICacheService cacheService,
    ICacheVersionResolver versionResolver,
    IOptions<CacheOptions> cacheOptions,
    ILogger<CachingBehavior<TResponse>> logger) : IPipelineBehavior<TResponse>
{
   private readonly ICacheService _cacheService = cacheService;
   private readonly ICacheVersionResolver _versionResolver = versionResolver;
   private readonly CacheOptions _cacheOptions = cacheOptions.Value;
   private readonly ILogger<CachingBehavior<TResponse>> _logger = logger;

   // Cache stampede protection: one semaphore per cache key
   // Note: We don't cleanup locks because:
   // 1. SemaphoreSlim.CurrentCount is not thread-safe for cleanup decisions (race conditions)
   // 2. Number of unique keys is typically bounded by application traffic patterns
   // 3. SemaphoreSlim is ~100 bytes - negligible memory footprint compared to cached data
   // 4. Alternative approaches (WeakReference, conditional removal) introduce real complexity and potential bugs
   private static readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new();

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

      string version = _versionResolver.Resolve(cacheable);
      string lockKey = BuildLockKey(cacheable.CacheKey, version);

      // Try to get from cache
      CacheEntry<TResponse>? cachedEntry = await _cacheService.GetAsync<TResponse>(
          cacheable.CacheKey,
          version,
          cancellationToken
      );

      // Check for cache hit
      if (cachedEntry?.HasValue is true)
      {
         _logger.LogDebug("Cache hit for feature {Feature}. TraceId: {TraceId}", cacheable.CacheKey.Feature, Activity.Current?.TraceId);
         return cachedEntry.Value!;
      }

      _logger.LogDebug("Cache miss for feature {Feature}. TraceId: {TraceId}", cacheable.CacheKey.Feature, Activity.Current?.TraceId);

      // Cache stampede protection
      SemaphoreSlim keyLock = _locks.GetOrAdd(lockKey, _ => new SemaphoreSlim(1, 1));

      await keyLock.WaitAsync(cancellationToken);
      try
      {
         // Double-check: another thread might have populated the cache while we waited
         cachedEntry = await _cacheService.GetAsync<TResponse>(cacheable.CacheKey, version, cancellationToken);

         if (cachedEntry?.HasValue is true)
         {
            _logger.LogDebug("Cache hit after lock acquisition for feature {Feature}. TraceId: {TraceId}",
            cacheable.CacheKey.Feature,
            Activity.Current?.TraceId);

            return cachedEntry.Value!;
         }

         // Execute handler (only one thread per key will reach here)
         TResponse? response = await nextHandler();

         // Cache the result if successful (using IResult interface for type-safe detection)
         bool shouldCache = response is IResult { IsSuccess: true };

         if (shouldCache)
         {
            // Resolve configuration: use query-specific values or fallback to global config
            TimeSpan duration = cacheable.CacheDuration ?? _cacheOptions.DefaultCacheDuration;
            bool useCompression = cacheable.UseCompression ?? _cacheOptions.EnableCompression;

            await _cacheService.SetAsync(
                cacheable.CacheKey,
                CacheEntry<TResponse>.Create(response),
                duration,
                version,
                useCompression,
                cancellationToken);

            _logger.LogDebug("Cached feature {Feature} for {Duration} (Compression: {UseCompression}). TraceId: {TraceId}",
                cacheable.CacheKey.Feature,
                duration,
                useCompression,
                Activity.Current?.TraceId);
         }

         return response;
      }
      finally
      {
         keyLock.Release();
      }
   }

   /// <summary>
   /// Builds a unique lock key for cache stampede protection.
   /// Format: "{version}:{feature}:{value}"
   /// </summary>
   private static string BuildLockKey(ICacheKey cacheKey, string version)
       => $"{version}:{cacheKey.Feature}:{cacheKey.Value}";
}
