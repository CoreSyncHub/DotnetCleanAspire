using Microsoft.AspNetCore.Cors.Infrastructure;

namespace Presentation.DependencyInjection;

internal static partial class PresentationDependencyInjection
{
    extension(IServiceCollection services)
    {
        public IServiceCollection ConfigureCors(IConfiguration configuration, IHostEnvironment environment)
        {
            Action<CorsOptions> corsOptions = environment.IsDevelopment()
                ? DevelopmentCorsOptions
                : GetProductionCorsOptions(configuration);

            services.AddCors(corsOptions);
            return services;
        }
    }

    private static readonly Action<CorsOptions> DevelopmentCorsOptions = options =>
    {
        options.AddDefaultPolicy(policy =>
        {
            policy.AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        });
    };

    private static Action<CorsOptions> GetProductionCorsOptions(IConfiguration configuration) => options =>
    {
        string[] allowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];

        options.AddDefaultPolicy(policy =>
        {
            if (allowedOrigins.Length > 0)
            {
                policy.WithOrigins(allowedOrigins)
                      .AllowAnyMethod()
                      .AllowAnyHeader()
                      .AllowCredentials();
            }
            else
            {
                // No origins configured = block all cross-origin requests
                policy.SetIsOriginAllowed(_ => false);
            }
        });
    };
}
