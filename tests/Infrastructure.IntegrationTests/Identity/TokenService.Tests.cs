using Application.Abstractions.Identity;
using Application.Abstractions.Identity.Dtos;
using Domain.Abstractions;
using Infrastructure.Identity.Entities;
using Infrastructure.IntegrationTests.Fixtures;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Infrastructure.IntegrationTests.Identity;

[Collection(nameof(IntegrationTestCollection))]
public sealed class TokenServiceTests(TestContainersFixture containersFixture) : IAsyncLifetime, IDisposable
{
    private TestsWebApplicationFactory? _factory;
    private IServiceScope? _scope;
    private ITokenService _tokenService = null!;
    // UserManager is disposed via IServiceScope disposal
#pragma warning disable CA2213
    private UserManager<ApplicationUser> _userManager = null!;
#pragma warning restore CA2213

    public async Task InitializeAsync()
    {
        _factory = new TestsWebApplicationFactory(
            containersFixture.RedisConnectionString,
            containersFixture.PostgresConnectionString);

        await _factory.EnsureDatabaseCreatedAsync();

        _scope = _factory.Services.CreateScope();
        _tokenService = _scope.ServiceProvider.GetRequiredService<ITokenService>();
        _userManager = _scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _scope?.Dispose();
        _factory?.Dispose();
    }

    #region GenerateTokensAsync tests

