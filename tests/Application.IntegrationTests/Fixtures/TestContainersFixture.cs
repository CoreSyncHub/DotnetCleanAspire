using Testcontainers.PostgreSql;

namespace Application.IntegrationTests.Fixtures;

/// <summary>
/// Manages the PostgreSQL container lifecycle.
/// Shared across all tests in ApplicationTestCollection via ICollectionFixture.
/// Migrations are applied once here — not repeated per test class.
/// </summary>
public sealed class TestContainersFixture : IAsyncLifetime
{
    private PostgreSqlContainer? _postgresContainer;

    public string PostgresConnectionString { get; private set; } = string.Empty;

    public async Task InitializeAsync()
    {
        _postgresContainer = new PostgreSqlBuilder("postgres:16-alpine")
            .WithDatabase("testdb")
            .WithUsername("testuser")
            .WithPassword("testpassword")
            .Build();

        await _postgresContainer.StartAsync();

        PostgresConnectionString = _postgresContainer.GetConnectionString();

        // Apply migrations once for the entire collection
        using var factory = new TestsWebApplicationFactory(PostgresConnectionString);
        await factory.EnsureDatabaseCreatedAsync();
    }

    public async Task DisposeAsync()
    {
        if (_postgresContainer is not null)
            await _postgresContainer.DisposeAsync();
    }
}
