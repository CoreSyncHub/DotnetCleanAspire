namespace Presentation.Abstractions;

/// <summary>
/// Defines a contract for endpoint registration.
/// Implement this interface to create feature-specific endpoints that will be automatically discovered and mapped.
/// </summary>
internal interface IEndpoint
{
   /// <summary>
   /// Maps the endpoint routes to the application.
   /// </summary>
   /// <param name="app">The endpoint route builder.</param>
   /// <param name="versions">The API version set for versioning support.</param>
   void MapEndpoint(IEndpointRouteBuilder app, ApiVersionSet versions);
}
