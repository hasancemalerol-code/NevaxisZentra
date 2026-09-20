using Microsoft.EntityFrameworkCore;
using NevaxisZentra.Persistence.ContinuousOptimization;
using NevaxisZentra.Persistence.ThroughputIntelligence;

namespace NevaxisZentra.Persistence.Tests;

public sealed class ContinuousOptimizationPermissionTests
{
    [Fact]
    public void ContinuousOptimizationPermissionKeysAreStable()
    {
        Assert.Equal("continuous-optimization.read", ContinuousOptimizationPermissionKeys.Read);
        Assert.Equal("continuous-optimization.manage", ContinuousOptimizationPermissionKeys.Manage);
        Assert.Equal("continuous-optimization.run", ContinuousOptimizationPermissionKeys.Run);
    }
}

public sealed class ContinuousOptimizationEntityMetadataTests
{
    [Fact]
    public void ContinuousOptimizationEntitiesRequireTenantAndFacility()
    {
        using var context = CreateContext();
        Assert.False(context.Model.FindEntityType(typeof(ContinuousOptimizationRunRecord))!.FindProperty(nameof(ContinuousOptimizationRunRecord.TenantId))!.IsNullable);
        Assert.False(context.Model.FindEntityType(typeof(ContinuousOptimizationOpportunityRecord))!.FindProperty(nameof(ContinuousOptimizationOpportunityRecord.FacilityId))!.IsNullable);
        Assert.False(context.Model.FindEntityType(typeof(ContinuousOptimizationPolicyRecord))!.FindProperty(nameof(ContinuousOptimizationPolicyRecord.TenantId))!.IsNullable);
    }

