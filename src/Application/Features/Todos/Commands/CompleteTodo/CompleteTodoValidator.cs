using Domain.Todos.Errors;

namespace Application.Features.Todos.Commands.CompleteTodo;

/// <summary>
/// Validator for CompleteTodoCommand.
/// </summary>
internal sealed class CompleteTodoCommandValidator : AbstractValidator<CompleteTodoCommand>
{
   public CompleteTodoCommandValidator()
   {
      RuleFor(x => x.Id)
          .NotEmpty()
          .WithErrorCode(TodoErrors.Codes.IdRequired);
   }
}
