using Microsoft.EntityFrameworkCore;
using NevaxisZentra.Persistence.ScenarioRecovery;
using NevaxisZentra.Persistence.ThroughputIntelligence;
using NevaxisZentra.Persistence.ThroughputOptimizer;

namespace NevaxisZentra.Persistence.Tests;

public sealed class ScenarioRecoveryPermissionTests
{
    [Fact]
    public void ScenarioRecoveryPermissionKeysAreStable()
    {
        Assert.Equal("scenario-recovery.read", ScenarioRecoveryPermissionKeys.Read);
        Assert.Equal("scenario-recovery.manage", ScenarioRecoveryPermissionKeys.Manage);
        Assert.Equal("scenario-recovery.simulate", ScenarioRecoveryPermissionKeys.Simulate);
        Assert.Equal("scenario-recovery.execute", ScenarioRecoveryPermissionKeys.Execute);
    }
}

public sealed class ScenarioRecoveryEntityMetadataTests
{
    [Fact]
    public void ScenarioRecoveryEntitiesRequireTenantAndFacility()
    {
        using var context = CreateContext();
        Assert.False(context.Model.FindEntityType(typeof(ScenarioRecoveryRunRecord))!.FindProperty(nameof(ScenarioRecoveryRunRecord.TenantId))!.IsNullable);
        Assert.False(context.Model.FindEntityType(typeof(ScenarioRecoveryOptionRecord))!.FindProperty(nameof(ScenarioRecoveryOptionRecord.FacilityId))!.IsNullable);
        Assert.False(context.Model.FindEntityType(typeof(ScenarioRecoveryPredictionAccuracyRecord))!.FindProperty(nameof(ScenarioRecoveryPredictionAccuracyRecord.TenantId))!.IsNullable);
    }

