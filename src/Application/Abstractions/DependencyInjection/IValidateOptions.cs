namespace Application.Abstractions.DependencyInjection;

public interface IValidateOptions<in TOptions> where TOptions : class
{
    /// <summary>
    /// Validates the options instance.
    /// </summary>
    /// <returns>True if the options are valid; otherwise, false.</returns>
    ValidateOptionsResult Validate(string? name, TOptions options);
}
