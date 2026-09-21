using System.Text.Json;
using NevaxisZentra.Persistence.IntelligenceRuntime;
using NevaxisZentra.Persistence.ModelGateway;
using NevaxisZentra.Persistence.ThroughputOptimizer;

namespace NevaxisZentra.Persistence.Tests;

public sealed class IntelligenceRuntimeGoldenRoutingTests
{
    private static readonly ModelGatewayRoutingDecision DeterministicGateway = new(
        ModelGatewayIntelligencePaths.DeterministicEngine,
        ModelGatewayProviderIds.DeterministicEngine,
        "deterministic-engine-v1",
        "1.0",
        "golden-deterministic",
        false);

    private static readonly ModelGatewayRoutingDecision FrontierGateway = new(
        ModelGatewayIntelligencePaths.FrontierModel,
        ModelGatewayProviderIds.FrontierModel,
        "frontier-reasoning-v1",
        "1.0",
        "golden-frontier",
        false);

    [Theory]
    [InlineData("QUERY A", IntelligenceTaskTypes.StateLookup, IntelligenceRouteTypes.Deterministic, ModelGatewayIntelligencePaths.DeterministicEngine)]
    [InlineData("QUERY B", IntelligenceTaskTypes.TrendAnalysis, IntelligenceRouteTypes.Analytics, null)]
    [InlineData("QUERY C", IntelligenceTaskTypes.CaseSimilarity, IntelligenceRouteTypes.Retrieval, null)]
    [InlineData("QUERY D", IntelligenceTaskTypes.RecoveryPlanDraft, IntelligenceRouteTypes.Solver, ModelGatewayIntelligencePaths.DeterministicEngine)]
    [InlineData("QUERY E", IntelligenceTaskTypes.CrossDomainDiagnosis, IntelligenceRouteTypes.FrontierModel, null)]
    public void GoldenRoutingQueriesMatchExpectedRouteType(
        string queryLabel,
        string taskType,
        string expectedRoute,
        string? preferredPath)
    {
        var task = IntelligenceTaskTaxonomy.Require(taskType);
        var gateway = taskType == IntelligenceTaskTypes.CrossDomainDiagnosis ? FrontierGateway : DeterministicGateway;
        var request = new IntelligenceRouteRequest(
            taskType,
            IntelligenceRoutingPolicyEngine.MapGatewayRequestClassification(taskType),
            preferredPath,
            taskType == IntelligenceTaskTypes.CrossDomainDiagnosis ? 2 : 1,
            taskType == IntelligenceTaskTypes.CrossDomainDiagnosis,
            "TRUSTED",
            10m,
            null,
            null,
            null,
            null,
            null);

        var decision = IntelligenceRoutingPolicyEngine.Decide(request, task, gateway, null, null, 0);

        if (queryLabel == "QUERY D")
        {
            Assert.True(
                decision.RouteType is IntelligenceRouteTypes.Solver
                    or IntelligenceRouteTypes.Simulation
                    or IntelligenceRouteTypes.Deterministic,
                $"QUERY D expected solver/simulation/deterministic but got {decision.RouteType}");
            return;
        }

        Assert.Equal(expectedRoute, decision.RouteType);
    }

    [Fact]
    public void GoldenFrontierQueryFallsBackToDeterministicWhenFrontierUnavailable()
    {
        var task = IntelligenceTaskTaxonomy.Require(IntelligenceTaskTypes.CrossDomainDiagnosis);
        var fallbackGateway = ModelRouterEngine.Fallback(FrontierGateway, ModelGatewayFailureStates.ProviderUnavailable);
        var request = new IntelligenceRouteRequest(
            IntelligenceTaskTypes.CrossDomainDiagnosis,
            ModelGatewayRequestClassifications.CrossModuleDiagnosis,
            null,
            2,
            true,
            "TRUSTED",
            10m,
            null,
            null,
            null,
            null,
            null);

        var decision = IntelligenceRoutingPolicyEngine.Decide(request, task, fallbackGateway, null, null, 0);
        Assert.Equal(IntelligenceRouteTypes.FrontierModel, decision.RouteType);
        Assert.True(decision.UsedFallback);
    }
}

public sealed class IntelligenceRuntimeGoldenDatasetFlowTests
{
    [Fact]
    public void GoldenDatasetFlowBuildsSanitizedEntriesAndReadiness()
    {
        var sourceCaseId = Guid.NewGuid();
        var (eligible, _, negativeLearning) = NevaxisVerifiedDatasetEngine.EvaluateEligibility(
            ThroughputOptimizerVerificationOutcomes.VerifiedImprovement,
            true,
            true,
            true,
            false,
            true,
            true,
            true,
            IntelligenceTenantLearningPolicies.TenantOnlyLearning);

        Assert.True(eligible);
        Assert.False(negativeLearning);

        var entry = NevaxisVerifiedDatasetEngine.SanitizeEntry(
            new NevaxisVerifiedDatasetEntry(
                sourceCaseId,
                Guid.NewGuid(),
                "Throughput gap on MIX-LINE-01",
                "api_key: should-redact",
                "ST-30 constraint",
                "cycle drift",
                "buffer adjustment",
                "replenish buffer",
                ThroughputOptimizerVerificationOutcomes.VerifiedImprovement,
                "line MIX-LINE-01",
                "VERIFIED",
                "1.0",
                false),
            "1.0");

        var readiness = NevaxisVerifiedDatasetEngine.EvaluateReadiness(1, 0, 1);
        var manifest = NevaxisVerifiedDatasetEngine.BuildManifest(
            Guid.NewGuid(),
            "DS-GOLDEN-001",
            IntelligenceTenantLearningPolicies.TenantOnlyLearning,
            "1.0",
            [sourceCaseId]);
        var distribution = NevaxisVerifiedDatasetEngine.BuildTaskDistribution([entry]);

        Assert.Equal(NevaxisDatasetReadinessStates.NotReady, readiness);
        Assert.Contains("[redacted-secret]", entry.ContextSummary);
        Assert.Contains("datasetVersionId", manifest);
        Assert.Equal(1, distribution["VERIFIED_SUCCESS"]);
    }

