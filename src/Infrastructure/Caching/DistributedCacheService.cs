using Application.Abstractions.Caching;
using Application.DependencyInjection.Options;
using Infrastructure.Caching.Observability;
using Infrastructure.Caching.Serializers;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using System.Diagnostics;

namespace Infrastructure.Caching;

internal sealed class DistributedCacheService(
    IDistributedCache cache,
    ICacheSerializer serializer,
    IOptions<CacheOptions> cacheOptions,
    CacheMetrics metrics,
    ILogger<DistributedCacheService> logger,
    IConnectionMultiplexer? redis = null) : ICacheService
{
   private const string CacheFeatureTag = "cache.feature";
   private const string CacheRedisHitTag = "cache.redis_hit";
   private const string CacheLogicalHitTag = "cache.logical_hit";
   private static readonly ActivitySource ActivitySource = new("DotnetCleanAspire.Caching");
   private readonly IDistributedCache _cache = cache;
   private readonly ICacheSerializer _serializer = serializer;
   private readonly CacheOptions _cacheOptions = cacheOptions.Value;
   private readonly CacheMetrics _metrics = metrics;
   private readonly ILogger<DistributedCacheService> _logger = logger;
   private readonly IConnectionMultiplexer? _redis = redis;

   public async Task<CacheEntry<T>?> GetAsync<T>(ICacheKey key, string? version = null, CancellationToken cancellationToken = default)
   {
      using Activity? activity = ActivitySource.StartActivity("cache.get");
      activity?.SetTag(CacheFeatureTag, key.Feature);

      var stopwatch = Stopwatch.StartNew();
      try
      {
         string redisKey = BuildRedisKey(key, version);
         byte[]? bytes = await _cache.GetAsync(redisKey, cancellationToken);

         // Redis hit = bytes != null
         bool redisHit = bytes is not null;

         // Redis miss: key not found in Redis
         if (!redisHit)
         {
            stopwatch.Stop();
            _metrics.RecordMiss(key.Feature);
            activity?.SetTag(CacheRedisHitTag, false);
            activity?.SetTag(CacheLogicalHitTag, false);
            _metrics.RecordOperationDuration("get", stopwatch.Elapsed.TotalMilliseconds, success: true);
            return null;
         }

         // Measure deserialization time separately
         var deserializeStopwatch = Stopwatch.StartNew();
         CacheEntry<T>? entry;

         try
         {
            entry = _serializer.Deserialize<CacheEntry<T>>(bytes);
         }
         catch (Exception ex)
         {
            // Deserialization failed - entry is corrupted or incompatible type
            deserializeStopwatch.Stop();
            stopwatch.Stop();

            _logger.LogWarning(ex, "Cache deserialization failed for key '{Key}'. Removing corrupted entry.", redisKey);

            await _cache.RemoveAsync(redisKey, cancellationToken);

            // Also remove from feature tag set
            if (_redis is not null)
            {
               IDatabase db = _redis.GetDatabase();
               string tagSetKey = BuildFeatureTagSetKey(key.Feature);
               await db.SetRemoveAsync(tagSetKey, redisKey);
            }

            _metrics.RecordMiss(key.Feature);
            _metrics.RecordOperationDuration("get", stopwatch.Elapsed.TotalMilliseconds, success: true);
            activity?.SetTag(CacheRedisHitTag, false);
            activity?.SetTag(CacheLogicalHitTag, false);
            activity?.SetTag("cache.deserialization_failed", true);
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);

            return null;
         }

         deserializeStopwatch.Stop();
         stopwatch.Stop();

         // Record deserialization metrics
         _metrics.RecordSerializationDuration("deserialize", deserializeStopwatch.Elapsed.TotalMilliseconds, _serializer.Name);

         // Logical hit = entry exists (deserialization succeeded)
         // This allows distinguishing:
         // - Redis hit + logical hit = normal cache hit (entry.HasValue == true)
         // - Redis hit + no logical hit = cached null value (entry.HasValue == false)
         bool logicalHit = entry is not null;

         if (logicalHit)
         {
            _metrics.RecordHit(key.Feature);
            activity?.SetTag(CacheRedisHitTag, true);
            activity?.SetTag(CacheLogicalHitTag, true);
            activity?.SetTag("cache.has_value", entry!.HasValue);
         }
         else
         {
            // Deserialization returned null (should not happen with current serializers)
            _logger.LogWarning("Cache deserialization returned null for key '{Key}'. Removing entry.", redisKey);

            await _cache.RemoveAsync(redisKey, cancellationToken);

            // Also remove from feature tag set
            if (_redis is not null)
            {
               IDatabase db = _redis.GetDatabase();
               string tagSetKey = BuildFeatureTagSetKey(key.Feature);
               await db.SetRemoveAsync(tagSetKey, redisKey);
            }

            _metrics.RecordMiss(key.Feature);
            activity?.SetTag(CacheRedisHitTag, true);
            activity?.SetTag(CacheLogicalHitTag, false);
            activity?.SetTag("cache.deserialization_failed", true);
         }

         _metrics.RecordOperationDuration("get", stopwatch.Elapsed.TotalMilliseconds, success: true);

         return entry;
      }
      catch
      {
         stopwatch.Stop();
         _metrics.RecordOperationDuration("get", stopwatch.Elapsed.TotalMilliseconds, success: false);
         activity?.SetStatus(ActivityStatusCode.Error);
         throw;
      }
   }

   public async Task SetAsync<T>(
       ICacheKey key,
       CacheEntry<T> entry,
       TimeSpan? expiration = null,
       string? version = null,
       bool? useCompression = null,
       CancellationToken cancellationToken = default)
   {
      using Activity? activity = ActivitySource.StartActivity("cache.set");
      activity?.SetTag(CacheFeatureTag, key.Feature);

      var stopwatch = Stopwatch.StartNew();
      try
      {
         // Resolve configuration: use provided values or fallback to global config
         TimeSpan duration = expiration ?? _cacheOptions.DefaultCacheDuration;
         bool shouldCompress = useCompression ?? _cacheOptions.EnableCompression;

         activity?.SetTag("cache.expiration_seconds", duration.TotalSeconds);

         var options = new DistributedCacheEntryOptions
         {
            AbsoluteExpirationRelativeToNow = duration
         };

         // Measure serialization time and capture metrics
         var serializeStopwatch = Stopwatch.StartNew();
         SerializationResult serializationResult = _serializer.SerializeWithMetrics(
             entry,
             shouldCompress,
             _cacheOptions.CompressionThresholdBytes);
         serializeStopwatch.Stop();

         // Record serialization metrics
         _metrics.RecordSerializationDuration("serialize", serializeStopwatch.Elapsed.TotalMilliseconds, _serializer.Name);

         // Record compression ratio if compression was applied
         if (serializationResult.IsCompressed)
         {
            _metrics.RecordCompressionRatio(
                serializationResult.OriginalSize,
                serializationResult.FinalSize,
                _serializer.Name);
         }

         string redisKey = BuildRedisKey(key, version);
         await _cache.SetAsync(redisKey, serializationResult.Bytes, options, cancellationToken);

         // Add key to feature tag set for O(1) invalidation
         if (_redis is not null)
         {
            await AddKeyToFeatureTagSetAsync(key.Feature, redisKey, duration);
         }

         stopwatch.Stop();

         // Record metrics
         _metrics.RecordEntrySize(serializationResult.Bytes.Length, serializationResult.IsCompressed);
         _metrics.RecordOperationDuration("set", stopwatch.Elapsed.TotalMilliseconds, success: true);

         activity?.SetTag("cache.entry_size_bytes", serializationResult.Bytes.Length);
         activity?.SetTag("cache.compressed", serializationResult.IsCompressed);
         if (serializationResult.IsCompressed)
         {
            activity?.SetTag("cache.compression_ratio", serializationResult.CompressionRatio);
         }
      }
      catch
      {
         stopwatch.Stop();
         _metrics.RecordOperationDuration("set", stopwatch.Elapsed.TotalMilliseconds, success: false);
         activity?.SetStatus(ActivityStatusCode.Error);
         throw;
      }
   }

   public async Task RemoveAsync(ICacheKey key, string? version = null, CancellationToken cancellationToken = default)
   {
      using Activity? activity = ActivitySource.StartActivity("cache.remove");
      activity?.SetTag(CacheFeatureTag, key.Feature);

      var stopwatch = Stopwatch.StartNew();
      try
      {
         string redisKey = BuildRedisKey(key, version);
         await _cache.RemoveAsync(redisKey, cancellationToken);

         // Also remove from feature tag set to keep it clean
         if (_redis is not null)
         {
            IDatabase db = _redis.GetDatabase();
            string tagSetKey = BuildFeatureTagSetKey(key.Feature);
            await db.SetRemoveAsync(tagSetKey, redisKey);
         }

         stopwatch.Stop();
         _metrics.RecordInvalidation(key.Feature, count: 1);
         _metrics.RecordOperationDuration("remove", stopwatch.Elapsed.TotalMilliseconds, success: true);
      }
      catch
      {
         stopwatch.Stop();
         _metrics.RecordOperationDuration("remove", stopwatch.Elapsed.TotalMilliseconds, success: false);
         activity?.SetStatus(ActivityStatusCode.Error);
         throw;
      }
   }

   public async Task RemoveByFeatureAsync(string feature, CancellationToken cancellationToken = default)
   {
      using Activity? activity = ActivitySource.StartActivity("cache.invalidate");
      activity?.SetTag(CacheFeatureTag, feature);

      var stopwatch = Stopwatch.StartNew();
      try
      {
         if (_redis is null)
         {
            // Fallback: IDistributedCache doesn't support pattern/feature-based deletion
            // In production with Redis, IConnectionMultiplexer should be injected
            _logger.LogWarning("Cannot invalidate feature '{Feature}' - Redis connection not available. " +
                               "Feature-based invalidation requires IConnectionMultiplexer. " +
                               "Individual cache entries will still expire naturally.", feature);
            stopwatch.Stop();
            _metrics.RecordOperationDuration("invalidate", stopwatch.Elapsed.TotalMilliseconds, success: true);
            return;
         }

         // O(1) tag-based invalidation by feature
         int count = await RemoveByFeatureTagAsync(feature);

         stopwatch.Stop();
         _metrics.RecordInvalidation(feature, count);
         _metrics.RecordOperationDuration("invalidate", stopwatch.Elapsed.TotalMilliseconds, success: true);

         activity?.SetTag("cache.invalidated_count", count);
      }
      catch
      {
         stopwatch.Stop();
         _metrics.RecordOperationDuration("invalidate", stopwatch.Elapsed.TotalMilliseconds, success: false);
         activity?.SetStatus(ActivityStatusCode.Error);
         throw;
      }
   }

   /// <summary>
   /// Builds the Redis key from an ICacheKey and optional version.
   /// Format: "{version}:{feature}:{value}" or "{feature}:{value}" if no version.
   /// </summary>
   private static string BuildRedisKey(ICacheKey key, string? version)
   {
      return version is not null
          ? $"{version}:{key.Feature}:{key.Value}"
          : $"{key.Feature}:{key.Value}";
   }

   /// <summary>
   /// Builds the Redis key for a feature tag set.
   /// Format: "tag:{feature}"
   /// </summary>
   private static string BuildFeatureTagSetKey(string feature)
       => $"tag:{feature}";

   /// <summary>
   /// Adds a cache key to its feature tag set in Redis.
   /// This enables O(1) feature-based invalidation.
   /// </summary>
   private async Task AddKeyToFeatureTagSetAsync(string feature, string redisKey, TimeSpan expiration)
   {
      if (_redis is null)
         return;

      IDatabase db = _redis.GetDatabase();
      string tagSetKey = BuildFeatureTagSetKey(feature);

      // Add key to feature tag set
      await db.SetAddAsync(tagSetKey, redisKey);

      // Set expiration on tag set (slightly longer than cache entries to avoid orphans)
      await db.KeyExpireAsync(tagSetKey, expiration.Add(TimeSpan.FromMinutes(5)));
   }

   /// <summary>
   /// Removes all keys associated with a feature tag (O(1) operation).
   /// </summary>
   private async Task<int> RemoveByFeatureTagAsync(string feature)
   {
      if (_redis is null)
         return 0;

      IDatabase db = _redis.GetDatabase();
      string tagSetKey = BuildFeatureTagSetKey(feature);

      // Get all keys in the feature tag set
      RedisValue[] keys = await db.SetMembersAsync(tagSetKey);

      if (keys.Length is 0)
         return 0;

      // Delete all keys in the tag set
      await db.KeyDeleteAsync([.. keys.Select(k => (RedisKey)k.ToString())]);

      // Delete the tag set itself
      await db.KeyDeleteAsync(tagSetKey);

      return keys.Length;
   }
}
