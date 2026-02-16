using Application.Abstractions.DependencyInjection;

namespace Application.DependencyInjection.Options;

public sealed class OidcOptionsValidator : OptionsValidator<OidcOptions>
{
    private static readonly HashSet<string> ValidUsernameClaimTypes =
    [
        "preferred_username", "name", "given_name", "nickname", "email", "sub"
    ];

    public override ValidateOptionsResult Validate(string? name, OidcOptions options)
    {
        if (!options.Enabled)
            return ValidateOptionsResult.Success;

        List<string> errors = [];

        ValidateProvider(options.Provider, errors);
        ValidateRedirectUris(options.AllowedRedirectUris, errors);

        return ToResult(errors);
    }

    private static void ValidateProvider(OidcProviderOptions provider, List<string> errors)
    {
        const string prefix = "Provider";

        // Authority
        ValidateRequired(provider.Authority, $"{prefix}.{nameof(provider.Authority)}", errors);
        if (!string.IsNullOrWhiteSpace(provider.Authority))
        {
            if (!Uri.TryCreate(provider.Authority, UriKind.Absolute, out Uri? uri))
                errors.Add($"{prefix}.{nameof(provider.Authority)} '{provider.Authority}' is not a valid absolute URI.");
            else if (uri.Scheme != Uri.UriSchemeHttps)
                errors.Add($"{prefix}.{nameof(provider.Authority)} must use HTTPS.");
        }

        // ClientId
        ValidateRequired(provider.ClientId, $"{prefix}.{nameof(provider.ClientId)}", errors);

        // Scopes
        if (provider.Scopes.Count == 0)
            errors.Add($"{prefix}.{nameof(provider.Scopes)} must contain at least one scope.");
        else if (!provider.Scopes.Contains("openid"))
            errors.Add($"{prefix}.{nameof(provider.Scopes)} must contain 'openid' (required by OIDC spec).");

        // GroupClaimType
        ValidateRequired(provider.GroupClaimType, $"{prefix}.{nameof(provider.GroupClaimType)}", errors);

        // UsernameClaimType
        ValidateRequired(provider.UsernameClaimType, $"{prefix}.{nameof(provider.UsernameClaimType)}", errors);
        if (!string.IsNullOrWhiteSpace(provider.UsernameClaimType)
            && !ValidUsernameClaimTypes.Contains(provider.UsernameClaimType))
        {
            errors.Add(
                $"{prefix}.{nameof(provider.UsernameClaimType)} '{provider.UsernameClaimType}' " +
                $"is not a recognized claim type. Valid values: {string.Join(", ", ValidUsernameClaimTypes)}.");
        }

        // GroupToRoleMapping
        ValidateGroupToRoleMapping(provider.GroupToRoleMapping, prefix, errors);

        // Callback paths
        ValidateCallbackPath(provider.CallbackPath, $"{prefix}.{nameof(provider.CallbackPath)}", errors);
        ValidateCallbackPath(provider.SignedOutCallbackPath, $"{prefix}.{nameof(provider.SignedOutCallbackPath)}", errors);
    }

    private static void ValidateGroupToRoleMapping(
        Dictionary<string, string> mapping, string prefix, List<string> errors)
    {
        foreach ((string? group, string? role) in mapping)
        {
            if (string.IsNullOrWhiteSpace(group))
                errors.Add($"{prefix}.{nameof(OidcProviderOptions.GroupToRoleMapping)} contains an empty group key.");

            if (string.IsNullOrWhiteSpace(role))
                errors.Add($"{prefix}.{nameof(OidcProviderOptions.GroupToRoleMapping)}['{group}'] has an empty role value.");
        }
    }

    private static void ValidateCallbackPath(string? path, string propertyName, List<string> errors)
    {
        ValidateRequired(path, propertyName, errors);

        if (string.IsNullOrWhiteSpace(path))
            return;

        if (path.StartsWith('/'))
            errors.Add($"{propertyName} should not start with '/'.");

        if (path.Contains(' ', StringComparison.Ordinal))
            errors.Add($"{propertyName} must not contain spaces.");
    }

    private static void ValidateRedirectUris(IReadOnlyList<string> uris, List<string> errors)
    {
        const string propertyName = nameof(OidcOptions.AllowedRedirectUris);

        if (uris.Count == 0)
        {
            errors.Add($"{propertyName} must contain at least one redirect URI when OIDC is enabled.");
            return;
        }

        for (int i = 0; i < uris.Count; i++)
        {
            string uri = uris[i];

            if (!Uri.TryCreate(uri, UriKind.Absolute, out Uri? parsed))
            {
                errors.Add($"{propertyName}[{i}] '{uri}' is not a valid absolute URI.");
                continue;
            }

            if (parsed.Scheme != Uri.UriSchemeHttps && parsed.Host != "localhost")
                errors.Add($"{propertyName}[{i}] '{uri}' must use HTTPS (HTTP is only allowed for localhost).");
        }
    }
}
