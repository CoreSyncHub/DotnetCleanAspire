using Application.Abstractions.Pagination;
using Application.Features.Todos.Commands.CompleteTodo;
using Application.Features.Todos.Commands.CreateTodo;
using Application.Features.Todos.Dtos;
using Application.Features.Todos.Queries.GetTodoById;
using Application.Features.Todos.Queries.GetTodos;
using Presentation.Abstractions;
using Presentation.Extensions;

namespace Presentation.Endpoints;

/// <summary>
/// Endpoints for Todo operations.
/// </summary>
internal sealed class TodoEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app, ApiVersionSet versions)
    {
        RouteGroupBuilder group = app
            .MapGroup("/api/v{version:apiVersion}/todos")
            .WithApiVersionSet(versions)
            .WithTags("Todos");

        group.MapPost("/", CreateTodo)
            .WithName("CreateTodo")
            .WithSummary("Creates a new todo item")
            .WithDescription("Creates a new todo item with the specified title.")
            .Produces<ApiResponse<CreateTodoDto>>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .MapToApiVersion(1, 0);

        group.MapGet("/{id}", GetTodoById)
            .WithName("GetTodoById")
            .WithSummary("Gets a todo by ID")
            .WithDescription("Retrieves a specific todo item by its unique identifier.")
            .Produces<ApiResponse<TodoDto>>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .MapToApiVersion(1, 0);

        group.MapGet("/", GetTodos)
            .WithName("GetTodos")
            .WithSummary("Gets a paginated list of todos")
            .WithDescription("Retrieves a paginated list of todo items with cursor-based pagination.")
            .Produces<ApiResponse<CursorPageResponse<TodoDto>>>()
            .MapToApiVersion(1, 0);

        group.MapPatch("/{id}/complete", CompleteTodo)
            .WithName("CompleteTodo")
            .WithSummary("Marks a todo as completed")
            .WithDescription("Updates the status of a todo item to completed.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .MapToApiVersion(1, 0);
    }

    /// <summary>
    /// Creates a new todo.
    /// </summary>
    private static async Task<IResult> CreateTodo(
        CreateTodoCommand command,
        IDispatcher dispatcher,
        CancellationToken cancellationToken
    )
    {
        Result<CreateTodoDto> result = await dispatcher.Send(command, cancellationToken);
        return result.Match(
            onSuccess: dto => Results.Created($"/api/v1/todos/{dto.Id}", ApiResponse<CreateTodoDto>.From(dto)),
            onFailure: _ => result.ToHttpResult()
        );
    }

    private static async Task<IResult> GetTodoById(
        Id id,
        IDispatcher dispatcher,
        CancellationToken cancellationToken
    )
    {
        GetTodoByIdQuery query = new(id);
        Result<TodoDto> result = await dispatcher.Send(query, cancellationToken);

        return result.ToHttpResult();
    }

    private static async Task<IResult> GetTodos(
        IDispatcher dispatcher,
        string? cursor = null,
        int pageSize = 20,
        string direction = "forward",
        bool sortByCreatedAt = false,
        bool descending = false,
        CancellationToken cancellationToken = default
    )
    {
        CursorDirection cursorDirection = direction.Equals("backward", StringComparison.OrdinalIgnoreCase)
            ? CursorDirection.Backward
            : CursorDirection.Forward;

        CursorPageRequest pagination = cursor is null
            ? CursorPageRequest.First(pageSize)
            : cursorDirection == CursorDirection.Forward
                ? CursorPageRequest.Next(cursor, pageSize)
                : CursorPageRequest.Previous(cursor, pageSize);

        GetTodosQuery query = new()
        {
            Pagination = pagination,
            SortByCreatedAt = sortByCreatedAt,
            Descending = descending
        };

        Result<CursorPageResponse<TodoDto>> result = await dispatcher.Send(query, cancellationToken);

        return result.ToHttpResult();
    }

    private static async Task<IResult> CompleteTodo(
        Id id,
        IDispatcher dispatcher,
        CancellationToken cancellationToken)
    {
        CompleteTodoCommand command = new(id);
        Result<Unit> result = await dispatcher.Send(command, cancellationToken);
        return result.ToHttpResult();
    }
}
