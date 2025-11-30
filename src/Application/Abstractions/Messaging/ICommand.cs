namespace Application.Abstractions.Messaging;

/// <summary>
/// Represents a command that returns a result with no value.
/// </summary>
public interface ICommand : ICommand<Unit>;

/// <summary>
/// Represents a command that modifies state and returns a result.
/// </summary>
/// <typeparam name="TResponse">The type of the response wrapped in a Result.</typeparam>
public interface ICommand<TResponse> : IRequest<Result<TResponse>>;
