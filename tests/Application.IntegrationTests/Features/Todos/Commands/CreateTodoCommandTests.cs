using Domain.Todos.Enums;

namespace Application.IntegrationTests.Features.Todos.Commands;

public sealed class CreateTodoCommandTests(TestContainersFixture fixture)
    : ApplicationIntegrationTestBase(fixture)
{
    [Fact]
    public async Task Handle_WithValidTitle_ShouldPersistTodoToDatabase()
    {
        // Arrange
        var command = new CreateTodoCommand("Learn Clean Architecture");

        // Act
        Result<CreateTodoDto> result = await Dispatcher.Send(command);

        // Assert — result
        result.IsSuccess.ShouldBeTrue();
        result.Value.Title.ShouldBe("Learn Clean Architecture");

        // Assert — persisted in DB (AsNoTracking forces DB roundtrip, bypasses EF tracker)
        var todoId = Id.Parse(result.Value.Id, null);
        Domain.Todos.Entities.Todo? todo = await DbContext.Todos
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == todoId);

        todo.ShouldNotBeNull();
        todo!.Title.Value.ShouldBe("Learn Clean Architecture");
        todo.Status.ShouldBe(TodoStatus.Pending);
    }

    [Fact]
    public async Task Handle_WithEmptyTitle_ShouldReturnValidationFailure()
    {
        // Arrange
        var command = new CreateTodoCommand(string.Empty);

        // Act
        Result<CreateTodoDto> result = await Dispatcher.Send(command);

        // Assert — ValidationBehavior caught it before the handler ran
        result.IsFailure.ShouldBeTrue();
        result.Error.Type.ShouldBe(ErrorType.Validation);
    }

    [Fact]
    public async Task Handle_WithTitleExceedingMaxLength_ShouldReturnValidationFailure()
    {
        // Arrange — TodoTitle max length is 100 (TodoConfiguration.HasMaxLength(100))
        var command = new CreateTodoCommand(new string('a', 101));

        // Act
        Result<CreateTodoDto> result = await Dispatcher.Send(command);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.Type.ShouldBe(ErrorType.Validation);
    }

    [Fact]
    public async Task Handle_WithValidTitle_ShouldReturnCreatedSuccessType()
    {
        // Arrange
        var command = new CreateTodoCommand("Test todo");

        // Act
        Result<CreateTodoDto> result = await Dispatcher.Send(command);

        // Assert — handler returns SuccessType.Created
        result.IsSuccess.ShouldBeTrue();
        result.SuccessType.ShouldBe(SuccessType.Created);
    }
}
