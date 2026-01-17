using Presentation.Middleware;

namespace Presentation.DependencyInjection;

internal static partial class PresentationDependencyInjection
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddExceptionHandling()
        {
            services.AddExceptionHandler<GlobalExceptionHandler>();
            services.AddProblemDetails(options =>
            {
                options.CustomizeProblemDetails = context =>
               {
                   context.ProblemDetails.Extensions["traceId"] = context.HttpContext.TraceIdentifier;
               };
            });

            return services;
        }
    }
}
