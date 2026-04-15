namespace Application.IntegrationTests.Features.Auth.Commands;

public sealed class LoginCommandTests(TestContainersFixture fixture)
    : ApplicationIntegrationTestBase(fixture)
{
    [Fact]
    public async Task Handle_WithValidCredentials_ShouldReturnTokens()
    {
        // Arrange — register a user first
        await RegisterUserAsync("login@example.com");

        var command = new LoginCommand("login@example.com", "Test1234!");

        // Act
        Result<AuthTokensDto> result = await Dispatcher.Send(command);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.AccessToken.ShouldNotBeNullOrEmpty();
        result.Value.RefreshToken.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public async Task Handle_WithUnknownEmail_ShouldReturnUnauthorizedError()
    {
        // Arrange — no user registered
        var command = new LoginCommand("unknown@example.com", "Test1234!");

        // Act
        Result<AuthTokensDto> result = await Dispatcher.Send(command);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe(AuthErrors.Codes.InvalidCredentials);
    }

    [Fact]
    public async Task Handle_WithWrongPassword_ShouldReturnUnauthorizedError()
    {
        // Arrange
        await RegisterUserAsync("login@example.com");
        var command = new LoginCommand("login@example.com", "WrongPassword1!");

        // Act
        Result<AuthTokensDto> result = await Dispatcher.Send(command);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe(AuthErrors.Codes.InvalidCredentials);
    }
}
