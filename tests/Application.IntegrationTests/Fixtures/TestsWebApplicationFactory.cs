using Application.IntegrationTests.Fakes;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Npgsql;

namespace Application.IntegrationTests.Fixtures;

/// <summary>
/// WebApplicationFactory configured for Application integration tests.
/// Substitutes: ICacheService (NullCacheService), IAuthCodeService (stub), IUser (TestUser).
/// Replaces the PostgreSQL connection with the TestContainers instance.
/// Redis is kept as a dummy string — it is never actually used since ICacheService is replaced.
/// </summary>
public sealed class TestsWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _postgresConnectionString;

    public TestsWebApplicationFactory(string postgresConnectionString)
    {
        _postgresConnectionString = postgresConnectionString;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        // Inject connection strings before any Aspire configuration runs
        builder.UseSetting("ConnectionStrings:cleanaspire-db", _postgresConnectionString);
        builder.UseSetting("ConnectionStrings:redis", "localhost:6379"); // dummy — never used

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:cleanaspire-db"] = _postgresConnectionString,
                ["ConnectionStrings:redis"] = "localhost:6379",
            });
        });

        base.ConfigureWebHost(builder);

        builder.ConfigureTestServices(services =>
        {
            // 1. Replace IUser with controllable TestUser (scoped — same instance within a test)
            services.RemoveAll<IUser>();
            services.AddScoped<TestUser>();
            services.AddScoped<IUser>(sp => sp.GetRequiredService<TestUser>());

            // 2. Replace ICacheService with no-op (cache behavior runs but does nothing)
            services.RemoveAll<ICacheService>();
            services.AddSingleton<ICacheService, NullCacheService>();

            // 3. Replace IAuthCodeService with in-memory stub
            services.RemoveAll<IAuthCodeService>();
            services.AddSingleton<IAuthCodeService, AuthCodeServiceStub>();

            // 4. Replace the PostgreSQL connection with the TestContainers instance
            services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
            services.RemoveAll<ApplicationDbContext>();
            services.RemoveAll<NpgsqlDataSource>();

            services.AddNpgsqlDataSource(_postgresConnectionString);
            services.AddDbContext<ApplicationDbContext>((sp, options) =>
            {
                NpgsqlDataSource dataSource = sp.GetRequiredService<NpgsqlDataSource>();
                options.UseNpgsql(dataSource);
            });
        });
    }

    /// <summary>
    /// Applies EF Core migrations to the test database.
    /// Call this once per container lifecycle (in TestContainersFixture).
    /// </summary>
    public async Task EnsureDatabaseCreatedAsync()
    {
        using IServiceScope scope = Services.CreateScope();
        ApplicationDbContext dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await dbContext.Database.MigrateAsync();
    }
}
