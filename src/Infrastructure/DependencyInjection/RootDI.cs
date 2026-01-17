using Microsoft.Extensions.Hosting;

namespace Infrastructure.DependencyInjection;

public static partial class InfrastructureDependencyInjectionRoot
{
    extension(IHostApplicationBuilder builder)
    {
        /// <summary>
        /// Adds Infrastructure layer services with Aspire integration.
        /// </summary>
        /// <returns>The host application builder for chaining.</returns>
        public IHostApplicationBuilder AddInfrastructure()
        {
            builder
                .AddAuth()
                .AddPersistence()
                .AddCaching();

            return builder;
        }
    }
}
