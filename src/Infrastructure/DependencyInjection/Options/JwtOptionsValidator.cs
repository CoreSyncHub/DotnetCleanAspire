using Application.Abstractions.DependencyInjection;

namespace Infrastructure.DependencyInjection.Options;

internal sealed class JwtOptionsValidator : OptionsValidator<JwtOptions>
{
    private const int MinKeyLength = 32;

    public override ValidateOptionsResult Validate(string? name, JwtOptions options)
    {
        List<string> errors = [];

        ValidateIssuer(options.Issuer, errors);
        ValidateAudience(options.Audience, errors);
        ValidateKey(options.Key, errors);

        return ToResult(errors);
    }

    private static void ValidateIssuer(string issuer, List<string> errors)
    {
        const string property = nameof(JwtOptions.Issuer);

        ValidateRequired(issuer, property, errors);

        if (!string.IsNullOrWhiteSpace(issuer)
            && !Uri.TryCreate(issuer, UriKind.Absolute, out _))
        {
            errors.Add($"{property} '{issuer}' is not a valid absolute URI.");
        }
    }

    private static void ValidateAudience(string audience, List<string> errors)
    {
        const string property = nameof(JwtOptions.Audience);

        ValidateRequired(audience, property, errors);

        if (!string.IsNullOrWhiteSpace(audience)
            && !Uri.TryCreate(audience, UriKind.Absolute, out _))
        {
            errors.Add($"{property} '{audience}' is not a valid absolute URI.");
        }
    }

    private static void ValidateKey(string key, List<string> errors)
    {
        const string property = nameof(JwtOptions.Key);

        ValidateRequired(key, property, errors);

        if (string.IsNullOrWhiteSpace(key))
            return;

        if (key.Length < MinKeyLength)
            errors.Add($"{property} must be at least {MinKeyLength} characters (got {key.Length}).");

        if (key is "CHANGE_ME" or "your-secret-key" or "super-secret-key")
            errors.Add($"{property} contains a placeholder value. Use a real secret.");
    }
}
