using Domain.Abstractions;
using Domain.Todos.Errors;
using Domain.Todos.ValueObjects;

namespace Domain.UnitTests.Todos.ValueObjects;

public class TodoTitleTests
{
   [Fact]
   public void Create_Should_Create_TodoTitle_Successfully()
   {
      // Arrange
      string titleValue = "Test Todo Title";

      // Act
      Result<TodoTitle> titleResult = TodoTitle.Create(titleValue);

      // Assert
      titleResult
          .Tap(title => title.Value.ShouldBe(titleValue))
          .IsSuccess.ShouldBeTrue();
   }

   [Fact]
   public void Create_Should_Fail_If_Title_Is_Empty()
   {
      // Arrange
      string titleValue = "";

      // Act
      Result<TodoTitle> titleResult = TodoTitle.Create(titleValue);

      // Assert
      titleResult.IsFailure.ShouldBeTrue();
      titleResult.Error.ShouldBe(TodoErrors.TitleRequired);
   }

   [Fact]
   public void Create_Should_Fail_If_Title_Is_Too_Long()
   {
      // Arrange
      string titleValue = new('A', 101); // 101 characters

      // Act
      Result<TodoTitle> titleResult = TodoTitle.Create(titleValue);

      // Assert
      titleResult.IsFailure.ShouldBeTrue();
      titleResult.Error.ShouldBe(TodoErrors.TitleTooLong);
   }
}
