using Presentation.Abstractions;

namespace Presentation.Extensions;

/// <summary>
/// Extensions for automatic endpoint discovery and registration.
/// </summary>
internal static class EndpointExtensions
{
   /// <summary>
   /// Automatically discovers and maps all endpoints implementing <see cref="IEndpoint"/>.
   /// </summary>
   /// <param name="app">The web application.</param>
   /// <param name="versions">The API version set for versioning support.</param>
   /// <returns>The web application for chaining.</returns>
   public static WebApplication MapEndpoints(this WebApplication app, ApiVersionSet versions)
   {
      IEnumerable<IEndpoint> endpoints = DiscoverEndpoints();

      foreach (IEndpoint endpoint in endpoints)
      {
         endpoint.MapEndpoint(app, versions);
      }

      return app;
   }

   private static IEnumerable<IEndpoint> DiscoverEndpoints()
   {
      return Assembly.GetExecutingAssembly()
          .GetTypes()
          .Where(type => typeof(IEndpoint).IsAssignableFrom(type)
                         && type is { IsInterface: false, IsAbstract: false })
          .Select(Activator.CreateInstance)
          .Cast<IEndpoint>();
   }
}
