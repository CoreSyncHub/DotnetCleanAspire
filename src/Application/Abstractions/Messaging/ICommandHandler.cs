namespace Application.Abstractions.Messaging;

/// <summary>
/// Defines a handler for a command that returns no value.
/// </summary>
/// <typeparam name="TCommand">The type of command being handled.</typeparam>
public interface ICommandHandler<TCommand> : ICommandHandler<TCommand, Unit>
    where TCommand : ICommand;

/// <summary>
/// Defines a handler for a command.
/// </summary>
/// <typeparam name="TCommand">The type of command being handled.</typeparam>
/// <typeparam name="TResponse">The type of response from the handler.</typeparam>
public interface ICommandHandler<TCommand, TResponse> : IRequestHandler<TCommand, Result<TResponse>>
    where TCommand : ICommand<TResponse>;
