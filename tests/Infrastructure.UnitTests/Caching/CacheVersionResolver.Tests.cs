using Application.Abstractions.Caching;
using Application.DependencyInjection.Options;
using Infrastructure.Caching;
using Microsoft.Extensions.Options;

namespace Infrastructure.UnitTests.Caching;

public class CacheVersionResolverTests
{
    private readonly CacheOptions _cacheOptions;
    private readonly CacheVersionResolver _resolver;

    public CacheVersionResolverTests()
    {
        _cacheOptions = new CacheOptions
        {
            GlobalVersion = "v1",
            FeatureVersions = new Dictionary<string, string>
            {
                ["todos"] = "v2",
                ["users"] = "v3"
            }
        };
        _resolver = new CacheVersionResolver(Options.Create(_cacheOptions));
    }

    #region Resolve(ICacheable) tests

    [Fact]
    public void Resolve_Cacheable_WithExplicitVersion_ShouldReturnExplicitVersion()
    {
        // Arrange
        var cacheable = new TestCacheable("todos", "42", explicitVersion: "custom-v1");

        // Act
        string result = _resolver.Resolve(cacheable);

        // Assert
        result.ShouldBe("custom-v1");
    }

    [Fact]
    public void Resolve_Cacheable_WithoutExplicitVersion_WithFeatureVersion_ShouldReturnFeatureVersion()
    {
        // Arrange
        var cacheable = new TestCacheable("todos", "42", explicitVersion: null);

        // Act
        string result = _resolver.Resolve(cacheable);

        // Assert
        result.ShouldBe("v2");
    }

    [Fact]
    public void Resolve_Cacheable_WithoutExplicitVersion_WithoutFeatureVersion_ShouldReturnGlobalVersion()
    {
        // Arrange
        var cacheable = new TestCacheable("products", "123", explicitVersion: null);

        // Act
        string result = _resolver.Resolve(cacheable);

        // Assert
        result.ShouldBe("v1");
    }

    [Fact]
    public void Resolve_Cacheable_WithEmptyExplicitVersion_ShouldReturnFeatureVersion()
    {
        // Arrange
        var cacheable = new TestCacheable("users", "99", explicitVersion: "");

        // Act
        string result = _resolver.Resolve(cacheable);

        // Assert
        result.ShouldBe("v3");
    }

    [Fact]
    public void Resolve_Cacheable_ExplicitVersionTakesPrecedence_OverFeatureVersion()
    {
        // Arrange - "todos" has feature version "v2", but we provide explicit "override"
        var cacheable = new TestCacheable("todos", "1", explicitVersion: "override");

        // Act
        string result = _resolver.Resolve(cacheable);

        // Assert
        result.ShouldBe("override");
    }

    #endregion

    #region Resolve(ICacheKey) tests

    [Fact]
    public void Resolve_CacheKey_WithFeatureVersion_ShouldReturnFeatureVersion()
    {
        // Arrange
        var cacheKey = new TestCacheKey("todos", "42");

        // Act
        string result = _resolver.Resolve(cacheKey);

        // Assert
        result.ShouldBe("v2");
    }

    [Fact]
    public void Resolve_CacheKey_WithoutFeatureVersion_ShouldReturnGlobalVersion()
    {
        // Arrange
        var cacheKey = new TestCacheKey("orders", "100");

        // Act
        string result = _resolver.Resolve(cacheKey);

        // Assert
        result.ShouldBe("v1");
    }

    [Fact]
    public void Resolve_CacheKey_DifferentFeatures_ShouldReturnCorrectVersions()
    {
        // Arrange
        var todoKey = new TestCacheKey("todos", "1");
        var userKey = new TestCacheKey("users", "1");
        var productKey = new TestCacheKey("products", "1");

        // Act & Assert
        _resolver.Resolve(todoKey).ShouldBe("v2");
        _resolver.Resolve(userKey).ShouldBe("v3");
        _resolver.Resolve(productKey).ShouldBe("v1"); // Falls back to global
    }

    #endregion

    #region Edge cases

    [Fact]
    public void Resolve_WithEmptyFeatureVersions_ShouldFallbackToGlobal()
    {
        // Arrange
        var options = new CacheOptions
        {
            GlobalVersion = "global-v1",
            FeatureVersions = []
        };
        var resolver = new CacheVersionResolver(Options.Create(options));
        var cacheKey = new TestCacheKey("any-feature", "123");

        // Act
        string result = resolver.Resolve(cacheKey);

        // Assert
        result.ShouldBe("global-v1");
    }

    [Fact]
    public void Resolve_Cacheable_AndCacheKey_SameFeature_ShouldBeConsistent()
    {
        // Arrange
        const string feature = "todos";
        var cacheable = new TestCacheable(feature, "1", explicitVersion: null);
        var cacheKey = new TestCacheKey(feature, "1");

        // Act
        string cacheableVersion = _resolver.Resolve(cacheable);
        string cacheKeyVersion = _resolver.Resolve(cacheKey);

        // Assert - Both should resolve to the same version for consistency
        cacheableVersion.ShouldBe(cacheKeyVersion);
    }

    #endregion

    #region Test helpers

    private sealed record TestCacheKey(string Feature, string Value) : ICacheKey;

    private sealed class TestCacheable(string feature, string value, string? explicitVersion) : ICacheable
    {
        public ICacheKey CacheKey { get; } = new TestCacheKey(feature, value);
        public string? Version => explicitVersion;
    }

    #endregion
}
