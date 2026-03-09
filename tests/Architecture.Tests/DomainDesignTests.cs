using System.Reflection;
using Domain.Abstractions;

namespace Architecture.Tests;

/// <summary>
/// Tests that verify Domain-Driven Design patterns in the Domain layer.
/// </summary>
public class DomainDesignTests
{
    private static readonly Assembly DomainAssembly = typeof(Entity).Assembly;

    #region Entity Tests

    [Fact]
    public void Entities_ShouldInherit_FromEntityOrAggregateRoot()
    {
        TestResult result = Types
            .InAssembly(DomainAssembly)
            .That()
            .ResideInNamespaceContaining("Entities")
            .And()
            .AreClasses()
            .And()
            .AreNotAbstract()
            .Should()
            .Inherit(typeof(Entity))
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(GetFailingTypes(result));
    }

    [Fact]
    public void Entities_ShouldBeSealed()
    {
        TestResult result = Types
            .InAssembly(DomainAssembly)
            .That()
            .Inherit(typeof(Entity))
            .And()
            .AreNotAbstract()
            .Should()
            .BeSealed()
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(GetFailingTypes(result));
    }

    #endregion

    #region Value Object Tests

    [Fact]
    public void ValueObjects_ShouldBeSealed()
    {
        TestResult result = Types
            .InAssembly(DomainAssembly)
            .That()
            .ResideInNamespaceContaining("ValueObjects")
            .And()
            .AreClasses()
            .Should()
            .BeSealed()
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(GetFailingTypes(result));
    }

    #endregion

    #region Domain Event Tests

    [Fact]
    public void DomainEventImplementations_ShouldBeSealed()
    {
        // Domain event implementations (not the base class) should be sealed
        TestResult result = Types
            .InAssembly(DomainAssembly)
            .That()
            .Inherit(typeof(DomainEventBase))
            .And()
            .AreNotAbstract()
            .Should()
            .BeSealed()
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(GetFailingTypes(result));
    }

    [Fact]
    public void DomainEventImplementations_ShouldResideInEventsNamespace()
    {
        TestResult result = Types
            .InAssembly(DomainAssembly)
            .That()
            .Inherit(typeof(DomainEventBase))
            .And()
            .AreNotAbstract()
            .Should()
            .ResideInNamespaceContaining("Events")
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(GetFailingTypes(result));
    }

    #endregion

    #region Domain Errors Tests

    [Fact]
    public void DomainErrors_ShouldBeStaticClasses()
    {
        TestResult result = Types
            .InAssembly(DomainAssembly)
            .That()
            .HaveNameEndingWith("Errors", StringComparison.Ordinal)
            .And()
            .ResideInNamespaceContaining("Errors")
            .Should()
            .BeStatic()
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(GetFailingTypes(result));
    }

    #endregion

    private static string GetFailingTypes(TestResult result)
    {
        return result.FailingTypeNames is null
            ? string.Empty
            : $"Failing types: {string.Join(", ", result.FailingTypeNames)}";
    }
}
