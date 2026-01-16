using Application.Abstractions.Behaviors;
using Application.Abstractions.Messaging;
using Domain.Abstractions;
using Microsoft.Extensions.Logging;

namespace Application.UnitTests.Abstractions.Behaviors;

public class LoggingBehaviorTests
{
    private readonly Mock<ILogger<LoggingBehavior<Result<TestResponse>>>> _loggerMock;
    private readonly LoggingBehavior<Result<TestResponse>> _behavior;

    public LoggingBehaviorTests()
    {
        _loggerMock = new Mock<ILogger<LoggingBehavior<Result<TestResponse>>>>();
        _behavior = new LoggingBehavior<Result<TestResponse>>(_loggerMock.Object);
    }

    [Fact]
    public async Task Handle_WithSuccessfulRequest_ShouldLogStartAndCompletion()
    {
        // Arrange
        TestCommand request = new();
        var response = Result<TestResponse>.Success(new TestResponse("Success"));

        Task<Result<TestResponse>> next()
        {
            return Task.FromResult(response);
        }

        // Act
        Result<TestResponse> result = await _behavior.Handle(request, next, CancellationToken.None);

        // Assert
        result.ShouldBe(response);

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Handling") && v.ToString()!.Contains("TestCommand")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Handled") && v.ToString()!.Contains("TestCommand")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenHandlerThrows_ShouldLogErrorAndRethrow()
    {
        // Arrange
        TestCommand request = new();
        var exception = new InvalidOperationException("Test exception");

        Task<Result<TestResponse>> next()
        {
            throw exception;
        }

        // Act & Assert
        await Should.ThrowAsync<InvalidOperationException>(async () =>
            await _behavior.Handle(request, next, CancellationToken.None));

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Handling")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Error handling") && v.ToString()!.Contains("TestCommand")),
                exception,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldMeasureExecutionTime()
    {
        // Arrange
        TestCommand request = new();
        var response = Result<TestResponse>.Success(new TestResponse("Success"));

        async Task<Result<TestResponse>> next()
        {
            await Task.Delay(100); // Simulate work
            return response;
        }

        // Act
        Result<TestResponse> result = await _behavior.Handle(request, next, CancellationToken.None);

        // Assert
        result.ShouldBe(response);

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Handled") && v.ToString()!.Contains("ms")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithCancellation_ShouldLogError()
    {
        // Arrange
        TestCommand request = new();
        CancellationTokenSource cts = new();
        await cts.CancelAsync();

        static Task<Result<TestResponse>> next()
        {
            throw new OperationCanceledException();
        }

        // Act & Assert
        await Should.ThrowAsync<OperationCanceledException>(async () =>
            await _behavior.Handle(request, next, cts.Token));

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Error handling")),
                It.IsAny<OperationCanceledException>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    // Test helpers
    public sealed record TestResponse(string Message);
    public sealed record TestCommand : ICommand<TestResponse>;
}
