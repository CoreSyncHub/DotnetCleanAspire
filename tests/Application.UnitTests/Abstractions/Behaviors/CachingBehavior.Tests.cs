using Application.Abstractions.Behaviors;
using Application.Abstractions.Caching;
using Application.Abstractions.Messaging;
using Application.DependencyInjection.Options;
using Domain.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Application.UnitTests.Abstractions.Behaviors;

public class CachingBehaviorTests
{
    private readonly Mock<ICacheService> _cacheServiceMock;
    private readonly Mock<ICacheVersionResolver> _versionResolverMock;
    private readonly Mock<ILogger<CachingBehavior<Result<TestDto>>>> _loggerMock;
    private readonly IOptions<CacheOptions> _cacheOptions;
    private readonly CachingBehavior<Result<TestDto>> _behavior;

    public CachingBehaviorTests()
    {
        _cacheServiceMock = new Mock<ICacheService>();
        _versionResolverMock = new Mock<ICacheVersionResolver>();
        _loggerMock = new Mock<ILogger<CachingBehavior<Result<TestDto>>>>();
        _cacheOptions = Options.Create(new CacheOptions
        {
            DefaultCacheDuration = TimeSpan.FromMinutes(5),
            EnableCompression = false
        });

        // Setup default version resolution
        _versionResolverMock
            .Setup(x => x.Resolve(It.IsAny<ICacheable>()))
            .Returns("v1");

        _behavior = new CachingBehavior<Result<TestDto>>(_cacheServiceMock.Object, _versionResolverMock.Object, _cacheOptions, _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_WithCacheableQueryAndCacheHit_ShouldReturnCachedValue()
    {
        // Arrange
        TestCacheableQuery query = new();
        var cachedResult = Result<TestDto>.Success(new TestDto("Cached"));
        var cacheEntry = CacheEntry<Result<TestDto>>.Create(cachedResult);

        _cacheServiceMock
            .Setup(x => x.GetAsync<Result<TestDto>>(It.IsAny<ICacheKey>(), "v1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(cacheEntry);

        bool nextHandlerCalled = false;
        Task<Result<TestDto>> next()
        {
            nextHandlerCalled = true;
            throw new InvalidOperationException("Should not call handler on cache hit");
        }

        // Act
        Result<TestDto> result = await _behavior.Handle(query, next, CancellationToken.None);

        // Assert
        nextHandlerCalled.ShouldBeFalse();
        result.Value.Value.ShouldBe("Cached");
        result.IsSuccess.ShouldBeTrue();
        _cacheServiceMock.Verify(
            x => x.GetAsync<Result<TestDto>>(It.IsAny<ICacheKey>(), "v1", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithCacheableQueryAndCacheMiss_ShouldExecuteHandlerAndCache()
    {
        // Arrange
        TestCacheableQuery query = new();
        var handlerResult = Result<TestDto>.Success(new TestDto("Fresh"));

        _cacheServiceMock
            .Setup(x => x.GetAsync<Result<TestDto>>(It.IsAny<ICacheKey>(), "v1", It.IsAny<CancellationToken>()))
            .ReturnsAsync((CacheEntry<Result<TestDto>>?)null);

        bool nextHandlerCalled = false;
        Task<Result<TestDto>> next()
        {
            nextHandlerCalled = true;
            return Task.FromResult(handlerResult);
        }

        // Act
        Result<TestDto> result = await _behavior.Handle(query, next, CancellationToken.None);

        // Assert
        nextHandlerCalled.ShouldBeTrue();
        result.Value.Value.ShouldBe("Fresh");
        result.IsSuccess.ShouldBeTrue();
        _cacheServiceMock.Verify(
            x => x.SetAsync(
                It.IsAny<ICacheKey>(),
                It.Is<CacheEntry<Result<TestDto>>>(e => e.HasValue && e.Value.IsSuccess),
                query.CacheDuration,
                "v1",
                It.IsAny<bool?>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithNonCacheableRequest_ShouldSkipCaching()
    {
        // Arrange
        TestNonCacheableQuery nonCacheableQuery = new();
        var handlerResult = Result<TestDto>.Success(new TestDto("Data"));

        bool nextHandlerCalled = false;
        Task<Result<TestDto>> next()
        {
            nextHandlerCalled = true;
            return Task.FromResult(handlerResult);
        }

        // Act
        Result<TestDto> result = await _behavior.Handle(nonCacheableQuery, next, CancellationToken.None);

        // Assert
        nextHandlerCalled.ShouldBeTrue();
        result.Value.Value.ShouldBe("Data");
        result.IsSuccess.ShouldBeTrue();
        _cacheServiceMock.Verify(
            x => x.GetAsync<Result<TestDto>>(It.IsAny<ICacheKey>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _cacheServiceMock.Verify(
            x => x.SetAsync(It.IsAny<ICacheKey>(), It.IsAny<CacheEntry<Result<TestDto>>>(), It.IsAny<TimeSpan>(), It.IsAny<string>(), It.IsAny<bool?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WithFailureResult_ShouldNotCache()
    {
        // Arrange
        TestCacheableQuery query = new();
        var failureResult = Result<TestDto>.Failure(
            new ResultError("Error", "Something failed", ErrorType.Failure));

        _cacheServiceMock
            .Setup(x => x.GetAsync<Result<TestDto>>(It.IsAny<ICacheKey>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CacheEntry<Result<TestDto>>?)null);

        Task<Result<TestDto>> next()
        {
            return Task.FromResult(failureResult);
        }

        // Act
        Result<TestDto> result = await _behavior.Handle(query, next, CancellationToken.None);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.Message.ShouldBe("Something failed");
        _cacheServiceMock.Verify(
            x => x.SetAsync(
                It.IsAny<ICacheKey>(),
                It.IsAny<CacheEntry<Result<TestDto>>>(),
                It.IsAny<TimeSpan>(),
                It.IsAny<string?>(),
                It.IsAny<bool?>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // Test helpers
    public sealed record TestDto(string Value);

    public sealed record TestCacheableQuery : IQuery<TestDto>, ICacheable
    {
        public ICacheKey CacheKey => new CacheKey("test", "key");
        public TimeSpan CacheDuration => TimeSpan.FromMinutes(5);
    }

    public sealed record TestNonCacheableQuery : IQuery<TestDto>;
}
