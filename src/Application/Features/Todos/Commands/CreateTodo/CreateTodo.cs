using Application.Features.Todos.Cache;
using Application.Features.Todos.Dtos;
using Domain.Todos.Entities;
using Domain.Todos.Enums;
using Domain.Todos.ValueObjects;

namespace Application.Features.Todos.Commands.CreateTodo;

/// <summary>
/// Command to create a new Todo.
/// </summary>
/// <param name="Title">The title of the todo.</param>
public sealed record CreateTodoCommand(string Title) : ICommand<CreateTodoDto>, ICacheInvalidating
{
   public IReadOnlyCollection<string> FeaturesToInvalidate => [TodoCacheKeys.FeatureTag];
}

/// <summary>
/// Handler for creating a new Todo.
/// </summary>
internal sealed class CreateTodoCommandHandler(IApplicationDbContext dbContext) : ICommandHandler<CreateTodoCommand, CreateTodoDto>
{
   public async Task<Result<CreateTodoDto>> Handle(
       CreateTodoCommand request,
       CancellationToken cancellationToken = default)
   {
      // Create value object
      Result<Todo> todoResult = TodoTitle.Create(request.Title)
         .Bind(title => Todo.Create(title, TodoStatus.Pending));

      if (todoResult.IsFailure)
      {
         return Result<CreateTodoDto>.Failure(todoResult.Error);
      }

      Todo todo = todoResult.Value;

      // Persist
      dbContext.Todos.Add(todo);
      await dbContext.SaveChangesAsync(cancellationToken);

      // Return response with Created status
      return Result<CreateTodoDto>.Success(
          new CreateTodoDto(todo.Id.ToString(), todo.Title.Value),
          SuccessType.Created);
   }
}
