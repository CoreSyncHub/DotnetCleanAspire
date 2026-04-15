using Application.IntegrationTests.Fakes;
using System.Diagnostics.CodeAnalysis;

namespace Application.IntegrationTests.Base;

/// <summary>
/// Base class for all Application integration tests.
///
/// Each test METHOD gets a fresh factory + DI scope + TRUNCATE reset (xUnit creates
/// one instance per test method). This ensures full isolation between tests.
///
/// Exposes:
/// - Dispatcher     : send commands and queries through the full pipeline
/// - DbContext      : assert directly on persisted state (use AsNoTracking() for DB roundtrip)
/// - CurrentUser    : configure the authenticated user context per test
/// - IdentityService: generate tokens/reset codes for test setup
/// </summary>
[Collection(nameof(ApplicationTestCollection))]
[SuppressMessage("Design", "CA1001", Justification = "Disposal is handled via IAsyncLifetime.DisposeAsync")]
public abstract class ApplicationIntegrationTestBase : IAsyncLifetime
{
    private readonly TestsWebApplicationFactory _factory;
    private IServiceScope _scope = null!;

    protected IDispatcher Dispatcher { get; private set; } = null!;
    protected ApplicationDbContext DbContext { get; private set; } = null!;

    /// <summary>
    /// Controllable user context. Cast to TestUser when you need to set Id/Email.
    /// Resolved as TestUser from DI but exposed via IUser to respect accessibility.
    /// </summary>
    protected IUser CurrentUser { get; private set; } = null!;

    protected IIdentityService IdentityService { get; private set; } = null!;

    protected ApplicationIntegrationTestBase(TestContainersFixture fixture)
    {
        // One factory per test method — migrations already applied in fixture
        _factory = new TestsWebApplicationFactory(fixture.PostgresConnectionString);
    }

    public async Task InitializeAsync()
    {
        _scope = _factory.Services.CreateScope();

        Dispatcher = _scope.ServiceProvider.GetRequiredService<IDispatcher>();
        DbContext = _scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        CurrentUser = _scope.ServiceProvider.GetRequiredService<TestUser>();
        IdentityService = _scope.ServiceProvider.GetRequiredService<IIdentityService>();

        await ResetDatabaseAsync();
    }

    public async Task DisposeAsync()
    {
        _scope.Dispose();
        await _factory.DisposeAsync();
    }

    /// <summary>
    /// Truncates all tables before each test for full isolation.
    /// Table names are defined by EF configuration (snake_case).
    /// CASCADE handles FK-dependent tables automatically.
    /// </summary>
    private async Task ResetDatabaseAsync()
    {
        await DbContext.Database.ExecuteSqlRawAsync("""
            TRUNCATE TABLE todos, users, roles RESTART IDENTITY CASCADE;
            """);
    }

    /// <summary>
    /// Registers a user and returns their auth tokens.
    /// Used as a test setup helper for auth-related tests.
    /// </summary>
    protected async Task<AuthTokensDto> RegisterUserAsync(
        string email = "test@example.com",
        string password = "Test1234!")
    {
        Result<AuthTokensDto> result = await Dispatcher.Send(
            new RegisterCommand(email, password, password));

        result.IsSuccess.ShouldBeTrue($"RegisterUserAsync failed: {result.Error}");
        return result.Value;
    }
}
