using Domain.Todos.Errors;

namespace Application.Features.Todos.Commands.CreateTodo;

/// <summary>
/// Validator for CreateTodoCommand.
/// </summary>
internal sealed class CreateTodoCommandValidator : AbstractValidator<CreateTodoCommand>
{
   public CreateTodoCommandValidator()
   {
      RuleFor(x => x.Title)
          .NotEmpty()
          .WithErrorCode(TodoErrors.Codes.TitleRequired)
          .MaximumLength(100)
          .WithErrorCode(TodoErrors.Codes.TitleTooLong);
   }
}
