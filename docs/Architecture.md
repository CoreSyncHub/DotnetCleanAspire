# Pile technique

- **Runtime**: .NET 10 LTS, ASP.NET Minimal APIs.
- **API**: REST + OpenAPI (Swashbuckle/Scalar). Versioning par header ou url `/v{n}`.
- **Data**: PostgreSQL (Npgsql) + **EF** (80% des cas). Dapper pour hot paths ou lectures complexes.
- **Cache**: Redis (StackExchange.Redis).
- **Queue**: RabbitMQ + MassTransit (ou Azure Service Bus si cloud Azure).
- **Jobs**: Hangfire (si besoin UI/Suivi), sinon `BackgroundService`.
- **Auth**: JWT access + refresh, ASP.NET Core Identity si nécessité de gérer les comptes, sinon IdP externe (Entra/Okta/Auth0).
  - **Refresh tokens**: Stockage DB avec expiration/révocation. Rotation absolue avec "Grace Period" (ex: 30-60s) pour éviter les erreurs 401 sur requêtes parallèles front-end (race conditions).
- **Time**: Utilisation stricte de `System.TimeProvider` (DI Singleton) pour tout ce qui touche au temps (testabilité). `DateOnly`/`TimeOnly` préférés, `DateTimeOffset` pour les timestamps.
- **Mapping**: Mapping manuel (projections EF Select) ou Mapster.
- **Validation**: FluentValidation.
- **Observabilité**: OpenTelemetry (Traces, Metrics, Logs) export OTLP → Grafana Stack (Loki/Tempo/Prometheus).
- **Hardening**: RFC7807 `ProblemDetails`, headers sécurité, rate-limit middleware (`Microsoft.AspNetCore.RateLimiting` natif .NET 7+), CORS strict.
- **Health checks**: `Microsoft.Extensions.Diagnostics.HealthChecks` pour readiness/liveness Kubernetes, checks DB/Redis/MQ.

# Orchestration & Infra

- **Local/dev**: **Aspire** pour orchestrer l'ensemble (API, DB, Cache, MQ) avec dashboards Télémétrie/Logs unifiés.
- **CI/CD**: GitHub Actions / Azure DevOps. Build conteneur, Tests, Scan vulnérabilités (SCA), Push Registry.
- **Prod**:
  - Utiliser **Aspire (aspir8/azd)** pour générer les manifestes Kubernetes/Helm (si l'équipe Ops est à l'aise).
  - Sinon docker Compose pour petit hébergement, Helm + Kubernetes quand besoin d’élasticité/rolling.
- **Config**: Options pattern + un gestionnaire de secrets (Azure Key Vault, HashiCorp Vault, AWS Secreet manager...).
- **Packages**: Central Package Management (`Directory.Packages.props`), `Directory.Build.props` pour settings globaux (Warnings as Errors, LangVersion), analyzers (StyleCop, Roslynator), nullable enabled.

# Architecture applicative (monolith modulaire propre)

- **Couche**:
  - `Domain` (entités, value objects, events)
  - `Application` (UseCases, CQRS light, ports)
  - `Infrastructure` (EF, MQ, IO...)
  - `Presentation` (API).
- **Découpage**: **“vertical slices”** par feature. Pas de mega dossiers horizontaux.
- **CQRS pragmatique**: Commands/Queries séparés dans `Application`, un seul store (PostgreSQL). Pas d'Event Sourcing par défaut (sauf si audit strict/domaine complexe requis).
- **UseCases**: un handler = un cas d’usage, idempotent si possible.
- **Transactions**: EF Core `ExecutionStrategy` + `DbContext` scoped par requête. Commit en fin de command handler.
- **Domain events**: in-process + **Outbox** table pour publier vers MQ/bus de façon transactionnelle.
- **Exceptions vs Result**:
  - **Exceptions**: uniquement pour “unexpected/technique” (DB down, null ref...), log en Error, mappées en 500.
  - **Erreurs métier**: **Result pattern** ou `OneOf`/discriminated unions. Pas d’exceptions pour le flux normal.
- **Validation**:
  - **Entrée**: FluentValidation sur DTOs + pipeline (Behavior avant handler).
  - **Domaine**: invariants dans constructeurs/factories, value objects non invalides par design.
- **Idempotence**: clé d’idempotence sur endpoints mutateurs ou via outbox + dédup.
- **Pagination**: Cursored pagination
  - **API Publique**: Link Headers (RFC standard).
  - **API interne:** Enveloppe JSON (data, meta) pour facilité la consommation.

