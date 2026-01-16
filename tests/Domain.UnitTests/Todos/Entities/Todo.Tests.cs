using Domain.Abstractions;
using Domain.Todos.Entities;
using Domain.Todos.Enums;
using Domain.Todos.Errors;
using Domain.Todos.Events;
using Domain.Todos.ValueObjects;

namespace Domain.UnitTests.Todos.Entities;

public class TodoTests
{
   [Fact]
   public void Create_Should_Create_Todo_Successfully()
   {
      // Arrange
      TodoStatus status = TodoStatus.Pending;

      // Act
      Result<Todo> todoResult = TodoTitle.Create("Test Todo")
         .Bind(title => Todo.Create(title, status));

      // Assert
      todoResult
          .Tap(todo =>
          {
             todo.Title.Value.ShouldBe("Test Todo");
             todo.Status.ShouldBe(status);
          })
          .IsSuccess.ShouldBeTrue();
   }

   [Fact]
   public void Create_Should_Raise_TodoCreatedEvent()
   {
      // Arrange & Act
      Result<Todo> todoResult = TodoTitle.Create("Test Todo")
          .Bind(title => Todo.Create(title, TodoStatus.Pending));

      // Assert
      IReadOnlyCollection<IDomainEvent> domainEvents = todoResult.Value.DomainEvents;
      domainEvents.Count.ShouldBe(1);
      var todoCreatedEvent = domainEvents.Single() as TodoCreatedEvent;
      todoCreatedEvent.ShouldNotBeNull();
      todoCreatedEvent.TodoId.ShouldBe(todoResult.Value.Id);
      todoCreatedEvent.Title.ShouldBe("Test Todo");
   }

   [Fact]
   public void MarkAsCompleted_Should_Mark_Todo_As_Completed()
   {
      // Arrange
      TodoStatus status = TodoStatus.Pending;
      Result<Todo> todoResult = TodoTitle.Create("Test Todo")
         .Bind(title => Todo.Create(title, status));

      // Act
      Result<Unit> markResult = todoResult
         .Bind(todo => todo.MarkAsCompleted());

      // Assert
      markResult.IsSuccess.ShouldBeTrue();
      todoResult.Value.Status.ShouldBe(TodoStatus.Completed);
   }

   [Fact]
   public void MarkAsCompleted_Should_Fail_If_Already_Completed()
   {
      // Arrange
      TodoStatus status = TodoStatus.Completed;
      Result<Todo> todoResult = TodoTitle.Create("Test Todo")
         .Bind(title => Todo.Create(title, status));

      // Act
      Result<Unit> markResult = todoResult
         .Bind(todo => todo.MarkAsCompleted());

      // Assert
      markResult.IsFailure.ShouldBeTrue();
      markResult.Error.ShouldBe(TodoErrors.AlreadyCompleted);
   }

   [Fact]
   public void MarkAsCompleted_Should_Raise_TodoCompletedEvent()
   {
      // Arrange
      Result<Todo> todoResult = TodoTitle.Create("Test Todo")
         .Bind(title => Todo.Create(title, TodoStatus.Pending));

      todoResult.Value.ClearEvents();

      // Act
      Result<Unit> markResult = todoResult
         .Bind(todo => todo.MarkAsCompleted());

      // Assert
      markResult.IsSuccess.ShouldBeTrue();
      IReadOnlyCollection<IDomainEvent> domainEvents = todoResult.Value.DomainEvents;
      domainEvents.Count.ShouldBe(1);

      var todoCompletedEvent = domainEvents.Single() as TodoCompletedEvent;
      todoCompletedEvent.ShouldNotBeNull();
      todoCompletedEvent.TodoId.ShouldBe(todoResult.Value.Id);
   }

   [Fact]
   public void MarkAsCompleted_Should_Not_Raise_Event_If_Already_Completed()
   {
      // Arrange
      Result<Todo> todoResult = TodoTitle.Create("Test Todo")
         .Bind(title => Todo.Create(title, TodoStatus.Completed));

      todoResult.Value.ClearEvents();

      // Act
      Result<Unit> markResult = todoResult
         .Bind(todo => todo.MarkAsCompleted());

      // Assert
      markResult.IsFailure.ShouldBeTrue();
      todoResult.Value.DomainEvents.Count.ShouldBe(0);
   }

   [Fact]
   public void UpdateTitle_Should_Update_Todo_Title()
   {
      // Arrange
      TodoStatus status = TodoStatus.Pending;
      Result<Todo> todoResult = TodoTitle.Create("Old Title")
         .Bind(title => Todo.Create(title, status));
      Result<TodoTitle> newTitleResult = TodoTitle.Create("New Title");

      // Act
      Result<Unit> updateResult = todoResult
         .Bind(todo => newTitleResult
            .Bind(newTitle => todo.UpdateTitle(newTitle))
         );

      // Assert
      updateResult.IsSuccess.ShouldBeTrue();
      todoResult.Value.Title.Value.ShouldBe("New Title");
   }

   [Fact]
   public void UpdateTitle_Should_Raise_TodoTitleUpdatedEvent()
   {
      // Arrange
      Result<Todo> todoResult = TodoTitle.Create("Old Title")
         .Bind(title => Todo.Create(title, TodoStatus.Pending));
      Result<TodoTitle> newTitleResult = TodoTitle.Create("New Title");

      todoResult.Value.ClearEvents();

      // Act
      Result<Unit> updateResult = todoResult
         .Bind(todo => newTitleResult
            .Bind(newTitle => todo.UpdateTitle(newTitle))
         );

      // Assert
      updateResult.IsSuccess.ShouldBeTrue();
      IReadOnlyCollection<IDomainEvent> domainEvents = todoResult.Value.DomainEvents;
      domainEvents.Count.ShouldBe(1);

      var titleUpdatedEvent = domainEvents.Single() as TodoTitleUpdatedEvent;
      titleUpdatedEvent.ShouldNotBeNull();
      titleUpdatedEvent.TodoId.ShouldBe(todoResult.Value.Id);
      titleUpdatedEvent.OldTitle.ShouldBe("Old Title");
      titleUpdatedEvent.NewTitle.ShouldBe("New Title");
   }
}
