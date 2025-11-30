using FluentValidation.Results;
using System.Reflection;

namespace Application.Abstractions.Behaviors;

/// <summary>
/// Pipeline behavior that validates commands using FluentValidation.
/// Only applies to commands (ICommand), not queries.
/// </summary>
/// <typeparam name="TResponse">The type of response.</typeparam>
public sealed class ValidationBehavior<TResponse>(IServiceProvider serviceProvider) : IPipelineBehavior<TResponse>
{
   private readonly IServiceProvider _serviceProvider = serviceProvider;

   public async Task<TResponse> Handle(
       object request,
       RequestHandlerDelegate<TResponse> nextHandler,
       CancellationToken cancellationToken)
   {
      // Only validate commands
      Type requestType = request.GetType();
      if (!IsCommand(requestType))
      {
         return await nextHandler();
      }

      // Get validator for this request type
      Type validatorType = typeof(IValidator<>).MakeGenericType(requestType);

      if (_serviceProvider.GetService(validatorType) is not IValidator validator)
      {
         return await nextHandler();
      }

      // Validate
      var validationContext = new ValidationContext<object>(request);
      ValidationResult validationResult = await validator.ValidateAsync(validationContext, cancellationToken);

      if (validationResult.IsValid)
      {
         return await nextHandler();
      }

      // Convert validation errors to ResultError
      var errors = validationResult.Errors
          .GroupBy(e => e.PropertyName)
          .Select(g => new
          {
             Property = g.Key,
             Errors = g.Select(e => e.ErrorMessage).ToList()
          })
          .ToList();

      string errorMessage = string.Join("; ", errors.Select(e => $"{e.Property}: {string.Join(", ", e.Errors)}"));
      var resultError = new ResultError("Validation.Failed", errorMessage, ErrorType.Validation);

      // Return failure result
      return CreateFailureResult(resultError);
   }

   private static bool IsCommand(Type type)
   {
      return type.GetInterfaces().Any(i =>
          i.IsGenericType &&
          (i.GetGenericTypeDefinition() == typeof(ICommand<>) ||
           i == typeof(ICommand)));
   }

   private static TResponse CreateFailureResult(ResultError error)
   {
      Type responseType = typeof(TResponse);

      // Check if TResponse is Result<T>
      if (responseType.IsGenericType && responseType.GetGenericTypeDefinition() == typeof(Result<>))
      {
         Type innerType = responseType.GetGenericArguments()[0];
         MethodInfo? failureMethod = typeof(Result<>)
             .MakeGenericType(innerType)
             .GetMethod(nameof(Result<>.Failure), [typeof(ResultError)]);

         return (TResponse)failureMethod!.Invoke(null, [error])!;
      }

      throw new InvalidOperationException($"Cannot create failure result for type {responseType.Name}");
   }
}
