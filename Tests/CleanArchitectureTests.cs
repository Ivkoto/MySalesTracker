using ArchUnitNET.Domain;
using ArchUnitNET.Loader;
using ArchUnitNET.xUnit;
using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace ArchitectureTests;

public class CleanArchitectureTests
{
    private static readonly Architecture Architecture = new ArchLoader()
        .LoadAssemblies(
            typeof(MySalesTracker.Domain.Entities.Event).Assembly,
            typeof(MySalesTracker.Application.Services.EventService).Assembly,
            typeof(MySalesTracker.Infrastructure.Persistence.AppDbContext).Assembly,
            typeof(MySalesTracker.Web.Components.App).Assembly)
        .Build();

    private static readonly IObjectProvider<IType> DomainLayer =
        Types().That().ResideInAssembly(typeof(MySalesTracker.Domain.Entities.Event).Assembly).As("Domain Layer");

    private static readonly IObjectProvider<IType> ApplicationLayer =
        Types().That().ResideInAssembly(typeof(MySalesTracker.Application.Services.EventService).Assembly).As("Application Layer");

    private static readonly IObjectProvider<IType> InfrastructureLayer =
        Types().That().ResideInAssembly(typeof(MySalesTracker.Infrastructure.Persistence.AppDbContext).Assembly).As("Infrastructure Layer");

    private static readonly IObjectProvider<IType> WebLayer =
        Types().That().ResideInAssembly(typeof(MySalesTracker.Web.Components.App).Assembly).As("Web Layer");

    [Fact]
    public void Domain_Should_Not_Depend_On_Application()
    {
        var rule = Types()
            .That().Are(DomainLayer)
            .Should().NotDependOnAny(ApplicationLayer)
            .Because("Domain layer should not depend on Application layer in Clean Architecture");

        rule.Check(Architecture);
    }

    [Fact]
    public void Domain_Should_Not_Depend_On_Infrastructure()
    {
        var rule = Types()
            .That().Are(DomainLayer)
            .Should().NotDependOnAny(InfrastructureLayer)
            .Because("Domain layer should not depend on Infrastructure layer in Clean Architecture");

        rule.Check(Architecture);
    }

    [Fact]
    public void Domain_Should_Not_Depend_On_Web()
    {
        var rule = Types()
            .That().Are(DomainLayer)
            .Should().NotDependOnAny(WebLayer)
            .Because("Domain layer should not depend on Web layer in Clean Architecture");

        rule.Check(Architecture);
    }

    [Fact]
    public void Application_Should_Not_Depend_On_Infrastructure()
    {
        var rule = Types()
            .That().Are(ApplicationLayer)
            .Should().NotDependOnAny(InfrastructureLayer)
            .Because("Application layer should not depend on Infrastructure layer in Clean Architecture");

        rule.Check(Architecture);
    }

    [Fact]
    public void Application_Should_Not_Depend_On_Web()
    {
        var rule = Types()
            .That().Are(ApplicationLayer)
            .Should().NotDependOnAny(WebLayer)
            .Because("Application layer should not depend on Web layer in Clean Architecture");

        rule.Check(Architecture);
    }

    [Fact]
    public void Infrastructure_Should_Not_Depend_On_Web()
    {
        var rule = Types()
            .That().Are(InfrastructureLayer)
            .Should().NotDependOnAny(WebLayer)
            .Because("Infrastructure layer should not depend on Web layer in Clean Architecture");

        rule.Check(Architecture);
    }
}
