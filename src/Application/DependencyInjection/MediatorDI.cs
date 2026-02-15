using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace Application.DependencyInjection;

internal static partial class ApplicationDependencyInjection
{
    private static bool IsHandlerInterface(Type type)
    {
        return type == typeof(IRequestHandler<,>) ||
               type == typeof(ICommandHandler<,>) ||
               type == typeof(ICommandHandler<>) ||
               type == typeof(IQueryHandler<,>) ||
               type == typeof(IDomainEventHandler<>);
    }

    extension(IServiceCollection services)
    {
        /// <summary>
        /// Adds Application layer services with mediator configuration.
        /// </summary>
        /// <returns>The service collection for chaining.</returns>
        public IServiceCollection AddMediator()
        {
            var assembly = Assembly.GetExecutingAssembly();

            // Register Dispatcher
            services.AddScoped<IDispatcher, Dispatcher>();

            // Register all handlers
            services.AddHandlersFromAssembly(assembly);

            // Register all validators (includeInternalTypes to find internal validators)
            services.AddValidatorsFromAssembly(assembly, includeInternalTypes: true);

            // Register pipeline behaviors (order matters!)
            services.AddScoped(typeof(IPipelineBehavior<>), typeof(LoggingBehavior<>));
            services.AddScoped(typeof(IPipelineBehavior<>), typeof(ValidationBehavior<>));
            services.AddScoped(typeof(IPipelineBehavior<>), typeof(CachingBehavior<>));
            services.AddScoped(typeof(IPipelineBehavior<>), typeof(CacheInvalidationBehavior<>));

            return services;
        }

        private IServiceCollection AddHandlersFromAssembly(Assembly assembly)
        {
            // Find all handler types
            var handlerTypes = assembly.GetTypes()
                .Where(t => t is { IsClass: true, IsAbstract: false })
                .SelectMany(t => t.GetInterfaces()
                    .Where(i => i.IsGenericType && IsHandlerInterface(i.GetGenericTypeDefinition()))
                    .Select(i => new { Implementation = t, Interface = i }))
                .ToList();

            foreach (var handler in handlerTypes)
            {
                services.AddScoped(handler.Interface, handler.Implementation);
            }

            return services;
        }
    }
}
