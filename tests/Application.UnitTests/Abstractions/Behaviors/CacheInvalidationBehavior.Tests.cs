using Application.Abstractions.Behaviors;
using Application.Abstractions.Caching;
using Application.Abstractions.Messaging;
using Domain.Abstractions;
using Microsoft.Extensions.Logging;

namespace Application.UnitTests.Abstractions.Behaviors;

public class CacheInvalidationBehaviorTests
{
    private readonly Mock<ICacheService> _cacheServiceMock;
    private readonly Mock<ICacheVersionResolver> _versionResolverMock;
    private readonly Mock<ILogger<CacheInvalidationBehavior<Result<TestResponse>>>> _loggerMock;
    private readonly CacheInvalidationBehavior<Result<TestResponse>> _behavior;

    public CacheInvalidationBehaviorTests()
    {
        _cacheServiceMock = new Mock<ICacheService>();
        _versionResolverMock = new Mock<ICacheVersionResolver>();
        _loggerMock = new Mock<ILogger<CacheInvalidationBehavior<Result<TestResponse>>>>();

        // Setup default version resolution
        _versionResolverMock
            .Setup(x => x.Resolve(It.IsAny<ICacheKey>()))
            .Returns("v1");

        _behavior = new CacheInvalidationBehavior<Result<TestResponse>>(_cacheServiceMock.Object, _versionResolverMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_WithInvalidatingCommandAndSuccess_ShouldInvalidateCache()
    {
        // Arrange
        TestInvalidatingCommand command = new();
        var successResult = Result<TestResponse>.Success(new TestResponse("Success"));

        Task<Result<TestResponse>> next()
        {
            return Task.FromResult(successResult);
        }

        // Act
        Result<TestResponse> result = await _behavior.Handle(command, next, CancellationToken.None);

        // Assert
        result.ShouldBe(successResult);
        _versionResolverMock.Verify(x => x.Resolve(It.IsAny<ICacheKey>()), Times.Exactly(2));
        _cacheServiceMock.Verify(
            x => x.RemoveAsync(It.Is<ICacheKey>(k => k.Feature == "test"), "v1", It.IsAny<CancellationToken>()),
            Times.Once);
        _cacheServiceMock.Verify(
            x => x.RemoveAsync(It.Is<ICacheKey>(k => k.Feature == "another"), "v1", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithInvalidatingCommandAndFailure_ShouldNotInvalidateCache()
    {
        // Arrange
        TestInvalidatingCommand command = new();
        var failureResult = Result<TestResponse>.Failure(
            new ResultError("Error", "Command failed", ErrorType.Failure));

        Task<Result<TestResponse>> next()
        {
            return Task.FromResult(failureResult);
        }

        // Act
        Result<TestResponse> result = await _behavior.Handle(command, next, CancellationToken.None);

        // Assert
        result.ShouldBe(failureResult);
        _cacheServiceMock.Verify(
            x => x.RemoveAsync(It.IsAny<ICacheKey>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WithNonInvalidatingCommand_ShouldSkipInvalidation()
    {
        // Arrange
        TestCommand command = new();
        var successResult = Result<TestResponse>.Success(new TestResponse("Success"));

        Task<Result<TestResponse>> next()
        {
            return Task.FromResult(successResult);
        }

        // Act
        Result<TestResponse> result = await _behavior.Handle(command, next, CancellationToken.None);

        // Assert
        result.ShouldBe(successResult);
        _cacheServiceMock.Verify(
            x => x.RemoveAsync(It.IsAny<ICacheKey>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WithMultiplePatterns_ShouldInvalidateAllPatterns()
    {
        // Arrange
        TestMultiPatternCommand command = new();
        var successResult = Result<TestResponse>.Success(new TestResponse("Success"));

        Task<Result<TestResponse>> next()
        {
            return Task.FromResult(successResult);
        }

        // Act
        Result<TestResponse> result = await _behavior.Handle(command, next, CancellationToken.None);

        // Assert
        result.ShouldBe(successResult);
        _versionResolverMock.Verify(x => x.Resolve(It.IsAny<ICacheKey>()), Times.Exactly(3));
        _cacheServiceMock.Verify(
            x => x.RemoveAsync(It.Is<ICacheKey>(k => k.Feature == "feature1"), "v1", It.IsAny<CancellationToken>()),
            Times.Once);
        _cacheServiceMock.Verify(
            x => x.RemoveAsync(It.Is<ICacheKey>(k => k.Feature == "feature2"), "v1", It.IsAny<CancellationToken>()),
            Times.Once);
        _cacheServiceMock.Verify(
            x => x.RemoveAsync(It.Is<ICacheKey>(k => k.Feature == "feature3"), "v1", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenInvalidationFails_ShouldNotThrowException()
    {
        // Arrange
        TestInvalidatingCommand command = new();
        var successResult = Result<TestResponse>.Success(new TestResponse("Success"));

        _cacheServiceMock
            .Setup(x => x.RemoveAsync(It.IsAny<ICacheKey>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Cache service error"));

        Task<Result<TestResponse>> next()
        {
            return Task.FromResult(successResult);
        }

        // Act & Assert - Should not throw
        Result<TestResponse> result = await _behavior.Handle(command, next, CancellationToken.None);

        result.ShouldBe(successResult);
        _cacheServiceMock.Verify(
            x => x.RemoveAsync(It.IsAny<ICacheKey>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task Handle_WithFeaturesToInvalidate_ShouldInvalidateFeatures()
    {
        // Arrange
        TestFeatureInvalidatingCommand command = new();
        var successResult = Result<TestResponse>.Success(new TestResponse("Success"));

        Task<Result<TestResponse>> next()
        {
            return Task.FromResult(successResult);
        }

        // Act
        Result<TestResponse> result = await _behavior.Handle(command, next, CancellationToken.None);

        // Assert
        result.ShouldBe(successResult);
        _cacheServiceMock.Verify(
            x => x.RemoveByFeatureAsync("todos", It.IsAny<CancellationToken>()),
            Times.Once);
        _cacheServiceMock.Verify(
            x => x.RemoveByFeatureAsync("users", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithFeaturesToInvalidateAndFailure_ShouldNotInvalidateFeatures()
    {
        // Arrange
        TestFeatureInvalidatingCommand command = new();
        var failureResult = Result<TestResponse>.Failure(
            new ResultError("Error", "Command failed", ErrorType.Failure));

        Task<Result<TestResponse>> next()
        {
            return Task.FromResult(failureResult);
        }

        // Act
        Result<TestResponse> result = await _behavior.Handle(command, next, CancellationToken.None);

        // Assert
        result.ShouldBe(failureResult);
        _cacheServiceMock.Verify(
            x => x.RemoveByFeatureAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenFeatureInvalidationFails_ShouldNotThrowException()
    {
        // Arrange
        TestFeatureInvalidatingCommand command = new();
        var successResult = Result<TestResponse>.Success(new TestResponse("Success"));

        _cacheServiceMock
            .Setup(x => x.RemoveByFeatureAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Cache service error"));

        Task<Result<TestResponse>> next()
        {
            return Task.FromResult(successResult);
        }

        // Act & Assert - Should not throw
        Result<TestResponse> result = await _behavior.Handle(command, next, CancellationToken.None);

        result.ShouldBe(successResult);
        _cacheServiceMock.Verify(
            x => x.RemoveByFeatureAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task Handle_WithBothKeysAndFeatures_ShouldInvalidateBoth()
    {
        // Arrange
        TestMixedInvalidatingCommand command = new();
        var successResult = Result<TestResponse>.Success(new TestResponse("Success"));

        Task<Result<TestResponse>> next()
        {
            return Task.FromResult(successResult);
        }

        // Act
        Result<TestResponse> result = await _behavior.Handle(command, next, CancellationToken.None);

        // Assert
        result.ShouldBe(successResult);

        // Verify keys invalidation
        _cacheServiceMock.Verify(
            x => x.RemoveAsync(It.Is<ICacheKey>(k => k.Feature == "specific"), "v1", It.IsAny<CancellationToken>()),
            Times.Once);

        // Verify feature invalidation
        _cacheServiceMock.Verify(
            x => x.RemoveByFeatureAsync("entire-feature", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // Test helpers
    public sealed record TestResponse(string Message);

    public sealed record TestCommand : ICommand<TestResponse>;

    public sealed record TestInvalidatingCommand : ICommand<TestResponse>, ICacheInvalidating
    {
        public IReadOnlyCollection<ICacheKey> KeysToInvalidate =>
        [
            new CacheKey("test", "key1"),
            new CacheKey("another", "key2")
        ];
    }

    public sealed record TestMultiPatternCommand : ICommand<TestResponse>, ICacheInvalidating
    {
        public IReadOnlyCollection<ICacheKey> KeysToInvalidate =>
        [
            new CacheKey("feature1", "value1"),
            new CacheKey("feature2", "value2"),
            new CacheKey("feature3", "value3")
        ];
    }

    public sealed record TestFeatureInvalidatingCommand : ICommand<TestResponse>, ICacheInvalidating
    {
        public IReadOnlyCollection<string> FeaturesToInvalidate => ["todos", "users"];
    }

    public sealed record TestMixedInvalidatingCommand : ICommand<TestResponse>, ICacheInvalidating
    {
        public IReadOnlyCollection<ICacheKey> KeysToInvalidate =>
        [
            new CacheKey("specific", "key1")
        ];

        public IReadOnlyCollection<string> FeaturesToInvalidate => ["entire-feature"];
    }
}
