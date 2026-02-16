namespace Application.DependencyInjection.Options;

public sealed class AuthIdentityOptions
{
    public const string SectionName = "Identity";

    /// <summary>
    /// Indicates whether local user registration is enabled.
    /// Default: true
    /// </summary>
    public bool EnableLocalRegistration { get; init; } = true;

    /// <summary>
    /// Indicates whether email confirmation is required for new users.
    /// Default: false
    /// </summary>
    public bool RequireEmailConfirmation { get; init; }

    /// <summary>
    /// Indicates whether two-factor authentication (2FA) is enabled.
    /// Default: false
    /// </summary>
    public bool Enable2FA { get; init; }

    /// <summary>
    /// Password policy settings.
    /// </summary>
    public PasswordPolicyOptions PasswordPolicy { get; init; } = new();

    /// <summary>
    /// Account lockout settings.
    /// </summary>
    public AuthLockoutOptions Lockout { get; init; } = new();

    /// <summary>
    /// Token lifetime settings.
    /// </summary>
    public TokenLifetimeOptions Tokens { get; init; } = new();
}

public sealed class PasswordPolicyOptions
{
    /// <summary>
    /// Indicates whether at least one digit is required in the password.
    /// Default: true
    /// </summary>
    public bool RequireDigit { get; init; } = true;

    /// <summary>
    /// Indicates whether at least one lowercase letter is required in the password.
    /// Default: true
    /// </summary>
    public bool RequireLowercase { get; init; } = true;

    /// <summary>
    /// Indicates whether at least one uppercase letter is required in the password.
    /// Default: true
    /// </summary>
    public bool RequireUppercase { get; init; } = true;

    /// <summary>
    /// Indicates whether at least one non-alphanumeric character is required in the password.
    /// Default: true
    /// </summary>
    public bool RequireNonAlphanumeric { get; init; } = true;

    /// <summary>
    /// The minimum required length of the password.
    /// Default: 8
    /// </summary>
    public int MinimumLength { get; init; } = 8;
}

public sealed class AuthLockoutOptions
{
    /// <summary>
    /// The maximum number of failed access attempts before lockout.
    /// Default: 5
    /// </summary>
    public int MaxFailedAttempts { get; init; } = 5;

    /// <summary>
    /// The duration of the lockout period.
    /// Default: 15 minutes
    /// Set to TimeSpan.Zero for indefinite lockout until manually reset (or 00:00:00 in appsettings.json).
    /// </summary>
    public TimeSpan LockoutDuration { get; init; } = TimeSpan.FromMinutes(15);
}

public sealed class TokenLifetimeOptions
{
    /// <summary>
    /// The lifetime of access tokens.
    /// Default: 15 minutes
    /// </summary>
    public TimeSpan AccessTokenLifetime { get; init; } = TimeSpan.FromMinutes(15);

    /// <summary>
    /// The lifetime of refresh tokens.
    /// Default: 7 days
    /// </summary>
    public TimeSpan RefreshTokenLifetime { get; init; } = TimeSpan.FromDays(7);

    /// <summary>
    /// Indicates whether refresh token rotation is enabled.
    /// When enabled, a new refresh token is issued each time the access token is refreshed, and the old refresh token is invalidated. This enhances security by reducing the risk of token theft, but requires clients to handle token updates more carefully.
    /// Default: true
    /// </summary>
    public bool RefreshTokenRotation { get; init; } = true;
}
