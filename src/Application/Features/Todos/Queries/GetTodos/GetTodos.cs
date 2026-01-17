using Application.Features.Todos.Dtos;
using Domain.Todos.Entities;

namespace Application.Features.Todos.Queries.GetTodos;

/// <summary>
/// Query to get a paginated list of Todos.
/// </summary>
public sealed record GetTodosQuery : IPaginatedQuery<TodoDto>
{
    /// <inheritdoc />
    public CursorPageRequest Pagination { get; init; } = CursorPageRequest.First();

    /// <summary>
    /// Optional: Sort by creation date instead of ID.
    /// </summary>
    public bool SortByCreatedAt { get; init; }

    /// <summary>
    /// Optional: Sort in descending order.
    /// </summary>
    public bool Descending { get; init; }
}

/// <summary>
/// Handler for getting a paginated list of Todos.
/// </summary>
internal sealed class GetTodosQueryHandler(IApplicationDbContext dbContext) : IQueryHandler<GetTodosQuery, CursorPageResponse<TodoDto>>
{
    public async Task<Result<CursorPageResponse<TodoDto>>> Handle(
        GetTodosQuery request,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Todo> query = dbContext.Todos.AsNoTracking();

        CursorPageResponse<Todo> page;

        if (request.SortByCreatedAt)
        {
            // Sort by CreatedAt with Id as tiebreaker
            page = await query.ToCursorPageAsync(
                request.Pagination,
                t => t.Id,
                t => t.Created,
                request.Descending,
                cancellationToken);
        }
        else
        {
            // Default: sort by Id
            page = await query.ToCursorPageAsync(
                request.Pagination,
                t => t.Id,
                cancellationToken);
        }

        // Map to response
        var items = page.Items
            .Select(t => new TodoDto(
                t.Id.ToString(),
                t.Title.Value,
                t.Status.ToString(),
                t.Created))
            .ToList();

        return new CursorPageResponse<TodoDto>
        {
            Items = items,
            NextCursor = page.NextCursor,
            PreviousCursor = page.PreviousCursor
        };
    }
}
