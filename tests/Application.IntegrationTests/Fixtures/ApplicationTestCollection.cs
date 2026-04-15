namespace Application.IntegrationTests.Fixtures;

/// <summary>
/// Defines the xUnit collection that shares TestContainersFixture across all Application integration tests.
/// </summary>
[CollectionDefinition(nameof(ApplicationTestCollection))]
public sealed class ApplicationTestCollection : ICollectionFixture<TestContainersFixture>
{
    // This class is never instantiated. It is used by xUnit to define the collection.
}
