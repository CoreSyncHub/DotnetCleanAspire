namespace Application.Abstractions.DependencyInjection;

#pragma warning disable CA1032 // Implement standard exception constructors
public class OptionsValidationException : Exception
{
    public OptionsValidationException(
        string optionsName,
        Type optionsType,
        IEnumerable<string>? failureMessages)
    {
        OptionsName = optionsName;
        OptionsType = optionsType;
        Failures = failureMessages ?? [];
        Message = $"Validation failed for options '{optionsName}' of type '{optionsType.FullName}'. Failures: {string.Join("; ", Failures)}";
    }

    public string OptionsName { get; }
    public Type OptionsType { get; }
    public IEnumerable<string> Failures { get; }
    public override string Message { get; }
}
#pragma warning restore CA1032 // Implement standard exception constructors
