namespace Application.Abstractions.DependencyInjection;

public class ValidateOptionsResult
{
    /// <summary>
    /// Indicates that the options validation failed. If true, the FailureMessage or Failures properties should provide details about the failure.
    /// </summary>
    public bool Failed { get; protected set; }

    /// <summary>
    /// Indicates that the options validation succeeded. If true, the options are valid and can be used as configured.
    /// </summary>
    public bool Succeeded { get; protected set; }

    /// <summary>
    /// Indicates that the options validation was skipped. If true, the validation logic was not executed, and the options should be treated as valid by default. This can be used in scenarios where validation is optional or when certain conditions are not met to perform validation.
    /// </summary>
    public bool Skipped { get; protected set; }

    /// <summary>
    /// Provides a message describing the reason for validation failure. This should be set when the Failed property is true to give context about what went wrong during validation.
    /// </summary>
    public string? FailureMessage { get; protected set; }

    /// <summary>
    /// Provides a collection of messages describing multiple reasons for validation failure. This should be set when the Failed property is true to give detailed context about what went wrong during validation, especially when there are multiple issues to report.
    /// </summary>
    public IEnumerable<string>? Failures { get; protected set; }

    /// <summary>
    /// Creates a successful validation result. This static property can be used to quickly return a successful validation outcome without needing to instantiate a new object each time.
    /// </summary>
    public static ValidateOptionsResult Success { get; } = new ValidateOptionsResult { Succeeded = true };

    /// <summary>
    /// Creates a skipped validation result. This static property can be used to indicate that validation was intentionally skipped, allowing the options to be treated as valid by default without executing any validation logic.
    /// </summary>
    public static ValidateOptionsResult Skip { get; } = new ValidateOptionsResult { Skipped = true };

    /// <summary>
    /// Creates a failed validation result with a specific failure message. This static method allows you to easily generate a validation failure outcome by providing a descriptive message that explains why the validation failed.
    /// </summary>
    /// <param name="failureMessage">A message describing the reason for validation failure. This should provide context about what went wrong during validation.</param>
    public static ValidateOptionsResult Fail(string failureMessage)
    {
        return new ValidateOptionsResult { Failed = true, FailureMessage = failureMessage };
    }

    /// <summary>
    /// Creates a failed validation result with multiple failure messages. This static method allows you to generate a validation failure outcome by providing a collection of messages that explain the various reasons for validation failure, which can be especially useful when there are multiple issues to report during validation.
    /// </summary>
    /// <param name="failures">A collection of messages describing the reasons for validation failure. Each message should provide context about what went wrong during validation, allowing for a comprehensive understanding of the issues encountered.</param>
    public static ValidateOptionsResult Fail(IEnumerable<string> failures)
    {
        return new ValidateOptionsResult { Failed = true, Failures = failures };
    }
}
