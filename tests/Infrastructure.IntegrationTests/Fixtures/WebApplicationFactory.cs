using Application.Abstractions.Helpers;
using Domain.Abstractions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Infrastructure.IntegrationTests.Fixtures;

/// <summary>
/// WebApplicationFactory configured with real infrastructure containers (Redis, PostgreSQL, etc.).
/// </summary>
public sealed class TestsWebApplicationFactory : WebApplicationFactory<Program>
{
   private readonly string _redisConnectionString;

   public TestsWebApplicationFactory(string redisConnectionString)
   {
      _redisConnectionString = redisConnectionString;
      Console.WriteLine($"[TestsWebApplicationFactory] Created with Redis: {redisConnectionString}");
   }

   protected override void ConfigureWebHost(IWebHostBuilder builder)
   {
      builder.UseEnvironment("Testing");

      // Set connection string BEFORE any configuration is built
      builder.UseSetting("ConnectionStrings:redis", _redisConnectionString);

      builder.ConfigureAppConfiguration((context, config) =>
      {
         Console.WriteLine($"[ConfigureAppConfiguration] Setting redis connection string: {_redisConnectionString}");

         config.AddInMemoryCollection(new Dictionary<string, string?>
         {
            ["ConnectionStrings:redis"] = _redisConnectionString
            // TODO: Add PostgreSQL connection string here when needed
         });
      });

      base.ConfigureWebHost(builder);

      builder.ConfigureTestServices(services =>
      {
         services
           .RemoveAll<IUser>()
           .AddTransient(provider => Mock.Of<IUser>(u => u.Id == Id.New()));

         // TODO: Replace DbContext with Testcontainers PostgreSQL here when needed
      });
   }
}
