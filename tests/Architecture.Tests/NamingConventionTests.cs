using System.Reflection;

namespace Architecture.Tests;

/// <summary>
/// Tests that verify naming conventions across the codebase.
/// </summary>
public class NamingConventionTests
{
    private static readonly Assembly ApplicationAssembly = typeof(Application.Abstractions.Messaging.ICommand<>).Assembly;
    private static readonly Assembly InfrastructureAssembly = typeof(Infrastructure.Persistence.ApplicationDbContext).Assembly;
    private static readonly Assembly PresentationAssembly = Assembly.Load("Presentation");

    #region Command Handler Tests

    [Fact]
    public void CommandHandlerImplementations_ShouldHave_CommandHandlerSuffix()
    {
        TestResult result = Types
            .InAssembly(ApplicationAssembly)
            .That()
            .ImplementInterface(typeof(Application.Abstractions.Messaging.ICommandHandler<,>))
            .And()
            .AreClasses()
            .Should()
            .HaveNameEndingWith("CommandHandler", StringComparison.Ordinal)
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(GetFailingTypes(result));
    }

    [Fact]
    public void CommandHandlerImplementations_ShouldResideIn_ApplicationFeatures()
    {
        TestResult result = Types
            .InAssembly(ApplicationAssembly)
            .That()
            .ImplementInterface(typeof(Application.Abstractions.Messaging.ICommandHandler<,>))
            .And()
            .AreClasses()
            .Should()
            .ResideInNamespaceStartingWith("Application.Features")
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(GetFailingTypes(result));
    }

    #endregion

    #region Query Handler Tests

    [Fact]
    public void QueryHandlerImplementations_ShouldHave_QueryHandlerSuffix()
    {
        TestResult result = Types
            .InAssembly(ApplicationAssembly)
            .That()
            .ImplementInterface(typeof(Application.Abstractions.Messaging.IQueryHandler<,>))
            .And()
            .AreClasses()
            .Should()
            .HaveNameEndingWith("QueryHandler", StringComparison.Ordinal)
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(GetFailingTypes(result));
    }

    [Fact]
    public void QueryHandlerImplementations_ShouldResideIn_ApplicationFeatures()
    {
        TestResult result = Types
            .InAssembly(ApplicationAssembly)
            .That()
            .ImplementInterface(typeof(Application.Abstractions.Messaging.IQueryHandler<,>))
            .And()
            .AreClasses()
            .Should()
            .ResideInNamespaceStartingWith("Application.Features")
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(GetFailingTypes(result));
    }

    #endregion

    #region Validator Tests

    [Fact]
    public void Validators_ShouldHave_ValidatorSuffix()
    {
        TestResult result = Types
            .InAssembly(ApplicationAssembly)
            .That()
            .Inherit(typeof(FluentValidation.AbstractValidator<>))
            .Should()
            .HaveNameEndingWith("Validator", StringComparison.Ordinal)
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(GetFailingTypes(result));
    }

    #endregion

    #region Endpoint Tests

    [Fact]
    public void Endpoints_ShouldHave_EndpointsSuffix()
    {
        TestResult result = Types
            .InAssembly(PresentationAssembly)
            .That()
            .ResideInNamespace("Presentation.Endpoints")
            .And()
            .AreClasses()
            .And()
            .AreNotAbstract()
            .Should()
            .HaveNameEndingWith("Endpoints", StringComparison.Ordinal)
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(GetFailingTypes(result));
    }

    [Fact]
    public void Endpoints_ShouldResideIn_PresentationEndpoints()
    {
        TestResult result = Types
            .InAssembly(PresentationAssembly)
            .That()
            .HaveNameEndingWith("Endpoints", StringComparison.Ordinal)
            .Should()
            .ResideInNamespaceStartingWith("Presentation.Endpoints")
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(GetFailingTypes(result));
    }

    #endregion

    #region Service Tests

    [Fact]
    public void InfrastructureIdentityServices_ShouldHave_ServiceSuffix()
    {
        TestResult result = Types
            .InAssembly(InfrastructureAssembly)
            .That()
            .ResideInNamespace("Infrastructure.Identity.Services")
            .And()
            .AreClasses()
            .And()
            .AreNotAbstract()
            .Should()
            .HaveNameEndingWith("Service", StringComparison.Ordinal)
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
