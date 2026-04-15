namespace Application.IntegrationTests.Features.Auth.Queries;

public sealed class GetCurrentUserQueryTests(TestContainersFixture fixture)
    : ApplicationIntegrationTestBase(fixture)
{
    [Fact]
    public async Task Handle_WhenCurrentUserExists_ShouldReturnUserDto()
    {
        // Arrange — register a user, then configure CurrentUser to that user's ID
        await RegisterUserAsync("current@example.com");

        ApplicationUser? user = await DbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email == "current@example.com");

        user.ShouldNotBeNull();
        CurrentUser.Id = user!.Id;
        CurrentUser.Email = user.Email;

        // Act
        Result<UserDto> result = await Dispatcher.Send(new GetCurrentUserQuery());

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.Email.ShouldBe("current@example.com");
    }

    [Fact]
    public async Task Handle_WhenUserNotAuthenticated_ShouldReturnUnauthorizedError()
    {
        // Arrange — CurrentUser.Id is null by default (not authenticated)
        CurrentUser.Id.ShouldBeNull();

        // Act
        Result<UserDto> result = await Dispatcher.Send(new GetCurrentUserQuery());

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe(AuthErrors.Codes.UserNotAuthenticated);
    }
}
