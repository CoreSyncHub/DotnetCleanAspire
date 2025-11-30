using Application.Features.Todos.Dtos;
using Domain.Todos.Entities;
using Domain.Todos.Errors;

namespace Application.Features.Todos.Queries.GetTodoById;

/// <summary>
/// Query to get a Todo by its ID.
/// </summary>
/// <param name="TodoId">The ID of the todo to retrieve.</param>
public sealed record GetTodoByIdQuery(string TodoId) : IQuery<TodoDto>, ICacheable
{
   public string CacheKey => $"todos:{TodoId}";
   public TimeSpan CacheDuration => TimeSpan.FromMinutes(5);
}

/// <summary>
/// Handler for getting a Todo by ID.
/// </summary>
internal sealed class GetTodoByIdQueryHandler(IApplicationDbContext dbContext) : IQueryHandler<GetTodoByIdQuery, TodoDto>
{
   public async Task<Result<TodoDto>> Handle(
       GetTodoByIdQuery request,
       CancellationToken cancellationToken = default)
   {
      // Parse ID
      if (!Id.TryParse(request.TodoId, null, out Id id))
      {
         return TodoErrors.NotFound(Id.Empty);
      }

      // Find todo
      Todo? todo = await dbContext.Todos
          .AsNoTracking()
          .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

      if (todo is null)
      {
         return TodoErrors.NotFound(id);
      }

      // Map to response
      return new TodoDto(
          todo.Id.ToString(),
          todo.Title.Value,
          todo.Status.ToString(),
          todo.Created);
   }
}
