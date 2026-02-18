using Presentation.Constants;

namespace Presentation.Extensions;

internal static class RouteHandlerBuilderExtensions
{
    extension(RouteHandlerBuilder builder)
    {
        public RouteHandlerBuilder RequireStrictRateLimiting()
        {
            return builder.RequireRateLimiting(PolicyNames.StrictRateLimit);
        }
    }
}
