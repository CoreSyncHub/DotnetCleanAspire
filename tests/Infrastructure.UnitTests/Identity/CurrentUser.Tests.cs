using Domain.Abstractions;
using Infrastructure.Identity;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace Infrastructure.UnitTests.Identity;

public class CurrentUserTests
{
    private readonly Mock<IHttpContextAccessor> _httpContextAccessor;
    private readonly CurrentUser _currentUser;

    public CurrentUserTests()
    {
        _httpContextAccessor = new Mock<IHttpContextAccessor>();
        _currentUser = new CurrentUser(_httpContextAccessor.Object);
    }

    #region Id tests

    [Fact]
    public void Id_WithValidNameIdentifierClaim_ShouldReturnParsedId()
    {
        // Arrange
        var expectedId = Id.New();
        SetupAuthenticatedUser(expectedId.ToString(), "test@example.com");

        // Act
        Id? result = _currentUser.Id;

        // Assert
        result.ShouldNotBeNull();
        result.Value.ShouldBe(expectedId);
    }

    [Fact]
    public void Id_WithInvalidNameIdentifierClaim_ShouldReturnNull()
    {
        // Arrange
        SetupAuthenticatedUser("invalid-guid", "test@example.com");

        // Act
        Id? result = _currentUser.Id;

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public void Id_WithNoHttpContext_ShouldReturnNull()
    {
        // Arrange
        _httpContextAccessor.Setup(x => x.HttpContext).Returns((HttpContext?)null);

        // Act
        Id? result = _currentUser.Id;

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public void Id_WithNoUser_ShouldReturnNull()
    {
        // Arrange
        var httpContext = new Mock<HttpContext>();
        httpContext.Setup(x => x.User).Returns((ClaimsPrincipal?)null!);
        _httpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext.Object);

        // Act
        Id? result = _currentUser.Id;

        // Assert
        result.ShouldBeNull();
    }

    #endregion

    #region Email tests

    [Fact]
    public void Email_WithEmailClaim_ShouldReturnEmail()
    {
        // Arrange
        const string expectedEmail = "user@example.com";
        SetupAuthenticatedUser(Guid.NewGuid().ToString(), expectedEmail);

        // Act
        string? result = _currentUser.Email;

        // Assert
        result.ShouldBe(expectedEmail);
    }

    [Fact]
    public void Email_WithNoHttpContext_ShouldReturnNull()
    {
        // Arrange
        _httpContextAccessor.Setup(x => x.HttpContext).Returns((HttpContext?)null);

        // Act
        string? result = _currentUser.Email;

        // Assert
        result.ShouldBeNull();
    }

    #endregion

    #region IsAuthenticated tests

    [Fact]
    public void IsAuthenticated_WhenUserIsAuthenticated_ShouldReturnTrue()
    {
        // Arrange
        SetupAuthenticatedUser(Guid.NewGuid().ToString(), "test@example.com");

        // Act
        bool result = _currentUser.IsAuthenticated;

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void IsAuthenticated_WhenUserIsNotAuthenticated_ShouldReturnFalse()
    {
        // Arrange
        SetupUnauthenticatedUser();

        // Act
        bool result = _currentUser.IsAuthenticated;

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void IsAuthenticated_WithNoHttpContext_ShouldReturnFalse()
    {
        // Arrange
        _httpContextAccessor.Setup(x => x.HttpContext).Returns((HttpContext?)null);

        // Act
        bool result = _currentUser.IsAuthenticated;

        // Assert
        result.ShouldBeFalse();
    }

    #endregion

    #region GetRoles tests

    [Fact]
    public void GetRoles_WithMultipleRoles_ShouldReturnAllRoles()
    {
        // Arrange
        string[] expectedRoles = ["Admin", "User", "Manager"];
        SetupAuthenticatedUserWithRoles(Guid.NewGuid().ToString(), "test@example.com", expectedRoles);

        // Act
        IReadOnlyList<string> result = _currentUser.GetRoles();

        // Assert
        result.Count.ShouldBe(3);
        result.ShouldContain("Admin");
        result.ShouldContain("User");
        result.ShouldContain("Manager");
    }

    [Fact]
    public void GetRoles_WithNoRoles_ShouldReturnEmptyList()
    {
        // Arrange
        SetupAuthenticatedUser(Guid.NewGuid().ToString(), "test@example.com");

        // Act
        IReadOnlyList<string> result = _currentUser.GetRoles();

        // Assert
        result.ShouldBeEmpty();
    }

    [Fact]
    public void GetRoles_WithNoHttpContext_ShouldReturnEmptyList()
    {
        // Arrange
        _httpContextAccessor.Setup(x => x.HttpContext).Returns((HttpContext?)null);

        // Act
        IReadOnlyList<string> result = _currentUser.GetRoles();

        // Assert
        result.ShouldBeEmpty();
    }

    #endregion

    #region IsInRole tests

    [Fact]
    public void IsInRole_WhenUserHasRole_ShouldReturnTrue()
    {
        // Arrange
        SetupAuthenticatedUserWithRoles(Guid.NewGuid().ToString(), "test@example.com", ["Admin", "User"]);

        // Act
        bool result = _currentUser.IsInRole("Admin");

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void IsInRole_WhenUserDoesNotHaveRole_ShouldReturnFalse()
    {
        // Arrange
        SetupAuthenticatedUserWithRoles(Guid.NewGuid().ToString(), "test@example.com", ["User"]);

        // Act
        bool result = _currentUser.IsInRole("Admin");

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void IsInRole_WithNoHttpContext_ShouldReturnFalse()
    {
        // Arrange
        _httpContextAccessor.Setup(x => x.HttpContext).Returns((HttpContext?)null);

        // Act
        bool result = _currentUser.IsInRole("Admin");

        // Assert
        result.ShouldBeFalse();
    }

    #endregion

    #region Test helpers

    private void SetupAuthenticatedUser(string userId, string email)
    {
        SetupAuthenticatedUserWithRoles(userId, email, []);
    }

    private void SetupAuthenticatedUserWithRoles(string userId, string email, string[] roles)
    {
        List<Claim> claims =
        [
            new(ClaimTypes.NameIdentifier, userId),
            new(ClaimTypes.Email, email)
        ];

        foreach (string role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        ClaimsIdentity identity = new(claims, "TestAuth");
        ClaimsPrincipal principal = new(identity);

        var httpContext = new Mock<HttpContext>();
        httpContext.Setup(x => x.User).Returns(principal);
        _httpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext.Object);
    }

    private void SetupUnauthenticatedUser()
    {
        ClaimsIdentity identity = new(); // No authentication type = not authenticated
        ClaimsPrincipal principal = new(identity);

        var httpContext = new Mock<HttpContext>();
        httpContext.Setup(x => x.User).Returns(principal);
        _httpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext.Object);
    }

    #endregion
}
