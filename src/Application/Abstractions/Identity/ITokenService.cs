using Application.Abstractions.Identity.Dtos;

namespace Application.Abstractions.Identity;

public interface ITokenService
{
    /// <summary>
    /// Generates access and refresh tokens for the specified user ID and roles.
    /// </summary>
    /// <param name="userId">The ID of the user.</param>
    /// <param name="userEmail">The email of the user.</param>
    /// <param name="roles">The roles assigned to the user.</param>
    /// <param name="cancellationToken"></param>
    /// <returns>An <see cref="AuthTokensDto"/> containing the generated tokens.</returns>
    Task<AuthTokensDto> GenerateTokensAsync(Id userId, string userEmail, IEnumerable<string> roles, CancellationToken cancellationToken = default);

    /// <summary>
    /// Refreshes the access and refresh tokens using the provided tokens.
    /// </summary>
    /// <param name="accessToken">The expired access token.</param>
    /// <param name="refreshToken">The valid refresh token.</param>
    /// <param name="cancellationToken"></param>
    /// <returns>A <see cref="AuthTokensDto"/> containing the new tokens if successful; otherwise, an error result.</returns>
    Task<Result<AuthTokensDto>> RefreshTokensAsync(string accessToken, string refreshToken, CancellationToken cancellationToken = default);

    /// <summary>
    /// Revokes the specified refresh token.
    /// </summary>
    /// <param name="refreshToken">The refresh token to revoke.</param>
    /// <param name="cancellationToken"></param>
    /// <returns>A result indicating success or failure.</returns>
    Task<Result<Unit>> RevokeRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default);

    /// <summary>
    /// Revokes all refresh tokens associated with the specified user ID.
    /// </summary>
    /// <param name="userId">The ID of the user whose tokens are to be revoked.</param>
    /// <param name="cancellationToken"></param>
    /// <returns>A result indicating success or failure.</returns>
    Task<Result<Unit>> RevokeAllUserTokensAsync(Id userId, CancellationToken cancellationToken = default);
}
