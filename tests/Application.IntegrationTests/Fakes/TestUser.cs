namespace Application.IntegrationTests.Fakes;

/// <summary>
/// Configurable IUser implementation for integration tests.
/// Registered as Scoped so the same instance is shared within a test's DI scope.
/// Tests can set Id and Email to control the authenticated user context.
/// </summary>
public sealed class TestUser : IUser
{
    public Id? Id { get; set; }
    public string? Email { get; set; }
    public bool IsAuthenticated => Id is not null;

    public IReadOnlyList<string> GetRoles() => [];
    public bool IsInRole(string role) => false;
}
