using Presentation.Converters;
using System.Text.Json.Serialization.Metadata;

namespace Presentation.DependencyInjection;

internal static partial class PresentationDependencyInjection
{
    extension(IServiceCollection services)
    {
        public IServiceCollection ConfigureJsonOptions()
        {
            services.ConfigureHttpJsonOptions(options =>
            {
                options.SerializerOptions.Converters.Add(new IdJsonConverter());
                options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
                options.SerializerOptions.TypeInfoResolver = new DefaultJsonTypeInfoResolver
                {
                    Modifiers = { CacheInterfacePropertyFilter.ExcludeCacheProperties }
                };
            });

            return services;
        }
    }
}
