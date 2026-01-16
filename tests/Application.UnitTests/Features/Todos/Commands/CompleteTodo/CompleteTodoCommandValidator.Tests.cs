using Application.Features.Todos.Commands.CompleteTodo;
using Domain.Abstractions;
using Domain.Todos.Errors;
using FluentValidation.TestHelper;

namespace Application.UnitTests.Features.Todos.Commands.CompleteTodo;

public class CompleteTodoCommandValidatorTests
{
   private readonly CompleteTodoCommandValidator _validator = new();

   [Fact]
   public void Validate_WithValidId_ShouldNotHaveValidationError()
   {
      // Arrange
      var command = new CompleteTodoCommand(Id.New());

      // Act
      var result = _validator.TestValidate(command);

      // Assert
      result.ShouldNotHaveAnyValidationErrors();
   }

   [Fact]
   public void Validate_WithEmptyId_ShouldHaveValidationError()
   {
      // Arrange
      var command = new CompleteTodoCommand(Id.Empty);

      // Act
      var result = _validator.TestValidate(command);

      // Assert
      result.ShouldHaveValidationErrorFor(x => x.Id)
          .WithErrorCode(TodoErrors.Codes.IdRequired);
   }
}