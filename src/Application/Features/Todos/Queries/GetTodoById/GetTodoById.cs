using Application.Features.Todos.Dtos;
using Domain.Todos.Entities;
using Domain.Todos.Errors;
using System.Text.Json.Serialization;

namespace Application.Features.Todos.Queries.GetTodoById;

/// <summary>
/// Query to get a Todo by its ID.
/// </summary>
/// <param name="TodoId">The ID of the todo to retrieve.</param>
public sealed record GetTodoByIdQuery(Id Id) : IQuery<TodoDto>, ICacheable
{
   public string CacheKey => $"todos:{Id}";
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
      // Find todo
      Todo? todo = await dbContext.Todos
          .AsNoTracking()
          .FirstOrDefaultAsync(t => t.Id == request.Id, cancellationToken);
      if (todo is null)
      {
         return TodoErrors.NotFound(request.Id);
      }

      // Map to response
      return new TodoDto(
          todo.Id.ToString(),
          todo.Title.Value,
          todo.Status.ToString(),
          todo.Created);
   }
}