    [Fact]
    public void ContinuousOptimizationEntitiesHaveTenantQueryFilters()
    {
        using var context = CreateContext();
        Assert.NotEmpty(context.Model.FindEntityType(typeof(ContinuousOptimizationRunRecord))!.GetDeclaredQueryFilters());
        Assert.NotEmpty(context.Model.FindEntityType(typeof(ContinuousOptimizationOpportunityRecord))!.GetDeclaredQueryFilters());
        Assert.NotEmpty(context.Model.FindEntityType(typeof(ContinuousOptimizationPolicyRecord))!.GetDeclaredQueryFilters());
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

public sealed class ContinuousOptimizationQualificationTests
{
    private static readonly ContinuousOptimizationPolicyThresholds DefaultThresholds = new();

    [Fact]
    public void InsignificantVariationDoesNotQualify()
    {
        var trigger = new ContinuousOptimizationTriggerRequest(
            ContinuousOptimizationTriggerTypes.InsignificantVariation,
            ContinuousOptimizationTypes.Throughput,
            "line",
            Guid.NewGuid().ToString(),
            Guid.NewGuid(),
            0.1m,
            100m,
            100m,
            "minor",
            DateTimeOffset.UtcNow);

        var result = ContinuousOptimizationQualificationEngine.QualifyTrigger(
            trigger, DefaultThresholds, true, false, null, null, 0);

        Assert.False(result.Qualified);
        Assert.Equal(ContinuousOptimizationOutcomes.NoSignificantOpportunity, result.ReasonCode);
    }

    [Fact]
    public void MeaningfulThroughputDeviationQualifies()
    {
        var trigger = new ContinuousOptimizationTriggerRequest(
            ContinuousOptimizationTriggerTypes.ThroughputDeviationChanged,
            ContinuousOptimizationTypes.Throughput,
            "line",
            Guid.NewGuid().ToString(),
            Guid.NewGuid(),
            25m,
            75m,
            100m,
            "gap",
            DateTimeOffset.UtcNow);

        var result = ContinuousOptimizationQualificationEngine.QualifyTrigger(
            trigger, DefaultThresholds, true, false, null, null, 0);

        Assert.True(result.Qualified);
    }

    [Fact]
    public void CooldownBlocksQualification()
    {
        var trigger = new ContinuousOptimizationTriggerRequest(
            ContinuousOptimizationTriggerTypes.ThroughputDeviationChanged,
            ContinuousOptimizationTypes.Throughput,
            "line",
            Guid.NewGuid().ToString(),
            Guid.NewGuid(),
            30m,
            70m,
            100m,
            "gap",
            DateTimeOffset.UtcNow);

        var result = ContinuousOptimizationQualificationEngine.QualifyTrigger(
            trigger,
            DefaultThresholds,
            true,
            false,
            null,
            DateTimeOffset.UtcNow.AddMinutes(-5),
            0);

        Assert.False(result.Qualified);
        Assert.Equal(ContinuousOptimizationFailureCodes.CooldownActive, result.ReasonCode);
    }

    [Fact]
    public void DuplicateActiveRunIsBlocked()
    {
        var activeRun = new ContinuousOptimizationRunRecord
        {
            CurrentPhase = ContinuousOptimizationPhases.Analyzing,
        };

        var trigger = new ContinuousOptimizationTriggerRequest(
            ContinuousOptimizationTriggerTypes.ThroughputDeviationChanged,
            ContinuousOptimizationTypes.Throughput,
            "line",
            Guid.NewGuid().ToString(),
            Guid.NewGuid(),
            30m,
            70m,
            100m,
            "gap",
            DateTimeOffset.UtcNow);

        var result = ContinuousOptimizationQualificationEngine.QualifyTrigger(
            trigger, DefaultThresholds, true, false, activeRun, null, 0);

        Assert.False(result.Qualified);
        Assert.Equal(ContinuousOptimizationFailureCodes.DuplicateOpportunity, result.ReasonCode);
    }

    [Fact]
    public void PolicyPauseBlocksQualification()
    {
        var trigger = new ContinuousOptimizationTriggerRequest(
            ContinuousOptimizationTriggerTypes.ThroughputDeviationChanged,
            ContinuousOptimizationTypes.Throughput,
            "line",
            Guid.NewGuid().ToString(),
            Guid.NewGuid(),
            30m,
            70m,
            100m,
            "gap",
            DateTimeOffset.UtcNow);

        var result = ContinuousOptimizationQualificationEngine.QualifyTrigger(
            trigger, DefaultThresholds, true, true, null, null, 0);

        Assert.False(result.Qualified);
        Assert.Equal(ContinuousOptimizationFailureCodes.PolicyBlocked, result.ReasonCode);
    }

    [Fact]
    public void DailyBudgetBlocksQualification()
    {
        var trigger = new ContinuousOptimizationTriggerRequest(
            ContinuousOptimizationTriggerTypes.ThroughputDeviationChanged,
            ContinuousOptimizationTypes.Throughput,
            "line",
            Guid.NewGuid().ToString(),
            Guid.NewGuid(),
            30m,
            70m,
            100m,
            "gap",
            DateTimeOffset.UtcNow);

        var result = ContinuousOptimizationQualificationEngine.QualifyTrigger(
            trigger, DefaultThresholds, true, false, null, null, DefaultThresholds.MaxRunsPerLinePerDay);

        Assert.False(result.Qualified);
        Assert.Equal(ContinuousOptimizationFailureCodes.BudgetExceeded, result.ReasonCode);
    }
}

public sealed class ContinuousOptimizationOpportunityEngineTests
{
    [Fact]
    public void PrioritizationPrefersHigherBenefit()
    {
        var low = new OptimizationOpportunity(
            ContinuousOptimizationTypes.Throughput, "line", "a", "t", "Low", "low", 80, 70, 10, 0.8m,
            ContinuousOptimizationActionRiskClasses.Low, [], [], [], DateTimeOffset.UtcNow.AddHours(1));
        var high = new OptimizationOpportunity(
            ContinuousOptimizationTypes.Throughput, "line", "b", "t", "High", "high", 80, 60, 30, 0.8m,
            ContinuousOptimizationActionRiskClasses.Low, [], [], [], DateTimeOffset.UtcNow.AddHours(1));

        var ordered = ContinuousOptimizationOpportunityEngine.Prioritize([low, high]);
        Assert.Equal("High", ordered[0].Title);
    }

    [Fact]
    public void MinimumValueThresholdIsEnforced()
    {
        var opportunity = new OptimizationOpportunity(
            ContinuousOptimizationTypes.Throughput, "line", "a", "t", "Small", "small", 100, 95, 5, 0.9m,
            ContinuousOptimizationActionRiskClasses.Low, [], [], [], DateTimeOffset.UtcNow.AddHours(1));

        Assert.False(ContinuousOptimizationOpportunityEngine.MeetsMinimumValueThreshold(
            opportunity, new ContinuousOptimizationPolicyThresholds(MinimumValueThreshold: 15m)));
    }
}

public sealed class ContinuousOptimizationThroughputOpportunityTests
{
    [Fact]
    public void ThroughputOpportunityUsesCurrentConstraint()
    {
        var lineId = Guid.NewGuid();
        var overview = new ThroughputIntelligenceOverview(
            lineId,
            "MIX-LINE-01",
            "Mix Line",
            new ThroughputMeasurementResult(75, 80, 5, 0, 100, 75, 25, 75, 80, 5, 0, 1, "HIGH", "recent"),
            new TaktDemandResult(36, 36, 40, 100, 8, "HIGH", null),
            new ProductionGapResult(100, 75, 25, [], "HIGH", "HIGH"),
            [new ConstraintResult("CURRENT", null, "ST-30", "Assembly", null, null, "CYCLE", [], 25, "HIGH", "HIGH", "constraint")],
            new LossTreeResult([], 25, 25, 0, "HIGH", "HIGH"),
            [],
            [],
            [],
            [],
            [],
            [],
            [],
            [],
            "HIGH",
            12,
            40,
            6,
            "mp2-v1");

        var trigger = new ContinuousOptimizationTriggerRequest(
            ContinuousOptimizationTriggerTypes.ThroughputDeviationChanged,
            ContinuousOptimizationTypes.Throughput,
            "line",
            lineId.ToString(),
            lineId,
            25m,
            75m,
            100m,
            "gap",
            DateTimeOffset.UtcNow);

        var opportunity = ContinuousOptimizationQualificationEngine.BuildThroughputOpportunity(
            trigger, overview, new ContinuousOptimizationPolicyThresholds());

        Assert.Contains("constraint:ST-30", opportunity.Evidence);
        Assert.Equal(25m, opportunity.EstimatedBenefit);
    }
}

public sealed class ContinuousOptimizationCopilotIntegrationTests
{
    [Fact]
    public void OptimizationExplanationIntentIsDeterministic()
    {
        var result = FactoryCopilot.FactoryCopilotIntentClassifier.Classify(
            "Zentra neden bu optimizasyonu önerdi?",
            new FactoryCopilot.FactoryCopilotContextInput(FactoryCopilot.FactoryCopilotEntryPoints.Fabrika, LineCode: "MIX-LINE-01"));

        Assert.Equal(FactoryCopilot.FactoryCopilotIntents.OptimizationExplanation, result.Intent);
        Assert.Contains(FactoryCopilot.FactoryCopilotToolIds.GetOptimizationRun, result.RequiredTools);
        Assert.False(result.RequiresModel);
    }
}
