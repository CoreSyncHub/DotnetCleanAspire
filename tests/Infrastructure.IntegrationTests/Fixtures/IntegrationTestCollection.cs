namespace Infrastructure.IntegrationTests.Fixtures;

/// <summary>
/// Defines a test collection that shares test containers across all tests.
/// This significantly improves test performance by starting containers only once.
/// </summary>
[CollectionDefinition(nameof(IntegrationTestCollection))]
public sealed class IntegrationTestCollection : ICollectionFixture<TestContainersFixture>
{
   // This class is never instantiated. It's used by xUnit to define the collection.
}
