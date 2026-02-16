namespace Application.Abstractions.DependencyInjection;

/// <summary>
/// Base class for implementing options validation.
/// Inherit from this class and override the <see cref="Validate"/> method to provide custom validation logic.
/// </summary>
/// <typeparam name="TOptions">The type of options to validate.</typeparam>
public abstract class OptionsValidator<TOptions> : IValidateOptions<TOptions>
    where TOptions : class
{
    /// <summary>
    /// The name of the options type, used in error messages.
    /// </summary>
    protected virtual string OptionsName => typeof(TOptions).Name;

    /// <summary>
    /// Validates the options instance.
    /// </summary>
    /// <param name="name">The name of the options instance being validated.</param>
    /// <param name="options">The options instance to validate.</param>
    /// <returns>A <see cref="ValidateOptionsResult"/> indicating success or failure.</returns>
    public abstract ValidateOptionsResult Validate(string? name, TOptions options);

    /// <summary>
    /// Creates a failure result with a formatted message.
    /// </summary>
    /// <param name="propertyName">The name of the property that failed validation.</param>
    /// <param name="message">The validation error message.</param>
    /// <returns>A failed <see cref="ValidateOptionsResult"/>.</returns>
    protected static ValidateOptionsResult Fail(string propertyName, string message)
    {
        return ValidateOptionsResult.Fail($"{propertyName}: {message}");
    }

    /// <summary>
    /// Validates that a string property is not null or whitespace.
    /// </summary>
    /// <param name="value">The value to validate.</param>
    /// <param name="propertyName">The name of the property.</param>
    /// <param name="errors">The list to add errors to.</param>
    protected static void ValidateRequired(string? value, string propertyName, IList<string> errors)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            errors.Add($"{propertyName} is required and cannot be empty.");
        }
    }

    /// <summary>
    /// Validates that a value is greater than zero.
    /// </summary>
    /// <param name="value">The value to validate.</param>
    /// <param name="propertyName">The name of the property.</param>
    /// <param name="errors">The list to add errors to.</param>
    protected static void ValidatePositive(int value, string propertyName, IList<string> errors)
    {
        if (value <= 0)
        {
            errors.Add($"{propertyName} must be greater than zero.");
        }
    }

    /// <summary>
    /// Validates that a TimeSpan is positive.
    /// </summary>
    /// <param name="value">The value to validate.</param>
    /// <param name="propertyName">The name of the property.</param>
    /// <param name="errors">The list to add errors to.</param>
    protected static void ValidatePositive(TimeSpan value, string propertyName, IList<string> errors)
    {
        if (value <= TimeSpan.Zero)
        {
            errors.Add($"{propertyName} must be a positive duration.");
        }
    }

    /// <summary>
    /// Validates a property using a custom validation function, but only if the value is not null.
    /// This allows for optional properties that still need to be validated if provided.
    /// </summary>
    /// <param name="value">The value to validate.</param>
    /// <param name="propertyName">The name of the property.</param>
    /// <param name="errors">The list to add errors to.</param>
    /// <param name="validationFunc">The custom validation function to apply if the value is not null.</param>
    protected static void ValidateIfNotRequired(string? value, string propertyName, IList<string> errors, Action<string, string, IList<string>> validationFunc)
    {
        if (value != null)
        {
            validationFunc(value, propertyName, errors);
        }
    }

    /// <summary>
    /// Creates a result from a list of errors.
    /// Returns success if the list is empty, otherwise returns failure with all errors.
    /// </summary>
    /// <param name="errors">The list of validation errors.</param>
    /// <returns>A <see cref="ValidateOptionsResult"/>.</returns>
    protected static ValidateOptionsResult ToResult(IList<string> errors)
    {
        return errors.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(errors);
    }
}
