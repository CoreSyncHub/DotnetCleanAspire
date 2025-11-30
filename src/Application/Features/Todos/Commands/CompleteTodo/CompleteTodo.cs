using Domain.Todos.Entities;
using Domain.Todos.Errors;

namespace Application.Features.Todos.Commands.CompleteTodo;

/// <summary>
/// Command to mark a Todo as completed.
/// </summary>
/// <param name="Id">The ID of the todo to complete.</param>
public sealed record CompleteTodoCommand(Id Id) : ICommand;

/// <summary>
/// Handler for completing a Todo.
/// </summary>
internal sealed class CompleteTodoCommandHandler(IApplicationDbContext dbContext) : ICommandHandler<CompleteTodoCommand>
{
   public async Task<Result<Unit>> Handle(
       CompleteTodoCommand request,
       CancellationToken cancellationToken = default)
   {
      // Find todo
      Todo? todo = await dbContext.Todos
          .FirstOrDefaultAsync(t => t.Id == request.Id, cancellationToken);

      if (todo is null)
      {
         return TodoErrors.NotFound(request.Id);
      }

      // Mark as completed
      Result<Unit> result = todo.MarkAsCompleted();
      if (result.IsFailure)
      {
         return result;
      }

      // Persist
      await dbContext.SaveChangesAsync(cancellationToken);

      return Result.Success();
   }
}
