namespace Infrastructure.DependencyInjection.Options;

internal sealed class JwtOptions
{
    /// <summary>
    /// Configuration section name in appsettings.json.
    /// </summary>
    public const string SectionName = "Jwt";

    /// <summary>
    /// Get the issuer of the JWT tokens.
    /// Should be a valid URL or identifier.
    /// e.g., "https://mydomain.com"
    /// </summary>
    public required string Issuer { get; init; }

    /// <summary>
    /// Get the audience for the JWT tokens.
    /// Should match the intended recipients of the token.
    /// e.g., "https://myapi.com"
    /// </summary>
    public required string Audience { get; init; }

    /// <summary>
    /// Get the secret key used to sign the JWT tokens.
    /// Must be a sufficiently long and random string.
    /// </summary>
    public required string Key { get; init; }
}
