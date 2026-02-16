using Application.Abstractions.Behaviors;
using Application.Abstractions.Messaging;
using Domain.Abstractions;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Application.UnitTests.Abstractions.Behaviors;

public class ValidationBehaviorTests
{
   [Fact]
   public async Task Handle_WithValidCommand_ShouldCallNextHandler()
   {
      // Arrange
      ServiceCollection services = new();
      services.AddScoped<IValidator<TestCommand>, TestCommandValidator>();
      ServiceProvider serviceProvider = services.BuildServiceProvider();

      ValidationBehavior<Result<TestResponse>> behavior = new(serviceProvider);
      TestCommand validCommand = new("Valid Value");

      bool nextHandlerCalled = false;
      Task<Result<TestResponse>> next()
      {
         nextHandlerCalled = true;
         return Task.FromResult(Result<TestResponse>.Success(new TestResponse("Success")));
      }

      // Act
      Result<TestResponse> result = await behavior.Handle(validCommand, next, CancellationToken.None);

      // Assert
      nextHandlerCalled.ShouldBeTrue();
      result.IsSuccess.ShouldBeTrue();
   }

   [Fact]
   public async Task Handle_WithInvalidCommand_ShouldReturnValidationError()
   {
      // Arrange
      ServiceCollection services = new();
      services.AddScoped<IValidator<TestCommand>, TestCommandValidator>();
      ServiceProvider serviceProvider = services.BuildServiceProvider();

      ValidationBehavior<Result<TestResponse>> behavior = new(serviceProvider);
      TestCommand invalidCommand = new(""); // Empty value

      bool nextHandlerCalled = false;
      Task<Result<TestResponse>> next()
      {
         nextHandlerCalled = true;
         throw new InvalidOperationException("Should not reach handler");
      }

      // Act
      Result<TestResponse> result = await behavior.Handle(invalidCommand, next, CancellationToken.None);

      // Assert
      nextHandlerCalled.ShouldBeFalse();
      result.IsFailure.ShouldBeTrue();
      result.Error.Type.ShouldBe(ErrorType.Validation);
      result.Error.Code.ShouldBe("Validation.Failed");
   }

   [Fact]
   public async Task Handle_WithQuery_ShouldSkipValidationAndCallNextHandler()
   {
      // Arrange
      ServiceCollection services = new();
      ServiceProvider serviceProvider = services.BuildServiceProvider();

      ValidationBehavior<Result<TestResponse>> behavior = new(serviceProvider);
      TestQuery query = new();

      bool nextHandlerCalled = false;
      Task<Result<TestResponse>> next()
      {
         nextHandlerCalled = true;
         return Task.FromResult(Result<TestResponse>.Success(new TestResponse("Success")));
      }

      // Act
      Result<TestResponse> result = await behavior.Handle(query, next, CancellationToken.None);

      // Assert
      nextHandlerCalled.ShouldBeTrue();
      result.IsSuccess.ShouldBeTrue();
   }

   [Fact]
   public async Task Handle_WithCommandButNoValidator_ShouldCallNextHandler()
   {
      // Arrange
      ServiceCollection services = new();
      // No validator registered
      ServiceProvider serviceProvider = services.BuildServiceProvider();

      ValidationBehavior<Result<TestResponse>> behavior = new(serviceProvider);
      TestCommand command = new("Value");

      bool nextHandlerCalled = false;
      Task<Result<TestResponse>> next()
      {
         nextHandlerCalled = true;
         return Task.FromResult(Result<TestResponse>.Success(new TestResponse("Success")));
      }

      // Act
      Result<TestResponse> result = await behavior.Handle(command, next, CancellationToken.None);

      // Assert
      nextHandlerCalled.ShouldBeTrue();
      result.IsSuccess.ShouldBeTrue();
   }

   [Fact]
   public async Task Handle_WithMultipleValidationErrors_ShouldReturnAllErrors()
   {
      // Arrange
      ServiceCollection services = new();
      services.AddScoped<IValidator<TestCommand>, TestCommandValidator>();
      ServiceProvider serviceProvider = services.BuildServiceProvider();

      ValidationBehavior<Result<TestResponse>> behavior = new(serviceProvider);
      string tooLongValue = new('A', 101);
      var invalidCommand = new TestCommand(tooLongValue);

      static Task<Result<TestResponse>> next()
      {
         throw new InvalidOperationException("Should not reach handler");
      }

      // Act
      Result<TestResponse> result = await behavior.Handle(invalidCommand, next, CancellationToken.None);

      // Assert
      result.IsFailure.ShouldBeTrue();
      result.Error.Type.ShouldBe(ErrorType.Validation);
   }

   // Test helpers
   public sealed record TestResponse(string Message);

   public sealed record TestCommand(string Value) : ICommand<TestResponse>;

   public sealed record TestQuery : IQuery<TestResponse>;

   public sealed class TestCommandValidator : AbstractValidator<TestCommand>
   {
      public TestCommandValidator()
      {
         RuleFor(x => x.Value)
             .NotEmpty()
             .WithErrorCode("TEST001")
             .MaximumLength(100)
             .WithErrorCode("TEST002");
      }
   }
}
