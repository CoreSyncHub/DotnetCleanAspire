namespace Application.Abstractions.Messaging;

/// <summary>
/// Marker interface for all requests (commands and queries).
/// </summary>
/// <typeparam name="TResponse">The type of the response.</typeparam>
public interface IRequest<out TResponse>;
