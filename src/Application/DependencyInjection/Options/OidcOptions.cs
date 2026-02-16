namespace Application.DependencyInjection.Options;

public sealed class OidcOptions
{
    public const string SectionName = "Oidc";

    /// <summary>
    /// Indicates whether OIDC authentication is enabled.
    /// Default: false
    /// </summary>
    public bool Enabled { get; init; }

    /// <summary>
    /// Indicates whether local authentication should be disabled when OIDC is enabled.
    /// Default: false
    /// </summary>
    public bool DisableLocalAuthWhenEnabled { get; init; }

    /// <summary>
    /// List of allowed redirect URIs after authentication.
    /// </summary>
    public IReadOnlyList<string> AllowedRedirectUris { get; init; } = [];

    /// <summary>
    /// OIDC provider configuration.
    /// </summary>
    public OidcProviderOptions Provider { get; init; } = new();
}

public sealed class OidcProviderOptions
{
    /// <summary>
    /// The authority (issuer) URL of the OIDC provider.
    /// </summary>
    public string Authority { get; init; } = string.Empty;

    /// <summary>
    /// The client ID registered with the OIDC provider.
    /// </summary>
    public string ClientId { get; init; } = string.Empty;

    /// <summary>
    /// The client secret registered with the OIDC provider.
    /// </summary>
    public string? ClientSecret { get; init; }

    /// <summary>
    /// The scopes to request from the OIDC provider.
    /// Defaults to "openid", "profile", "email", and "groups".
    /// </summary>
    public IList<string> Scopes { get; init; } = ["openid", "profile", "email", "groups"];

    /// <summary>
    /// The claim type to use for group membership (e.g., "groups", "roles").
    /// Defaults to "groups".
    /// </summary>
    public string GroupClaimType { get; init; } = "groups";

    /// <summary>
    /// The claim type to use for the username (e.g., "preferred_username", "name", "given_name", "nickname").
    /// Defaults to "preferred_username".
    /// </summary>
    public string UsernameClaimType { get; init; } = "preferred_username";

    /// <summary>
    /// Mapping of OIDC groups to application roles.
    /// E.g., { "admin-group": "Admin", "users-group": "User" }.
    /// </summary>
    public Dictionary<string, string> GroupToRoleMapping { get; init; } = [];

    /// <summary>
    /// The callback path for OIDC sign-in.
    /// </summary>
    public string CallbackPath { get; init; } = "api/v1/auth/oidc/callback";

    /// <summary>
    /// The callback path for OIDC sign-out.
    /// </summary>
    public string SignedOutCallbackPath { get; init; } = "api/v1/auth/oidc/signout-callback";
}
