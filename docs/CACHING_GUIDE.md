# Caching System Guide

This guide explains how to use the distributed caching system provided by the .NET Clean Architecture with Aspire template.

This caching system is opinionated by design.
It favors correctness, simplicity, and operational safety over flexibility.

## Table of Contents

1. [Overview](#overview)
2. [Architecture](#architecture)
3. [Caching Queries](#caching-queries)
4. [Cache Invalidation (Commands)](#cache-invalidation-commands)
5. [Cache Key Management](#cache-key-management)
6. [Configuration](#configuration)
7. [Cache Versioning](#cache-versioning)
8. [Compression](#compression)
9. [Observability](#observability)
10. [Best Practices](#best-practices)

## Overview

The caching system is integrated into the Mediator pipeline via two automatic behaviors:

- **CachingBehavior**: Caches query results
- **CacheInvalidationBehavior**: Invalidates cache after commands

### Key Features

- Redis distributed cache via `IDistributedCache`
- Cache stampede protection (double-check locking)
- O(1) feature-based invalidation via Redis Sets
- Compression support (Brotli)
- Global and feature-specific versioning
- Complete observability (metrics, logs, traces)
- Multiple serializers support (JSON, MessagePack)

## Architecture

```txt
┌──────────────────────────────────────────────────────────────┐
│                      Mediator Pipeline                       │
├──────────────────────────────────────────────────────────────┤
│                                                              │
│  Request → CachingBehavior → Handler → CacheInvalidation     │
│              (Queries)                    (Commands)         │
│                  ↓                             ↓             │
│            ICacheService                 ICacheService       │
│                  ↓                              ↓            │
│                      DistributedCacheService                 │
│                                ↓                             │
│                     IDistributedCache (Redis)                │
└──────────────────────────────────────────────────────────────┘
```

## Caching Queries

### Step 1: Implement the `ICacheable` interface

To make a query cacheable, implement the `ICacheable` interface:

```csharp
using Application.Abstractions.Caching;
using Application.Features.Todos.Cache;

public sealed record GetTodoByIdQuery(Id Id) : IQuery<TodoDto>, ICacheable
{
    // Structured cache key
    public ICacheKey CacheKey => TodoCacheKeys.ById(Id);

    // Cache duration
    public TimeSpan CacheDuration => TimeSpan.FromMinutes(5);

    // (Optional) Specific version for this query
    public string? Version => null;

    // (Optional) Enable compression for this entry
    public bool UseCompression => false;
}
```

### `ICacheable` Properties

| Property         | Type        | Required | Description                          |
| ---------------- | ----------- | -------- | ------------------------------------ |
| `CacheKey`       | `ICacheKey` | Yes      | Structured cache key (feature:value) |
| `CacheDuration`  | `TimeSpan`  | Yes      | Cache entry lifetime                 |
| `Version`        | `string?`   | No       | Version override (default: null)     |
| `UseCompression` | `bool`      | No       | Enable compression (default: false)  |

### Step 2: Handler remains unchanged

The handler doesn't need to know about caching:

```csharp
internal sealed class GetTodoByIdQueryHandler(IApplicationDbContext dbContext)
    : IQueryHandler<GetTodoByIdQuery, TodoDto>
{
    public async Task<Result<TodoDto>> Handle(
        GetTodoByIdQuery request,
        CancellationToken cancellationToken)
    {
        // Your normal business logic
        Todo? todo = await dbContext.Todos
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == request.Id, cancellationToken);

        if (todo is null)
        {
            return TodoErrors.NotFound(request.Id);
        }

        return new TodoDto(
            todo.Id.ToString(),
            todo.Title.Value,
            todo.Status.ToString(),
            todo.Created);
    }
}
```

### How it works

1. The `CachingBehavior` automatically intercepts the request
2. It checks if the request implements `ICacheable`
3. It looks for the entry in Redis with the versioned key
4. **Cache hit**: Returns the value immediately
5. **Cache miss**: Executes the handler and caches the result on success

### Cache Stampede Protection

The system automatically protects against cache stampede:

- Only one thread per key executes the handler
- Other threads wait and retrieve the cached value
- Double-check locking optimizes concurrent access

## Cache Invalidation (Commands)

### Step 1: Implement the `ICacheInvalidating` interface

For a command to invalidate cache, implement `ICacheInvalidating`:

```csharp
using Application.Abstractions.Caching;
using Application.Features.Todos.Cache;

public sealed record CreateTodoCommand(string Title)
    : ICommand<CreateTodoDto>, ICacheInvalidating
{
    // Option 1: Invalidate an entire feature (recommended)
    public IReadOnlyCollection<string>? FeaturesToInvalidate => [TodoCacheKeys.FeatureTag];

    // Option 2: Invalidate specific keys
    public IReadOnlyCollection<ICacheKey>? KeysToInvalidate => null;
}
```

### Invalidation Strategies

#### 1. Feature-based invalidation (O(1) - Recommended)

Invalidates all entries of a feature in a single operation:

```csharp
public sealed record CreateTodoCommand(string Title)
    : ICommand<CreateTodoDto>, ICacheInvalidating
{
    // Invalidates all caches for the "todos" feature
    public IReadOnlyCollection<string>? FeaturesToInvalidate => [TodoCacheKeys.FeatureTag];
}
```

**Use this strategy when:**

- Creating/updating/deleting entities
- The cache impact is broad (e.g., lists, aggregations)
- You want simplicity and performance

#### 2. Key-specific invalidation

Invalidates only specific entries:

```csharp
public sealed record UpdateTodoCommand(Id TodoId, string Title)
    : ICommand<Unit>, ICacheInvalidating
{
    // Invalidates only the specific todo and the list
    public IReadOnlyCollection<ICacheKey>? KeysToInvalidate =>
    [
        TodoCacheKeys.ById(TodoId),
        TodoCacheKeys.List()
    ];
}
```

**Use this strategy when:**

- You know exactly which entries are affected
- You want to preserve other caches as much as possible
- The operation is very targeted

#### 3. Mixed invalidation

Combines both approaches:

```csharp
public sealed record BulkDeleteTodosCommand(IReadOnlyCollection<Id> TodoIds)
    : ICommand<Unit>, ICacheInvalidating
{
    // Invalidates specific entries + entire feature
    public IReadOnlyCollection<ICacheKey>? KeysToInvalidate =>
        TodoIds.Select(id => TodoCacheKeys.ById(id)).ToList();

    public IReadOnlyCollection<string>? FeaturesToInvalidate => [TodoListCacheKeys.FeatureTag];
}
```

### Step 2: Handler remains unchanged

The handler doesn't need to know about invalidation:

```csharp
internal sealed class CreateTodoCommandHandler(IApplicationDbContext dbContext)
    : ICommandHandler<CreateTodoCommand, CreateTodoDto>
{
    public async Task<Result<CreateTodoDto>> Handle(
        CreateTodoCommand request,
        CancellationToken cancellationToken)
    {
        // Your normal business logic
        Result<Todo> todoResult = TodoTitle.Create(request.Title)
            .Bind(title => Todo.Create(title, TodoStatus.Pending));

        if (todoResult.IsFailure)
        {
            return Result<CreateTodoDto>.Failure(todoResult.Error);
        }

        Todo todo = todoResult.Value;

        dbContext.Todos.Add(todo);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result<CreateTodoDto>.Success(
            new CreateTodoDto(todo.Id.ToString(), todo.Title.Value),
            SuccessType.Created);
    }
}
```

### How it works

1. The handler executes normally
2. If the result indicates success (`IResult.IsSuccess == true`)
3. The `CacheInvalidationBehavior` automatically invalidates:
   - Keys specified in `KeysToInvalidate`
   - All entries of features in `FeaturesToInvalidate`

### O(1) Feature-based Invalidation

Feature-based invalidation uses Redis Sets for O(1) performance:

- Each cache key is added to a Redis Set `tag:{feature}`
- Invalidation retrieves all keys from the Set and deletes them
- No pattern scanning, no expensive loops

## Cache Key Management

### Create a centralized key class

For each feature, create a static class with your keys:

```csharp
namespace Application.Features.Todos.Cache;

/// <summary>
/// Centralized cache key builder for Todo feature.
/// Prevents magic strings and typos in cache key construction.
/// </summary>
public static class TodoCacheKeys
{
    private const string Feature = "todos";

    // Exact keys
    public static ICacheKey ById(Id id) => new CacheKey(Feature, id.ToString());
    public static ICacheKey List() => new CacheKey(Feature, "list");

    // Tag for feature invalidation
    public static string FeatureTag => Feature;
}
```

### Best Practices for Keys

1. **Always use `CacheKey(feature, value)`**
   - Structure: `feature:value` (e.g., `todos:42`)
   - Prevents collisions between features
   - Compatible with feature-based invalidation

2. **Centralize your keys in a static class**
   - Avoids magic strings
   - Facilitates maintenance
   - IntelliSense and refactoring

3. **Consistent naming**

   ```csharp
   // ✅ Good
   ById(id)       → "todos:42"
   List()         → "todos:list"
   ListPending()  → "todos:list-pending"

   // ❌ Bad
   GetById(id)    → confusion with handler
   todo42         → not structured
   "todos:list"   → magic string
   ```

4. **No wildcards in keys**
   - Patterns (`todos:*`) are not supported for invalidation
   - Use feature-based invalidation instead

> [!NOTE]
> Although Redis supports wildcard key deletion, this system deliberately avoids pattern-based invalidation.
> Reasons:
>
> - Pattern scans are O(N)
> - They introduce unpredictable performance costs
> - They are error-prone and hard to reason about

### Redis Key Format

Keys are automatically built with the format:

```
{version}:{feature}:{value}
```

Examples:

- `v1:todos:42` (GetTodoById with id=42)
- `v1:todos:list` (GetTodosList)
- `v2:users:123` (GetUserById with feature version v2)

## Configuration

### Configuration in appsettings.json

```json
{
  "Caching": {
    // Global cache version
    "GlobalVersion": "v1",

    // Feature-specific versions (overrides global version)
    "FeatureVersions": {
      "todos": "v2",
      "users": "v1"
    },

    // Default cache duration for all queries (can be overridden per-query)
    "DefaultCacheDuration": "00:05:00", // 5 minutes (format: hh:mm:ss)

    // Enable compression globally (can be overridden per-query)
    "EnableCompression": false,

    // Minimum size before compression (bytes)
    "CompressionThresholdBytes": 1024
  }
}
```

### Configuration Fallback Strategy

The caching system uses a **configuration fallback strategy** where queries can override global settings:

1. **Cache Duration**:
   - Query specifies `CacheDuration` → uses query value
   - Query returns `null` → uses `CacheOptions.DefaultCacheDuration` from appsettings.json

2. **Compression**:
   - Query specifies `UseCompression` → uses query value
   - Query returns `null` → uses `CacheOptions.EnableCompression` from appsettings.json

3. **Versioning**:
   - Query specifies `Version` → uses query value
   - Query returns `null` and feature has version in `FeatureVersions` → uses feature version
   - Otherwise → uses `CacheOptions.GlobalVersion`

**Example: Using global configuration**

```csharp
public sealed record GetTodoByIdQuery(Id Id) : IQuery<TodoDto>, ICacheable
{
    public ICacheKey CacheKey => TodoCacheKeys.ById(Id);

    // All properties return null = use global configuration
    // Duration: uses CacheOptions.DefaultCacheDuration (5 minutes)
    // Compression: uses CacheOptions.EnableCompression (false)
    // Version: uses feature version or GlobalVersion
}
```

**Example: Overriding specific settings**

```csharp
public sealed record GetLargeTodoListQuery : IQuery<List<TodoDto>>, ICacheable
{
    public ICacheKey CacheKey => TodoCacheKeys.List();

    // Override duration for this specific query
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(10);

    // Override compression for large lists
    public bool? UseCompression => true;

    // Version: still uses feature version or global version (null = default)
}
```

### Serializer Configuration

By default, the template uses **System.Text.Json** for serialization.

To use **MessagePack** (more performant):

1. The package is already included in the template
2. Configure in [Infrastructure/DependencyInjection.cs](../src/Infrastructure/DependencyInjection.cs):

```csharp
services.AddSingleton<ICacheSerializer, MessagePackCacheSerializer>();
// Instead of:
// services.AddSingleton<ICacheSerializer, JsonCacheSerializer>();
```

**Comparison:**

| Serializer      | Advantages                                   | Disadvantages                               |
| --------------- | -------------------------------------------- | ------------------------------------------- |
| **JSON**        | Human-readable, easy debugging, wide support | Slower, larger                              |
| **MessagePack** | 2-5x faster, 30-50% more compact             | Binary (not readable), requires annotations |

### Compression Configuration

#### Global

```json
{
  "Caching": {
    "EnableCompression": true,
    "CompressionThresholdBytes": 1024
  }
}
```

#### Per query

```csharp
public sealed record GetLargeTodoListQuery : IQuery<List<TodoDto>>, ICacheable
{
    public ICacheKey CacheKey => TodoCacheKeys.List();
    public TimeSpan CacheDuration => TimeSpan.FromMinutes(10);

    // Override: enable compression for this query
    public bool UseCompression => true;
}
```

## Cache Versioning

Versioning allows mass cache invalidation without deleting keys.

### 3 Levels of Versioning

1. **Global version** (all caches)
2. **Feature-specific version** (an entire feature)
3. **Query-specific version** (a specific query)

### Usage Scenarios

#### 1. Invalidate all cache (major deployment)

```json
{
  "Caching": {
    "GlobalVersion": "v2" // Was "v1"
  }
}
```

Restart the application. All caches become inaccessible.

#### 2. Invalidate a feature (model change)

```json
{
  "Caching": {
    "GlobalVersion": "v1",
    "FeatureVersions": {
      "todos": "v3" // Was "v2"
    }
  }
}
```

All caches for the "todos" feature become inaccessible.

#### 3. Invalidate a specific query

```csharp
public sealed record GetTodoStatsQuery : IQuery<TodoStatsDto>, ICacheable
{
    public ICacheKey CacheKey => TodoCacheKeys.Stats();
    public TimeSpan CacheDuration => TimeSpan.FromHours(1);

    // Explicit version for this query
    public string Version => "v2";  // Was "v1"
}
```

Recompile. Only caches for this query become inaccessible.

### Priority Order

```
Query version > Feature version > Global version
```

### Advantages vs Manual Invalidation

| Aspect      | Versioning                      | Manual Invalidation            |
| ----------- | ------------------------------- | ------------------------------ |
| Performance | Instant (no I/O)                | Requires Redis calls           |
| Simplicity  | Config change + restart         | Custom code, patterns, scripts |
| Scope       | Granular (global/feature/query) | Depends on implementation      |
| Cleanup     | Automatic (Redis expiration)    | Requires manual deletion       |

## Compression

### When to Use Compression

**Enable compression if:**

- Your payloads exceed 1 KB (configurable)
- You cache lists, collections
- You have lots of text (descriptions, content)
- Network latency is more expensive than CPU

**Disable compression if:**

- Your payloads are small (<1 KB)
- Your data is already compressed (images, binaries)
- You're optimizing for CPU

### Compression Configuration

```json
{
  "Caching": {
    "EnableCompression": true,
    "CompressionThresholdBytes": 1024
  }
}
```

### Compression Metrics

The system automatically records:

- **Original size** vs **compressed size**
- **Compression ratio** (e.g., 3.5x)
- **Compression/decompression duration**

Visualize these metrics in your observability system.

### Compression Algorithm

Default: **Brotli** (better ratio, modern web standard)

Alternative: **Gzip** (universal support, slightly less performant)

Configure in [Infrastructure/Caching/Serializers/\*.cs](../src/Infrastructure/Caching/Serializers/).

## Observability

### Available Metrics

The system automatically exposes metrics via OpenTelemetry:

#### Cache Metrics

- `cache.hit_count` - Number of cache hits per feature
- `cache.miss_count` - Number of cache misses per feature
- `cache.hit_ratio` - Hit ratio (0-1)

#### Operation Metrics

- `cache.operation_duration` - Operation duration (get/set/remove/invalidate)
- `cache.operation_success` - Operation success/failures

#### Size Metrics

- `cache.entry_size_bytes` - Cache entry size
- `cache.compressed_entry_size_bytes` - Size after compression

#### Compression Metrics

- `cache.compression_ratio` - Compression ratio (e.g., 3.5)
- `cache.serialization_duration` - Serialization/deserialization duration

#### Invalidation Metrics

- `cache.invalidation_count` - Number of invalidated entries per feature

### Distributed Traces

Each cache operation creates an OpenTelemetry span:

- `cache.get` - Cache read
- `cache.set` - Cache write
- `cache.remove` - Key deletion
- `cache.invalidate` - Feature invalidation

**Available tags:**

- `cache.feature` - Affected feature
- `cache.redis_hit` - Redis hit (true/false)
- `cache.logical_hit` - Logical hit (true/false)
- `cache.has_value` - Contains value (true/false)
- `cache.entry_size_bytes` - Entry size
- `cache.compressed` - Compressed (true/false)
- `cache.compression_ratio` - Compression ratio

### Structured Logs

The system automatically logs:

```csharp
// Cache hit
[Debug] Cache hit for feature {Feature}. TraceId: {TraceId}

// Cache miss
[Debug] Cache miss for feature {Feature}. TraceId: {TraceId}

// Caching
[Debug] Cached feature {Feature} for {Duration} (Compression: {UseCompression}). TraceId: {TraceId}

// Invalidation
[Debug] Invalidated cache key - Feature: {Feature}, Value: {Value}, Version: {Version}
[Debug] Invalidated entire feature: {Feature}

// Errors
[Warning] Failed to invalidate cache key - Feature: {Feature}, Value: {Value}
[Warning] Cache deserialization failed for key '{Key}'. Removing corrupted entry.
```

### Recommended Dashboard

Create a dashboard with:

1. **Cache hit ratio** by feature (line chart)
2. **Operation duration** (p50, p95, p99)
3. **Entry size** (histogram)
4. **Average compression ratio**
5. **Invalidations per feature** (bar chart)

## Best Practices

### 1. Only Cache Queries

- **Queries** are idempotent and can be cached
- **Commands** modify state and should not be cached

```csharp
// ✅ Good - Query with ICacheable
public sealed record GetTodoByIdQuery(Id Id) : IQuery<TodoDto>, ICacheable

// ❌ Bad - Command should not implement ICacheable
public sealed record CreateTodoCommand(string Title) : ICommand<Unit>, ICacheable
```

### 2. Define Appropriate Cache Durations

| Data Type       | Recommended Duration | Example               |
| --------------- | -------------------- | --------------------- |
| Static data     | 1-24 hours           | Categories, countries |
| Frequent data   | 5-15 minutes         | Lists, searches       |
| Individual data | 5-10 minutes         | Entity details        |
| Real-time data  | 30-60 seconds        | Statistics, counters  |
| Volatile data   | Don't cache          | Active user data      |

```csharp
// Static data
public TimeSpan CacheDuration => TimeSpan.FromHours(1);

// Frequent data
public TimeSpan CacheDuration => TimeSpan.FromMinutes(10);

// Real-time data
public TimeSpan CacheDuration => TimeSpan.FromSeconds(30);
```

### 3. Centralize Cache Keys

```csharp
// ✅ Good
public static class TodoCacheKeys
{
    private const string Feature = "todos";
    public static ICacheKey ById(Id id) => new CacheKey(Feature, id.ToString());
    public static ICacheKey List() => new CacheKey(Feature, "list");
    public static string FeatureTag => Feature;
}

// In the query
public ICacheKey CacheKey => TodoCacheKeys.ById(Id);

// ❌ Bad
public ICacheKey CacheKey => new CacheKey("todos", Id.ToString());
```

### 4. Invalidate Broadly Rather Than Precisely

When in doubt, invalidate the entire feature:

```csharp
// ✅ Good - Simple and safe
public IReadOnlyCollection<string>? FeaturesToInvalidate => [TodoCacheKeys.FeatureTag];

// ❌ Bad - Complex and risky (might forget keys)
public IReadOnlyCollection<ICacheKey>? KeysToInvalidate =>
[
    TodoCacheKeys.ById(TodoId),
    TodoCacheKeys.List(),
    TodoCacheKeys.ListPending(),
    TodoCacheKeys.ListCompleted(),
    // ... what if we forget a key?
];
```

**Principle:** Better a cache miss than stale cache.

### 5. Use Compression for Large Lists

```csharp
public sealed record GetAllTodosQuery : IQuery<List<TodoDto>>, ICacheable
{
    public ICacheKey CacheKey => TodoCacheKeys.List();
    public TimeSpan CacheDuration => TimeSpan.FromMinutes(5);

    // Enable compression for large lists
    public bool UseCompression => true;
}
```

### 6. Monitor Hit Ratios

A good hit ratio is generally:

- **> 70%** for static data
- **> 50%** for frequent data
- **> 30%** for individual data

If your hit ratio is low:

- Cache duration might be too short
- Data is too volatile (consider not caching)
- Invalidation is too aggressive

### 7. Test Without Cache in Development

To temporarily disable cache in development:

```json
{
  "Caching": {
    "GlobalVersion": "dev-{timestamp}" // Changes on each startup
  }
}
```

Or remove the `ICacheable` interface from your queries during development.

### 8. Don't Cache Sensitive Data

Redis cache can be:

- Shared between environments (be careful!)
- Accessible by multiple applications
- Persisted to disk

**Don't cache:**

- Tokens, secrets, passwords
- PII data without encryption
- Data with fine-grained permissions (use application cache)

### 9. Handle Null Values

The system automatically distinguishes:

- **Cache miss** (`null`) - Key doesn't exist
- **Cached null value** (`CacheEntry.Empty()`) - Key exists with null value

But in practice, only cache successes:

```csharp
// ✅ The system already does this automatically
// Only results with IsSuccess=true are cached

if (todo is null)
{
    return Result.Failure(TodoErrors.NotFound(request.Id));
}
// This error won't be cached (default behavior)
```

### 10. Document Cache Strategies

Document your caching decisions in code:

```csharp
/// <summary>
/// Gets all pending todos for a user.
/// Cache strategy:
/// - Duration: 2 minutes (lists change frequently)
/// - Invalidation: On any todo status change
/// - Compression: Enabled (large lists expected)
/// </summary>
public sealed record GetPendingTodosQuery(Id UserId) : IQuery<List<TodoDto>>, ICacheable
{
    public ICacheKey CacheKey => TodoCacheKeys.ListPending(UserId);
    public TimeSpan CacheDuration => TimeSpan.FromMinutes(2);
    public bool UseCompression => true;
}
```

## Troubleshooting

### Stale Cache

**Symptom:** Cached data doesn't reflect changes.

**Solutions:**

1. Verify your command implements `ICacheInvalidating`
2. Verify you're invalidating the correct feature or keys
3. Check logs to confirm invalidation
4. As a last resort, change the cache version

### Performance Degradation

**Symptom:** Cache operations are slow.

**Solutions:**

1. Check network latency to Redis
2. Enable compression for large entries
3. Reduce cache duration to limit entry size
4. Use MessagePack instead of JSON
5. Check `cache.operation_duration` metrics

### Cache Stampede

**Symptom:** Multiple threads execute the same handler simultaneously.

**Solution:** The system already protects against this automatically. If you observe the issue:

1. Verify the cache key is identical between requests
2. Verify the version is identical
3. Check logs for "Cache hit after lock acquisition" message

### Serialization Errors

**Symptom:** Logs show "Cache deserialization failed".

**Solutions:**

1. Verify your DTOs are serializable
2. If using MessagePack, add required attributes
3. Change cache version to ignore corrupted entries
4. Corrupted entry is automatically removed

### Redis Unavailable

**Symptom:** Exceptions during cache operations.

**Solution:** Exceptions bubble up to the handler. Add resilience:

> By default, cache failures do not break application logic.
> Failures are logged and the handler executes normally.
>
> If strict behavior is required, resilience policies can be adjusted.

```csharp
// Configure Polly in DependencyInjection.cs
services.AddStackExchangeRedisCache(options => { })
    .AddResilience(/* configure your policy */);
```

The system continues to work without cache on failure.

## Complete Examples

### Example 1: Simple Query with Cache

```csharp
// Query
public sealed record GetTodoByIdQuery(Id Id) : IQuery<TodoDto>, ICacheable
{
    public ICacheKey CacheKey => TodoCacheKeys.ById(Id);
    public TimeSpan CacheDuration => TimeSpan.FromMinutes(5);
}

// Handler (unchanged)
internal sealed class GetTodoByIdQueryHandler(IApplicationDbContext dbContext)
    : IQueryHandler<GetTodoByIdQuery, TodoDto>
{
    public async Task<Result<TodoDto>> Handle(
        GetTodoByIdQuery request,
        CancellationToken cancellationToken)
    {
        TodoDto? projection = await dbContext.Todos
            .AsNoTracking()
            .Select(t => new TodoDto(
                t.Id.ToString(),
                t.Title.Value,
                t.Status.ToString(),
                t.Created
              )
            )
            .FirstOrDefaultAsync(t => t.Id == request.Id, cancellationToken);

        if (projection is null)
        {
            return Result.Failure(TodoErrors.AlreadyCompleted);
        }

        return Result.Success(projection);
    }
}

// Centralized keys
public static class TodoCacheKeys
{
    private const string Feature = "todos";
    public static ICacheKey ById(Id id) => new CacheKey(Feature, id.ToString());
    public static string FeatureTag => Feature;
}
```

### Example 2: Command with Invalidation

```csharp
// Command
public sealed record UpdateTodoCommand(Id TodoId, string Title)
    : ICommand<Unit>, ICacheInvalidating
{
    // Invalidate entire "todos" feature
    public IReadOnlyCollection<string>? FeaturesToInvalidate => [TodoCacheKeys.FeatureTag];
}

// Handler (unchanged)
internal sealed class UpdateTodoCommandHandler(IApplicationDbContext dbContext)
    : ICommandHandler<UpdateTodoCommand, Unit>
{
    public async Task<Result<Unit>> Handle(
        UpdateTodoCommand request,
        CancellationToken cancellationToken)
    {
        Todo? todo = await dbContext.Todos
            .FirstOrDefaultAsync(t => t.Id == request.TodoId, cancellationToken);

        if (todo is null)
        {
            return Result.Failure(TodoErrors.NotFound(request.TodoId));
        }

        Result<TodoTitle> titleResult = TodoTitle.Create(request.Title);
        if (titleResult.IsFailure)
        {
            return Result<Unit>.Failure(titleResult.Error);
        }

        todo.UpdateTitle(titleResult.Value);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result<Unit>.Success(Unit.Value);
    }
}
```

### Example 3: Query with Compression

```csharp
// Query for large list
public sealed record GetAllTodosQuery : IQuery<List<TodoDto>>, ICacheable
{
    public ICacheKey CacheKey => TodoCacheKeys.List();
    public TimeSpan CacheDuration => TimeSpan.FromMinutes(10);

    // Enable compression for large list
    public bool UseCompression => true;
}

// Handler
internal sealed class GetAllTodosQueryHandler(IApplicationDbContext dbContext)
    : IQueryHandler<GetAllTodosQuery, List<TodoDto>>
{
    public async Task<Result<List<TodoDto>>> Handle(
        GetAllTodosQuery request,
        CancellationToken cancellationToken)
    {
        List<TodoDto> todos = await dbContext.Todos
            .AsNoTracking()
            .Select(t => new TodoDto(
                t.Id.ToString(),
                t.Title.Value,
                t.Status.ToString(),
                t.Created))
            .ToListAsync(cancellationToken);

        return todos;
    }
}
```

### Example 4: Targeted Invalidation

```csharp
// Command with precise invalidation
public sealed record CompleteTodoCommand(Id TodoId)
    : ICommand<Unit>, ICacheInvalidating
{
    // Invalidate only this todo and the list
    public IReadOnlyCollection<ICacheKey>? KeysToInvalidate =>
    [
        TodoCacheKeys.ById(TodoId),
        TodoCacheKeys.List()
    ];
}
```

## References

### Key Files

- [CachingBehavior.cs](../src/Application/Abstractions/Behaviors/CachingBehavior.cs) - Caching pipeline
- [CacheInvalidationBehavior.cs](../src/Application/Abstractions/Behaviors/CacheInvalidationBehavior.cs) - Invalidation pipeline
- [ICacheable.cs](../src/Application/Abstractions/Caching/ICacheable.cs) - Interface for queries
- [ICacheInvalidating.cs](../src/Application/Abstractions/Caching/ICacheInvalidating.cs) - Interface for commands
- [DistributedCacheService.cs](../src/Infrastructure/Caching/DistributedCacheService.cs) - Redis implementation
- [CacheOptions.cs](../src/Application/Abstractions/Caching/CacheOptions.cs) - Configuration

### External Documentation

- [Redis Caching Best Practices](https://redis.io/docs/manual/patterns/caching/)
- [OpenTelemetry Metrics](https://opentelemetry.io/docs/reference/specification/metrics/)
- [MessagePack for C#](https://github.com/MessagePack-CSharp/MessagePack-CSharp)

---

For questions or suggestions for improvement, see the [contribution guide](../CONTRIBUTING.md).
