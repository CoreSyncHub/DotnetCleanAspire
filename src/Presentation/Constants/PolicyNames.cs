namespace Presentation.Constants;

/// <summary>
/// Contains constant names for policies used across the presentation layer.
/// </summary>
internal static class PolicyNames
{
    /// <summary>
    /// Rate limiting policy for sensitive endpoints (login, password reset, etc.).
    /// </summary>
    public const string StrictRateLimit = "strict";
}
