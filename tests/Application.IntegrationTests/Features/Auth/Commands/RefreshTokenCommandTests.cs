using Application.DependencyInjection.Options;
using Microsoft.Extensions.Options;

namespace Application.IntegrationTests.Features.Auth.Commands;

public sealed class RefreshTokenCommandTests(TestContainersFixture fixture) : ApplicationIntegrationTestBase(fixture)
{
    private IOptions<TokenLifetimeOptions> TokenLifetimeOptions => GetService<IOptions<TokenLifetimeOptions>>();

    [Fact]
    public async Task Handle_WithValidTokens_ShouldReturnNewTokens()
    {
        // Arrange
        AuthTokensDto original = await RegisterUserAsync();

        var command = new RefreshTokenCommand(original.AccessToken, original.RefreshToken);

        // Act
        Result<AuthTokensDto> result = await Dispatcher.Send(command);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        // Rotation is enabled by default — new tokens are different from the original ones
        result.Value.RefreshToken.ShouldNotBe(original.RefreshToken);
        result.Value.AccessToken.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public async Task Handle_WithInvalidRefreshToken_ShouldReturnError()
    {
        // Arrange
        AuthTokensDto original = await RegisterUserAsync();

        var command = new RefreshTokenCommand(original.AccessToken, "invalid-refresh-token");

        // Act
        Result<AuthTokensDto> result = await Dispatcher.Send(command);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe(AuthErrors.Codes.InvalidRefreshToken);
    }

    [Fact]
    public async Task Handle_WithAlreadyUsedRefreshToken_ShouldReturnError()
    {
        // Arrange — use the token once, then try again
        AuthTokensDto original = await RegisterUserAsync();
        await Dispatcher.Send(new RefreshTokenCommand(original.AccessToken, original.RefreshToken));

        // Act — replay the original (now revoked) refresh token
        // For race condition, taken has grace period, we need to wait until it's fully revoked before retrying
        await Task.Delay(TokenLifetimeOptions.Value.RefreshTokenRotationGracePeriod + TimeSpan.FromSeconds(1));
        Result<AuthTokensDto> result = await Dispatcher.Send(
            new RefreshTokenCommand(original.AccessToken, original.RefreshToken));

        // Assert — rotation invalidates the old token
        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe(AuthErrors.Codes.InvalidRefreshToken);
    }
}
