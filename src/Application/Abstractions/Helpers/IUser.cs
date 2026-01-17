namespace Application.Abstractions.Helpers;

/// <summary>
/// Provides access to the current authenticated user's identity.
/// </summary>
/// <remarks>
/// This interface abstracts the user context from the HTTP layer,
/// allowing application services to access user information without
/// depending on ASP.NET Core infrastructure.
/// <para>
/// The implementation typically extracts claims from the current
/// <see cref="System.Security.Claims.ClaimsPrincipal"/> via HttpContext.
/// </para>
/// </remarks>
public interface IUser
{
   /// <summary>
   /// Gets the unique identifier of the authenticated user.
   /// </summary>
   /// <value>
   /// The user's unique identifier (typically from the "sub" or "NameIdentifier" claim),
   /// or <c>null</c> if the user is not authenticated.
   /// </value>
   string? Id { get; }
}
