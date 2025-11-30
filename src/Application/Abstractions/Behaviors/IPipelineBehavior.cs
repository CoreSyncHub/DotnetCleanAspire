namespace Application.Abstractions.Behaviors;

/// <summary>
/// Defines a pipeline behavior that wraps request handling.
/// Behaviors are executed in the order they are registered.
/// </summary>
/// <typeparam name="TResponse">The type of response from the handler.</typeparam>
public interface IPipelineBehavior<TResponse>
{
   /// <summary>
   /// Pipeline handler. Perform any additional behavior and call next() to continue the pipeline.
   /// </summary>
   /// <param name="request">The incoming request.</param>
   /// <param name="nextHandler">The delegate for the next action in the pipeline.</param>
   /// <param name="cancellationToken">Cancellation token.</param>
   /// <returns>The response.</returns>
   Task<TResponse> Handle(
       object request,
       RequestHandlerDelegate<TResponse> nextHandler,
       CancellationToken cancellationToken);
}
