using Microsoft.EntityFrameworkCore;
using NevaxisFactOS.Persistence.ContextKnowledge;
using NevaxisFactOS.Persistence.DigitalWorkforce;
using NevaxisFactOS.Persistence.ModelGateway;
using NevaxisFactOS.Persistence.MixedProduction;
using NevaxisFactOS.Persistence.ThroughputIntelligence;
using NevaxisFactOS.Persistence.ThroughputOptimizer;

namespace NevaxisFactOS.Persistence.Tests;

public sealed class Mp3PermissionTests
{
    [Fact]
    public void ModelGatewayAndThroughputOptimizerPermissionKeysAreStable()
    {
        Assert.Equal("model-gateway.read", ModelGatewayPermissionKeys.Read);
        Assert.Equal("model-gateway.route", ModelGatewayPermissionKeys.Route);
        Assert.Equal("throughput-optimizer.read", ThroughputOptimizerPermissionKeys.Read);
        Assert.Equal("throughput-optimizer.manage", ThroughputOptimizerPermissionKeys.Manage);
        Assert.Equal("throughput-optimizer.run", ThroughputOptimizerPermissionKeys.Run);
    }
}

public sealed class Mp3EntityMetadataTests
{
    [Fact]
    public void Mp3EntitiesRequireTenantAndFacility()
    {
        using var context = CreateContext();
        Assert.False(context.Model.FindEntityType(typeof(ModelGatewayAuditRecord))!.FindProperty(nameof(ModelGatewayAuditRecord.TenantId))!.IsNullable);
        Assert.False(context.Model.FindEntityType(typeof(ThroughputOptimizerRunRecord))!.FindProperty(nameof(ThroughputOptimizerRunRecord.FacilityId))!.IsNullable);
        Assert.False(context.Model.FindEntityType(typeof(VerifiedImprovementRecord))!.FindProperty(nameof(VerifiedImprovementRecord.TenantId))!.IsNullable);
    }

