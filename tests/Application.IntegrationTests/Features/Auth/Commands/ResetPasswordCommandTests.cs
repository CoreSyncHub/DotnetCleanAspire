namespace Application.IntegrationTests.Features.Auth.Commands;

public sealed class ResetPasswordCommandTests(TestContainersFixture fixture)
    : ApplicationIntegrationTestBase(fixture)
{
    [Fact]
    public async Task Handle_WithValidToken_ShouldChangePasswordSuccessfully()
    {
        // Arrange
        const string email = "reset@example.com";
        const string oldPassword = "Test1234!";
        const string newPassword = "NewPass5678!";

        await RegisterUserAsync(email, oldPassword);

        // Generate reset token via IdentityService (simulates what the email link would contain)
        Result<string> tokenResult = await IdentityService.GeneratePasswordResetTokenAsync(email);
        tokenResult.IsSuccess.ShouldBeTrue();

        var command = new ResetPasswordCommand(email, tokenResult.Value, newPassword, newPassword);

        // Act
        Result<Unit> result = await Dispatcher.Send(command);

        // Assert
        result.IsSuccess.ShouldBeTrue();

        // Assert — old password no longer works
        Result<AuthTokensDto> oldLogin = await Dispatcher.Send(new LoginCommand(email, oldPassword));
        oldLogin.IsFailure.ShouldBeTrue();

        // Assert — new password works
        Result<AuthTokensDto> newLogin = await Dispatcher.Send(new LoginCommand(email, newPassword));
        newLogin.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task Handle_WithInvalidToken_ShouldReturnError()
    {
        // Arrange
        await RegisterUserAsync("reset@example.com");

        var command = new ResetPasswordCommand(
            "reset@example.com",
            "invalid-token",
            "NewPass5678!",
            "NewPass5678!");

        // Act
        Result<Unit> result = await Dispatcher.Send(command);

        // Assert
        result.IsFailure.ShouldBeTrue();
    }
}
