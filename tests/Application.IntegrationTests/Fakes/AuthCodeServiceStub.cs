using System.Collections.Concurrent;

namespace Application.IntegrationTests.Fakes;

/// <summary>
/// In-memory stub for IAuthCodeService.
/// Simulates the OIDC code exchange without an external provider.
/// </summary>
internal sealed class AuthCodeServiceStub : IAuthCodeService
{
    private readonly ConcurrentDictionary<string, AuthTokensDto> _codes = new();

    public Task<string> CreateCodeAsync(
        AuthTokensDto tokens,
        CancellationToken cancellationToken = default)
    {
        string code = Guid.NewGuid().ToString("N");
        _codes[code] = tokens;
        return Task.FromResult(code);
    }

    public Task<AuthTokensDto?> ExchangeCodeAsync(
        string code,
        CancellationToken cancellationToken = default)
    {
        _codes.TryRemove(code, out AuthTokensDto? tokens);
        return Task.FromResult(tokens);
    }
}
