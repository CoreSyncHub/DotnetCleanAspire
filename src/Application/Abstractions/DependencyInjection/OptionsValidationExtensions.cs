using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Application.Abstractions.DependencyInjection;

/// <summary>
/// Extension methods for registering options with validation at startup.
/// </summary>
public static class OptionsValidationExtensions
{
    /// <summary>
    /// Adds options with validation at startup.
    /// The options will be bound from the configuration section and validated when the application starts.
    /// </summary>
    /// <typeparam name="TOptions">The type of options to configure.</typeparam>
    /// <typeparam name="TValidator">The validator type that implements <see cref="IValidateOptions{TOptions}"/>.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration to bind from.</param>
    /// <param name="sectionName">The configuration section name.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddOptionsWithValidation<TOptions, TValidator>(
        this IServiceCollection services,
        IConfiguration configuration,
        string sectionName)
        where TOptions : class
        where TValidator : class, IValidateOptions<TOptions>
    {
        // Configure the options from the configuration section
        services.Configure<TOptions>(configuration.GetSection(sectionName));

        // Register the validator
        services.AddSingleton<IValidateOptions<TOptions>, TValidator>();

        // Register the validation descriptor for startup validation
        services.AddSingleton<IOptionsValidationDescriptor, OptionsValidationDescriptor<TOptions>>();

        // Ensure the startup validator is registered (only once)
        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<Microsoft.Extensions.Hosting.IHostedService, ValidateOptionsOnStartup>());

        return services;
    }

    /// <summary>
    /// Adds options with validation at startup using a validator instance.
    /// </summary>
    /// <typeparam name="TOptions">The type of options to configure.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration to bind from.</param>
    /// <param name="sectionName">The configuration section name.</param>
    /// <param name="validator">The validator instance.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddOptionsWithValidation<TOptions>(
        this IServiceCollection services,
        IConfiguration configuration,
        string sectionName,
        IValidateOptions<TOptions> validator)
        where TOptions : class
    {
        // Configure the options from the configuration section
        services.Configure<TOptions>(configuration.GetSection(sectionName));

        // Register the validator instance
        services.AddSingleton(validator);

        // Register the validation descriptor for startup validation
        services.AddSingleton<IOptionsValidationDescriptor, OptionsValidationDescriptor<TOptions>>();

        // Ensure the startup validator is registered (only once)
        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<Microsoft.Extensions.Hosting.IHostedService, ValidateOptionsOnStartup>());

        return services;
    }

    /// <summary>
    /// Adds options with validation at startup using a validation delegate.
    /// </summary>
    /// <typeparam name="TOptions">The type of options to configure.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration to bind from.</param>
    /// <param name="sectionName">The configuration section name.</param>
    /// <param name="validate">The validation delegate.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddOptionsWithValidation<TOptions>(
        this IServiceCollection services,
        IConfiguration configuration,
        string sectionName,
        Func<TOptions, ValidateOptionsResult> validate)
        where TOptions : class
    {
        // Configure the options from the configuration section
        services.Configure<TOptions>(configuration.GetSection(sectionName));

        // Register the validator using a delegate wrapper
        services.AddSingleton<IValidateOptions<TOptions>>(new DelegateOptionsValidator<TOptions>(validate));

        // Register the validation descriptor for startup validation
        services.AddSingleton<IOptionsValidationDescriptor, OptionsValidationDescriptor<TOptions>>();

        // Ensure the startup validator is registered (only once)
        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<Microsoft.Extensions.Hosting.IHostedService, ValidateOptionsOnStartup>());

        return services;
    }

    /// <summary>
    /// Registers an additional validator for options that are already configured.
    /// </summary>
    /// <typeparam name="TOptions">The type of options to validate.</typeparam>
    /// <typeparam name="TValidator">The validator type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddOptionsValidator<TOptions, TValidator>(this IServiceCollection services)
        where TOptions : class
        where TValidator : class, IValidateOptions<TOptions>
    {
        services.AddSingleton<IValidateOptions<TOptions>, TValidator>();
        return services;
    }
}

/// <summary>
/// Internal validator that wraps a validation delegate.
/// </summary>
internal sealed class DelegateOptionsValidator<TOptions>(Func<TOptions, ValidateOptionsResult> validate)
    : IValidateOptions<TOptions>
    where TOptions : class
{
    public ValidateOptionsResult Validate(string? name, TOptions options) => validate(options);
}
