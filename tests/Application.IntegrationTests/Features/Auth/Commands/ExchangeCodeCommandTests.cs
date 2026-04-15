namespace Application.IntegrationTests.Features.Auth.Commands;

public sealed class ExchangeCodeCommandTests(TestContainersFixture fixture)
    : ApplicationIntegrationTestBase(fixture)
{
    [Fact]
    public async Task Handle_WithValidCode_ShouldReturnTokens()
    {
        // Arrange — create a code via the stub (simulates what OidcCallbackCommand would do)
        var storedTokens = new AuthTokensDto(
            "access-token",
            "refresh-token",
            DateTimeOffset.UtcNow.AddHours(1));

        IAuthCodeService authCodeService = GetService<IAuthCodeService>();
        string code = await authCodeService.CreateCodeAsync(storedTokens);

        var command = new ExchangeCodeCommand(code);

        // Act
        Result<AuthTokensDto> result = await Dispatcher.Send(command);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.AccessToken.ShouldBe("access-token");
        result.Value.RefreshToken.ShouldBe("refresh-token");
    }

    [Fact]
    public async Task Handle_WithInvalidCode_ShouldReturnError()
    {
        // Arrange
        var command = new ExchangeCodeCommand("this-code-does-not-exist");

        // Act
        Result<AuthTokensDto> result = await Dispatcher.Send(command);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe(AuthErrors.Codes.InvalidOrExpiredCode);
    }

    [Fact]
    public async Task Handle_WithAlreadyUsedCode_ShouldReturnError()
    {
        // Arrange — create and exchange a code once
        var storedTokens = new AuthTokensDto("access", "refresh", DateTimeOffset.UtcNow.AddHours(1));
        IAuthCodeService authCodeService = GetService<IAuthCodeService>();
        string code = await authCodeService.CreateCodeAsync(storedTokens);
        await Dispatcher.Send(new ExchangeCodeCommand(code));

        // Act — replay the same code (single-use)
        Result<AuthTokensDto> result = await Dispatcher.Send(new ExchangeCodeCommand(code));

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe(AuthErrors.Codes.InvalidOrExpiredCode);
    }
}
