using Microsoft.EntityFrameworkCore;
using NevaxisZentra.Persistence.DigitalWorkforce;
using NevaxisZentra.Persistence.Integration;
using NevaxisZentra.Persistence.MixedProduction;
using NevaxisZentra.Persistence.RealFactoryPilot;
using NevaxisZentra.Persistence.ThroughputIntelligence;
using NevaxisZentra.Persistence.ThroughputOptimizer;

namespace NevaxisZentra.Persistence.Tests;

public sealed class RealFactoryPilotPermissionTests
{
    [Fact]
    public void PermissionKeysAreStable()
    {
        Assert.Equal("real-factory-pilot.read", RealFactoryPilotPermissionKeys.Read);
        Assert.Equal("real-factory-pilot.manage", RealFactoryPilotPermissionKeys.Manage);
        Assert.Equal("real-factory-pilot.run", RealFactoryPilotPermissionKeys.Run);
    }
}

public sealed class RealFactoryPilotEntityMetadataTests
{
    [Fact]
    public void EntitiesRequireTenantAndFacility()
    {
        using var context = CreateContext();
        Assert.False(context.Model.FindEntityType(typeof(RealFactoryPilotRunRecord))!.FindProperty(nameof(RealFactoryPilotRunRecord.TenantId))!.IsNullable);
        Assert.False(context.Model.FindEntityType(typeof(RealFactoryFieldMappingRecord))!.FindProperty(nameof(RealFactoryFieldMappingRecord.FacilityId))!.IsNullable);
        Assert.False(context.Model.FindEntityType(typeof(RealFactoryPilotEvidencePackRecord))!.FindProperty(nameof(RealFactoryPilotEvidencePackRecord.TenantId))!.IsNullable);
    }

    [Fact]
    public void EntitiesHaveTenantQueryFilters()
    {
        using var context = CreateContext();
        Assert.NotEmpty(context.Model.FindEntityType(typeof(RealFactoryShadowSessionRecord))!.GetDeclaredQueryFilters());
        Assert.NotEmpty(context.Model.FindEntityType(typeof(RealFactoryDataQualityRecord))!.GetDeclaredQueryFilters());
        Assert.NotEmpty(context.Model.FindEntityType(typeof(Mp2CalibrationVersionRecord))!.GetDeclaredQueryFilters());
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

public sealed class RealFactoryPilotEngineTests
{
    [Fact]
    public void AcceptanceBDuplicateIdempotencyKeyIsStable()
    {
        var tenantId = Guid.NewGuid();
        var facilityId = Guid.NewGuid();
        var connectorId = Guid.NewGuid();
        var key1 = RealFactoryFieldMappingEngine.BuildIdempotencyKey(
            tenantId, facilityId, connectorId, "PLC.M04.GoodPulse", MixedProductionEventTypes.Good, "100");
        var key2 = RealFactoryFieldMappingEngine.BuildIdempotencyKey(
            tenantId, facilityId, connectorId, "PLC.M04.GoodPulse", MixedProductionEventTypes.Good, "100");
        Assert.Equal(key1, key2);
    }

    [Fact]
    public void AcceptanceCBadMappingWithPlcWriteIsRejected()
    {
        var result = RealFactoryFieldMappingEngine.Validate([
            new FactoryFieldMappingRule("PLC.Write", "bool", "machine", "m1", "plc.write", null, null, null, null, "source"),
        ]);
        Assert.False(result.IsValid);
    }

    [Fact]
    public void AcceptanceAStaleConnectorBlocksAutonomousAction()
    {
        var plc = new IntegrationConnector { HealthStatus = IntegrationHealthStatuses.Stale };
        var mes = new IntegrationConnector { HealthStatus = IntegrationHealthStatuses.Healthy };
        var quality = RealFactoryDataQualityEngine.Assess(plc, mes, 30, DateTimeOffset.UtcNow.AddHours(-2), 0, 0, DateTimeOffset.UtcNow);
        Assert.True(quality.BlocksAutonomousAction);
        Assert.Contains("plc-stale-or-offline", quality.Issues);
    }

    [Fact]
    public void AcceptanceDShadowModeBlocksClassCActions()
    {
        var safetyClass = RealFactoryActionSafetyEngine.Classify(ThroughputIntelligenceScenarioTypes.ReassignResource);
        Assert.Equal(RealFactoryActionSafetyClasses.ControlledProduction, safetyClass);
        Assert.True(RealFactoryActionSafetyEngine.BlocksAutonomousExecution(safetyClass, shadowBlocksPhysical: true));
    }

    [Fact]
    public void AcceptanceEQualityFailurePreventsVerifiedImprovementLabel()
    {
        var baseline = new VerifiedImprovementBaseline(100m, 100, 5, 1, 10m, "ST-30", "OPERATOR_WAIT");
        var actual = new VerifiedImprovementActual(130m, 130, 12, 1, 10m, "ST-30", "OPERATOR_WAIT");
        var outcome = VerifiedImprovementEngine.Evaluate(baseline, 20m, actual, true, false, true);
        Assert.Equal(ThroughputOptimizerVerificationOutcomes.QualityGuardFailed, outcome);
    }

    [Fact]
    public void ClassDActionsAreForbiddenWithoutExplicitPolicy()
    {
        Assert.True(RealFactoryActionSafetyEngine.IsForbiddenWithoutExplicitPolicy(
            RealFactoryActionSafetyClasses.PhysicalProcessChange));
    }

    [Fact]
    public void DiscoveryProducesPlcAndMesFields()
    {
        Assert.NotEmpty(RealFactoryDiscoveryEngine.DiscoverPlcFields());
        Assert.NotEmpty(RealFactoryDiscoveryEngine.DiscoverMesFields());
    }

    [Fact]
    public void SyntheticAcceptanceKindIsDistinctFromRealFactory()
    {
        Assert.NotEqual(
            RealFactoryPilotAcceptanceKinds.Synthetic,
            RealFactoryPilotAcceptanceKinds.RealFactory);
    }
}

public sealed class RealFactoryPilotRegressionTests
{
    [Fact]
    public void Mp1Mp2Mp3EntitiesRemainRegistered()
    {
        using var context = CreateContext();
        Assert.NotNull(context.Model.FindEntityType(typeof(ProductionLine)));
        Assert.NotNull(context.Model.FindEntityType(typeof(LineThroughputTarget)));
        Assert.NotNull(context.Model.FindEntityType(typeof(ThroughputOptimizerRunRecord)));
        Assert.NotNull(context.Model.FindEntityType(typeof(IntegrationConnector)));
    }

    [Fact]
    public void ThroughputOptimizerWorkerRemainsCanonical()
    {
        Assert.Contains(
            DigitalWorkforceCanonicalWorkers.Definitions,
            d => d.Code == DigitalWorkforceWorkerCodes.ThroughputOptimizer);
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
