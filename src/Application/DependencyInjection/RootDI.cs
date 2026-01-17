using Microsoft.Extensions.Hosting;

namespace Application.DependencyInjection;

public static class ApplicationDependencyInjectionRoot
{
    extension(IHostApplicationBuilder builder)
    {
        public IHostApplicationBuilder AddApplication()
        {
            builder.Services.AddMediator();
            return builder;
        }
    }
}
