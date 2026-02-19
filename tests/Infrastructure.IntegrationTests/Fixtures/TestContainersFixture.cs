using Testcontainers.PostgreSql;
using Testcontainers.Redis;

namespace Infrastructure.IntegrationTests.Fixtures;

/// <summary>
/// Fixture that manages the lifecycle of test containers (Redis, PostgreSQL, etc.).
/// Shared across all integration tests via ICollectionFixture for performance.
/// </summary>
public sealed class TestContainersFixture : IAsyncLifetime
{
    private RedisContainer? _redisContainer;
    private PostgreSqlContainer? _postgresContainer;

    public string RedisConnectionString { get; private set; } = string.Empty;
    public string PostgresConnectionString { get; private set; } = string.Empty;

    public async Task InitializeAsync()
    {
        Console.WriteLine("[TestContainersFixture] Starting containers...");

        // Create and start Redis container
        _redisContainer = new RedisBuilder("redis:7-alpine").Build();

        // Create and start PostgreSQL container
        _postgresContainer = new PostgreSqlBuilder("postgres:16-alpine")
            .WithDatabase("testdb")
            .WithUsername("testuser")
            .WithPassword("testpassword")
            .Build();

        await Task.WhenAll(
            _redisContainer.StartAsync(),
            _postgresContainer.StartAsync());

        RedisConnectionString = _redisContainer.GetConnectionString();
        PostgresConnectionString = _postgresContainer.GetConnectionString();

        Console.WriteLine($"[TestContainersFixture] Redis started: {RedisConnectionString}");
        Console.WriteLine($"[TestContainersFixture] PostgreSQL started: {PostgresConnectionString}");
    }

    public async Task DisposeAsync()
    {
        Console.WriteLine("[TestContainersFixture] Disposing containers...");

        List<Task> disposeTasks = [];

        if (_redisContainer != null)
        {
            disposeTasks.Add(_redisContainer.DisposeAsync().AsTask());
        }

        if (_postgresContainer != null)
        {
            disposeTasks.Add(_postgresContainer.DisposeAsync().AsTask());
        }

        await Task.WhenAll(disposeTasks);
    }
}
