# Testing Strategy

This boilerplate follows a comprehensive testing strategy covering all layers of Clean Architecture.

## Test Structure

```
tests/
├── Domain.UnitTests/              # Pure domain logic unit tests
├── Application.UnitTests/         # Unit tests with mocks (behaviors, validators)
├── Application.IntegrationTests/  # CQRS integration tests with TestContainers
└── Infrastructure.IntegrationTests/  # Integration tests with TestContainers
```

### Why No Presentation Tests?

The Presentation layer (Minimal API endpoints) contains **no business logic** - only routing and orchestration. This logic is already tested via:

- **Application.IntegrationTests**: Tests commands/queries
- **ASP.NET Core**: Framework is already tested by Microsoft

Adding E2E tests for Presentation would be redundant and primarily test the framework itself.

---

## 1. Domain.UnitTests

**Goal**: Test pure business logic without external dependencies.

### What We Test

- ✅ **Entities**: Creation, methods, validation, domain events
- ✅ **Value Objects**: Validation, equality, immutability
- ✅ **Domain Events**: Proper event raising
- ✅ **Business Rules**: Complex business logic

### Examples

#### TodoTests.cs

```csharp
[Fact]
public void MarkAsCompleted_WhenAlreadyCompleted_ShouldReturnFailure()
{
    // Arrange
    var title = TodoTitle.Create("Test").Value;
    var todo = Todo.Create(title, TodoStatus.Completed).Value;

    // Act
    var result = todo.MarkAsCompleted();

    // Assert
    result.IsFailure.ShouldBeTrue();
    result.Error.ShouldBe(TodoErrors.AlreadyCompleted);
}
```

#### TodoTitleTests.cs

```csharp
[Theory]
[InlineData("")]
[InlineData(" ")]
[InlineData(null)]
public void Create_WithInvalidTitle_ShouldReturnFailure(string? invalidTitle)
{
    // Act
    var result = TodoTitle.Create(invalidTitle!);

    // Assert
    result.IsFailure.ShouldBeTrue();
    result.Error.ShouldBe(TodoErrors.TitleRequired);
}
```

### Principles

- **No Mocks**: Pure unit tests
- **Fast**: Execute in milliseconds
- **Isolated**: No external dependencies
- **Deterministic**: Always same result

---

## 2. Application.UnitTests

**Goal**: Test behaviors, validators, and mappers with mocks.

### What We Test

- ✅ **Pipeline Behaviors**: ValidationBehavior, CachingBehavior, LoggingBehavior
- ✅ **FluentValidation Validators**: Command validation
- ✅ **Mappers/Extensions**: Data transformations

### Examples

#### ValidationBehaviorTests.cs

```csharp
[Fact]
public async Task Handle_WithInvalidCommand_ShouldReturnValidationErrors()
{
    // Arrange
    var validator = new CreateTodoCommandValidator();
    var behavior = new ValidationBehavior<CreateTodoDto>(new[] { validator });
    var invalidCommand = new CreateTodoCommand(""); // Empty title

    RequestHandlerDelegate<CreateTodoDto> next = () =>
        throw new InvalidOperationException("Should not reach handler");

    // Act
    var result = await behavior.Handle(invalidCommand, next, CancellationToken.None);

    // Assert
    result.IsFailure.ShouldBeTrue();
    result.Error.Type.ShouldBe(ErrorType.Validation);
}
```

#### CreateTodoCommandValidatorTests.cs

```csharp
[Fact]
public void Validate_WithEmptyTitle_ShouldHaveValidationError()
{
    // Arrange
    var validator = new CreateTodoCommandValidator();
    var command = new CreateTodoCommand("");

    // Act
    var result = validator.Validate(command);

    // Assert
    result.IsValid.ShouldBeFalse();
    result.Errors.ShouldContain(e => e.PropertyName == nameof(command.Title));
}
```

### Principles

- **Lightweight Mocks**: Use Moq only for abstractions
- **Behavior Testing**: Verify interactions, not implementation
- **Isolation**: Test each behavior independently

---

## 3. Application.IntegrationTests

**Goal**: Test commands/queries with a real database.

### What We Test

