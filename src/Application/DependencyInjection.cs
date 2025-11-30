using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace Application;

/// <summary>
/// Extension methods for registering Application layer services.
/// </summary>
public static class ApplicationDependencyInjection
{
   /// <summary>
   /// Adds Application layer services to the service collection.
   /// </summary>
   /// <param name="services">The service collection.</param>
   /// <returns>The service collection for chaining.</returns>
   public static IServiceCollection AddApplication(this IServiceCollection services)
   {
      var assembly = Assembly.GetExecutingAssembly();

      // Register Dispatcher
      services.AddScoped<IDispatcher, Dispatcher>();

      // Register all handlers
      services.AddHandlers(assembly);

      // Register all validators
      services.AddValidatorsFromAssembly(assembly);

      // Register pipeline behaviors (order matters!)
      services.AddScoped(typeof(IPipelineBehavior<>), typeof(LoggingBehavior<>));
      services.AddScoped(typeof(IPipelineBehavior<>), typeof(ValidationBehavior<>));
      services.AddScoped(typeof(IPipelineBehavior<>), typeof(CachingBehavior<>));

      return services;
   }

   private static IServiceCollection AddHandlers(this IServiceCollection services, Assembly assembly)
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

   private static bool IsHandlerInterface(Type type)
   {
      return type == typeof(IRequestHandler<,>) ||
             type == typeof(ICommandHandler<,>) ||
             type == typeof(ICommandHandler<>) ||
             type == typeof(IQueryHandler<,>) ||
             type == typeof(IDomainEventHandler<>);
   }
}
