using Application.DependencyInjection.Options;
using Infrastructure.Identity.Handlers;
using Microsoft.Extensions.Options;
using System.Security.Claims;

namespace Infrastructure.UnitTests.Identity;

public class OidcClaimsTransformerTests
{
    #region Disabled OIDC tests

    [Fact]
    public async Task TransformAsync_WhenOidcDisabled_ShouldReturnUnmodifiedPrincipal()
    {
        // Arrange
        OidcOptions options = CreateOptions(enabled: false);
        OidcClaimsTransformer transformer = new(Options.Create(options));
        ClaimsPrincipal principal = CreateAuthenticatedPrincipal(groups: ["admin-group"]);

        // Act
        ClaimsPrincipal result = await transformer.TransformAsync(principal);

        // Assert
        result.ShouldBe(principal);
        result.IsInRole("Admin").ShouldBeFalse();
    }

    #endregion

    #region Unauthenticated user tests

    [Fact]
    public async Task TransformAsync_WhenUserNotAuthenticated_ShouldReturnUnmodifiedPrincipal()
    {
        // Arrange
        OidcOptions options = CreateOptions(enabled: true);
        OidcClaimsTransformer transformer = new(Options.Create(options));
        ClaimsPrincipal principal = CreateUnauthenticatedPrincipal();

        // Act
        ClaimsPrincipal result = await transformer.TransformAsync(principal);

        // Assert
        result.ShouldBe(principal);
    }

    #endregion

    #region Group to role mapping tests

    [Fact]
    public async Task TransformAsync_WithMatchingGroup_ShouldAddRoleClaim()
    {
        // Arrange
        OidcOptions options = CreateOptions(
            enabled: true,
            groupToRoleMapping: new Dictionary<string, string>
            {
                ["admin-group"] = "Admin"
            });
        OidcClaimsTransformer transformer = new(Options.Create(options));
        ClaimsPrincipal principal = CreateAuthenticatedPrincipal(groups: ["admin-group"]);

        // Act
        ClaimsPrincipal result = await transformer.TransformAsync(principal);

        // Assert
        result.IsInRole("Admin").ShouldBeTrue();
    }

    [Fact]
    public async Task TransformAsync_WithMultipleMatchingGroups_ShouldAddAllRoleClaims()
    {
        // Arrange
        OidcOptions options = CreateOptions(
            enabled: true,
            groupToRoleMapping: new Dictionary<string, string>
            {
                ["admin-group"] = "Admin",
                ["users-group"] = "User",
                ["managers-group"] = "Manager"
            });
        OidcClaimsTransformer transformer = new(Options.Create(options));
        ClaimsPrincipal principal = CreateAuthenticatedPrincipal(groups: ["admin-group", "users-group"]);

        // Act
        ClaimsPrincipal result = await transformer.TransformAsync(principal);

        // Assert
        result.IsInRole("Admin").ShouldBeTrue();
        result.IsInRole("User").ShouldBeTrue();
        result.IsInRole("Manager").ShouldBeFalse();
    }

    [Fact]
    public async Task TransformAsync_WithNoMatchingGroups_ShouldNotAddRoleClaims()
    {
        // Arrange
        OidcOptions options = CreateOptions(
            enabled: true,
            groupToRoleMapping: new Dictionary<string, string>
            {
                ["admin-group"] = "Admin"
            });
        OidcClaimsTransformer transformer = new(Options.Create(options));
        ClaimsPrincipal principal = CreateAuthenticatedPrincipal(groups: ["other-group"]);

        // Act
        ClaimsPrincipal result = await transformer.TransformAsync(principal);

        // Assert
        result.IsInRole("Admin").ShouldBeFalse();
    }

    [Fact]
    public async Task TransformAsync_WithEmptyGroupMapping_ShouldNotAddRoleClaims()
    {
        // Arrange
        OidcOptions options = CreateOptions(
            enabled: true,
            groupToRoleMapping: []);
        OidcClaimsTransformer transformer = new(Options.Create(options));
        ClaimsPrincipal principal = CreateAuthenticatedPrincipal(groups: ["some-group"]);

        // Act
        ClaimsPrincipal result = await transformer.TransformAsync(principal);

        // Assert
        result.FindAll(ClaimTypes.Role).ShouldBeEmpty();
    }

