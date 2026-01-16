using Application.Features.Todos.Commands.CreateTodo;
using Domain.Todos.Errors;
using FluentValidation.TestHelper;

namespace Application.UnitTests.Features.Todos.Commands.CreateTodo;

public class CreateTodoCommandValidatorTests
{
   private readonly CreateTodoCommandValidator _validator = new();

   [Fact]
   public void Validate_WithValidTitle_ShouldNotHaveValidationError()
   {
      // Arrange
      var command = new CreateTodoCommand("Valid Todo Title");

      // Act
      var result = _validator.TestValidate(command);

      // Assert
      result.ShouldNotHaveAnyValidationErrors();
   }

   [Theory]
   [InlineData("")]
   [InlineData(" ")]
   [InlineData(null)]
   public void Validate_WithEmptyTitle_ShouldHaveValidationError(string? title)
   {
      // Arrange
      var command = new CreateTodoCommand(title!);

      // Act
      var result = _validator.TestValidate(command);

      // Assert
      result.ShouldHaveValidationErrorFor(x => x.Title)
          .WithErrorCode(TodoErrors.Codes.TitleRequired);
   }

   [Fact]
   public void Validate_WithTitleTooLong_ShouldHaveValidationError()
   {
      // Arrange
      string longTitle = new('A', 101); // 101 characters
      var command = new CreateTodoCommand(longTitle);

      // Act
      var result = _validator.TestValidate(command);

      // Assert
      result.ShouldHaveValidationErrorFor(x => x.Title)
          .WithErrorCode(TodoErrors.Codes.TitleTooLong);
   }

   [Fact]
   public void Validate_WithTitleExactlyMaxLength_ShouldNotHaveValidationError()
   {
      // Arrange
      string maxLengthTitle = new('A', 100); // Exactly 100 characters
      var command = new CreateTodoCommand(maxLengthTitle);

      // Act
      var result = _validator.TestValidate(command);

      // Assert
      result.ShouldNotHaveAnyValidationErrors();
   }
}