# Pipelines et patterns utiles

- **MediatR** optionnel. Possibilité de faire un **dispatcher maison** simple, pour éviter la version commerciale, mais garder un pipeline:
  - `Logging` → `Idempotency` → `ValidationBehavior` → `AuthorizationBehavior` → `UnitOfWorkBehavior` → `Handler` → `OutboxFlushBehavior` → `CachingBehavior` (pour queries).
- **Polly**: Retries (avec Jitter) + Circuit Breaker sur appels IO externes. Timeouts stricts partout.
- **Caching**:
  - `IMemoryCache` : Cache court (per-request ou <1min).
  - `IDistributedCache` (Redis) : Données partagées. Invalidation par tags/clés.

# Contrats d’API et erreurs

- **ProblemDetails** partout, codes:
  - 400 validation, 401/403 auth, 404 not found, 409 conflict métier, 422 règle métier si pertinent, 500 inconnu.
- **OpenAPI** soigné, exemples, schémas d’erreurs standardisés. NSwag ou autre solutions de génération de code pour générer le client api pour l'application consommatrice.

# Tests

- **Unitaires**: Domain pur, UseCases sans EF, mocks des ports.
- **Intégration**: `WebApplicationFactory` + **Testcontainers** PostgreSQL/Redis/Rabbit. Tests end-to-end minimaux sur scénarios clés.
- **Architecture**: `NetArchTest` (ou `ArchUnit`) pour faire respecter les règles (ex: "Le Domain ne dépend pas de l'Infra", "Tout CommandHandler a un Validator").
- **Performance**: BenchmarkDotNet sur hot paths, `AsNoTracking` par défaut sur queries lecture.

# Sécurité et conformité

- **JWT** courts + rotation refresh. Pinned scopes/claims côté UseCases.
- **Input hardening**: size limits, allow-list MIME, antivirus si upload.
- **Logs**: pas de PII sensible. Corrélation `trace_id` partout. Masquage secrets.

# Structure de dossiers (exemple)

```
src/
  MyApp.Api/          // Presentation
  MyApp.Application/  // Commands, Queries, DTOs, Handlers, Behaviors, Interfaces
  MyApp.Domain/       // Entities, ValueObjects, DomainEvents, Specs
  MyApp.Infrastructure// EF, Repos, Migrations, MQ, Email, Files, Redis
  MyApp.Contracts/    // DTOs partagés si besoin
tests/
  MyApp.Tests.Unit/
  MyApp.Tests.Integration/
```

# Exemples rapides

## Result pattern minimal

```csharp
public readonly record struct Error(string Code, string Message);

public readonly struct Result<T>
{
    public bool IsSuccess { get; }
    public T? Value { get; }
    public Error? Error { get; }
    private Result(T value) { IsSuccess = true; Value = value; }
    private Result(Error error) { IsSuccess = false; Error = error; }
    public static Result<T> Ok(T value) => new(value);
    public static Result<T> Fail(string code, string message) => new(new Error(code, message));
}
```

## Mapping vers ProblemDetails

```csharp
app.UseExceptionHandler(cfg => cfg.Run(async ctx =>
{
    var ex = ctx.Features.Get<IExceptionHandlerFeature>()?.Error;
    var problem = ex switch
    {
        ValidationException ve => new ProblemDetails { Status = 400, Title="Validation failed", Detail=string.Join("; ", ve.Errors.Select(e=>e.ErrorMessage)) },
        _ => new ProblemDetails { Status = 500, Title="Server error" }
    };
    ctx.Response.StatusCode = problem.Status ?? 500;
    await ctx.Response.WriteAsJsonAsync(problem);
}));
```

# Check-list “qualité”

- [ ] **Code**: Nullable enabled, TreatWarningsAsErrors, Analyzers (Roslynator/Sonar), SonarQube...
- [ ] **Git**: Conventional Commits, PR Templates, ADRs pour décisions d’archi
- [ ] **Sécurité**: Pas de secrets dans le code, Scan de dépendances (SCA) dans la CI.
- [ ] **Input**: Limite taille requêtes, Whitelist MIME types uploads, Sanitize inputs.
- [ ] **Observabilité**: Logs structurés sans PII, TraceID propagé (Front -> API -> DB/MQ).
- [ ] **SLOs et alertes** : Basées sur SLI (latence P95, taux erreur, saturation).