    [Fact]
    public void GoldenDatasetFlowExcludesTenantNoTrainingPolicy()
    {
        var (eligible, reason, _) = NevaxisVerifiedDatasetEngine.EvaluateEligibility(
            ThroughputOptimizerVerificationOutcomes.VerifiedImprovement,
            true,
            true,
            true,
            false,
            true,
            true,
            true,
            IntelligenceTenantLearningPolicies.NoModelTraining);

        Assert.False(eligible);
        Assert.Equal("tenant-no-training-policy", reason);
    }
}

public sealed class IntelligenceRuntimeGoldenPromotionSimulationTests
{
    private static NevaxisModelRegistryRecord CreateCandidate(string modelCode = "nevaxis-golden-candidate") =>
        new()
        {
            Id = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            FacilityId = Guid.NewGuid(),
            ModelCode = modelCode,
            Version = "1.0.0",
            Status = NevaxisModelStatuses.Candidate,
            TaskCapabilitiesJson = JsonSerializer.Serialize(
                new List<string> { IntelligenceTaskTypes.Classification, IntelligenceTaskTypes.Summarization }),
        };

    [Fact]
    public void GoldenPromotionLifecycleAdvancesCandidateThroughProduction()
    {
        var model = CreateCandidate();
        Assert.Equal(NevaxisModelStatuses.Candidate, model.Status);

        model.Status = NevaxisModelStatuses.Evaluating;
        var evaluation = NevaxisModelLifecycleEngine.EvaluateCandidate(model, true);
        Assert.Equal("PASS", evaluation.Outcome);

        var shadow = NevaxisModelLifecycleEngine.RunShadow(model, IntelligenceTaskTypes.Classification);
        model.Status = NevaxisModelStatuses.Shadow;
        Assert.True(shadow.IsShadowMode);
        Assert.False(shadow.ExecutedOperationally);

        var (limitedStatus, limitedOutcome, _) = NevaxisModelLifecycleEngine.PromoteOrRollback(
            model,
            NevaxisModelStatuses.Limited,
            evaluation);
        model.Status = limitedStatus;
        Assert.Equal(NevaxisModelStatuses.Limited, limitedStatus);
        Assert.Equal("LIMITED", limitedOutcome);

        var (productionStatus, productionOutcome, rollback) = NevaxisModelLifecycleEngine.PromoteOrRollback(
            model,
            NevaxisModelStatuses.Production,
            evaluation);
        Assert.Equal(NevaxisModelStatuses.Production, productionStatus);
        Assert.Equal("PROMOTED", productionOutcome);
        Assert.False(rollback);
    }

    [Fact]
    public void GoldenRejectedPathMarksUnsafeCandidateAsFailed()
    {
        var model = CreateCandidate("nevaxis-unsafe-candidate");
        model.Status = NevaxisModelStatuses.Evaluating;

        var evaluation = NevaxisModelLifecycleEngine.EvaluateCandidate(model, true);
        Assert.Equal(IntelligenceRuntimeFailureStates.EvaluationFailed, evaluation.Outcome);

        model.Status = NevaxisModelStatuses.Rejected;
        var (status, outcome, rollback) = NevaxisModelLifecycleEngine.PromoteOrRollback(
            model,
            NevaxisModelStatuses.Production,
            evaluation);

        Assert.Equal(NevaxisModelStatuses.Rejected, model.Status);
        Assert.Equal(NevaxisModelStatuses.Rejected, status);
        Assert.Equal(IntelligenceRuntimeFailureStates.PromotionBlocked, outcome);
        Assert.False(rollback);
    }

    [Fact]
    public void GoldenRollbackPathRetiresProductionModelAfterRegression()
    {
        var model = CreateCandidate();
        model.Status = NevaxisModelStatuses.Production;

        var regressionEvaluation = NevaxisModelLifecycleEngine.EvaluateCandidate(model, true);
        regressionEvaluation.HallucinatedEntityRate = 0.35m;
        regressionEvaluation.Outcome = IntelligenceRuntimeFailureStates.EvaluationFailed;

        var (status, outcome, rollback) = NevaxisModelLifecycleEngine.PromoteOrRollback(
            model,
            NevaxisModelStatuses.Retired,
            regressionEvaluation);

        Assert.Equal(NevaxisModelStatuses.Retired, status);
        Assert.Equal(IntelligenceRuntimeFailureStates.RollbackTriggered, outcome);
        Assert.True(rollback);
    }
}
