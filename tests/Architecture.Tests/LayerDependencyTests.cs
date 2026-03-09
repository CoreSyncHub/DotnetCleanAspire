using System.Reflection;

namespace Architecture.Tests;

/// <summary>
/// Tests that verify the Clean Architecture dependency rules.
/// The dependency flow should be: Presentation -> Infrastructure -> Application -> Domain
/// No layer should depend on an outer layer.
/// </summary>
public class LayerDependencyTests
{
    private static readonly Assembly DomainAssembly = typeof(Domain.Abstractions.Entity).Assembly;
    private static readonly Assembly ApplicationAssembly = typeof(Application.Abstractions.Messaging.ICommand<>).Assembly;
    private static readonly Assembly InfrastructureAssembly = typeof(Infrastructure.Persistence.ApplicationDbContext).Assembly;
    private static readonly Assembly PresentationAssembly = Assembly.Load("Presentation");

    #region Domain Layer Tests

    [Fact]
    public void Domain_ShouldNotDependOn_Application()
    {
        TestResult result = Types
            .InAssembly(DomainAssembly)
            .ShouldNot()
            .HaveDependencyOn("Application")
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(GetFailingTypes(result));
    }

    [Fact]
    public void Domain_ShouldNotDependOn_Infrastructure()
    {
        TestResult result = Types
            .InAssembly(DomainAssembly)
            .ShouldNot()
            .HaveDependencyOn("Infrastructure")
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(GetFailingTypes(result));
    }

    [Fact]
    public void Domain_ShouldNotDependOn_Presentation()
    {
        TestResult result = Types
            .InAssembly(DomainAssembly)
            .ShouldNot()
            .HaveDependencyOn("Presentation")
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(GetFailingTypes(result));
    }

    [Fact]
    public void Domain_ShouldNotDependOn_MicrosoftAspNetCore()
    {
        TestResult result = Types
            .InAssembly(DomainAssembly)
            .ShouldNot()
            .HaveDependencyOn("Microsoft.AspNetCore")
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(GetFailingTypes(result));
    }

    [Fact]
    public void Domain_ShouldNotDependOn_EntityFramework()
    {
        TestResult result = Types
            .InAssembly(DomainAssembly)
            .ShouldNot()
            .HaveDependencyOn("Microsoft.EntityFrameworkCore")
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(GetFailingTypes(result));
    }

    #endregion

    #region Application Layer Tests

    [Fact]
    public void Application_ShouldNotDependOn_Infrastructure()
    {
        TestResult result = Types
            .InAssembly(ApplicationAssembly)
            .ShouldNot()
            .HaveDependencyOn("Infrastructure")
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(GetFailingTypes(result));
    }

    [Fact]
    public void Application_ShouldNotDependOn_Presentation()
    {
        TestResult result = Types
            .InAssembly(ApplicationAssembly)
            .ShouldNot()
            .HaveDependencyOn("Presentation")
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(GetFailingTypes(result));
    }

    [Fact]
    public void Application_ShouldNotDependOn_MicrosoftAspNetCore()
    {
        TestResult result = Types
            .InAssembly(ApplicationAssembly)
            .ShouldNot()
            .HaveDependencyOn("Microsoft.AspNetCore")
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(GetFailingTypes(result));
    }

    #endregion

    #region Infrastructure Layer Tests

    [Fact]
    public void Infrastructure_ShouldNotDependOn_Presentation()
    {
        TestResult result = Types
            .InAssembly(InfrastructureAssembly)
            .ShouldNot()
            .HaveDependencyOn("Presentation")
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(GetFailingTypes(result));
    }

    #endregion

    #region Presentation Layer Tests

    [Theory]
    [InlineData("Entities")]
    [InlineData("ValueObjects")]
    [InlineData("Events")]
    [InlineData("Errors")]
    public void Presentation_ShouldNotDependOn_DomainInternals(string domainNamespaceSuffix)
    {
        // Presentation should only depend on Domain.Abstractions, not concrete domain types
        // This ensures proper separation and allows for API versioning via DTOs
        TestResult result = Types
            .InAssembly(PresentationAssembly)
            .That()
            .ResideInNamespaceStartingWith("Presentation.Endpoints")
            .ShouldNot()
            .HaveDependencyOnAny(GetDomainNamespacesEndingWith(domainNamespaceSuffix))
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(GetFailingTypes(result));
    }

    private static string[] GetDomainNamespacesEndingWith(string suffix)
    {
        // Get all namespaces from Domain assembly that end with the specified suffix
        return Types
            .InAssembly(DomainAssembly)
            .GetTypes()
            .Select(t => t.Namespace)
            .Where(ns => ns is not null && ns.EndsWith(suffix, StringComparison.Ordinal))
            .Distinct()
            .ToArray()!;
    }

    #endregion

    private static string GetFailingTypes(TestResult result)
    {
        return result.FailingTypeNames is null
            ? string.Empty
            : $"Failing types: {string.Join(", ", result.FailingTypeNames)}";
    }
}
