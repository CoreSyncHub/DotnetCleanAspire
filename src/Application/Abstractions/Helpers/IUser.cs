namespace Application.Abstractions.Helpers;

/// <summary>
/// Provides access to the current authenticated user's identity.
/// </summary>
/// <remarks>
/// This interface abstracts the user context from the HTTP layer,
/// allowing application services to access user information without
/// depending on ASP.NET Core infrastructure.
/// </remarks>
public interface IUser
{
    /// <summary>
    /// Gets the unique identifier of the authenticated user.
    /// </summary>
    Id? Id { get; }

    /// <summary>
    /// Gets the email address of the authenticated user.
    /// </summary>
    string? Email { get; }

    /// <summary>
    /// Gets a value indicating whether the current user is authenticated.
    /// </summary>
    bool IsAuthenticated { get; }

    /// <summary>
    /// Gets the roles assigned to the current user.
    /// </summary>
    IReadOnlyList<string> GetRoles();

    /// <summary>
    /// Checks if the current user is in the specified role.
    /// </summary>
    bool IsInRole(string role);
}
