using Application.Features.Todos.Dtos;
using Domain.Todos.Entities;
using Domain.Todos.Enums;
using Domain.Todos.ValueObjects;

namespace Application.Features.Todos.Commands.CreateTodo;

/// <summary>
/// Command to create a new Todo.
/// </summary>
/// <param name="Title">The title of the todo.</param>
public sealed record CreateTodoCommand(string Title) : ICommand<CreateTodoDto>;

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
      Result<TodoTitle> titleResult = TodoTitle.Create(request.Title);
      if (titleResult.IsFailure)
      {
         return titleResult.Error!;
      }

      // Create entity
      Result<Todo> todoResult = Todo.Create(titleResult.Value!, TodoStatus.Pending);
      if (todoResult.IsFailure)
      {
         return todoResult.Error!;
      }

      Todo todo = todoResult.Value!;

      // Persist
      dbContext.Todos.Add(todo);
      await dbContext.SaveChangesAsync(cancellationToken);

      // Return response with Created status
      return Result<CreateTodoDto>.Success(
          new CreateTodoDto(todo.Id.ToString(), todo.Title.Value),
          SuccessType.Created);
   }
}