- ✅ **Command Handlers**: Complete execution with real DB
- ✅ **Query Handlers**: Data retrieval with real DB
- ✅ **Transactions**: Rollback on error
- ✅ **Domain Events**: Automatic dispatching via interceptors
- ✅ **Pagination**: Cursor-based pagination with real data

### Setup

Uses **TestContainers** for PostgreSQL and Redis:

```csharp
public class ApplicationIntegrationTestBase : IAsyncLifetime
{
    private PostgreSqlContainer _postgresContainer = null!;
    private RedisContainer _redisContainer = null!;
    protected ApplicationDbContext DbContext = null!;
    protected IDispatcher Dispatcher = null!;

    public async Task InitializeAsync()
    {
        _postgresContainer = new PostgreSqlBuilder()
            .WithImage("postgres:17")
            .Build();

        _redisContainer = new RedisBuilder()
            .WithImage("redis:7-alpine")
            .Build();

        await _postgresContainer.StartAsync();
        await _redisContainer.StartAsync();

        // Configure DbContext with container connection string
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(_postgresContainer.GetConnectionString())
            .Options;

        DbContext = new ApplicationDbContext(options, /* interceptors */);
        await DbContext.Database.MigrateAsync();

        // Configure services
        var services = new ServiceCollection();
        // ... register handlers, behaviors, etc.
        Dispatcher = serviceProvider.GetRequiredService<IDispatcher>();
    }

    public async Task DisposeAsync()
    {
        await _postgresContainer.DisposeAsync();
        await _redisContainer.DisposeAsync();
    }
}
```

### Examples

#### CreateTodoCommandTests.cs

```csharp
public class CreateTodoCommandTests : ApplicationIntegrationTestBase
{
    [Fact]
    public async Task Handle_WithValidCommand_ShouldPersistToDatabase()
    {
        // Arrange
        var command = new CreateTodoCommand("Learn Clean Architecture");

        // Act
        var result = await Dispatcher.Send(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();

        // Verify in database
        var todo = await DbContext.Todos.FindAsync(result.Value.Id);
        todo.ShouldNotBeNull();
        todo!.Title.Value.ShouldBe("Learn Clean Architecture");
        todo.Status.ShouldBe(TodoStatus.Pending);
    }

    [Fact]
    public async Task Handle_ShouldDispatchDomainEvent()
    {
        // Arrange
        var command = new CreateTodoCommand("Test");
        var eventHandlerCalled = false;

        // Subscribe to domain event (via test-specific handler)
        // ... setup event handler mock

        // Act
        await Dispatcher.Send(command, CancellationToken.None);

        // Assert
        eventHandlerCalled.ShouldBeTrue();
    }
}
```

#### GetTodosQueryTests.cs

```csharp
[Fact]
public async Task Handle_WithCursorPagination_ShouldReturnCorrectPage()
{
    // Arrange - Seed data
    for (int i = 1; i <= 50; i++)
    {
        var title = TodoTitle.Create($"Todo {i}").Value;
        var todo = Todo.Create(title, TodoStatus.Pending).Value;
        DbContext.Todos.Add(todo);
    }
    await DbContext.SaveChangesAsync();

    var query = new GetTodosQuery
    {
        Pagination = CursorPageRequest.First(10)
    };

    // Act
    var result = await Dispatcher.Send(query, CancellationToken.None);

    // Assert
    result.IsSuccess.ShouldBeTrue();
    result.Value.Items.Count.ShouldBe(10);
    result.Value.HasNextPage.ShouldBeTrue();
    result.Value.EndCursor.ShouldNotBeNull();
}
```

### Principles

- **Isolated Environment**: Each test in a transaction (or new container)
- **Realistic Data**: Test with production-like volumes
- **Automatic Cleanup**: TestContainers clean up automatically
- **Slow Tests**: Acceptable as they test real integration

---

## 4. Infrastructure.IntegrationTests

**Goal**: Test infrastructure components with real external services using TestContainers.

### What We Test

- ✅ **Caching**: Redis distributed cache with real Redis container
- ✅ **OpenTelemetry**: Activity tracing for cache operations
- ✅ **Serializers**: JSON and MessagePack serialization
- ✅ **Error Handling**: Deserialization failures, cache misses
- ✅ **Performance**: Compression, serialization metrics