    [Fact]
    public async Task TransformAsync_WhenRoleAlreadyExists_ShouldNotDuplicateRole()
    {
        // Arrange
        OidcOptions options = CreateOptions(
            enabled: true,
            groupToRoleMapping: new Dictionary<string, string>
            {
                ["admin-group"] = "Admin"
            });
        OidcClaimsTransformer transformer = new(Options.Create(options));
        ClaimsPrincipal principal = CreateAuthenticatedPrincipal(groups: ["admin-group"], existingRoles: ["Admin"]);

        // Act
        ClaimsPrincipal result = await transformer.TransformAsync(principal);

        // Assert
        IEnumerable<Claim> adminRoles = result.FindAll(ClaimTypes.Role).Where(c => c.Value == "Admin");
        adminRoles.Count().ShouldBe(1);
    }

    #endregion

    #region Custom group claim type tests

    [Fact]
    public async Task TransformAsync_WithCustomGroupClaimType_ShouldUseConfiguredClaimType()
    {
        // Arrange
        OidcOptions options = CreateOptions(
            enabled: true,
            groupClaimType: "custom_groups",
            groupToRoleMapping: new Dictionary<string, string>
            {
                ["admin-group"] = "Admin"
            });
        OidcClaimsTransformer transformer = new(Options.Create(options));
        ClaimsPrincipal principal = CreateAuthenticatedPrincipalWithCustomGroupClaim(
            groupClaimType: "custom_groups",
            groups: ["admin-group"]);

        // Act
        ClaimsPrincipal result = await transformer.TransformAsync(principal);

        // Assert
        result.IsInRole("Admin").ShouldBeTrue();
    }

    [Fact]
    public async Task TransformAsync_WithMismatchedGroupClaimType_ShouldNotAddRoles()
    {
        // Arrange
        OidcOptions options = CreateOptions(
            enabled: true,
            groupClaimType: "groups",
            groupToRoleMapping: new Dictionary<string, string>
            {
                ["admin-group"] = "Admin"
            });
        OidcClaimsTransformer transformer = new(Options.Create(options));
        ClaimsPrincipal principal = CreateAuthenticatedPrincipalWithCustomGroupClaim(
            groupClaimType: "different_claim",
            groups: ["admin-group"]);

        // Act
        ClaimsPrincipal result = await transformer.TransformAsync(principal);

        // Assert
        result.IsInRole("Admin").ShouldBeFalse();
    }

    #endregion

    #region Edge cases

    [Fact]
    public async Task TransformAsync_WithNonClaimsIdentity_ShouldReturnUnmodifiedPrincipal()
    {
        // Arrange
        OidcOptions options = CreateOptions(enabled: true);
        OidcClaimsTransformer transformer = new(Options.Create(options));

        var mockIdentity = new Mock<System.Security.Principal.IIdentity>();
        mockIdentity.Setup(x => x.IsAuthenticated).Returns(true);
        ClaimsPrincipal principal = new(mockIdentity.Object);

        // Act
        ClaimsPrincipal result = await transformer.TransformAsync(principal);

        // Assert
        result.ShouldBe(principal);
    }

    #endregion

    #region Test helpers

    private static OidcOptions CreateOptions(
        bool enabled,
        string groupClaimType = "groups",
        Dictionary<string, string>? groupToRoleMapping = null)
    {
        return new OidcOptions
        {
            Enabled = enabled,
            Provider = new OidcProviderOptions
            {
                GroupClaimType = groupClaimType,
                GroupToRoleMapping = groupToRoleMapping ?? []
            }
        };
    }

    private static ClaimsPrincipal CreateAuthenticatedPrincipal(
        string[]? groups = null,
        string[]? existingRoles = null)
    {
        return CreateAuthenticatedPrincipalWithCustomGroupClaim("groups", groups, existingRoles);
    }

    private static ClaimsPrincipal CreateAuthenticatedPrincipalWithCustomGroupClaim(
        string groupClaimType,
        string[]? groups = null,
        string[]? existingRoles = null)
    {
        List<Claim> claims =
        [
            new(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
            new(ClaimTypes.Email, "user@example.com")
        ];

        foreach (string group in groups ?? [])
        {
            claims.Add(new Claim(groupClaimType, group));
        }

        foreach (string role in existingRoles ?? [])
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        ClaimsIdentity identity = new(claims, "OidcAuth");
        return new ClaimsPrincipal(identity);
    }

    private static ClaimsPrincipal CreateUnauthenticatedPrincipal()
    {
        ClaimsIdentity identity = new();
        return new ClaimsPrincipal(identity);
    }

    #endregion
}
