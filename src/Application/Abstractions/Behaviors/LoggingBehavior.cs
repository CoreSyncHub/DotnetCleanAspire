using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Application.Abstractions.Behaviors;

/// <summary>
/// Pipeline behavior that logs request execution details.
/// </summary>
/// <typeparam name="TResponse">The type of response.</typeparam>
public sealed class LoggingBehavior<TResponse>(ILogger<LoggingBehavior<TResponse>> logger) : IPipelineBehavior<TResponse>
{
   private readonly ILogger<LoggingBehavior<TResponse>> _logger = logger;

   public async Task<TResponse> Handle(
       object request,
       RequestHandlerDelegate<TResponse> nextHandler,
       CancellationToken cancellationToken)
   {
      string requestName = request.GetType().Name;

      _logger.LogInformation("Handling {RequestName}", requestName);

      var stopwatch = Stopwatch.StartNew();

      try
      {
         TResponse? response = await nextHandler();

         stopwatch.Stop();

         _logger.LogInformation(
             "Handled {RequestName} in {ElapsedMilliseconds}ms",
             requestName,
             stopwatch.ElapsedMilliseconds);

         return response;
      }
      catch (Exception ex)
      {
         stopwatch.Stop();

         _logger.LogError(
             ex,
             "Error handling {RequestName} after {ElapsedMilliseconds}ms",
             requestName,
             stopwatch.ElapsedMilliseconds);

         throw;
      }
   }
}
