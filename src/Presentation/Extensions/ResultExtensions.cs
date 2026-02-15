using Presentation.Abstractions;
using HttpResult = Microsoft.AspNetCore.Http.IResult;

namespace Presentation.Extensions;

/// <summary>
/// Extensions for converting <see cref="Result{T}"/> to <see cref="HttpResult"/>.
/// </summary>
internal static class ResultExtensions
{
   extension<T>(Result<T> result)
   {
      /// <summary>
      /// Converts a <see cref="Result{T}"/> to an appropriate <see cref="HttpResult"/>.
      /// </summary>
      /// <param name="result">The result to convert.</param>
      /// <param name="transform">Optional transformation function for the value.</param>
      /// <returns>An HTTP result representing the outcome.</returns>
      public HttpResult ToHttpResult(Func<T, object?>? transform = null)
      {
         return result.Match(
            onSuccess: value => ToSuccessResult(value, result.SuccessType, transform),
            onFailure: ToErrorResult);
      }
   }

   private static HttpResult ToSuccessResult<T>(T value, SuccessType successType, Func<T, object?>? transform)
   {
      object? responseData = transform is not null ? transform(value) : value;

      return successType switch
      {
         SuccessType.Created => Results.Created(string.Empty, ApiResponse<object?>.From(responseData)),
         SuccessType.NoContent => Results.NoContent(),
         _ => Results.Ok(ApiResponse<object?>.From(responseData))
      };
   }

   private static HttpResult ToErrorResult(ResultError error)
   {
      // For validation errors with structured errors dictionary, use ValidationProblem
      if (error.Type == ErrorType.Validation && error.ValidationErrors is not null)
      {
         return Results.ValidationProblem(
             error.ValidationErrors,
             title: "Validation Failed",
             extensions: new Dictionary<string, object?>
             {
                ["code"] = error.Code
             });
      }

      int statusCode = error.Type switch
      {
         ErrorType.Validation => StatusCodes.Status400BadRequest,
         ErrorType.NotFound => StatusCodes.Status404NotFound,
         ErrorType.Conflict => StatusCodes.Status409Conflict,
         ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
         ErrorType.Forbidden => StatusCodes.Status403Forbidden,
         _ => StatusCodes.Status500InternalServerError
      };

      return Results.Problem(
          statusCode: statusCode,
          title: GetTitleForStatusCode(statusCode),
          detail: error.Message,
          extensions: new Dictionary<string, object?>
          {
             ["code"] = error.Code
          });
   }

   private static string GetTitleForStatusCode(int statusCode) => statusCode switch
   {
      StatusCodes.Status400BadRequest => "Bad Request",
      StatusCodes.Status401Unauthorized => "Unauthorized",
      StatusCodes.Status403Forbidden => "Forbidden",
      StatusCodes.Status404NotFound => "Not Found",
      StatusCodes.Status409Conflict => "Conflict",
      _ => "Internal Server Error"
   };
}
