using Domain.Todos.Enums;
using Domain.Todos.Errors;

namespace Application.IntegrationTests.Features.Todos.Commands;

public sealed class CompleteTodoCommandTests(TestContainersFixture fixture)
    : ApplicationIntegrationTestBase(fixture)
{
    [Fact]
    public async Task Handle_WithExistingTodo_ShouldMarkAsCompleted()
    {
        // Arrange — create a todo first
        Result<CreateTodoDto> createResult = await Dispatcher.Send(new CreateTodoCommand("Todo to complete"));
        var todoId = Id.Parse(createResult.Value.Id, null);

        // Act
        Result<Unit> result = await Dispatcher.Send(new CompleteTodoCommand(todoId));

        // Assert — result
        result.IsSuccess.ShouldBeTrue();
        result.SuccessType.ShouldBe(SuccessType.NoContent);

        // Assert — persisted state (AsNoTracking forces DB roundtrip)
        Domain.Todos.Entities.Todo? todo = await DbContext.Todos
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == todoId);

        todo.ShouldNotBeNull();
        todo!.Status.ShouldBe(TodoStatus.Completed);
    }

    [Fact]
    public async Task Handle_WhenTodoNotFound_ShouldReturnNotFoundError()
    {
        // Arrange — an ID that does not exist
        var command = new CompleteTodoCommand(Id.New());

        // Act
        Result<Unit> result = await Dispatcher.Send(command);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.Type.ShouldBe(ErrorType.NotFound);
    }

    [Fact]
    public async Task Handle_WhenAlreadyCompleted_ShouldReturnDomainError()
    {
        // Arrange — create and complete a todo
        Result<CreateTodoDto> createResult = await Dispatcher.Send(new CreateTodoCommand("Already done"));
        var todoId = Id.Parse(createResult.Value.Id, null);
        await Dispatcher.Send(new CompleteTodoCommand(todoId));

        // Act — attempt to complete again
        Result<Unit> result = await Dispatcher.Send(new CompleteTodoCommand(todoId));

        // Assert — domain rule enforced by Todo.MarkAsCompleted()
        result.IsFailure.ShouldBeTrue();
        result.Error.Type.ShouldBe(ErrorType.Validation);
        result.Error.Code.ShouldBe(TodoErrors.Codes.AlreadyCompleted);
    }
}
