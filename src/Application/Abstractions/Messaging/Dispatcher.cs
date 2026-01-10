using Microsoft.Extensions.DependencyInjection;
using System.Collections.Concurrent;

namespace Application.Abstractions.Messaging;

/// <summary>
/// Default implementation of <see cref="IDispatcher"/>.
/// Resolves handlers from DI and executes pipeline behaviors.
/// </summary>
public sealed class Dispatcher(IServiceProvider serviceProvider) : IDispatcher
{
    private static readonly ConcurrentDictionary<Type, Type> HandlerTypeCache = new();

    public async Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        Type requestType = request.GetType();
        Type responseType = typeof(TResponse);

        // Get the handler type
        Type handlerType = HandlerTypeCache.GetOrAdd(
            requestType,
            type => typeof(IRequestHandler<,>).MakeGenericType(type, responseType));

        // Resolve the handler
        object handler = serviceProvider.GetService(handlerType)
            ?? throw new InvalidOperationException($"No handler registered for {requestType.Name}");

        // Get pipeline behaviors
        var behaviors = serviceProvider
            .GetServices<IPipelineBehavior<TResponse>>()
            .ToList();

        // Build the pipeline
        RequestHandlerDelegate<TResponse> pipeline = () => InvokeHandler(handler, request, cancellationToken);

        // Wrap with behaviors (in reverse order so first registered executes first)
        for (int i = behaviors.Count - 1; i >= 0; i--)
        {
            IPipelineBehavior<TResponse> behavior = behaviors[i];
            RequestHandlerDelegate<TResponse> next = pipeline;
            pipeline = () => behavior.Handle(request, next, cancellationToken);
        }

        return await pipeline();
    }

    public async Task Publish<TEvent>(TEvent domainEvent, CancellationToken cancellationToken = default)
        where TEvent : IDomainEvent
    {
        ArgumentNullException.ThrowIfNull(domainEvent);

        Type handlerType = typeof(IDomainEventHandler<>).MakeGenericType(domainEvent.GetType());
        IEnumerable<object?> handlers = serviceProvider.GetServices(handlerType);

        IEnumerable<Task> tasks = handlers
            .Cast<dynamic>()
            .Select(handler => (Task)handler.Handle((dynamic)domainEvent, cancellationToken));

        await Task.WhenAll(tasks);
    }

    private static async Task<TResponse> InvokeHandler<TResponse>(
        object handler,
        IRequest<TResponse> request,
        CancellationToken cancellationToken)
    {
        return await ((dynamic)handler).Handle((dynamic)request, cancellationToken);
    }
}

/// <summary>
/// Represents a delegate for the next step in the request pipeline.
/// </summary>
/// <typeparam name="TResponse">The type of response.</typeparam>
public delegate Task<TResponse> RequestHandlerDelegate<TResponse>();
