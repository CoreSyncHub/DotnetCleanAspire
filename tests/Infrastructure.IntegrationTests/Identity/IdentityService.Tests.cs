using Application.Abstractions.Identity;
using Application.Abstractions.Identity.Dtos;
using Domain.Abstractions;
using Domain.Users.Constants;
using Infrastructure.Identity.Entities;
using Infrastructure.IntegrationTests.Fixtures;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using ExternalLoginInfo = Application.Abstractions.Identity.Dtos.ExternalLoginInfo;

namespace Infrastructure.IntegrationTests.Identity;

[Collection(nameof(IntegrationTestCollection))]
public sealed class IdentityServiceTests(TestContainersFixture containersFixture) : IAsyncLifetime, IDisposable
{
    private TestsWebApplicationFactory? _factory;
    private IServiceScope? _scope;
    private IIdentityService _identityService = null!;
#pragma warning disable CA2213 // Disposed via IServiceScope
    private UserManager<ApplicationUser> _userManager = null!;
#pragma warning restore CA2213

    public async Task InitializeAsync()
    {
        _factory = new TestsWebApplicationFactory(
            containersFixture.RedisConnectionString,
            containersFixture.PostgresConnectionString);

        await _factory.EnsureDatabaseCreatedAsync();

        _scope = _factory.Services.CreateScope();
        _identityService = _scope.ServiceProvider.GetRequiredService<IIdentityService>();
        _userManager = _scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        // Seed required roles for external login tests
        RoleManager<ApplicationRole> roleManager = _scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
        foreach (string role in Roles.All)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new ApplicationRole { Name = role });
            }
        }
    }

    public Task DisposeAsync() => Task.CompletedTask;

    public void Dispose()
    {
        _scope?.Dispose();
        _factory?.Dispose();
    }

    #region CreateUserAsync tests

    [Fact]
    public async Task CreateUserAsync_WithValidData_ShouldCreateUser()
    {
        // Arrange
        const string email = "newuser@example.com";
        const string password = "Test123!";

        // Act
        Result<Id> result = await _identityService.CreateUserAsync(email, password);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.IsEmpty.ShouldBeFalse();

        ApplicationUser? user = await _userManager.FindByEmailAsync(email);
        user.ShouldNotBeNull();
        user.Email.ShouldBe(email);
    }

    [Fact]
    public async Task CreateUserAsync_WithExistingEmail_ShouldReturnError()
    {
        // Arrange
        const string email = "existing@example.com";
        await CreateTestUserAsync(email);

        // Act
        Result<Id> result = await _identityService.CreateUserAsync(email, "Test123!");

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("Auth.EmailAlreadyExists");
    }

    [Fact]
    public async Task CreateUserAsync_WithWeakPassword_ShouldReturnValidationError()
    {
        // Arrange
        const string email = "weakpass@example.com";
        const string weakPassword = "123";

        // Act
        Result<Id> result = await _identityService.CreateUserAsync(email, weakPassword);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("Auth.CreateFailed");
    }

    #endregion

    #region ValidateCredentialsAsync tests

    [Fact]
    public async Task ValidateCredentialsAsync_WithValidCredentials_ShouldSucceed()
    {
        // Arrange
        const string email = "validcreds@example.com";
        const string password = "Test123!";
        await CreateTestUserAsync(email, password);

        // Act
        Result<Unit> result = await _identityService.ValidateCredentialsAsync(email, password);

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task ValidateCredentialsAsync_WithInvalidPassword_ShouldReturnError()
    {
        // Arrange
        const string email = "invalidpass@example.com";
        await CreateTestUserAsync(email, "Test123!");

        // Act
        Result<Unit> result = await _identityService.ValidateCredentialsAsync(email, "WrongPassword!");

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("Auth.InvalidCredentials");
    }

    [Fact]
    public async Task ValidateCredentialsAsync_WithNonExistentUser_ShouldReturnError()
    {
        // Act
        Result<Unit> result = await _identityService.ValidateCredentialsAsync("nonexistent@example.com", "Test123!");

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("Auth.InvalidCredentials");
    }

    #endregion

    #region Password reset tests

    [Fact]
    public async Task GeneratePasswordResetTokenAsync_WithValidEmail_ShouldReturnToken()
    {
        // Arrange
        const string email = "resettoken@example.com";
        await CreateTestUserAsync(email);

        // Act
        Result<string> result = await _identityService.GeneratePasswordResetTokenAsync(email);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public async Task GeneratePasswordResetTokenAsync_WithNonExistentUser_ShouldReturnError()
    {
        // Act
        Result<string> result = await _identityService.GeneratePasswordResetTokenAsync("nonexistent@example.com");

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("Auth.UserNotFound");
    }

    [Fact]
    public async Task ResetPasswordAsync_WithValidToken_ShouldSucceed()
    {
        // Arrange
        const string email = "resetpass@example.com";
        const string newPassword = "NewPassword123!";
        await CreateTestUserAsync(email, "OldPassword123!");

        Result<string> tokenResult = await _identityService.GeneratePasswordResetTokenAsync(email);
        tokenResult.IsSuccess.ShouldBeTrue();

        // Act
        Result<Unit> result = await _identityService.ResetPasswordAsync(email, tokenResult.Value, newPassword);

        // Assert
        result.IsSuccess.ShouldBeTrue();

        // Verify new password works
        Result<Unit> loginResult = await _identityService.ValidateCredentialsAsync(email, newPassword);
        loginResult.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task ResetPasswordAsync_WithInvalidToken_ShouldReturnError()
    {
        // Arrange
        const string email = "invalidtoken@example.com";
        await CreateTestUserAsync(email);

        // Act
        Result<Unit> result = await _identityService.ResetPasswordAsync(email, "invalid-token", "NewPassword123!");

        // Assert
        result.IsFailure.ShouldBeTrue();
    }

    #endregion

    #region Role management tests

    [Fact]
    public async Task AddToRoleAsync_WithValidUserAndRole_ShouldSucceed()
    {
        // Arrange
        ApplicationUser user = await CreateTestUserAsync("addrole@example.com");
        const string role = "Admin";

        // Act
        Result<Unit> result = await _identityService.AddToRoleAsync(user.Id, role);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        (await _userManager.IsInRoleAsync(user, role)).ShouldBeTrue();
    }

    [Fact]
    public async Task AddToRoleAsync_WhenAlreadyInRole_ShouldSucceed()
    {
        // Arrange
        ApplicationUser user = await CreateTestUserAsync("alreadyrole@example.com");
        const string role = "Admin";
        await _identityService.AddToRoleAsync(user.Id, role);

        // Act
        Result<Unit> result = await _identityService.AddToRoleAsync(user.Id, role);

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task AddToRoleAsync_WithNonExistentUser_ShouldReturnError()
    {
        // Act
        Result<Unit> result = await _identityService.AddToRoleAsync(Id.New(), "Admin");

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("Auth.UserNotFound");
    }

    [Fact]
    public async Task IsInRoleAsync_WhenUserHasRole_ShouldReturnTrue()
    {
        // Arrange
        ApplicationUser user = await CreateTestUserAsync("hasrole@example.com");
        await _identityService.AddToRoleAsync(user.Id, "Admin");

        // Act
        bool result = await _identityService.IsInRoleAsync(user.Id, "Admin");

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public async Task IsInRoleAsync_WhenUserDoesNotHaveRole_ShouldReturnFalse()
    {
        // Arrange
        ApplicationUser user = await CreateTestUserAsync("norole@example.com");

        // Act
        bool result = await _identityService.IsInRoleAsync(user.Id, "Admin");

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public async Task GetRolesAsync_ShouldReturnAllUserRoles()
    {
        // Arrange
        ApplicationUser user = await CreateTestUserAsync("multiroles@example.com");
        await _identityService.AddToRoleAsync(user.Id, "Admin");
        await _identityService.AddToRoleAsync(user.Id, "Manager");

        // Act
        IReadOnlyList<string> roles = await _identityService.GetRolesAsync(user.Id);

        // Assert
        roles.Count.ShouldBe(2);
        roles.ShouldContain("Admin");
        roles.ShouldContain("Manager");
    }

    #endregion

    #region GetUser tests

    [Fact]
    public async Task GetUserByIdAsync_WithValidId_ShouldReturnUser()
    {
        // Arrange
        ApplicationUser user = await CreateTestUserAsync("getbyid@example.com");

        // Act
        UserDto? result = await _identityService.GetUserByIdAsync(user.Id);

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldBe(user.Id);
        result.Email.ShouldBe(user.Email);
    }

    [Fact]
    public async Task GetUserByIdAsync_WithNonExistentId_ShouldReturnNull()
    {
        // Act
        UserDto? result = await _identityService.GetUserByIdAsync(Id.New());

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public async Task GetUserByEmailAsync_WithValidEmail_ShouldReturnUser()
    {
        // Arrange
        const string email = "getbyemail@example.com";
        await CreateTestUserAsync(email);

        // Act
        UserDto? result = await _identityService.GetUserByEmailAsync(email);

        // Assert
        result.ShouldNotBeNull();
        result.Email.ShouldBe(email);
    }

    [Fact]
    public async Task GetUserByEmailAsync_WithNonExistentEmail_ShouldReturnNull()
    {
        // Act
        UserDto? result = await _identityService.GetUserByEmailAsync("nonexistent@example.com");

        // Assert
        result.ShouldBeNull();
    }

    #endregion

    #region External login tests

    [Fact]
    public async Task GetOrCreateExternalUserAsync_WithNewUser_ShouldCreateUser()
    {
        // Arrange
        var loginInfo = new ExternalLoginInfo(
            Provider: "Google",
            ProviderKey: "google-123",
            Email: "external@example.com",
            Name: "External User",
            Groups: []);

        // Act
        Result<UserDto> result = await _identityService.GetOrCreateExternalUserAsync(loginInfo);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.Email.ShouldBe("external@example.com");

        ApplicationUser? user = await _userManager.FindByEmailAsync("external@example.com");
        user.ShouldNotBeNull();
        user.ExternalProvider.ShouldBe("Google");
        user.ExternalProviderKey.ShouldBe("google-123");
    }

    [Fact]
    public async Task GetOrCreateExternalUserAsync_WithExistingExternalUser_ShouldReturnExistingUser()
    {
        // Arrange
        var loginInfo = new ExternalLoginInfo(
            Provider: "Google",
            ProviderKey: "google-existing",
            Email: "existingexternal@example.com",
            Name: "Existing User",
            Groups: []);

        // Create the user first
        await _identityService.GetOrCreateExternalUserAsync(loginInfo);

        // Act
        Result<UserDto> result = await _identityService.GetOrCreateExternalUserAsync(loginInfo);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.Email.ShouldBe("existingexternal@example.com");
    }

    [Fact]
    public async Task GetOrCreateExternalUserAsync_WithExistingLocalUser_ShouldLinkAccount()
    {
        // Arrange
        const string email = "localuser@example.com";
        await CreateTestUserAsync(email);

        var loginInfo = new ExternalLoginInfo(
            Provider: "Google",
            ProviderKey: "google-link",
            Email: email,
            Name: "Local User",
            Groups: []);

        // Act
        Result<UserDto> result = await _identityService.GetOrCreateExternalUserAsync(loginInfo);

        // Assert
        result.IsSuccess.ShouldBeTrue();

        ApplicationUser? user = await _userManager.FindByEmailAsync(email);
        user.ShouldNotBeNull();
        user.ExternalProvider.ShouldBe("Google");
        user.ExternalProviderKey.ShouldBe("google-link");
    }

    [Fact]
    public async Task LinkExternalLoginAsync_WithValidUser_ShouldSucceed()
    {
        // Arrange
        ApplicationUser user = await CreateTestUserAsync("linkexternal@example.com");

        // Act
        Result<Unit> result = await _identityService.LinkExternalLoginAsync(user.Id, "GitHub", "github-123");

        // Assert
        result.IsSuccess.ShouldBeTrue();

        ApplicationUser? updatedUser = await _userManager.FindByIdAsync(user.Id.ToString());
        updatedUser.ShouldNotBeNull();
        updatedUser.ExternalProvider.ShouldBe("GitHub");
        updatedUser.ExternalProviderKey.ShouldBe("github-123");
    }

    #endregion

    #region UpdateLastLoginAsync tests

    [Fact]
    public async Task UpdateLastLoginAsync_WithValidUser_ShouldUpdateTimestamp()
    {
        // Arrange
        ApplicationUser user = await CreateTestUserAsync("lastlogin@example.com");
        DateTimeOffset? originalLastLogin = user.LastLoginAt;

        // Act
        Result<Unit> result = await _identityService.UpdateLastLoginAsync(user.Id);

        // Assert
        result.IsSuccess.ShouldBeTrue();

        ApplicationUser? updatedUser = await _userManager.FindByIdAsync(user.Id.ToString());
        updatedUser.ShouldNotBeNull();
        updatedUser.LastLoginAt.ShouldNotBeNull();
        updatedUser.LastLoginAt.Value.ShouldBeGreaterThan(originalLastLogin ?? DateTimeOffset.MinValue);
    }

    [Fact]
    public async Task UpdateLastLoginAsync_WithNonExistentUser_ShouldReturnError()
    {
        // Act
        Result<Unit> result = await _identityService.UpdateLastLoginAsync(Id.New());

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("Auth.UserNotFound");
    }

    #endregion

    #region Test helpers

    private async Task<ApplicationUser> CreateTestUserAsync(string email, string password = "Test123!")
    {
        ApplicationUser user = new()
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true
        };

        IdentityResult result = await _userManager.CreateAsync(user, password);
        result.Succeeded.ShouldBeTrue($"Failed to create test user: {string.Join(", ", result.Errors.Select(e => e.Description))}");

        return user;
    }

    #endregion
}
