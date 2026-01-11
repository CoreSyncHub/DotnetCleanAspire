# Dispatcher Implementation Alternatives

## Current Implementation (Using `dynamic`)

**Advantages:**
- Simple and concise
- Performance cached via `HandlerTypeCache`
- Standard pattern used by MediatR and similar libraries

**Trade-offs:**
- Uses `dynamic` keyword (runtime type resolution)
- Loses compile-time type safety within implementation
- IDE IntelliSense doesn't work on dynamic calls

## Alternative 1: Pure Reflection (More Verbose)

```csharp
private static async Task<TResponse> InvokeHandler<TResponse>(
    object handler,
    IRequest<TResponse> request,
    CancellationToken cancellationToken)
{
    MethodInfo? handleMethod = handler.GetType()
        .GetMethod("Handle", [request.GetType(), typeof(CancellationToken)]);

    if (handleMethod is null)
        throw new InvalidOperationException($"Handler does not have Handle method");

    object? result = handleMethod.Invoke(handler, [request, cancellationToken]);

    if (result is Task<TResponse> task)
        return await task;

    throw new InvalidOperationException($"Handler returned unexpected type");
}
```

**Verdict:** More code, no real benefit over `dynamic`

## Alternative 2: Generic Constraints (Not Possible)

This would be ideal but C# doesn't support:
```csharp
// ❌ This doesn't compile
private static async Task<TResponse> InvokeHandler<TRequest, TResponse>(
    IRequestHandler<TRequest, TResponse> handler,  // Can't cast object to this
    TRequest request,
    CancellationToken cancellationToken)
    where TRequest : IRequest<TResponse>
{
    return await handler.Handle(request, cancellationToken);
}
```

**Verdict:** Type system limitation

## Alternative 3: Source Generators (Future)

With C# source generators, you could generate strongly-typed dispatcher code at compile time:

```csharp
// Generated code
public async Task<CreateTodoDto> Send(CreateTodoCommand request, ...)
{
    var handler = serviceProvider.GetRequiredService<ICommandHandler<CreateTodoCommand, CreateTodoDto>>();
    return await handler.Handle(request, cancellationToken);
}
```

**Verdict:**
- Best type safety
- Requires build-time code generation
- More complex setup
- Possibly future improvement

## Recommendation

**Keep the current `dynamic` implementation** because:

1. It's the industry standard (MediatR, Wolverine, etc. all use similar approaches)
2. The performance impact is negligible with caching
3. The API surface is type-safe (users never see `dynamic`)
4. Alternatives are more complex without significant benefit
5. Well-documented usage (see XML docs on Dispatcher class)

The use of `dynamic` here is a **pragmatic choice** - it's the simplest solution that works reliably.
