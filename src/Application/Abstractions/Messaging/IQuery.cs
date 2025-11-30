namespace Application.Abstractions.Messaging;

/// <summary>
/// Represents a query that reads state and returns a result.
/// </summary>
/// <typeparam name="TResponse">The type of the response wrapped in a Result.</typeparam>
public interface IQuery<TResponse> : IRequest<Result<TResponse>>;
