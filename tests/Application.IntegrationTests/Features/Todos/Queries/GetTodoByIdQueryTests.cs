namespace Application.IntegrationTests.Features.Todos.Queries;

public sealed class GetTodoByIdQueryTests(TestContainersFixture fixture)
    : ApplicationIntegrationTestBase(fixture)
{
    [Fact]
    public async Task Handle_WithExistingTodo_ShouldReturnDto()
    {
        // Arrange
        Result<CreateTodoDto> createResult = await Dispatcher.Send(new CreateTodoCommand("My todo"));
        var todoId = Id.Parse(createResult.Value.Id, null);

        // Act
        Result<TodoDto> result = await Dispatcher.Send(new GetTodoByIdQuery(todoId));

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.Id.ShouldBe(todoId.ToString());
        result.Value.Title.ShouldBe("My todo");
    }

    [Fact]
    public async Task Handle_WhenTodoNotFound_ShouldReturnNotFoundError()
    {
        // Arrange
        var query = new GetTodoByIdQuery(Id.New());

        // Act
        Result<TodoDto> result = await Dispatcher.Send(query);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.Type.ShouldBe(ErrorType.NotFound);
    }
}
