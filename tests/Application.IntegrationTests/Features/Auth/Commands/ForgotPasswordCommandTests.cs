namespace Application.IntegrationTests.Features.Auth.Commands;

public sealed class ForgotPasswordCommandTests(TestContainersFixture fixture)
    : ApplicationIntegrationTestBase(fixture)
{
    [Fact]
    public async Task Handle_WithKnownEmail_ShouldReturnSuccess()
    {
        // Arrange
        await RegisterUserAsync("known@example.com");
        var command = new ForgotPasswordCommand("known@example.com");

        // Act
        Result<Unit> result = await Dispatcher.Send(command);

        // Assert — always succeeds to prevent email enumeration
        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task Handle_WithUnknownEmail_ShouldReturnSuccess()
    {
        // Arrange — no user registered with this email
        var command = new ForgotPasswordCommand("unknown@example.com");

        // Act
        Result<Unit> result = await Dispatcher.Send(command);

        // Assert — returns success even for unknown emails (security: prevents enumeration)
        result.IsSuccess.ShouldBeTrue();
    }
}
