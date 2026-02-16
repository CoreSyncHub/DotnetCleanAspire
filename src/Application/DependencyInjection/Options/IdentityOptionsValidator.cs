using Application.Abstractions.DependencyInjection;

namespace Application.DependencyInjection.Options;

public sealed class AuthIdentityOptionsValidator : OptionsValidator<AuthIdentityOptions>
{
    public override ValidateOptionsResult Validate(string? name, AuthIdentityOptions options)
    {
        List<string> errors = [];

        ValidatePasswordPolicy(options.PasswordPolicy, errors);
        ValidateLockout(options.Lockout, errors);
        ValidateTokens(options.Tokens, errors);

        return ToResult(errors);
    }

    private static void ValidatePasswordPolicy(PasswordPolicyOptions policy, List<string> errors)
    {
        const string prefix = "PasswordPolicy";

        if (policy.MinimumLength < 6)
            errors.Add($"{prefix}.{nameof(policy.MinimumLength)} must be at least 6 (got {policy.MinimumLength}).");

        if (policy.MinimumLength > 128)
            errors.Add($"{prefix}.{nameof(policy.MinimumLength)} must not exceed 128 (got {policy.MinimumLength}).");

        int requiredCharTypes = new[] {
            policy.RequireDigit,
            policy.RequireLowercase,
            policy.RequireUppercase,
            policy.RequireNonAlphanumeric
        }.Count(r => r);

        if (policy.MinimumLength < requiredCharTypes)
            errors.Add($"{prefix}.{nameof(policy.MinimumLength)} ({policy.MinimumLength}) " +
                $"cannot be less than the number of required character types ({requiredCharTypes}).");
    }

    private static void ValidateLockout(AuthLockoutOptions lockout, List<string> errors)
    {
        const string prefix = "Lockout";

        if (lockout.MaxFailedAttempts is < 0 or > 20)
            errors.Add($"{prefix}.{nameof(lockout.MaxFailedAttempts)} must be between 0 and 20 (got {lockout.MaxFailedAttempts}).");

        if (lockout.LockoutDuration < TimeSpan.Zero)
            errors.Add($"{prefix}.{nameof(lockout.LockoutDuration)} must be at least 0 (got {lockout.LockoutDuration}).");

        if (lockout.LockoutDuration > TimeSpan.FromHours(24))
            errors.Add($"{prefix}.{nameof(lockout.LockoutDuration)} must not exceed 24 hours.");
    }

    private static void ValidateTokens(TokenLifetimeOptions tokens, List<string> errors)
    {
        const string prefix = "Tokens";

        if (tokens.AccessTokenLifetime < TimeSpan.FromMinutes(1))
            errors.Add($"{prefix}.{nameof(tokens.AccessTokenLifetime)} must be at least 1 minute.");

        if (tokens.AccessTokenLifetime > TimeSpan.FromHours(24))
            errors.Add($"{prefix}.{nameof(tokens.AccessTokenLifetime)} must not exceed 24 hours.");

        if (tokens.RefreshTokenLifetime < TimeSpan.FromHours(1))
            errors.Add($"{prefix}.{nameof(tokens.RefreshTokenLifetime)} must be at least 1 hour.");

        if (tokens.RefreshTokenLifetime > TimeSpan.FromDays(90))
            errors.Add($"{prefix}.{nameof(tokens.RefreshTokenLifetime)} must not exceed 90 days.");

        if (tokens.RefreshTokenLifetime <= tokens.AccessTokenLifetime)
            errors.Add($"{prefix}.{nameof(tokens.RefreshTokenLifetime)} must be greater than {nameof(tokens.AccessTokenLifetime)}.");
    }
}
