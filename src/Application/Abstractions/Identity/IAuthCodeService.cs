using Application.Abstractions.Identity.Dtos;

namespace Application.Abstractions.Identity;

/// <summary>
/// Service for creating and exchanging temporary authorization codes.
/// Used to securely pass tokens after OIDC authentication without exposing them in URLs.
/// </summary>
public interface IAuthCodeService
{
    /// <summary>
    /// Creates a temporary, single-use authorization code that can be exchanged for tokens.
    /// </summary>
    /// <param name="tokens">The authentication tokens to store.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A secure, random authorization code.</returns>
    Task<string> CreateCodeAsync(AuthTokensDto tokens, CancellationToken cancellationToken = default);

    /// <summary>
    /// Exchanges an authorization code for tokens. The code is invalidated after use.
    /// </summary>
    /// <param name="code">The authorization code to exchange.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The tokens if the code is valid, null otherwise.</returns>
    Task<AuthTokensDto?> ExchangeCodeAsync(string code, CancellationToken cancellationToken = default);
}