    [Fact]
    public void ScenarioRecoveryEntitiesHaveTenantQueryFilters()
    {
        using var context = CreateContext();
        Assert.NotEmpty(context.Model.FindEntityType(typeof(ScenarioRecoveryRunRecord))!.GetDeclaredQueryFilters());
        Assert.NotEmpty(context.Model.FindEntityType(typeof(ScenarioRecoveryOptionRecord))!.GetDeclaredQueryFilters());
        Assert.NotEmpty(context.Model.FindEntityType(typeof(ScenarioRecoveryPredictionAccuracyRecord))!.GetDeclaredQueryFilters());
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

public sealed class ScenarioRecoveryEngineTests
{
    [Fact]
    public void UnknownActionTypeIsRejected()
    {
        var ex = Assert.Throws<ScenarioRecoveryException>(() =>
            ScenarioRecoveryActionRegistry.ValidateOrReject("FAKE_ACTION", "ST-30", null));
        Assert.Equal(ScenarioRecoveryFailureCodes.UnsupportedAction, ex.Code);
    }

    [Fact]
    public void MultiActionConflictDetectsDuplicateOperatorMove()
    {
        var actions = new[]
        {
            ScenarioRecoveryActionRegistry.ValidateOrReject(
                ScenarioRecoveryActionTypes.MoveOperator,
                "ST-30",
                new Dictionary<string, string> { ["operatorCode"] = "OP-ASM-02", ["targetStationCode"] = "ST-40" }),
            ScenarioRecoveryActionRegistry.ValidateOrReject(
                ScenarioRecoveryActionTypes.MoveOperator,
                "ST-30",
                new Dictionary<string, string> { ["operatorCode"] = "OP-ASM-02", ["targetStationCode"] = "ST-20" }),
        };

        var conflicts = ScenarioRecoveryMultiActionConflictDetector.DetectConflicts(actions);
        Assert.Contains("duplicate-operator-move", conflicts);
    }

    [Fact]
    public void RecommendationUsesExplicitScoreComponentsNotOpaqueAiScore()
    {
        var baseline = BuildSnapshot(612m, 0m, "ST-30", "ST-30");
        var optionA = BuildOption("OPTION_A", baseline, 674m, 62m, 0.2m, false);
        var optionB = BuildOption("OPTION_B", baseline, 650m, 38m, 0.15m, false);
        var objectives = new[]
        {
            new ScenarioRecoveryObjective(ScenarioRecoveryObjectiveTypes.ProtectOtif, "Protect OTIF", 3, true),
            new ScenarioRecoveryObjective(ScenarioRecoveryObjectiveTypes.MaximizeVerifiedThroughput, "Throughput", 2, false),
        };

        var rationale = ScenarioRecoveryRecommendationEngine.Recommend([baseline, optionA, optionB], objectives);
        Assert.Equal("OPTION_A", rationale.RecommendedOptionLabel);
        Assert.All(rationale.ScoreComponents, component => Assert.DoesNotContain("ai", component, StringComparison.OrdinalIgnoreCase));
        Assert.True(rationale.IsPolicyDriven);
    }

    [Fact]
    public void DominanceMarksInferiorOption()
    {
        var baseline = BuildSnapshot(612m, 0m, "ST-30", "ST-30");
        var dominated = BuildOption("OPTION_B", baseline, 640m, 28m, 0.4m, false);
        var dominant = BuildOption("OPTION_A", baseline, 670m, 58m, 0.2m, false);
        var dominatedLabels = ScenarioRecoveryDominanceEngine.MarkDominatedOptions([baseline, dominated, dominant]);
        Assert.Contains("OPTION_B", dominatedLabels);
        Assert.DoesNotContain("OPTION_A", dominatedLabels);
    }

    [Fact]
    public void InsufficientContextMarksInfeasible()
    {
        var baseline = BuildSnapshot(612m, 0m, "ST-30", "ST-30");
        var (state, reasons) = ScenarioRecoveryFeasibilityEngine.Evaluate(
            baseline.Simulation,
            [],
            ThroughputIntelligenceDataQualityStates.Insufficient);
        Assert.Equal(ScenarioRecoveryFeasibilityStates.Infeasible, state);
        Assert.Contains("insufficient-or-conflicted-context", reasons);
    }

    [Fact]
    public void VerifiedImprovementRegressionMapsToScenarioOutcome()
    {
        var mapped = ScenarioRecoveryVerificationMapper.MapOutcome(ThroughputOptimizerVerificationOutcomes.Regression);
        Assert.Equal(ScenarioRecoveryVerificationOutcomes.Regression, mapped);
    }

    [Fact]
    public void GoldenRecoveryOptionGeneratorProducesBaselineAndAbc()
    {
        var options = ScenarioRecoveryOptionGenerator.GenerateGoldenRecoveryOptions(
            ThroughputIntelligenceFixtureCodes.AssemblyStationCode);
        Assert.Equal(4, options.Count);
        Assert.Contains(options, o => o.Label == ScenarioRecoveryOptionLabels.Baseline);
        Assert.Contains(options, o => o.Label == ScenarioRecoveryOptionLabels.OptionA);
        Assert.Contains(options, o => o.Label == ScenarioRecoveryOptionLabels.OptionB);
        Assert.Contains(options, o => o.Label == ScenarioRecoveryOptionLabels.OptionC);
    }

    private static ScenarioRecoveryOptionSnapshot BuildOption(
        string label,
        ScenarioRecoveryOptionSnapshot baseline,
        decimal throughput,
        decimal delta,
        decimal risk,
        bool requiresApproval)
    {
        var simulation = baseline.Simulation with
        {
            PredictedGoodThroughputPerHour = throughput,
            DeltaGoodThroughputPerHour = delta,
            OperationalRiskScore = risk,
        };

        return baseline with
        {
            OptionLabel = label,
            Title = label,
            Simulation = simulation,
            RequiresApproval = requiresApproval,
            FeasibilityState = ScenarioRecoveryFeasibilityStates.Feasible,
        };
    }

    private static ScenarioRecoveryOptionSnapshot BuildSnapshot(
        decimal throughput,
        decimal delta,
        string before,
        string after)
    {
        var simulation = new ScenarioMetricSnapshot(
            throughput,
            throughput,
            delta,
            1m,
            1m,
            10m,
            10m,
            80m,
            new ConstraintResult(
                ThroughputIntelligenceConstraintRoles.Current,
                Guid.NewGuid(),
                before,
                before,
                null,
                null,
                ThroughputIntelligenceConstraintTypes.Capacity,
                [],
                10m,
                "HIGH",
                ThroughputIntelligenceDataQualityStates.Good,
                "baseline constraint"),
            new ConstraintResult(
                ThroughputIntelligenceConstraintRoles.Current,
                Guid.NewGuid(),
                after,
                after,
                null,
                null,
                ThroughputIntelligenceConstraintTypes.Capacity,
                [],
                8m,
                "HIGH",
                ThroughputIntelligenceDataQualityStates.Good,
                "predicted constraint"),
            0.1m,
            0.2m,
            ThroughputIntelligenceScenarioEligibility.Eligible,
            ThroughputIntelligenceDataQualityStates.Good,
            "HIGH",
            [],
            []);

        return new ScenarioRecoveryOptionSnapshot(
            ScenarioRecoveryOptionLabels.Baseline,
            "Baseline",
            ScenarioRecoveryCategories.ProductionRecovery,
            ScenarioRecoveryFeasibilityStates.Feasible,
            [],
            [],
            simulation,
            ScenarioRecoveryComparisonEngine.BuildMetrics(simulation, simulation),
            [],
            false,
            false,
            0m);
    }
}
