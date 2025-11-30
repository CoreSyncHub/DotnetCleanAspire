using Application.Abstractions.Caching;
using Microsoft.Extensions.Caching.Distributed;
using StackExchange.Redis;
using System.Text.Json;

namespace Infrastructure.Caching;

internal sealed class DistributedCacheService(
    IDistributedCache cache,
    IConnectionMultiplexer? redis = null) : ICacheService
{
   private static readonly JsonSerializerOptions JsonOptions = new()
   {
      PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
      WriteIndented = false
   };

   public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
   {
      byte[]? bytes = await cache.GetAsync(key, cancellationToken);

      if (bytes is null || bytes.Length == 0)
      {
         return default;
      }

      return JsonSerializer.Deserialize<T>(bytes, JsonOptions);
   }

   public async Task SetAsync<T>(
       string key,
       T value,
       TimeSpan expiration,
       CancellationToken cancellationToken = default)
   {
      var options = new DistributedCacheEntryOptions
      {
         AbsoluteExpirationRelativeToNow = expiration
      };

      byte[] bytes = JsonSerializer.SerializeToUtf8Bytes(value, JsonOptions);

      await cache.SetAsync(key, bytes, options, cancellationToken);
   }

   public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
   {
      await cache.RemoveAsync(key, cancellationToken);
   }

   public async Task RemoveByPatternAsync(string pattern, CancellationToken cancellationToken = default)
   {
      if (redis is null)
      {
         // Fallback: IDistributedCache doesn't support pattern deletion
         // In production with Redis, IConnectionMultiplexer should be injected
         return;
      }

      IDatabase db = redis.GetDatabase();
      IServer server = redis.GetServer(redis.GetEndPoints().First());

      await foreach (RedisKey key in server.KeysAsync(pattern: pattern))
      {
         await db.KeyDeleteAsync(key);
      }
   }
}
