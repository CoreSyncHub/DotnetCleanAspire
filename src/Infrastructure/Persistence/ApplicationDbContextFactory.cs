using Application.Abstractions.Helpers;
using Application.Abstractions.Messaging;
using Infrastructure.Persistence.Interceptors;
using Microsoft.EntityFrameworkCore.Design;

namespace Infrastructure.Persistence;

internal sealed class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        DbContextOptionsBuilder<ApplicationDbContext> optionsBuilder = new();
        optionsBuilder.UseNpgsql("Host=localhost;Database=designtime;Username=postgres;Password=postgres");

        NullUser nullUser = new();
        NullDispatcher nullDispatcher = new();
        AuditableEntityInterceptor auditableInterceptor = new(nullUser);
        DomainEventDispatchInterceptor domainEventInterceptor = new(nullDispatcher);

        return new ApplicationDbContext(optionsBuilder.Options, auditableInterceptor, domainEventInterceptor);
    }

    private sealed class NullUser : IUser
    {
        public Id? Id => null;
        public string? Email => null;
        public bool IsAuthenticated => false;
        public IReadOnlyList<string> Roles => [];
        public bool IsInRole(string role) => false;
    }

    private sealed class NullDispatcher : IDispatcher
    {
        public Task Publish<TEvent>(
            TEvent domainEvent,
            CancellationToken cancellationToken = default) where TEvent : Domain.Abstractions.IDomainEvent =>
            Task.CompletedTask;

        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default) =>
            Task.FromResult(default(TResponse)!);
    }
}