### Architecture: WebApplicationFactory Pattern

We use `WebApplicationFactory<Program>` with shared TestContainers for optimal performance:

```csharp
// TestContainersFixture - Manages container lifecycle
public sealed class TestContainersFixture : IAsyncLifetime
{
    private RedisContainer? _redisContainer;
    public string RedisConnectionString { get; private set; } = string.Empty;

    public async Task InitializeAsync()
    {
        _redisContainer = new RedisBuilder("redis:7-alpine").Build();
        await _redisContainer.StartAsync();
        RedisConnectionString = _redisContainer.GetConnectionString();
    }

    public async Task DisposeAsync()
    {
        if (_redisContainer != null)
            await _redisContainer.DisposeAsync();
    }
}

// TestsWebApplicationFactory - Configures app with test dependencies
public sealed class TestsWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _redisConnectionString;

    public TestsWebApplicationFactory(string redisConnectionString)
    {
        _redisConnectionString = redisConnectionString;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:redis", _redisConnectionString);

        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:redis"] = _redisConnectionString
            });
        });

        base.ConfigureWebHost(builder);

        builder.ConfigureTestServices(services =>
        {
            // Replace services with test doubles if needed
            services.RemoveAll<IUser>()
                .AddTransient(provider => Mock.Of<IUser>());
        });
    }
}

// Test Collection - Shares containers across all tests
[CollectionDefinition(nameof(IntegrationTestCollection))]
public sealed class IntegrationTestCollection : ICollectionFixture<TestContainersFixture>
{
}
```

**Key Benefits**:

- **Performance**: Containers start once, shared across all tests (~2-5s startup, then ~10-50ms per test)
- **Realistic**: Uses real Redis, real serializers, real DI container
- **Isolation**: Each test creates its own factory instance but shares the same container
- **Cleanup**: TestContainers automatically dispose containers

### Examples

#### CacheTracingTests.cs

```csharp
[Collection(nameof(IntegrationTestCollection))]
public sealed class CacheTracingTests : IDisposable
{
    private readonly ActivityListener _activityListener;
    private readonly List<Activity> _activities = [];
    private readonly ICacheService _cacheService;
    private readonly TestsWebApplicationFactory _factory;

    public CacheTracingTests(TestContainersFixture containersFixture)
    {
        _activityListener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == "DotnetCleanAspire.Caching",
            Sample = (ref ActivityCreationOptions<ActivityContext> _) =>
                ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = activity => _activities.Add(activity)
        };
        ActivitySource.AddActivityListener(_activityListener);

        // Create factory with Redis connection from shared fixture
        _factory = new TestsWebApplicationFactory(containersFixture.RedisConnectionString);

        // Get real ICacheService from DI (DistributedCacheService implementation)
        _cacheService = _factory.Services.GetRequiredService<ICacheService>();
    }

    [Fact]
    public async Task GetAsync_ShouldCreateActivity_WithCorrectTags()
    {
        // Arrange
        var cacheKey = new CacheKey("todos", "123");
        await _cacheService.SetAsync(cacheKey, CacheEntry<string>.Create("test-value"),
            TimeSpan.FromMinutes(5));
        _activities.Clear();

        // Act
        await _cacheService.GetAsync<string>(cacheKey);

        // Assert
        Activity? activity = _activities.FirstOrDefault(a => a.DisplayName == "cache.get");
        activity.ShouldNotBeNull();
        activity.Tags.Any(t => t.Key == "cache.feature" && t.Value?.ToString() == "todos")
            .ShouldBeTrue();
    }

    [Fact]
    public async Task GetAsync_OnSerializationError_ShouldReturnNull()
    {
        // Arrange - Set a string value
        var cacheKey = new CacheKey("error", "incompatible-type");
        await _cacheService.SetAsync(cacheKey, CacheEntry<string>.Create("test-value"),
            TimeSpan.FromMinutes(5));
        _activities.Clear();

        // Act - Try to get as int (should fail deserialization)
        var result = await _cacheService.GetAsync<int>(cacheKey);

        // Assert - Should return null and log error
        result.ShouldBeNull();
        Activity? activity = _activities.FirstOrDefault(a => a.DisplayName == "cache.get");
        activity.ShouldNotBeNull();
        activity.Tags.Any(t => t.Key == "cache.deserialization_failed" &&
            t.Value?.ToString() == "True").ShouldBeTrue();
    }

    public void Dispose()
    {
        _activityListener.Dispose();
        _factory.Dispose();
    }
}
```

