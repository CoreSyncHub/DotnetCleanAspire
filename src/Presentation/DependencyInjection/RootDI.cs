namespace Presentation.DependencyInjection;

internal static class PresentationDependencyInjectionRoot
{
    extension(IHostApplicationBuilder builder)
    {
        public IHostApplicationBuilder AddPresentation()
        {
            IConfigurationManager configuration = builder.Configuration;
            IHostEnvironment environment = builder.Environment;

            builder.AddObservability();
            builder.Services
                .AddOpenApi()
                .AddApiVersioning()
                .ConfigureCors(configuration, environment)
                .ConfigureRateLimiter(configuration)
                .AddExceptionHandling()
                .ConfigureJsonOptions();
            return builder;
        }
    }
}
