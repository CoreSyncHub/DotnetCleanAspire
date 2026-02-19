using Application.Abstractions.Caching;
using Application.Abstractions.Identity.Dtos;
using Infrastructure.Identity.Services;

namespace Infrastructure.UnitTests.Identity;

public class AuthCodeServiceTests
{
    private readonly Mock<ICacheService> _cacheService;
    private readonly AuthCodeService _authCodeService;

    public AuthCodeServiceTests()
    {
        _cacheService = new Mock<ICacheService>();
        _authCodeService = new AuthCodeService(_cacheService.Object);
    }

    #region CreateCodeAsync tests

    [Fact]
    public async Task CreateCodeAsync_ShouldReturnNonEmptyCode()
    {
        // Arrange
        AuthTokensDto tokens = CreateTestTokens();

        // Act
        string code = await _authCodeService.CreateCodeAsync(tokens);

        // Assert
        code.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public async Task CreateCodeAsync_ShouldGenerateUrlSafeCode()
    {
        // Arrange
        AuthTokensDto tokens = CreateTestTokens();

        // Act
        string code = await _authCodeService.CreateCodeAsync(tokens);

        // Assert - URL-safe means no +, /, or = characters
        code.ShouldNotContain("+");
        code.ShouldNotContain("/");
        code.ShouldNotContain("=");
    }

    [Fact]
    public async Task CreateCodeAsync_ShouldStoreTokensInCache()
    {
        // Arrange
        AuthTokensDto tokens = CreateTestTokens();

        // Act
        string code = await _authCodeService.CreateCodeAsync(tokens);

        // Assert
        _cacheService.Verify(x => x.SetAsync(
            It.Is<ICacheKey>(k => k.Feature == "auth_codes" && k.Value == code),
            It.Is<CacheEntry<AuthTokensDto>>(e => e.HasValue && e.Value == tokens),
            It.Is<TimeSpan?>(t => t == TimeSpan.FromMinutes(1)),
            It.IsAny<string?>(),
            It.Is<bool?>(b => b == false),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateCodeAsync_ShouldGenerateUniqueCodesForEachCall()
    {
        // Arrange
        AuthTokensDto tokens = CreateTestTokens();

        // Act
        string code1 = await _authCodeService.CreateCodeAsync(tokens);
        string code2 = await _authCodeService.CreateCodeAsync(tokens);

        // Assert
        code1.ShouldNotBe(code2);
    }

    [Fact]
    public async Task CreateCodeAsync_ShouldRespectCancellationToken()
    {
        // Arrange
        AuthTokensDto tokens = CreateTestTokens();
        using CancellationTokenSource cts = new();
        CancellationToken cancellationToken = cts.Token;

        // Act
        await _authCodeService.CreateCodeAsync(tokens, cancellationToken);

        // Assert
        _cacheService.Verify(x => x.SetAsync(
            It.IsAny<ICacheKey>(),
            It.IsAny<CacheEntry<AuthTokensDto>>(),
            It.IsAny<TimeSpan?>(),
            It.IsAny<string?>(),
            It.IsAny<bool?>(),
            cancellationToken), Times.Once);
    }

    #endregion

    #region ExchangeCodeAsync tests

    [Fact]
    public async Task ExchangeCodeAsync_WithValidCode_ShouldReturnTokens()
    {
        // Arrange
        const string code = "valid-code";
        AuthTokensDto expectedTokens = CreateTestTokens();

        _cacheService.Setup(x => x.GetAsync<AuthTokensDto>(
            It.Is<ICacheKey>(k => k.Feature == "auth_codes" && k.Value == code),
            It.IsAny<string?>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(CacheEntry<AuthTokensDto>.Create(expectedTokens));

        // Act
        AuthTokensDto? result = await _authCodeService.ExchangeCodeAsync(code);

        // Assert
        result.ShouldNotBeNull();
        result.AccessToken.ShouldBe(expectedTokens.AccessToken);
        result.RefreshToken.ShouldBe(expectedTokens.RefreshToken);
    }

    [Fact]
    public async Task ExchangeCodeAsync_WithValidCode_ShouldRemoveCodeFromCache()
    {
        // Arrange
        const string code = "valid-code";
        AuthTokensDto tokens = CreateTestTokens();

        _cacheService.Setup(x => x.GetAsync<AuthTokensDto>(
            It.IsAny<ICacheKey>(),
            It.IsAny<string?>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(CacheEntry<AuthTokensDto>.Create(tokens));

        // Act
        await _authCodeService.ExchangeCodeAsync(code);

        // Assert
        _cacheService.Verify(x => x.RemoveAsync(
            It.Is<ICacheKey>(k => k.Feature == "auth_codes" && k.Value == code),
            It.IsAny<string?>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExchangeCodeAsync_WithInvalidCode_ShouldReturnNull()
    {
        // Arrange
        const string code = "invalid-code";

        _cacheService.Setup(x => x.GetAsync<AuthTokensDto>(
            It.IsAny<ICacheKey>(),
            It.IsAny<string?>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync((CacheEntry<AuthTokensDto>?)null);

        // Act
        AuthTokensDto? result = await _authCodeService.ExchangeCodeAsync(code);

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public async Task ExchangeCodeAsync_WithInvalidCode_ShouldNotRemoveFromCache()
    {
        // Arrange
        const string code = "invalid-code";

        _cacheService.Setup(x => x.GetAsync<AuthTokensDto>(
            It.IsAny<ICacheKey>(),
            It.IsAny<string?>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync((CacheEntry<AuthTokensDto>?)null);

        // Act
        await _authCodeService.ExchangeCodeAsync(code);

        // Assert
        _cacheService.Verify(x => x.RemoveAsync(
            It.IsAny<ICacheKey>(),
            It.IsAny<string?>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ExchangeCodeAsync_WithEmptyCacheEntry_ShouldReturnNull()
    {
        // Arrange
        const string code = "code-with-empty-entry";

        _cacheService.Setup(x => x.GetAsync<AuthTokensDto>(
            It.IsAny<ICacheKey>(),
            It.IsAny<string?>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(CacheEntry<AuthTokensDto>.Empty());

        // Act
        AuthTokensDto? result = await _authCodeService.ExchangeCodeAsync(code);

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public async Task ExchangeCodeAsync_ShouldRespectCancellationToken()
    {
        // Arrange
        const string code = "test-code";
        using CancellationTokenSource cts = new();
        CancellationToken cancellationToken = cts.Token;

        _cacheService.Setup(x => x.GetAsync<AuthTokensDto>(
            It.IsAny<ICacheKey>(),
            It.IsAny<string?>(),
            cancellationToken))
            .ReturnsAsync((CacheEntry<AuthTokensDto>?)null);

        // Act
        await _authCodeService.ExchangeCodeAsync(code, cancellationToken);

        // Assert
        _cacheService.Verify(x => x.GetAsync<AuthTokensDto>(
            It.IsAny<ICacheKey>(),
            It.IsAny<string?>(),
            cancellationToken), Times.Once);
    }

    #endregion

    #region Single-use code behavior tests

    [Fact]
    public async Task ExchangeCodeAsync_CalledTwice_SecondCallShouldReturnNull()
    {
        // Arrange
        const string code = "single-use-code";
        AuthTokensDto tokens = CreateTestTokens();

        // First call returns tokens, second call returns null (simulating removed entry)
        _cacheService.SetupSequence(x => x.GetAsync<AuthTokensDto>(
            It.Is<ICacheKey>(k => k.Value == code),
            It.IsAny<string?>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(CacheEntry<AuthTokensDto>.Create(tokens))
            .ReturnsAsync((CacheEntry<AuthTokensDto>?)null);

        // Act
        AuthTokensDto? firstResult = await _authCodeService.ExchangeCodeAsync(code);
        AuthTokensDto? secondResult = await _authCodeService.ExchangeCodeAsync(code);

        // Assert
        firstResult.ShouldNotBeNull();
        secondResult.ShouldBeNull();
    }

    #endregion

    #region Test helpers

    private static AuthTokensDto CreateTestTokens()
    {
        return new AuthTokensDto(
            AccessToken: "test-access-token",
            RefreshToken: "test-refresh-token",
            ExpiresAt: DateTimeOffset.UtcNow.AddHours(1));
    }

    #endregion
}
