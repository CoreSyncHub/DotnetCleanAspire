# .NET Clean Architecture Boilerplate with Aspire

A production-ready .NET 10 boilerplate implementing Clean Architecture principles with Aspire for cloud-native development.

## Features

### Architecture

- **Clean Architecture** with clear layer separation (Domain, Application, Infrastructure, Presentation)
- **CQRS Pattern** with command and query separation
- **Domain-Driven Design** with rich domain models, aggregates, and value objects
- **Result Pattern** for railway-oriented programming
- **Pipeline Behaviors** for cross-cutting concerns (logging, validation, caching)

### Technology Stack

- **.NET 10** with C# 14 features (extension methods)
- **ASP.NET Core Minimal APIs** for lightweight endpoints
- **Entity Framework Core 10** with PostgreSQL
- **Redis** for distributed caching
- **.NET Aspire** for orchestration and cloud-native development
- **FluentValidation** for input validation
- **ULID** for sortable unique identifiers

### Aspire Integration

- **PostgreSQL** with automatic health checks and PgAdmin
- **Redis** with data persistence and RedisInsight
- **OpenTelemetry** for distributed tracing and metrics
- **Service Discovery** and resilience built-in

### Patterns Implemented

- Repository Pattern (via DbContext abstraction)
- Unit of Work (via SaveChangesAsync)
- Domain Events with automatic dispatching
- Cursor-based pagination for efficient data retrieval
- Audit trail with automatic timestamp tracking
- API versioning (URL and header-based)

## Project Structure

```
DotnetCleanAspire/
├── src/
│   ├── Domain/                 # Enterprise business rules
│   │   ├── Abstractions/       # Base entities, value objects, domain events
│   │   └── Todos/              # Todo aggregate with entities and value objects
│   ├── Application/            # Application business rules
│   │   ├── Abstractions/       # CQRS interfaces, behaviors
│   │   └── Features/           # Use cases organized by feature
│   ├── Infrastructure/         # External concerns
│   │   ├── Persistence/        # EF Core, migrations, interceptors
│   │   ├── Caching/            # Redis distributed cache
│   │   └── Identity/           # User context
│   ├── Presentation/           # API layer
│   │   ├── Endpoints/          # Minimal API endpoints
│   │   └── Middleware/         # Exception handling
│   ├── AspireHost/             # Aspire orchestration
│   └── ServiceDefaults/        # Shared Aspire configuration
└── docs/                       # Documentation
```

## Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Docker Desktop](https://www.docker.com/products/docker-desktop) (for PostgreSQL and Redis)
- Or [Podman](https://podman.io/) as an alternative container runtime
- [Aspire](https://learn.microsoft.com/dotnet/aspire/fundamentals/setup-tooling)

Install Aspire workload:

```bash
dotnet workload install aspire
```

### Quick Start

1. **Clone and restore packages:**

```bash
git clone https://github.com/yourusername/DotnetCleanAspire.git
cd DotnetCleanAspire
dotnet restore
```

2. **Configure secrets:**

```bash
# PostgreSQL password
dotnet user-secrets set "Parameters:postgres-password" "YourDevPassword" --project src/AspireHost

# JWT settings (optional, defaults provided)
dotnet user-secrets set "Jwt:Key" "your-secret-key-min-32-chars" --project src/Presentation
```

3. **Run with Aspire:**

```bash
# Using Aspire CLI (recommended)
aspire run

# Or using dotnet CLI
dotnet run --project src/AspireHost
```

4. **Access services:**

- Aspire Dashboard: `http://localhost:17158` (or port shown in console)
- API: `http://localhost:7000` (check Aspire dashboard for actual port)
- Swagger UI: `http://localhost:7000/swagger`
- PgAdmin: Available through Aspire dashboard
- RedisInsight: Available through Aspire dashboard

### Database Migrations

Migrations run automatically on startup (configurable in `appsettings.Development.json`).

To create a new migration:

```bash
dotnet ef migrations add YourMigrationName --project src/Infrastructure --startup-project src/Presentation
```

To apply migrations manually:

```bash
dotnet ef database update --project src/Infrastructure --startup-project src/Presentation
```

## Configuration

### Development

Configuration is in `appsettings.Development.json` and User Secrets:

```json
{
  "Jwt": {
    "Issuer": "https://your-api.com",
    "Audience": "https://your-api.com",
    "Key": "your-secret-key" // Use User Secrets!
  },
  "Ef": {
    "MigrateOnStartup": true
  }
}
```

### Production

- Use **Azure Key Vault** or **environment variables** for secrets
- Configure `appsettings.Production.json` with production values
- Set `MigrateOnStartup: false` and run migrations via CI/CD

## API Documentation

Swagger/OpenAPI documentation is available at `/swagger` in development mode.

### Example Endpoints

**Create Todo:**

```http
POST /api/v1/todos
Content-Type: application/json

{
  "title": "Learn Clean Architecture"
}
```

**Get Todos (with cursor pagination):**

```http
GET /api/v1/todos?pageSize=20&sortByCreatedAt=true&descending=true
```

**Complete Todo:**

```http
PATCH /api/v1/todos/{id}/complete
```

## Architecture Decisions

### Why ULID instead of GUID?

- Lexicographically sortable (time-ordered)
- Better for distributed systems
- More efficient database indexing
- Compatible with existing GUID infrastructure

### Why Cursor Pagination?

- More efficient for large datasets
- Consistent results even when data changes

### Why Result Type?

- Explicit error handling without exceptions
- Railway-oriented programming
- Better for business logic flow control

### Why Dynamic in Dispatcher?

See [docs/DISPATCHER_ALTERNATIVES.md](docs/DISPATCHER_ALTERNATIVES.md) for detailed analysis. TL;DR: It's a pragmatic choice used by industry-standard libraries like MediatR.

## Customizing the Template

### Adding a New Feature

1. **Define domain entities** in `src/Domain/YourFeature/`
2. **Create commands/queries** in `src/Application/Features/YourFeature/`
3. **Add handlers** implementing `ICommandHandler` or `IQueryHandler`
4. **Map endpoints** in `src/Presentation/Endpoints/YourFeatureEndpoints.cs`
5. **Run migrations** if database changes are needed

### Adding Pipeline Behaviors

Create a class implementing `IPipelineBehavior<TResponse>` in `src/Application/Abstractions/Behaviors/`.

Register it in `ApplicationDependencyInjection.cs`:

```csharp
builder.Services.AddScoped(typeof(IPipelineBehavior<>), typeof(YourBehavior<>));
```

## Production Checklist

Before deploying to production:

- [ ] Replace JWT secret with Key Vault reference
- [ ] Configure CORS for specific origins
- [ ] Set up structured logging (Serilog recommended)
- [ ] Configure rate limiting
- [ ] Add custom health checks for dependencies
- [ ] Set `MigrateOnStartup: false`
- [ ] Configure connection pooling for scale
- [ ] Set up monitoring and alerting
- [ ] Review and update CORS policies
- [ ] Enable HTTPS redirects
- [ ] Configure proper authentication/authorization

## Testing

This template includes example patterns for testing (optional during template instantiation):

```bash
# Run all tests
dotnet test

# Run specific test project
dotnet test tests/Application.Tests
```

## Contributing

This is a boilerplate template. Feel free to:

- Remove features you don't need
- Add your own patterns and practices
- Customize to fit your team's standards

## Resources

- [.NET Aspire Documentation](https://learn.microsoft.com/dotnet/aspire/)
- [Clean Architecture by Robert C. Martin](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)
- [Domain-Driven Design](https://martinfowler.com/bliki/DomainDrivenDesign.html)
- [CQRS Pattern](https://martinfowler.com/bliki/CQRS.html)

## License

MIT License - feel free to use this template for any project.

## Support

For issues and questions:

- Open an issue on GitHub
- Check [SETUP.md](SETUP.md) for configuration details
- Review [docs/](docs/) for architecture decisions
