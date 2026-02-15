namespace Application.Features.Auth.Errors;

public static class AuthErrors
{
    /// <summary>
    /// Error codes for the Auth feature.
    /// </summary>
    public static class Codes
    {
        public const string UserNotAuthenticated = "Auth.UserNotAuthenticated";
        public const string AccessTokenRequired = "Auth.AccessTokenRequired";
        public const string RefreshTokenRequired = "Auth.RefreshTokenRequired";
        public const string ResetTokenRequired = "Auth.ResetTokenRequired";
        public const string InvalidCredentials = "Auth.InvalidCredentials";
        public const string RegistrationDisabled = "Auth.RegistrationDisabled";
        public const string LocalAuthDisabled = "Auth.LocalAuthDisabled";
        public const string UserNotFound = "Auth.UserNotFound";
        public const string AccountLocked = "Auth.AccountLocked";
        public const string PasswordRequired = "Auth.PasswordRequired";
        public const string PasswordTooShort = "Auth.PasswordTooShort";
        public const string PasswordsDoNotMatch = "Auth.PasswordsDoNotMatch";
        public const string EmailRequired = "Auth.EmailRequired";
        public const string InvalidEmail = "Auth.InvalidEmail";
        public const string EmailNotConfirmed = "Auth.EmailNotConfirmed";
        public const string EmailAlreadyExists = "Auth.EmailAlreadyExists";
        public const string InvalidToken = "Auth.InvalidToken";
        public const string InvalidRefreshToken = "Auth.InvalidRefreshToken";
        public const string InvalidOrExpiredCode = "Auth.InvalidOrExpiredCode";
        public const string TwoFactorRequired = "Auth.TwoFactorRequired";
        public const string Invalid2FACode = "Auth.Invalid2FACode";
        public const string ExternalLoginFailed = "Auth.ExternalLoginFailed";
        public const string OidcNotEnabled = "Auth.OidcNotEnabled";
        public const string OidcAuthenticationFailed = "Auth.OidcAuthenticationFailed";
    }

    public static ResultError UserNotAuthenticated => new(
        Codes.UserNotAuthenticated,
        "User is not authenticated.",
        ErrorType.Unauthorized);

    public static ResultError AccessTokenRequired => new(
        Codes.AccessTokenRequired,
        "You must be logged in to perform this action.",
        ErrorType.Validation);

    public static ResultError RefreshTokenRequired => new(
        Codes.RefreshTokenRequired,
        "Your session has expired. Please sign in again.",
        ErrorType.Validation);

    public static ResultError ResetTokenRequired => new(
        Codes.ResetTokenRequired,
        "This password reset link is no longer valid. Please request a new one.",
        ErrorType.Validation);

    public static ResultError InvalidCredentials => new(
        Codes.InvalidCredentials,
        "Invalid email or password.",
        ErrorType.Unauthorized);

    public static ResultError RegistrationDisabled => new(
        Codes.RegistrationDisabled,
        "Local registration is disabled.",
        ErrorType.Forbidden);

    public static ResultError LocalAuthDisabled => new(
        Codes.LocalAuthDisabled,
        "Local authentication is disabled. Please use SSO.",
        ErrorType.Forbidden);

    public static ResultError UserNotFound => new(
        Codes.UserNotFound,
        "User not found.",
        ErrorType.NotFound);

    public static ResultError AccountLocked => new(
        Codes.AccountLocked,
        "Account is locked due to too many failed attempts. Please try again later.",
        ErrorType.Forbidden);

    public static ResultError PasswordRequired => new(
        Codes.PasswordRequired,
        "Password is required.",
        ErrorType.Validation);

    public static ResultError PasswordTooShort(int minLength) => new(
        Codes.PasswordTooShort,
        "Password is too short. It must be at least {0} characters long.",
        [minLength],
        ErrorType.Validation);

    public static ResultError PasswordsDoNotMatch => new(
        Codes.PasswordsDoNotMatch,
        "Passwords do not match.",
        ErrorType.Validation);

    public static ResultError EmailRequired => new(
        Codes.EmailRequired,
        "Email address is required.",
        ErrorType.Validation);

    public static ResultError InvalidEmail => new(
        Codes.InvalidEmail,
        "The email address is not valid.",
        ErrorType.Validation);

    public static ResultError EmailAlreadyExists => new(
        Codes.EmailAlreadyExists,
        "An account with this email already exists.",
        ErrorType.Conflict);

    public static ResultError EmailNotConfirmed => new(
        Codes.EmailNotConfirmed,
        "Email address has not been confirmed.",
        ErrorType.Forbidden);

    public static ResultError InvalidToken => new(
        Codes.InvalidToken,
        "The token is invalid or has expired.",
        ErrorType.Unauthorized);

    public static ResultError InvalidRefreshToken => new(
        Codes.InvalidRefreshToken,
        "Invalid or expired refresh token.",
        ErrorType.Unauthorized);

    public static ResultError InvalidOrExpiredCode => new(
        Codes.InvalidOrExpiredCode,
        "Invalid or expired authorization code.",
        ErrorType.Unauthorized);

    public static ResultError TwoFactorRequired => new(
        Codes.TwoFactorRequired,
        "Two-factor authentication code is required.",
        ErrorType.Unauthorized);

    public static ResultError Invalid2FACode => new(
        Codes.Invalid2FACode,
        "Invalid two-factor authentication code.",
        ErrorType.Unauthorized);

    public static ResultError ExternalLoginFailed => new(
        Codes.ExternalLoginFailed,
        "External authentication failed.",
        ErrorType.Unauthorized);

    public static ResultError OidcNotEnabled => new(
        Codes.OidcNotEnabled,
        "OIDC authentication is not enabled.",
        ErrorType.NotFound);

    public static ResultError OidcAuthenticationFailed => new(
        Codes.OidcAuthenticationFailed,
        "Unable to retrieve user identifier from OIDC provider.",
        ErrorType.Unauthorized);
}
