namespace Application.IntegrationTests.Features.Todos.Queries;

public sealed class GetTodosQueryTests(TestContainersFixture fixture)
    : ApplicationIntegrationTestBase(fixture)
{
    [Fact]
    public async Task Handle_WithEmptyDatabase_ShouldReturnEmptyList()
    {
        var query = new GetTodosQuery();
        Result<CursorPageResponse<TodoDto>> result = await Dispatcher.Send(query);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Items.ShouldBeEmpty();
        result.Value.HasNextPage.ShouldBeFalse();
    }

    [Fact]
    public async Task Handle_WithLessThanPageSizeTodos_ShouldReturnAllWithoutNextCursor()
    {
        await Dispatcher.Send(new CreateTodoCommand("Todo A"));
        await Dispatcher.Send(new CreateTodoCommand("Todo B"));
        await Dispatcher.Send(new CreateTodoCommand("Todo C"));

        var query = new GetTodosQuery { Pagination = CursorPageRequest.First(20) };
        Result<CursorPageResponse<TodoDto>> result = await Dispatcher.Send(query);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Items.Count.ShouldBe(3);
        result.Value.HasNextPage.ShouldBeFalse();
    }

    [Fact]
    public async Task Handle_WithMoreTodosThanPageSize_ShouldReturnPageWithNextCursor()
    {
        for (int i = 1; i <= 15; i++)
            await Dispatcher.Send(new CreateTodoCommand($"Todo {i:D2}"));

        var query = new GetTodosQuery { Pagination = CursorPageRequest.First(10) };
        Result<CursorPageResponse<TodoDto>> result = await Dispatcher.Send(query);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Items.Count.ShouldBe(10);
        result.Value.HasNextPage.ShouldBeTrue();
        result.Value.NextCursor.ShouldNotBeNull();
    }

    [Fact]
    public async Task Handle_WithNextCursor_ShouldReturnRemainingItems()
    {
        for (int i = 1; i <= 15; i++)
            await Dispatcher.Send(new CreateTodoCommand($"Todo {i:D2}"));

        Result<CursorPageResponse<TodoDto>> firstPage = await Dispatcher.Send(
            new GetTodosQuery { Pagination = CursorPageRequest.First(10) });

        string nextCursor = firstPage.Value.NextCursor!;

        Result<CursorPageResponse<TodoDto>> result = await Dispatcher.Send(
            new GetTodosQuery { Pagination = CursorPageRequest.Next(nextCursor, 10) });

        result.IsSuccess.ShouldBeTrue();
        result.Value.Items.Count.ShouldBe(5);
        result.Value.HasNextPage.ShouldBeFalse();
    }

    [Fact]
    public async Task Handle_WithSortByCreatedAt_ShouldReturnItemsInOrder()
    {
        await Dispatcher.Send(new CreateTodoCommand("First"));
        await Dispatcher.Send(new CreateTodoCommand("Second"));
        await Dispatcher.Send(new CreateTodoCommand("Third"));

        var query = new GetTodosQuery
        {
            Pagination = CursorPageRequest.First(20),
            SortByCreatedAt = true,
            Descending = false
        };

        Result<CursorPageResponse<TodoDto>> result = await Dispatcher.Send(query);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Items.Count.ShouldBe(3);
        result.Value.Items[0].Title.ShouldBe("First");
        result.Value.Items[2].Title.ShouldBe("Third");
    }
}
