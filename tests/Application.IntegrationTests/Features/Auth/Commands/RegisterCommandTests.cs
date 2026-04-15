namespace Application.IntegrationTests.Features.Auth.Commands;

public sealed class RegisterCommandTests(TestContainersFixture fixture)
    : ApplicationIntegrationTestBase(fixture)
{
    [Fact]
    public async Task Handle_WithValidCredentials_ShouldCreateUserAndReturnTokens()
    {
        // Arrange
        var command = new RegisterCommand("user@example.com", "Test1234!", "Test1234!");

        // Act
        Result<AuthTokensDto> result = await Dispatcher.Send(command);

        // Assert — tokens returned
        result.IsSuccess.ShouldBeTrue();
        result.Value.AccessToken.ShouldNotBeNullOrEmpty();
        result.Value.RefreshToken.ShouldNotBeNullOrEmpty();
        result.Value.ExpiresAt.ShouldBeGreaterThan(DateTimeOffset.UtcNow);

        // Assert — user persisted in DB
        ApplicationUser? user = await DbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email == "user@example.com");

        user.ShouldNotBeNull();
    }

    [Fact]
    public async Task Handle_WithDuplicateEmail_ShouldReturnConflictError()
    {
        // Arrange — register once
        await RegisterUserAsync("duplicate@example.com");

        // Act — register again with same email
        Result<AuthTokensDto> result = await Dispatcher.Send(
            new RegisterCommand("duplicate@example.com", "Test1234!", "Test1234!"));

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe(AuthErrors.Codes.EmailAlreadyExists);
    }

    [Fact]
    public async Task Handle_WithPasswordTooShort_ShouldReturnValidationFailure()
    {
        // Arrange — min length is 8, "Ab1!" is only 4 chars
        var command = new RegisterCommand("user@example.com", "Ab1!", "Ab1!");

        // Act
        Result<AuthTokensDto> result = await Dispatcher.Send(command);

        // Assert — caught by RegisterCommandValidator (ValidationBehavior)
        result.IsFailure.ShouldBeTrue();
        result.Error.Type.ShouldBe(ErrorType.Validation);
    }

    [Fact]
    public async Task Handle_WithMismatchedPasswords_ShouldReturnValidationFailure()
    {
        // Arrange
        var command = new RegisterCommand("user@example.com", "Test1234!", "DifferentPass1!");

        // Act
        Result<AuthTokensDto> result = await Dispatcher.Send(command);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.Type.ShouldBe(ErrorType.Validation);
    }
}
