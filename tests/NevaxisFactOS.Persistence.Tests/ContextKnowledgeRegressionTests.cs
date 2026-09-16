using Microsoft.EntityFrameworkCore;
using NevaxisFactOS.Persistence.ContextKnowledge;
using NevaxisFactOS.Persistence.DigitalWorkforce;
using NevaxisFactOS.Persistence.Integration.FactoryEventSpine;
using NevaxisFactOS.Persistence.MixedProduction;
using NevaxisFactOS.Persistence.ThroughputIntelligence;

namespace NevaxisFactOS.Persistence.Tests;

public sealed class ContextKnowledgeTenantIsolationTests
{
    [Fact]
    public void AcceptanceDContextKnowledgeEntitiesHaveTenantQueryFilters()
    {
        using var context = CreateContext();
        Assert.NotEmpty(context.Model.FindEntityType(typeof(FactoryKnowledgeRecord))!.GetDeclaredQueryFilters());
        Assert.NotEmpty(context.Model.FindEntityType(typeof(ContextSnapshotRecord))!.GetDeclaredQueryFilters());
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

public sealed class ContextKnowledgeCrossModuleRegressionTests
{
    [Fact]
    public void AcceptanceLDigitalWorkCaseEntitiesRemainRegistered()
    {
        using var context = CreateContext();
        Assert.NotNull(context.Model.FindEntityType(typeof(DigitalWorkCase)));
        Assert.NotNull(context.Model.FindEntityType(typeof(DigitalWorkCaseParticipant)));
    }

    [Fact]
    public void AcceptanceMMp1MixedProductionEntitiesRemainRegistered()
    {
        using var context = CreateContext();
        Assert.NotNull(context.Model.FindEntityType(typeof(ProductionLine)));
        Assert.NotNull(context.Model.FindEntityType(typeof(ProductionStation)));
        Assert.NotNull(context.Model.FindEntityType(typeof(CanonicalProductionEvent)));
        Assert.NotNull(context.Model.FindEntityType(typeof(BufferResourceState)));
    }

    [Fact]
    public void AcceptanceNMp2ThroughputIntelligenceEntitiesRemainRegistered()
    {
        using var context = CreateContext();
        Assert.NotNull(context.Model.FindEntityType(typeof(LineThroughputTarget)));
        Assert.NotNull(context.Model.FindEntityType(typeof(ThroughputScenarioDefinition)));
        Assert.NotNull(context.Model.FindEntityType(typeof(ThroughputScenarioResult)));
    }

    [Fact]
    public void AcceptanceOIntegrationEventSpineEntitiesRemainRegistered()
    {
        using var context = CreateContext();
        Assert.NotNull(context.Model.FindEntityType(typeof(IntegrationCanonicalFactoryEvent)));
    }

    [Fact]
    public void ContextKnowledgeEntitiesAreRegistered()
    {
        using var context = CreateContext();
        Assert.NotNull(context.Model.FindEntityType(typeof(CanonicalOperationalStateRecord)));
        Assert.NotNull(context.Model.FindEntityType(typeof(FactoryKnowledgeRecord)));
        Assert.NotNull(context.Model.FindEntityType(typeof(ContextSnapshotRecord)));
        Assert.NotNull(context.Model.FindEntityType(typeof(DataLifecyclePolicyRecord)));
        Assert.NotNull(context.Model.FindEntityType(typeof(TelemetryAggregateRecord)));
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