    [Fact]
    public async Task GenerateTokensAsync_ShouldReturnValidTokens()
    {
        // Arrange
        ApplicationUser user = await CreateTestUserAsync("validtokens@example.com");

        // Act
        AuthTokensDto tokens = await _tokenService.GenerateTokensAsync(user.Id, user.Email!, ["User", "Admin"]);

        // Assert
        tokens.AccessToken.ShouldNotBeNullOrEmpty();
        tokens.RefreshToken.ShouldNotBeNullOrEmpty();
        tokens.ExpiresAt.ShouldBeGreaterThan(DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task GenerateTokensAsync_AccessToken_ShouldContainCorrectClaims()
    {
        // Arrange
        ApplicationUser user = await CreateTestUserAsync("claims@example.com");
        string[] roles = ["Admin"];

        // Act
        AuthTokensDto tokens = await _tokenService.GenerateTokensAsync(user.Id, user.Email!, roles);

        // Assert
        JwtSecurityTokenHandler handler = new();
        JwtSecurityToken jwt = handler.ReadJwtToken(tokens.AccessToken);

        jwt.Claims.First(c => c.Type == ClaimTypes.NameIdentifier).Value.ShouldBe(user.Id.ToString());
        jwt.Claims.First(c => c.Type == ClaimTypes.Email).Value.ShouldBe(user.Email);
        jwt.Claims.Any(c => c.Type == ClaimTypes.Role && c.Value == "Admin").ShouldBeTrue();
    }

    [Fact]
    public async Task GenerateTokensAsync_ShouldStoreRefreshTokenInDatabase()
    {
        // Arrange
        ApplicationUser user = await CreateTestUserAsync("stored@example.com");

        // Act
        AuthTokensDto tokens = await _tokenService.GenerateTokensAsync(user.Id, user.Email!, []);

        // Assert - Verify refresh token is stored by trying to use it
        Result<AuthTokensDto> refreshResult = await _tokenService.RefreshTokensAsync(
            tokens.AccessToken,
            tokens.RefreshToken);

        refreshResult.IsSuccess.ShouldBeTrue();
    }

    #endregion

    #region RefreshTokensAsync tests

    [Fact]
    public async Task RefreshTokensAsync_WithValidTokens_ShouldReturnNewTokens()
    {
        // Arrange
        ApplicationUser user = await CreateTestUserAsync("refresh@example.com");
        AuthTokensDto originalTokens = await _tokenService.GenerateTokensAsync(user.Id, user.Email!, ["User"]);

        // Act
        Result<AuthTokensDto> result = await _tokenService.RefreshTokensAsync(
            originalTokens.AccessToken,
            originalTokens.RefreshToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.AccessToken.ShouldNotBeNullOrEmpty();
        result.Value.AccessToken.ShouldNotBe(originalTokens.AccessToken);
    }

    [Fact]
    public async Task RefreshTokensAsync_WithInvalidRefreshToken_ShouldReturnError()
    {
        // Arrange
        ApplicationUser user = await CreateTestUserAsync("invalidrefresh@example.com");
        AuthTokensDto tokens = await _tokenService.GenerateTokensAsync(user.Id, user.Email!, []);

        // Act
        Result<AuthTokensDto> result = await _tokenService.RefreshTokensAsync(
            tokens.AccessToken,
            "invalid-refresh-token");

        // Assert
        result.IsFailure.ShouldBeTrue();
    }

    [Fact]
    public async Task RefreshTokensAsync_WithInvalidAccessToken_ShouldReturnError()
    {
        // Arrange
        ApplicationUser user = await CreateTestUserAsync("invalidaccess@example.com");
        AuthTokensDto tokens = await _tokenService.GenerateTokensAsync(user.Id, user.Email!, []);

        // Act
        Result<AuthTokensDto> result = await _tokenService.RefreshTokensAsync(
            "invalid-access-token",
            tokens.RefreshToken);

        // Assert
        result.IsFailure.ShouldBeTrue();
    }

    [Fact]
    public async Task RefreshTokensAsync_AfterRevocation_ShouldReturnError()
    {
        // Arrange
        ApplicationUser user = await CreateTestUserAsync("revoked@example.com");
        AuthTokensDto tokens = await _tokenService.GenerateTokensAsync(user.Id, user.Email!, []);

        // Revoke the token
        await _tokenService.RevokeRefreshTokenAsync(tokens.RefreshToken);

        // Act
        Result<AuthTokensDto> result = await _tokenService.RefreshTokensAsync(
            tokens.AccessToken,
            tokens.RefreshToken);

        // Assert
        result.IsFailure.ShouldBeTrue();
    }

    [Fact]
    public async Task RefreshTokensAsync_WithMismatchedUserId_ShouldReturnError()
    {
        // Arrange - Create tokens for two different users
        ApplicationUser user1 = await CreateTestUserAsync("user1@example.com");
        ApplicationUser user2 = await CreateTestUserAsync("user2@example.com");

        AuthTokensDto tokens1 = await _tokenService.GenerateTokensAsync(user1.Id, user1.Email!, []);
        AuthTokensDto tokens2 = await _tokenService.GenerateTokensAsync(user2.Id, user2.Email!, []);

        // Act - Try to refresh with mismatched tokens (access from user1, refresh from user2)
        Result<AuthTokensDto> result = await _tokenService.RefreshTokensAsync(
            tokens1.AccessToken,
            tokens2.RefreshToken);

        // Assert
        result.IsFailure.ShouldBeTrue();
    }

    #endregion

    #region RevokeRefreshTokenAsync tests

    [Fact]
    public async Task RevokeRefreshTokenAsync_WithValidToken_ShouldSucceed()
    {
        // Arrange
        ApplicationUser user = await CreateTestUserAsync("revoke@example.com");
        AuthTokensDto tokens = await _tokenService.GenerateTokensAsync(user.Id, user.Email!, []);

        // Act
        Result<Unit> result = await _tokenService.RevokeRefreshTokenAsync(tokens.RefreshToken);

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task RevokeRefreshTokenAsync_WithInvalidToken_ShouldReturnError()
    {
        // Act
        Result<Unit> result = await _tokenService.RevokeRefreshTokenAsync("nonexistent-token");

        // Assert
        result.IsFailure.ShouldBeTrue();
    }

    [Fact]
    public async Task RevokeRefreshTokenAsync_TokenShouldNotBeUsableAfterRevocation()
    {
        // Arrange
        ApplicationUser user = await CreateTestUserAsync("unusable@example.com");
        AuthTokensDto tokens = await _tokenService.GenerateTokensAsync(user.Id, user.Email!, []);

        // Act
        await _tokenService.RevokeRefreshTokenAsync(tokens.RefreshToken);
        Result<AuthTokensDto> refreshResult = await _tokenService.RefreshTokensAsync(
            tokens.AccessToken,
            tokens.RefreshToken);

        // Assert
        refreshResult.IsFailure.ShouldBeTrue();
    }

    #endregion

    #region RevokeAllUserTokensAsync tests

    [Fact]
    public async Task RevokeAllUserTokensAsync_ShouldRevokeAllTokensForUser()
    {
        // Arrange
        ApplicationUser user = await CreateTestUserAsync("revokeall@example.com");
        AuthTokensDto tokens1 = await _tokenService.GenerateTokensAsync(user.Id, user.Email!, []);
        AuthTokensDto tokens2 = await _tokenService.GenerateTokensAsync(user.Id, user.Email!, []);
        AuthTokensDto tokens3 = await _tokenService.GenerateTokensAsync(user.Id, user.Email!, []);

        // Act
        Result<Unit> result = await _tokenService.RevokeAllUserTokensAsync(user.Id);

        // Assert
        result.IsSuccess.ShouldBeTrue();

        // All tokens should be revoked
        (await _tokenService.RefreshTokensAsync(tokens1.AccessToken, tokens1.RefreshToken)).IsFailure.ShouldBeTrue();
        (await _tokenService.RefreshTokensAsync(tokens2.AccessToken, tokens2.RefreshToken)).IsFailure.ShouldBeTrue();
        (await _tokenService.RefreshTokensAsync(tokens3.AccessToken, tokens3.RefreshToken)).IsFailure.ShouldBeTrue();
    }

    [Fact]
    public async Task RevokeAllUserTokensAsync_ShouldNotAffectOtherUsers()
    {
        // Arrange
        ApplicationUser user1 = await CreateTestUserAsync("other1@example.com");
        ApplicationUser user2 = await CreateTestUserAsync("other2@example.com");

        AuthTokensDto tokens1 = await _tokenService.GenerateTokensAsync(user1.Id, user1.Email!, []);
        AuthTokensDto tokens2 = await _tokenService.GenerateTokensAsync(user2.Id, user2.Email!, []);

        // Act - Revoke all tokens for user1 only
        await _tokenService.RevokeAllUserTokensAsync(user1.Id);

        // Assert - User2's token should still work
        Result<AuthTokensDto> refreshResult = await _tokenService.RefreshTokensAsync(
            tokens2.AccessToken,
            tokens2.RefreshToken);

        refreshResult.IsSuccess.ShouldBeTrue();
    }

    #endregion

    #region Token rotation tests

    [Fact]
    public async Task RefreshTokensAsync_WithRotationEnabled_ShouldInvalidateOldRefreshToken()
    {
        // Arrange
        ApplicationUser user = await CreateTestUserAsync("rotation@example.com");
        AuthTokensDto originalTokens = await _tokenService.GenerateTokensAsync(user.Id, user.Email!, []);

        // Act - First refresh (should succeed and rotate token)
        Result<AuthTokensDto> firstRefresh = await _tokenService.RefreshTokensAsync(
            originalTokens.AccessToken,
            originalTokens.RefreshToken);

        // Second refresh with old token (should fail due to rotation)
        Result<AuthTokensDto> secondRefresh = await _tokenService.RefreshTokensAsync(
            originalTokens.AccessToken,
            originalTokens.RefreshToken);

        // Assert
        firstRefresh.IsSuccess.ShouldBeTrue();
        secondRefresh.IsFailure.ShouldBeTrue();
    }

    [Fact]
    public async Task RefreshTokensAsync_WithRotationEnabled_NewRefreshTokenShouldWork()
    {
        // Arrange
        ApplicationUser user = await CreateTestUserAsync("newtoken@example.com");
        AuthTokensDto originalTokens = await _tokenService.GenerateTokensAsync(user.Id, user.Email!, []);

        // Act - First refresh to get new tokens
        Result<AuthTokensDto> firstRefresh = await _tokenService.RefreshTokensAsync(
            originalTokens.AccessToken,
            originalTokens.RefreshToken);

        // Second refresh with new tokens
        Result<AuthTokensDto> secondRefresh = await _tokenService.RefreshTokensAsync(
            firstRefresh.Value.AccessToken,
            firstRefresh.Value.RefreshToken);

        // Assert
        firstRefresh.IsSuccess.ShouldBeTrue();
        secondRefresh.IsSuccess.ShouldBeTrue();
    }

    #endregion

    #region Test helpers

    private async Task<ApplicationUser> CreateTestUserAsync(string email)
    {
        ApplicationUser user = new()
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true
        };

        IdentityResult result = await _userManager.CreateAsync(user, "Test123!");
        result.Succeeded.ShouldBeTrue($"Failed to create test user: {string.Join(", ", result.Errors.Select(e => e.Description))}");

        return user;
    }

    #endregion
}
