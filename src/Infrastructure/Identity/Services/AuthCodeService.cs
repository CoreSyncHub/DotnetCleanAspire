using Application.Abstractions.Caching;
using Application.Abstractions.Identity;
using Application.Abstractions.Identity.Dtos;
using System.Security.Cryptography;

namespace Infrastructure.Identity.Services;

internal sealed class AuthCodeService(ICacheService cacheService) : IAuthCodeService
{
    private const string Feature = "auth_codes";
    private static readonly TimeSpan CodeExpiry = TimeSpan.FromMinutes(1);

    public async Task<string> CreateCodeAsync(AuthTokensDto tokens, CancellationToken cancellationToken = default)
    {
        string code = GenerateSecureCode();
        CacheKey key = new(Feature, code);

        await cacheService.SetAsync(
            key,
            CacheEntry<AuthTokensDto>.Create(tokens),
            CodeExpiry,
            useCompression: false,
            cancellationToken: cancellationToken);

        return code;
    }

    public async Task<AuthTokensDto?> ExchangeCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        CacheKey key = new(Feature, code);

        CacheEntry<AuthTokensDto>? entry = await cacheService.GetAsync<AuthTokensDto>(key, cancellationToken: cancellationToken);

        if (entry is null || !entry.HasValue)
        {
            return null;
        }

        // Remove immediately to ensure single use
        await cacheService.RemoveAsync(key, cancellationToken: cancellationToken);

        return entry.Value;
    }

    private static string GenerateSecureCode()
    {
        byte[] bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes)
            .Replace("+", "-", StringComparison.Ordinal)
            .Replace("/", "_", StringComparison.Ordinal)
            .TrimEnd('=');
    }
}
