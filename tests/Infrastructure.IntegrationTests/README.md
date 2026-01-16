# Infrastructure Integration Tests

This project contains integration tests that validate infrastructure components with real dependencies (Redis, PostgreSQL, etc.) using Testcontainers.

## Prerequisites

**Docker must be running** to execute these tests. The tests use [Testcontainers](https://dotnet.testcontainers.org/) to automatically spin up real Redis and PostgreSQL containers.

## Running the Tests

### With Docker Running

```bash
dotnet test
```

### Without Docker

If Docker is not available, run only unit tests:

```bash
# Run all tests except integration tests
dotnet test --filter "FullyQualifiedName!~Infrastructure.IntegrationTests"
```

## Architecture

### Test Collection

All integration tests use `[Collection(nameof(IntegrationTestCollection))]` to share a single `TestsWebApplicationFactory` instance across all tests. This significantly improves performance by:

- Starting containers only once (not per test)
- Reusing the same WebApplicationFactory for all tests
- Reducing test execution time

### TestsWebApplicationFactory

The `TestsWebApplicationFactory` extends `WebApplicationFactory<Program>` and:

- Starts a Redis container using Testcontainers
- Configures the application to use the test Redis instance
- Will support PostgreSQL container in the future (see TODOs in code)
- Implements `IAsyncLifetime` for proper container lifecycle management

### Test Structure

```csharp
[Collection(nameof(IntegrationTestCollection))]
public sealed class MyCacheTests
{
    private readonly TestsWebApplicationFactory _factory;

    public MyCacheTests(TestsWebApplicationFactory factory)
    {
        _factory = factory;

        // Get services from DI container
        var cacheService = factory.Services.GetRequiredService<ICacheService>();
    }
}
```

## Current Tests

### CacheTracing.Tests.cs

Tests OpenTelemetry activity tracing for cache operations:

- ✅ Activity creation for cache.get operations
- ✅ Activity tags (feature, cache key)
- ✅ Cache miss scenarios
- ✅ Cache set operations with compression tags
- ✅ Cache remove operations
- ✅ Feature-based invalidation
- ✅ Error handling and activity error status
- ✅ TraceId propagation

## Performance

With the shared `TestsWebApplicationFactory`:

- **Cold start**: ~2-5 seconds (container startup)
- **Per test**: ~10-50ms (no container overhead)
- **Total savings**: Significant for large test suites (containers start once, not per test class)

## Future Enhancements

See TODOs in `TestsWebApplicationFactory.cs`:

- [ ] Add PostgreSQL container support
- [ ] Add database seeding for integration tests
- [ ] Add test data cleanup between tests
