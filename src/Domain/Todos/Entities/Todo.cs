using Domain.Todos.Enums;
using Domain.Todos.Events;
using Domain.Todos.ValueObjects;

namespace Domain.Todos.Entities;

public sealed class Todo : AuditableAggregateRoot
{
   public TodoTitle Title { get; private set; }

   public TodoStatus Status { get; private set; }

#pragma warning disable CS8618 // Constructor for EF Core

   private Todo() { }

#pragma warning restore CS8618 // Constructor for EF Core

   private Todo(TodoTitle title, TodoStatus status) : base()
   {
      Title = title;
      Status = status;
   }

   public static Result<Todo> Create(TodoTitle title, TodoStatus status)
   {
      var todo = new Todo(title, status);
      todo.Raise(new TodoCreatedEvent(todo.Id, title.Value));
      return Result<Todo>.Success(todo);
   }

   public Result<Unit> MarkAsCompleted()
   {
      if (Status == TodoStatus.Completed)
      {
         return Result.Failure(new ResultError("Todo.AlreadyCompleted", "The todo is already completed.", ErrorType.Validation));
      }

      Status = TodoStatus.Completed;
      return Result.Success();
   }

   public Result<Unit> UpdateTitle(TodoTitle newTitle)
   {
      Title = newTitle;
      return Result.Success();
   }
}
