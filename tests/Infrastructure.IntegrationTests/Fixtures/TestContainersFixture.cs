using Testcontainers.Redis;

namespace Infrastructure.IntegrationTests.Fixtures;

/// <summary>
/// Fixture that manages the lifecycle of test containers (Redis, PostgreSQL, etc.).
/// Shared across all integration tests via ICollectionFixture for performance.
/// </summary>
public sealed class TestContainersFixture : IAsyncLifetime
{
    private RedisContainer? _redisContainer;

    public string RedisConnectionString { get; private set; } = string.Empty;

    public async Task InitializeAsync()
    {
        Console.WriteLine("[TestContainersFixture] Starting containers...");

        // Create and start Redis container
        _redisContainer = new RedisBuilder("redis:7-alpine").Build();
        await _redisContainer.StartAsync();

        RedisConnectionString = _redisContainer.GetConnectionString();
        Console.WriteLine($"[TestContainersFixture] Redis started: {RedisConnectionString}");

        // TODO: Start PostgreSQL container here when needed
    }

    public async Task DisposeAsync()
    {
        Console.WriteLine("[TestContainersFixture] Disposing containers...");

        if (_redisContainer != null)
        {
            await _redisContainer.DisposeAsync();
        }

        // TODO: Dispose PostgreSQL container here when needed
    }
}
