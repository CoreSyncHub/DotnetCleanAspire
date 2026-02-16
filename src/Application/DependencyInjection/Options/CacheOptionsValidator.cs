using Application.Abstractions.DependencyInjection;
using System.Globalization;

namespace Application.DependencyInjection.Options;

/// <summary>
/// Validator for <see cref="CacheOptions"/>.
/// Ensures cache configuration is valid at application startup.
/// </summary>
public sealed class CacheOptionsValidator : OptionsValidator<CacheOptions>
{
    public override ValidateOptionsResult Validate(string? name, CacheOptions options)
    {
        List<string> errors = [];

        ValidateRequired(options.GlobalVersion, nameof(options.GlobalVersion), errors);
        ValidateVersionFormat(options.GlobalVersion, nameof(options.GlobalVersion), errors);

        ValidateFeatureVersions(options.FeatureVersions, nameof(options.FeatureVersions), errors);

        ValidatePositive(options.CompressionThresholdBytes, nameof(options.CompressionThresholdBytes), errors);
        ValidatePositive(options.DefaultCacheDuration, nameof(options.DefaultCacheDuration), errors);

        return ToResult(errors);
    }

    private static void ValidateVersionFormat(
        string? version, string propertyName, IList<string> errors)
    {
        if (version is null)
            return;

        if (!version.StartsWith('v') || version.Length < 2 || !char.IsDigit(version[1]))
        {
            errors.Add(
                $"{propertyName} must be in the format 'v' followed by a number (e.g., 'v1').");
        }
    }

    private static void ValidateFeatureVersions(
        Dictionary<string, string> featureVersions, string propertyName, IList<string> errors)
    {
        foreach ((string? key, string? value) in featureVersions)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                errors.Add($"{propertyName} contains an empty or whitespace key.");
                continue;
            }

            ValidateVersionFormat(value, $"{propertyName}['{key}']", errors);
        }
    }
}
