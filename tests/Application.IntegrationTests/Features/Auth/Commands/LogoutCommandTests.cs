namespace Application.IntegrationTests.Features.Auth.Commands;

public sealed class LogoutCommandTests(TestContainersFixture fixture)
    : ApplicationIntegrationTestBase(fixture)
{
    [Fact]
    public async Task Handle_WithRefreshToken_ShouldRevokeTokenAndReturnSuccess()
    {
        // Arrange
        AuthTokensDto tokens = await RegisterUserAsync();
        var command = new LogoutCommand(tokens.RefreshToken);

        // Act
        Result<Unit> result = await Dispatcher.Send(command);

        // Assert — logout always succeeds
        result.IsSuccess.ShouldBeTrue();

        // Assert — the revoked token can no longer be used for refresh
        Result<AuthTokensDto> refreshResult = await Dispatcher.Send(
            new RefreshTokenCommand(tokens.AccessToken, tokens.RefreshToken));
        refreshResult.IsFailure.ShouldBeTrue();
    }

    [Fact]
    public async Task Handle_WithNoToken_ShouldRevokeAllUserTokensAndReturnSuccess()
    {
        // Arrange — register user and set as current user
        AuthTokensDto tokens = await RegisterUserAsync("logout@example.com");

        ApplicationUser? user = await DbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email == "logout@example.com");

        CurrentUser.Id = user!.Id;

        var command = new LogoutCommand(); // No specific token — revokes all for current user

        // Act
        Result<Unit> result = await Dispatcher.Send(command);

        // Assert
        result.IsSuccess.ShouldBeTrue();

        // Assert — token is revoked
        Result<AuthTokensDto> refreshResult = await Dispatcher.Send(
            new RefreshTokenCommand(tokens.AccessToken, tokens.RefreshToken));
        refreshResult.IsFailure.ShouldBeTrue();
    }
}
