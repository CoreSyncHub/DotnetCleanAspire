using Presentation.Constants;
using System.Threading.RateLimiting;

namespace Presentation.DependencyInjection;

internal static partial class PresentationDependencyInjection
{
    extension(IServiceCollection services)
    {
        public IServiceCollection ConfigureRateLimiter(IConfiguration configuration)
        {
            services.AddRateLimiter(options =>
            {
                // Return 429 Too Many Requests
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

                // Global fixed window limiter (default)
                options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
             {
                 // Partition by authenticated user ID or IP address
                 string partitionKey = context.User.Identity?.IsAuthenticated == true
                 ? context.User.FindFirst("sub")?.Value ?? context.User.Identity.Name ?? "anonymous"
                 : context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

                 int permitLimit = configuration.GetValue("RateLimiting:PermitLimit", 100);
                 int windowSeconds = configuration.GetValue("RateLimiting:WindowSeconds", 60);

                 return RateLimitPartition.GetFixedWindowLimiter(partitionKey, _ => new FixedWindowRateLimiterOptions
                 {
                     PermitLimit = permitLimit,
                     Window = TimeSpan.FromSeconds(windowSeconds),
                     QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                     QueueLimit = 0 // No queuing, reject immediately
                 });
             });

                // Named policy for stricter endpoints (e.g., login, password reset)
                options.AddPolicy(PolicyNames.StrictRateLimit, context =>
             {
                 string partitionKey = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

                 return RateLimitPartition.GetFixedWindowLimiter(partitionKey, _ => new FixedWindowRateLimiterOptions
                 {
                     PermitLimit = configuration.GetValue("RateLimiting:StrictPermitLimit", 10),
                     Window = TimeSpan.FromSeconds(configuration.GetValue("RateLimiting:StrictWindowSeconds", 60)),
                     QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                     QueueLimit = 0
                 });
             });
            });

            return services;
        }
    }
}
