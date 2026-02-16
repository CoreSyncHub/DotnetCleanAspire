using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Application.Abstractions.DependencyInjection;

/// <summary>
/// A hosted service that validates all registered options at application startup.
/// This ensures configuration errors are detected early, before the application starts processing requests.
/// </summary>
internal sealed class ValidateOptionsOnStartup(IServiceProvider serviceProvider) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        // Get all registered option validation descriptors
        IEnumerable<IOptionsValidationDescriptor> descriptors =
            serviceProvider.GetServices<IOptionsValidationDescriptor>();

        List<string> allFailures = [];

        foreach (IOptionsValidationDescriptor descriptor in descriptors)
        {
            ValidateOptionsResult result = descriptor.Validate(serviceProvider);

            if (result.Failed)
            {
                string optionName = descriptor.OptionsType.Name;

                if (result.Failures is not null)
                {
                    allFailures.AddRange(result.Failures.Select(f => $"[{optionName}] {f}"));
                }
                else if (result.FailureMessage is not null)
                {
                    allFailures.Add($"[{optionName}] {result.FailureMessage}");
                }
            }
        }

        if (allFailures.Count > 0)
        {
            throw new OptionsValidationException(
                "Application Options",
                typeof(object),
                allFailures);
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

/// <summary>
/// Marker interface for options validation descriptors.
/// Used to discover and execute validators at startup.
/// </summary>
internal interface IOptionsValidationDescriptor
{
    /// <summary>
    /// Gets the type of options being validated.
    /// </summary>
    Type OptionsType { get; }

    /// <summary>
    /// Validates the options using the provided service provider.
    /// </summary>
    /// <param name="serviceProvider">The service provider to resolve dependencies.</param>
    /// <returns>The validation result.</returns>
    ValidateOptionsResult Validate(IServiceProvider serviceProvider);
}

/// <summary>
/// Typed implementation of the options validation descriptor.
/// </summary>
/// <typeparam name="TOptions">The type of options to validate.</typeparam>
internal sealed class OptionsValidationDescriptor<TOptions> : IOptionsValidationDescriptor
    where TOptions : class
{
    public Type OptionsType => typeof(TOptions);

    public ValidateOptionsResult Validate(IServiceProvider serviceProvider)
    {
        // Get the options instance
        IOptions<TOptions> options = serviceProvider.GetRequiredService<IOptions<TOptions>>();

        // Get all validators for this options type
        IEnumerable<IValidateOptions<TOptions>> validators =
            serviceProvider.GetServices<IValidateOptions<TOptions>>();

        List<string> failures = [];

        foreach (IValidateOptions<TOptions> validator in validators)
        {
            ValidateOptionsResult result = validator.Validate(null, options.Value);

            if (result.Failed)
            {
                if (result.Failures is not null)
                {
                    failures.AddRange(result.Failures);
                }
                else if (result.FailureMessage is not null)
                {
                    failures.Add(result.FailureMessage);
                }
            }
        }

        return failures.Count > 0
            ? ValidateOptionsResult.Fail(failures)
            : ValidateOptionsResult.Success;
    }
}
