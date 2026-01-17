using Application.Abstractions.Behaviors;
using Application.Abstractions.Caching;
using Application.Abstractions.Messaging;
using Application.DependencyInjection.Options;
using Domain.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Application.UnitTests.Abstractions.Caching;

public class CacheVersioningTests
{
    private readonly Mock<ICacheService> _cacheServiceMock;
    private readonly Mock<ICacheVersionResolver> _versionResolverMock;
    private readonly Mock<ILogger<CachingBehavior<Result<TestDto>>>> _loggerMock;

    public CacheVersioningTests()
    {
        _cacheServiceMock = new Mock<ICacheService>();
        _versionResolverMock = new Mock<ICacheVersionResolver>();
        _loggerMock = new Mock<ILogger<CachingBehavior<Result<TestDto>>>>();
    }

    [Fact]
    public async Task Handle_WithGlobalVersion_ShouldPrefixKeyWithGlobalVersion()
    {
        // Arrange
        var cacheOptions = Options.Create(new CacheOptions());
        var behavior = new CachingBehavior<Result<TestDto>>(_cacheServiceMock.Object, _versionResolverMock.Object, cacheOptions, _loggerMock.Object);

        TestCacheableQuery query = new();
        var handlerResult = Result<TestDto>.Success(new TestDto("Data"));

        _versionResolverMock
            .Setup(x => x.Resolve(query))
            .Returns("v2");

        _cacheServiceMock
            .Setup(x => x.GetAsync<Result<TestDto>>(It.IsAny<ICacheKey>(), "v2", It.IsAny<CancellationToken>()))
            .ReturnsAsync((CacheEntry<Result<TestDto>>?)null);

        Task<Result<TestDto>> next() => Task.FromResult(handlerResult);

        // Act
        await behavior.Handle(query, next, CancellationToken.None);

        // Assert
        _versionResolverMock.Verify(x => x.Resolve(query), Times.Once);
        _cacheServiceMock.Verify(
            x => x.GetAsync<Result<TestDto>>(It.IsAny<ICacheKey>(), "v2", It.IsAny<CancellationToken>()),
            Times.Exactly(2)); // Called twice: before lock and after lock (cache stampede protection)
        _cacheServiceMock.Verify(
            x => x.SetAsync(It.IsAny<ICacheKey>(), It.IsAny<CacheEntry<Result<TestDto>>>(), It.IsAny<TimeSpan>(), "v2", It.IsAny<bool?>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithFeatureVersion_ShouldPrefixKeyWithFeatureVersion()
    {
        // Arrange
        var cacheOptions = Options.Create(new CacheOptions());
        var behavior = new CachingBehavior<Result<TestDto>>(_cacheServiceMock.Object, _versionResolverMock.Object, cacheOptions, _loggerMock.Object);

        TestFeatureCacheableQuery query = new(); // CacheKey = "users:list"
        var handlerResult = Result<TestDto>.Success(new TestDto("Data"));

        _versionResolverMock
            .Setup(x => x.Resolve(query))
            .Returns("v3");

        _cacheServiceMock
            .Setup(x => x.GetAsync<Result<TestDto>>(It.IsAny<ICacheKey>(), "v3", It.IsAny<CancellationToken>()))
            .ReturnsAsync((CacheEntry<Result<TestDto>>?)null);

        Task<Result<TestDto>> next() => Task.FromResult(handlerResult);

        // Act
        await behavior.Handle(query, next, CancellationToken.None);

        // Assert - should use feature version "v3" instead of global "v1"
        _versionResolverMock.Verify(x => x.Resolve(query), Times.Once);
        _cacheServiceMock.Verify(
            x => x.GetAsync<Result<TestDto>>(It.IsAny<ICacheKey>(), "v3", It.IsAny<CancellationToken>()),
            Times.Exactly(2)); // Called twice: before lock and after lock (cache stampede protection)
        _cacheServiceMock.Verify(
            x => x.SetAsync(It.IsAny<ICacheKey>(), It.IsAny<CacheEntry<Result<TestDto>>>(), It.IsAny<TimeSpan>(), "v3", It.IsAny<bool?>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithExplicitVersion_ShouldUseExplicitVersion()
    {
        // Arrange
        var cacheOptions = Options.Create(new CacheOptions());
        var behavior = new CachingBehavior<Result<TestDto>>(_cacheServiceMock.Object, _versionResolverMock.Object, cacheOptions, _loggerMock.Object);

        TestExplicitVersionQuery query = new(); // CacheKey = "test:key", Version = "v99"
        var handlerResult = Result<TestDto>.Success(new TestDto("Data"));

        _versionResolverMock
            .Setup(x => x.Resolve(query))
            .Returns("v99");

        _cacheServiceMock
            .Setup(x => x.GetAsync<Result<TestDto>>(It.IsAny<ICacheKey>(), "v99", It.IsAny<CancellationToken>()))
            .ReturnsAsync((CacheEntry<Result<TestDto>>?)null);

        Task<Result<TestDto>> next() => Task.FromResult(handlerResult);

        // Act
        await behavior.Handle(query, next, CancellationToken.None);

        // Assert - should use explicit version "v99" ignoring global and feature versions
        _versionResolverMock.Verify(x => x.Resolve(query), Times.Once);
        _cacheServiceMock.Verify(
            x => x.GetAsync<Result<TestDto>>(It.IsAny<ICacheKey>(), "v99", It.IsAny<CancellationToken>()),
            Times.Exactly(2)); // Called twice: before lock and after lock (cache stampede protection)
        _cacheServiceMock.Verify(
            x => x.SetAsync(It.IsAny<ICacheKey>(), It.IsAny<CacheEntry<Result<TestDto>>>(), It.IsAny<TimeSpan>(), "v99", It.IsAny<bool?>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithVersionChange_ShouldNotFindOldCacheEntry()
    {
        // Arrange
        TestCacheableQuery query = new();
        var handlerResult = Result<TestDto>.Success(new TestDto("Fresh"));

        _cacheServiceMock
            .Setup(x => x.GetAsync<Result<TestDto>>(It.IsAny<ICacheKey>(), "v1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(CacheEntry<Result<TestDto>>.Create(Result<TestDto>.Success(new TestDto("Cached"))));

        _cacheServiceMock
            .Setup(x => x.GetAsync<Result<TestDto>>(It.IsAny<ICacheKey>(), "v2", It.IsAny<CancellationToken>()))
            .ReturnsAsync((CacheEntry<Result<TestDto>>?)null);

        _versionResolverMock
            .Setup(x => x.Resolve(query))
            .Returns("v2");

        Task<Result<TestDto>> next() => Task.FromResult(handlerResult);

        // Act - now using v2, should not find v1 cache
        var cacheOptions = Options.Create(new CacheOptions());
        var behavior = new CachingBehavior<Result<TestDto>>(_cacheServiceMock.Object, _versionResolverMock.Object, cacheOptions, _loggerMock.Object);

        Result<TestDto> result = await behavior.Handle(query, next, CancellationToken.None);

        // Assert - should execute handler and get fresh data
        result.Value.Value.ShouldBe("Fresh");
        _versionResolverMock.Verify(x => x.Resolve(query), Times.Once);
        _cacheServiceMock.Verify(
            x => x.SetAsync(It.IsAny<ICacheKey>(), It.IsAny<CacheEntry<Result<TestDto>>>(), It.IsAny<TimeSpan>(), "v2", It.IsAny<bool?>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // Test helpers
    public sealed record TestDto(string Value);

    public sealed record TestCacheableQuery : IQuery<TestDto>, ICacheable
    {
        public ICacheKey CacheKey => new CacheKey("test", "key");
        public TimeSpan CacheDuration => TimeSpan.FromMinutes(5);
    }

    public sealed record TestFeatureCacheableQuery : IQuery<TestDto>, ICacheable
    {
        public ICacheKey CacheKey => new CacheKey("users", "list");
        public TimeSpan CacheDuration => TimeSpan.FromMinutes(5);
    }

    public sealed record TestExplicitVersionQuery : IQuery<TestDto>, ICacheable
    {
        public ICacheKey CacheKey => new CacheKey("test", "key");
        public TimeSpan CacheDuration => TimeSpan.FromMinutes(5);
        public string? Version => "v99";
    }
}
