using Domain.Todos.Enums;
using Domain.Todos.Errors;
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
      Todo todo = new(title, status);
      todo.Raise(new TodoCreatedEvent(todo.Id, title.Value));
      return Result<Todo>.Success(todo);
   }

   public Result<Unit> MarkAsCompleted()
   {
      if (Status is TodoStatus.Completed)
      {
         return Result.Failure(TodoErrors.AlreadyCompleted);
      }

      Status = TodoStatus.Completed;
      Raise(new TodoCompletedEvent(Id));
      return Result.Success();
   }

   public Result<Unit> UpdateTitle(TodoTitle newTitle)
   {
      string oldTitle = Title.Value;
      Title = newTitle;
      Raise(new TodoTitleUpdatedEvent(Id, oldTitle, newTitle.Value));
      return Result.Success();
   }
}
