namespace Presentation.Middleware;

/// <summary>
/// Global exception handler that converts unhandled exceptions to ProblemDetails responses.
/// </summary>
internal sealed class GlobalExceptionHandler(
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
   public async ValueTask<bool> TryHandleAsync(
       HttpContext httpContext,
       Exception exception,
       CancellationToken cancellationToken)
   {
      logger.LogError(exception, "Unhandled exception occurred: {Message}", exception.Message);

      ProblemDetails problemDetails = exception switch
      {
         ArgumentException => CreateProblemDetails(
             StatusCodes.Status400BadRequest,
             "Bad Request",
             exception.Message),

         UnauthorizedAccessException => CreateProblemDetails(
             StatusCodes.Status401Unauthorized,
             "Unauthorized",
             "You are not authorized to access this resource."),

         InvalidOperationException => CreateProblemDetails(
             StatusCodes.Status422UnprocessableEntity,
             "Unprocessable Entity",
             exception.Message),

         _ => CreateProblemDetails(
             StatusCodes.Status500InternalServerError,
             "Internal Server Error",
             "An unexpected error occurred. Please try again later.")
      };

      problemDetails.Extensions["traceId"] = httpContext.TraceIdentifier;

      httpContext.Response.StatusCode = problemDetails.Status ?? StatusCodes.Status500InternalServerError;
      httpContext.Response.ContentType = "application/problem+json";

      await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

      return true;
   }

   private static ProblemDetails CreateProblemDetails(int statusCode, string title, string detail) =>
       new()
       {
          Status = statusCode,
          Title = title,
          Detail = detail,
          Type = $"https://httpstatuses.com/{statusCode}"
       };
}
