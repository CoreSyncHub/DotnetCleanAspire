using Application.Abstractions.Helpers;
using Domain.Abstractions;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Npgsql;

namespace Infrastructure.IntegrationTests.Fixtures;

/// <summary>
/// WebApplicationFactory configured with real infrastructure containers (Redis, PostgreSQL, etc.).
/// </summary>
public sealed class TestsWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _redisConnectionString;
    private readonly string _postgresConnectionString;

    public TestsWebApplicationFactory(string redisConnectionString, string postgresConnectionString = "")
    {
        _redisConnectionString = redisConnectionString;
        _postgresConnectionString = postgresConnectionString;
        Console.WriteLine($"[TestsWebApplicationFactory] Created with Redis: {redisConnectionString}");
        if (!string.IsNullOrEmpty(postgresConnectionString))
        {
            Console.WriteLine($"[TestsWebApplicationFactory] Created with PostgreSQL: {postgresConnectionString}");
        }
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        // Set connection strings BEFORE any configuration is built
        builder.UseSetting("ConnectionStrings:redis", _redisConnectionString);
        if (!string.IsNullOrEmpty(_postgresConnectionString))
        {
            builder.UseSetting("ConnectionStrings:cleanaspire-db", _postgresConnectionString);
        }

        builder.ConfigureAppConfiguration((context, config) =>
        {
            Console.WriteLine($"[ConfigureAppConfiguration] Setting redis connection string: {_redisConnectionString}");

            var settings = new Dictionary<string, string?>
            {
                ["ConnectionStrings:redis"] = _redisConnectionString
            };

            if (!string.IsNullOrEmpty(_postgresConnectionString))
            {
                settings["ConnectionStrings:cleanaspire-db"] = _postgresConnectionString;
            }

            config.AddInMemoryCollection(settings);
        });

        base.ConfigureWebHost(builder);

        builder.ConfigureTestServices(services =>
        {
            services
                .RemoveAll<IUser>()
                .AddTransient(provider => Mock.Of<IUser>(u => u.Id == Id.New()));

            // Replace DbContext with Testcontainers PostgreSQL if connection string is provided
            if (!string.IsNullOrEmpty(_postgresConnectionString))
            {
                // Remove existing DbContext registrations
                services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
                services.RemoveAll<ApplicationDbContext>();
                services.RemoveAll<NpgsqlDataSource>();

                // Register new data source with test connection string
                services.AddNpgsqlDataSource(_postgresConnectionString);

                // Re-register DbContext
                services.AddDbContext<ApplicationDbContext>((sp, options) =>
                {
                    NpgsqlDataSource dataSource = sp.GetRequiredService<NpgsqlDataSource>();
                    options.UseNpgsql(dataSource);
                });
            }
        });
    }

    /// <summary>
    /// Ensures the database is created and migrations are applied.
    /// Call this after creating the factory to set up the test database.
    /// </summary>
    public async Task EnsureDatabaseCreatedAsync()
    {
        using IServiceScope scope = Services.CreateScope();
        ApplicationDbContext dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await dbContext.Database.MigrateAsync();
    }
}
