using Microsoft.EntityFrameworkCore;
using NevaxisZentra.Persistence.DigitalWorkforce;
using NevaxisZentra.Persistence.Integration.FactoryEventSpine;
using NevaxisZentra.Persistence.MixedProduction;
using NevaxisZentra.Persistence.ThroughputIntelligence;

namespace NevaxisZentra.Persistence.Tests;

public sealed class ThroughputIntelligenceTenantIsolationTests
{
    [Fact]
    public void OperatorSkillProfileHasTenantQueryFilter()
    {
        using var context = CreateContext();
        var entityType = context.Model.FindEntityType(typeof(OperatorSkillProfile));
        Assert.NotNull(entityType);
        Assert.NotEmpty(entityType.GetDeclaredQueryFilters());
    }

    [Fact]
    public void ThroughputScenarioDefinitionHasTenantQueryFilter()
    {
        using var context = CreateContext();
        var entityType = context.Model.FindEntityType(typeof(ThroughputScenarioDefinition));
        Assert.NotNull(entityType);
        Assert.NotEmpty(entityType.GetDeclaredQueryFilters());
    }

    private static NevaxisZentraDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<NevaxisZentraDbContext>()
            .UseNpgsql(
                "Host=localhost;Database=nevaxiszentra_test;Username=postgres;Password=postgres",
                npgsql => npgsql.MigrationsAssembly(NevaxisZentraDbContext.MigrationsAssemblyName))
            .Options;
        return new NevaxisZentraDbContext(options);
    }
}

public sealed class ThroughputIntelligenceCrossModuleRegressionTests
{
    [Fact]
    public void Mp1MixedProductionEntitiesRemainRegistered()
    {
        using var context = CreateContext();
        Assert.NotNull(context.Model.FindEntityType(typeof(ProductionLine)));
        Assert.NotNull(context.Model.FindEntityType(typeof(CanonicalProductionEvent)));
        Assert.NotNull(context.Model.FindEntityType(typeof(BufferResourceState)));
    }

    [Fact]
    public void FactoryEventSpineEntitiesRemainRegistered()
    {
        using var context = CreateContext();
        Assert.NotNull(context.Model.FindEntityType(typeof(IntegrationCanonicalFactoryEvent)));
    }

    [Fact]
    public void DigitalWorkCaseEntitiesRemainRegistered()
    {
        using var context = CreateContext();
        Assert.NotNull(context.Model.FindEntityType(typeof(DigitalWorkCase)));
    }

    [Fact]
    public void InjectionMachineResourceTypeUnchanged()
    {
        Assert.Equal("machine", MixedProductionResourceTypes.Machine);
    }

    private static NevaxisZentraDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<NevaxisZentraDbContext>()
            .UseNpgsql(
                "Host=localhost;Database=nevaxiszentra_test;Username=postgres;Password=postgres",
                npgsql => npgsql.MigrationsAssembly(NevaxisZentraDbContext.MigrationsAssemblyName))
            .Options;
        return new NevaxisZentraDbContext(options);
    }
}