### Unit Tests vs Integration Tests

**When to Use Unit Tests** ([Infrastructure.UnitTests](../tests/Infrastructure.UnitTests)):

- Testing serializers in isolation (JsonCacheSerializer, MessagePackCacheSerializer)
- Testing helpers and utilities
- Testing logic that doesn't require external dependencies
- Fast feedback during development

**When to Use Integration Tests** ([Infrastructure.IntegrationTests](../tests/Infrastructure.IntegrationTests)):

- Testing caching behavior with real Redis
- Testing OpenTelemetry activity tracing
- Testing error scenarios (deserialization failures, connection issues)
- Testing compression and performance characteristics
- Validating configuration and DI registration

### Principles

- **Real Dependencies**: Use TestContainers, avoid mocks for infrastructure
- **Shared Containers**: One container per test suite, not per test
- **WebApplicationFactory**: Test with full DI container and configuration
- **Realistic Scenarios**: Test edge cases (deserialization errors, cache misses)
- **Observability**: Verify metrics and tracing work correctly

---

## Useful Commands

```bash
# Run all tests
dotnet test

# Run a specific test project
dotnet test tests/Domain.UnitTests

# Run with code coverage
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=cobertura

# Run in parallel (faster)
dotnet test --parallel

# Run only fast tests (unit tests)
dotnet test --filter FullyQualifiedName~UnitTests

# Run only integration tests
dotnet test --filter FullyQualifiedName~IntegrationTests
```

---

## Best Practices

### Test Naming

Format: `MethodName_Condition_ExpectedResult`

```csharp
✅ MarkAsCompleted_WhenAlreadyCompleted_ShouldReturnFailure
✅ Create_WithValidTitle_ShouldReturnSuccess
❌ TestMarkAsCompleted
❌ Test1
```

### AAA Pattern

Always use Arrange-Act-Assert:

```csharp
[Fact]
public void MyTest()
{
    // Arrange - Prepare data
    var title = TodoTitle.Create("Test").Value;

    // Act - Execute action
    var result = Todo.Create(title, TodoStatus.Pending);

    // Assert - Verify result
    result.IsSuccess.ShouldBeTrue();
}
```

### Shouldly

Prefer Shouldly for readable assertions:

```csharp
✅ result.IsSuccess.ShouldBeTrue();
✅ todo.Title.Value.ShouldBe("Test");
✅ todos.Count.ShouldBe(10);
✅ activity.ShouldNotBeNull();
❌ Assert.True(result.IsSuccess);
❌ Assert.Equal("Test", todo.Title.Value);
```

### Avoid Flaky Tests

- ⚠️ Avoid `Thread.Sleep()` or arbitrary delays
- ⚠️ Don't depend on test execution order
- ⚠️ Completely isolate each test
- ✅ Use fixtures to share setup
- ✅ Clean up after each test

---

## Coverage Metrics

Recommended targets:

| Layer                       | Target Coverage | Test Type         |
| --------------------------- | --------------- | ----------------- |
| **Domain**                  | 95%+            | Unit Tests        |
| **Application (Behaviors)** | 90%+            | Unit Tests        |
| **Application (Handlers)**  | 80%+            | Integration Tests |
| **Infrastructure**          | 70%+            | Integration Tests |

**Note**: 100% coverage is not the goal. Focus on critical paths and complex business logic.

---

## Resources

- [xUnit Documentation](https://xunit.net/)
- [Shouldly Documentation](https://docs.shouldly.org/)
- [TestContainers Documentation](https://dotnet.testcontainers.org/)
- [Moq Quick Reference](https://github.com/devlooped/moq/wiki/Quickstart)
- [WebApplicationFactory Testing](https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests)
