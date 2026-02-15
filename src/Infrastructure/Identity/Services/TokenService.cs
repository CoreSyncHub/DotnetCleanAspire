using Application.Abstractions.Identity;
using Application.Abstractions.Identity.Dtos;
using Application.DependencyInjection.Options;
using Application.Features.Auth.Errors;
using Infrastructure.DependencyInjection.Options;
using Infrastructure.Identity.Entities;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;

namespace Infrastructure.Identity.Services;

internal sealed class TokenService(
    ApplicationDbContext dbContext,
    IOptions<JwtOptions> jwtOptions,
    IOptions<AuthIdentityOptions> identityOptions,
    IHttpContextAccessor httpContextAccessor) : ITokenService
{
    /// <inheritdoc/>
    public Task<AuthTokensDto> GenerateTokensAsync(
        Id userId,
        string userEmail,
        IEnumerable<string> roles,
        CancellationToken cancellationToken = default)
    {
        return GenerateTokensAsync(userId, userEmail, roles, existingRefreshToken: null, cancellationToken);
    }

    private async Task<AuthTokensDto> GenerateTokensAsync(
        Id userId,
        string userEmail,
        IEnumerable<string> roles,
        string? existingRefreshToken,
        CancellationToken cancellationToken)
    {
        JwtOptions jwt = jwtOptions.Value;
        TokenLifetimeOptions tokenConfig = identityOptions.Value.Tokens;

        DateTimeOffset expiresAt = DateTimeOffset.UtcNow.Add(tokenConfig.AccessTokenLifetime);

        List<Claim> claims =
        [
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Email, userEmail),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        ];

        foreach (string role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        SymmetricSecurityKey key = new(Encoding.UTF8.GetBytes(jwt.Key));
        SigningCredentials credentials = new(key, SecurityAlgorithms.HmacSha256);

        JwtSecurityToken token = new(
            issuer: jwt.Issuer,
            audience: jwt.Audience,
            claims: claims,
            expires: expiresAt.UtcDateTime,
            signingCredentials: credentials);

        string accessToken = new JwtSecurityTokenHandler().WriteToken(token);

        string refreshTokenValue = existingRefreshToken ?? GenerateSecureToken();
        string refreshTokenHash = HashToken(refreshTokenValue);

        RefreshTokenEntity refreshToken = new()
        {
            TokenHash = refreshTokenHash,
            UserId = userId,
            ExpiresAt = DateTimeOffset.UtcNow.Add(tokenConfig.RefreshTokenLifetime),
            CreatedByIp = httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString()
        };

        dbContext.RefreshTokens.Add(refreshToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new AuthTokensDto(accessToken, refreshTokenValue, expiresAt);
    }

    /// <inheritdoc/>
    public async Task<Result<AuthTokensDto>> RefreshTokensAsync(
        string accessToken,
        string refreshToken,
        CancellationToken cancellationToken = default)
    {
        string refreshTokenHash = HashToken(refreshToken);

        RefreshTokenEntity? storedToken = await dbContext.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.TokenHash == refreshTokenHash, cancellationToken);

        if (storedToken is null || !storedToken.IsActive)
        {
            return AuthErrors.InvalidRefreshToken;
        }

        ClaimsPrincipal? principal = GetPrincipalFromExpiredToken(accessToken);
        string? rawUserId = principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        string? rawEmail = principal?.FindFirst(ClaimTypes.Email)?.Value;
        if (rawUserId is null || !Id.TryParse(rawUserId, null, out Id userId) || rawEmail is null)
        {
            return AuthErrors.InvalidToken;
        }

        if (userId != storedToken.UserId)
        {
            return AuthErrors.InvalidRefreshToken;
        }

        IReadOnlyList<string> roles = await GetUserRolesAsync(storedToken.UserId, cancellationToken);

        string? rotatedRefreshToken = null;
        if (identityOptions.Value.Tokens.RefreshTokenRotation)
        {
            rotatedRefreshToken = GenerateSecureToken();
            storedToken.RevokedAt = DateTimeOffset.UtcNow;
            storedToken.ReplacedByTokenHash = HashToken(rotatedRefreshToken);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return await GenerateTokensAsync(
            storedToken.UserId,
            rawEmail,
            roles,
            rotatedRefreshToken,
            cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<Result<Unit>> RevokeRefreshTokenAsync(
        string refreshToken,
        CancellationToken cancellationToken = default)
    {
        string tokenHash = HashToken(refreshToken);

        RefreshTokenEntity? token = await dbContext.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.TokenHash == tokenHash, cancellationToken);

        if (token is null)
        {
            return AuthErrors.InvalidRefreshToken;
        }

        token.RevokedAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    /// <inheritdoc/>
    public async Task<Result<Unit>> RevokeAllUserTokensAsync(
        Id userId,
        CancellationToken cancellationToken = default)
    {
        List<RefreshTokenEntity> tokens = await dbContext.RefreshTokens
            .Where(rt => rt.UserId == userId && rt.RevokedAt == null)
            .ToListAsync(cancellationToken);

        DateTimeOffset now = DateTimeOffset.UtcNow;
        foreach (RefreshTokenEntity token in tokens)
        {
            token.RevokedAt = now;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    private static string GenerateSecureToken()
    {
        byte[] randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }

    private static string HashToken(string token)
    {
        byte[] bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToBase64String(bytes);
    }

    private ClaimsPrincipal? GetPrincipalFromExpiredToken(string token)
    {
        JwtOptions jwt = jwtOptions.Value;

        TokenValidationParameters validationParameters = new()
        {
            ValidateIssuer = true,
            ValidateAudience = true,
#pragma warning disable CA5404  // Lifetime validation is explicitly disabled here to allow extracting claims from expired tokens.
            ValidateLifetime = false,
#pragma warning restore CA5404
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwt.Issuer,
            ValidAudience = jwt.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key))
        };

        JwtSecurityTokenHandler tokenHandler = new();

        try
        {
            return tokenHandler.ValidateToken(token, validationParameters, out _);
        }
        catch
        {
            return null;
        }
    }

    private async Task<IReadOnlyList<string>> GetUserRolesAsync(
        Id userId,
        CancellationToken cancellationToken)
    {
        return await dbContext.UserRoles
            .Where(ur => ur.UserId == userId)
            .Join(dbContext.Roles, ur => ur.RoleId, r => r.Id, (_, r) => r.Name!)
            .ToListAsync(cancellationToken);
    }
}