    [Fact]
    public void Mp3EntitiesHaveTenantQueryFilters()
    {
        using var context = CreateContext();
        Assert.NotEmpty(context.Model.FindEntityType(typeof(ModelGatewayAuditRecord))!.GetDeclaredQueryFilters());
        Assert.NotEmpty(context.Model.FindEntityType(typeof(ThroughputOptimizerRunRecord))!.GetDeclaredQueryFilters());
        Assert.NotEmpty(context.Model.FindEntityType(typeof(VerifiedTrainingDatasetCandidateRecord))!.GetDeclaredQueryFilters());
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

public sealed class ModelGatewayEngineTests
{
    [Fact]
    public void AcceptanceCDeterministicCalculationRoutesToDeterministicEngine()
    {
        var routing = ModelRouterEngine.Route(
            new ModelGatewayRequest(
                Guid.NewGuid(), Guid.NewGuid(), null, null,
                ModelGatewayRequestClassifications.ThroughputCalculation,
                "throughput", new ModelGatewayContextReference(null, null, null, null, null, "HIGH"),
                1, false),
            null);
        Assert.Equal(ModelGatewayIntelligencePaths.DeterministicEngine, routing.IntelligencePath);
    }

    [Fact]
    public void AcceptanceDComplexDiagnosisIsModelEligible()
    {
        var routing = ModelRouterEngine.Route(
            new ModelGatewayRequest(
                Guid.NewGuid(), Guid.NewGuid(), null, null,
                ModelGatewayRequestClassifications.CrossModuleDiagnosis,
                "diagnosis", new ModelGatewayContextReference(null, "RUNNING", null, "ST-30", null, "HIGH"),
                2, true),
            new ModelCostBudgetState(10m, 100m, 5m, false));
        Assert.True(
            routing.IntelligencePath is ModelGatewayIntelligencePaths.FrontierModel
                or ModelGatewayIntelligencePaths.LocalModel
                or ModelGatewayIntelligencePaths.DeterministicEngine);
    }

    [Fact]
    public void AcceptanceEInvalidSchemaProposalIsRejected()
    {
        var result = OperationalProposalValidator.Validate(new OperationalProposal(
            "", [], [], "", [], 0.5m, [], []));
        Assert.False(result.Accepted);
    }

    [Fact]
    public void AcceptanceFUnauthorizedToolActionIsRejected()
    {
        var result = OperationalProposalValidator.Validate(new OperationalProposal(
            "Test",
            ["evidence-1"],
            [new OperationalProposalAction("plc.write", "machine", "m1", "unsafe", ["e1"])],
            "bad",
            [],
            0.5m,
            [],
            []));
        Assert.False(result.Accepted);
        Assert.Contains(result.RejectionReasons, r => r.StartsWith("unauthorized-action-type", StringComparison.Ordinal));
    }

    [Fact]
    public void AcceptanceGProviderUnavailableFallsBackToDeterministic()
    {
        var original = new ModelGatewayRoutingDecision(
            ModelGatewayIntelligencePaths.FrontierModel,
            ModelGatewayProviderIds.FrontierModel,
            "frontier", "1.0",
            "test", false);
        var fallback = ModelRouterEngine.Fallback(original, ModelGatewayFailureStates.ProviderUnavailable);
        Assert.True(fallback.UsedFallback);
    }
}

public sealed class ThroughputOptimizerEngineTests
{
    [Fact]
    public void AcceptanceACorrelationKeyIsStablePerLine()
    {
        var lineId = Guid.NewGuid();
        var key = ThroughputObserveEngine.BuildCorrelationKey(lineId);
        Assert.Contains(lineId.ToString("N"), key, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AcceptanceNCompletedActionWithoutImprovementIsNotVerifiedSuccess()
    {
        var baseline = new VerifiedImprovementBaseline(100m, 100, 5, 1, 10m, "ST-30", "OPERATOR_WAIT");
        var actual = new VerifiedImprovementActual(100m, 100, 5, 1, 10m, "ST-30", "OPERATOR_WAIT");
        var outcome = VerifiedImprovementEngine.Evaluate(baseline, 20m, actual, true, true, true);
        Assert.Equal(ThroughputOptimizerVerificationOutcomes.NoImprovement, outcome);
    }

    [Fact]
    public void AcceptanceOQualityGuardFailureFailsVerification()
    {
        var baseline = new VerifiedImprovementBaseline(100m, 100, 5, 1, 10m, "ST-30", "OPERATOR_WAIT");
        var actual = new VerifiedImprovementActual(130m, 130, 5, 1, 10m, "ST-30", "OPERATOR_WAIT");
        var outcome = VerifiedImprovementEngine.Evaluate(baseline, 20m, actual, true, false, true);
        Assert.Equal(ThroughputOptimizerVerificationOutcomes.QualityGuardFailed, outcome);
    }

    [Fact]
    public void AcceptancePQualityGuardFailureFailsVerificationSafety()
    {
        var baseline = new VerifiedImprovementBaseline(100m, 100, 5, 1, 10m, "ST-30", "OPERATOR_WAIT");
        var actual = new VerifiedImprovementActual(130m, 130, 5, 1, 10m, "ST-30", "OPERATOR_WAIT");
        var outcome = VerifiedImprovementEngine.Evaluate(baseline, 20m, actual, true, true, false);
        Assert.Equal(ThroughputOptimizerVerificationOutcomes.SafetyGuardFailed, outcome);
    }

    [Fact]
    public void AcceptanceQVerifiedImprovementEligibleForDataset()
    {
        var (status, _, exportEligible) = VerifiedDatasetGovernanceEngine.Evaluate(
            ThroughputOptimizerVerificationOutcomes.VerifiedImprovement, true, true, false);
        Assert.Equal(VerifiedDatasetEligibilityStatuses.Eligible, status);
        Assert.True(exportEligible);
    }

    [Fact]
    public void AcceptanceRUnverifiedOutcomeNotEligibleForDataset()
    {
        var (status, reason, _) = VerifiedDatasetGovernanceEngine.Evaluate(
            ThroughputOptimizerVerificationOutcomes.NoImprovement, true, true, false);
        Assert.Equal(VerifiedDatasetEligibilityStatuses.Ineligible, status);
        Assert.Equal("unverified-outcome", reason);
    }

    [Fact]
    public void ThroughputOptimizerWorkerIsRegisteredInCanonicalCatalog()
    {
        Assert.Contains(
            DigitalWorkforceCanonicalWorkers.Definitions,
            d => d.Code == DigitalWorkforceWorkerCodes.ThroughputOptimizer);
    }

    [Fact]
    public void ThroughputEventsRouteToThroughputOptimizerWorker()
    {
        var workerId = DigitalWorkforceEventDispatcher.ResolveWorkerId("throughput.gap");
        Assert.Equal(DevelopmentPilotDigitalWorkforceIds.WorkerThroughputOptimizer, workerId);
    }

    [Fact]
    public void AcceptanceMVerifiedImprovementRequiresSufficientDelta()
    {
        var baseline = new VerifiedImprovementBaseline(412m, 412, 8, 2, 12m, "ST-30", "OPERATOR_WAIT");
        var actual = new VerifiedImprovementActual(500m, 500, 8, 2, 10m, "ST-20", "CAPACITY");
        var outcome = VerifiedImprovementEngine.Evaluate(baseline, 68m, actual, true, true, true);
        Assert.Equal(ThroughputOptimizerVerificationOutcomes.VerifiedImprovement, outcome);
    }
}

public sealed class Mp3RegressionTests
{
    [Fact]
    public void AcceptanceTMp1EntitiesRemainRegistered()
    {
        using var context = CreateContext();
        Assert.NotNull(context.Model.FindEntityType(typeof(ProductionLine)));
        Assert.NotNull(context.Model.FindEntityType(typeof(CanonicalProductionEvent)));
    }

    [Fact]
    public void AcceptanceUMp2EntitiesRemainRegistered()
    {
        using var context = CreateContext();
        Assert.NotNull(context.Model.FindEntityType(typeof(LineThroughputTarget)));
        Assert.NotNull(context.Model.FindEntityType(typeof(ThroughputScenarioDefinition)));
    }

    [Fact]
    public void AcceptanceVContextKnowledgeEntitiesRemainRegistered()
    {
        using var context = CreateContext();
        Assert.NotNull(context.Model.FindEntityType(typeof(CanonicalOperationalStateRecord)));
        Assert.NotNull(context.Model.FindEntityType(typeof(FactoryKnowledgeRecord)));
    }

    [Fact]
    public void AcceptanceWDigitalWorkforceEntitiesRemainRegistered()
    {
        using var context = CreateContext();
        Assert.NotNull(context.Model.FindEntityType(typeof(DigitalWorkCase)));
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
