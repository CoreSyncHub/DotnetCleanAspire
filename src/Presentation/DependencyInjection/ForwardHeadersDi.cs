using Microsoft.AspNetCore.HttpOverrides;

namespace Presentation.DependencyInjection;

internal static partial class PresentationDependencyInjection
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddForwardHeaders()
        {
            services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders =
                    ForwardedHeaders.XForwardedFor |
                    ForwardedHeaders.XForwardedProto |
                    ForwardedHeaders.XForwardedHost;

                options.KnownIPNetworks.Clear();
                options.KnownProxies.Clear();
            });

            return services;
        }
    }
}
