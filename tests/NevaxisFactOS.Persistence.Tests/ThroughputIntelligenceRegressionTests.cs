using Microsoft.EntityFrameworkCore;
using NevaxisFactOS.Persistence.DigitalWorkforce;
using NevaxisFactOS.Persistence.Integration.FactoryEventSpine;
using NevaxisFactOS.Persistence.MixedProduction;
using NevaxisFactOS.Persistence.ThroughputIntelligence;

namespace NevaxisFactOS.Persistence.Tests;

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

    private static NevaxisFactOSDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<NevaxisFactOSDbContext>()
            .UseNpgsql(
                "Host=localhost;Database=nevaxisfactos_test;Username=postgres;Password=postgres",
                npgsql => npgsql.MigrationsAssembly(NevaxisFactOSDbContext.MigrationsAssemblyName))
            .Options;
        return new NevaxisFactOSDbContext(options);
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

    private static NevaxisFactOSDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<NevaxisFactOSDbContext>()
            .UseNpgsql(
                "Host=localhost;Database=nevaxisfactos_test;Username=postgres;Password=postgres",
                npgsql => npgsql.MigrationsAssembly(NevaxisFactOSDbContext.MigrationsAssemblyName))
            .Options;
        return new NevaxisFactOSDbContext(options);
    }
}
