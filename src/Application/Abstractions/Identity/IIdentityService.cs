using Application.Abstractions.Identity.Dtos;
using Domain.Abstractions;

namespace Application.Abstractions.Identity;

public interface IIdentityService
{
    /// <summary>
    /// Creates a new user with the specified email and password.
    /// </summary>
    /// <param name="email">The email of the new user.</param>
    /// <param name="password">The password for the new user.</param>
    /// <param name="cancellationToken"></param>
    /// <returns>A result containing the user ID if successful.</returns>
    Task<Result<Id>> CreateUserAsync(string email, string password, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates the user's credentials.
    /// </summary>
    /// <param name="email">The user's email.</param>
    /// <param name="password">The user's password.</param>
    /// <param name="cancellationToken"></param>
    /// <returns>A result indicating whether the credentials are valid.</returns>
    Task<Result<Unit>> ValidateCredentialsAsync(string email, string password, CancellationToken cancellationToken = default);

    /// <summary>
    /// Confirms the user's email using the provided token.
    /// </summary>
    /// <param name="userId">The ID of the user.</param>
    /// <param name="token">The email confirmation token.</param>
    /// <param name="cancellationToken"></param>
    /// <returns>A result indicating success or failure.</returns>
    Task<Result<Unit>> ConfirmEmailAsync(Id userId, string token, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a password reset token for the user with the specified email.
    /// </summary>
    /// <param name="email">The user's email.</param>
    /// <param name="cancellationToken"></param>
    /// <returns>A result containing the password reset token.</returns>
    Task<Result<string>> GeneratePasswordResetTokenAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resets the user's password using the provided token and new password.
    /// </summary>
    /// <param name="email">The user's email.</param>
    /// <param name="token">The password reset token.</param>
    /// <param name="newPassword">The new password.</param>
    /// <param name="cancellationToken"></param>
    /// <returns>A result indicating success or failure.</returns>
    Task<Result<Unit>> ResetPasswordAsync(string email, string token, string newPassword, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds the user to the specified role.
    /// </summary>
    /// <param name="userId">The ID of the user.</param>
    /// <param name="role">The role to add the user to.</param>
    /// <param name="cancellationToken"></param>
    /// <returns>A result indicating success or failure.</returns>
    Task<Result<Unit>> AddToRoleAsync(Id userId, string role, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if the user is in the specified role.
    /// </summary>
    /// <param name="userId">The ID of the user.</param>
    /// <param name="role">The role to check.</param>
    /// <param name="cancellationToken"></param>
    /// <returns>True if the user is in the role; otherwise, false.</returns>
    Task<bool> IsInRoleAsync(Id userId, string role, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the roles of the specified user.
    /// </summary>
    /// <param name="userId">The ID of the user.</param>
    /// <param name="cancellationToken"></param>
    /// <returns>A read-only list of roles.</returns>
    Task<IReadOnlyList<string>> GetRolesAsync(Id userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the user by their ID.
    /// </summary>
    /// <param name="userId">The ID of the user.</param>
    /// <param name="cancellationToken"></param>
    /// <returns>The user DTO if found; otherwise, null.</returns>
    Task<UserDto?> GetUserByIdAsync(Id userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the user by their email.
    /// </summary>
    /// <param name="email">The email of the user.</param>
    /// <param name="cancellationToken"></param>
    /// <returns>The user DTO if found; otherwise, null.</returns>
    Task<UserDto?> GetUserByEmailAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets or creates an external user based on the provided login information.
    /// </summary>
    /// <param name="loginInfo">The external login information.</param>
    /// <param name="cancellationToken"></param>
    /// <returns>A result containing the user DTO.</returns>
    Task<Result<UserDto>> GetOrCreateExternalUserAsync(ExternalLoginInfo loginInfo, CancellationToken cancellationToken = default);

    /// <summary>
    /// Links an external login to the specified user.
    /// </summary>
    /// <param name="userId">The ID of the user.</param>
    /// <param name="provider">The external login provider.</param>
    /// <param name="providerKey">The external login provider key.</param>
    /// <param name="cancellationToken"></param>
    /// <returns>A result indicating success or failure.</returns>
    Task<Result<Unit>> LinkExternalLoginAsync(Id userId, string provider, string providerKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the last login time for the specified user.
    /// </summary>
    /// <param name="userId">The ID of the user.</param>
    /// <param name="cancellationToken"></param>
    /// <returns>A result indicating success or failure.</returns>
    Task<Result<Unit>> UpdateLastLoginAsync(Id userId, CancellationToken cancellationToken = default);
}
