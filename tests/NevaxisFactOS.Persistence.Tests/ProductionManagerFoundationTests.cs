using NevaxisFactOS.Persistence.Authorization;
using NevaxisFactOS.Persistence.ProductionManager;

namespace NevaxisFactOS.Persistence.Tests;

public sealed class ProductionManagerPermissionKeyTests
{
    [Fact]
    public void PermissionKeysAreStableAndBounded()
    {
        Assert.Equal("production-manager.read", ProductionManagerPermissionKeys.Read);
        Assert.Equal("production-manager.simulate", ProductionManagerPermissionKeys.Simulate);
        Assert.Equal("production-manager.decide", ProductionManagerPermissionKeys.Decide);
        Assert.Equal("production-manager.approve", ProductionManagerPermissionKeys.Approve);
        Assert.Equal("production-manager.autonomy.manage", ProductionManagerPermissionKeys.AutonomyManage);
        Assert.Equal("production-manager.policy.manage", ProductionManagerPermissionKeys.PolicyManage);
    }
}

public sealed class FactoryPredictionEngineTests
{
    [Fact]
    public void EvaluateProducesOrderDelayPredictionWhenBehindSchedule()
    {
        var state = SampleState(behindSchedule: true);
        var predictions = FactoryPredictionEngine.Evaluate(state, []);

        Assert.Contains(predictions, p => p.PredictionType == ProductionManagerDecisionTypes.OrderDelay);
        Assert.All(predictions, p => Assert.Equal(ProductionManagerDataClassifications.Predicted, p.DataClassification));
    }

    [Fact]
    public void CooldownPreventsDuplicatePredictions()
    {
        var state = SampleState(behindSchedule: true);
        var existing = FactoryPredictionEngine.Evaluate(state, []);
        var orderDelay = existing.First(p => p.PredictionType == ProductionManagerDecisionTypes.OrderDelay);
        var second = FactoryPredictionEngine.Evaluate(state, [orderDelay]);

        Assert.DoesNotContain(second, p => p.PredictionType == ProductionManagerDecisionTypes.OrderDelay);
    }

    private static FactoryStateSnapshot SampleState(bool behindSchedule = false) => new()
    {
        Id = Guid.NewGuid(),
        TenantId = Guid.NewGuid(),
        FacilityId = Guid.NewGuid(),
        CapturedAtUtc = DateTimeOffset.UtcNow,
        ProductionStateJson = behindSchedule ? """{"behindScheduleCount":2}""" : "{}",
        MachineStateJson = """{"stopped":1}""",
        MaterialStateJson = """{"shortageSignals":1}""",
        QualityStateJson = """{"holdCount":1}""",
        MoldStateJson = """{"moldCases":1}""",
        CapacityStateJson = """{"bottleneck":"capacity"}""",
        FreshnessJson = "{}",
    };
}

public sealed class DecisionOptionGeneratorTests
{
    [Fact]
    public void OrderDelayGeneratesAtLeastThreeOptions()
    {
        var state = new FactoryStateSnapshot { MachineStateJson = """{"stopped":0}""" };
        var policy = new ProductionManagerPolicy { ProhibitedDecisionsJson = "[]" };
        var options = DecisionOptionGenerator.Generate(ProductionManagerDecisionTypes.OrderDelay, state, policy);

        Assert.True(options.Count >= 3);
        Assert.Contains(options, o => o.OptionCode == "B" && !o.IsRejected);
    }

    [Fact]
    public void InvalidMachineOptionRejected()
    {
        var state = new FactoryStateSnapshot { MachineStateJson = """{"stopped":2}""" };
        var policy = new ProductionManagerPolicy { ProhibitedDecisionsJson = "[]" };
        var options = DecisionOptionGenerator.Generate(ProductionManagerDecisionTypes.OrderDelay, state, policy);
        var im09 = options.First(o => o.OptionCode == "B");

        Assert.True(im09.IsRejected);
        Assert.Contains("IM-09", im09.RejectionReason, StringComparison.Ordinal);
    }
}

public sealed class DecisionScoringEngineTests
{
    [Fact]
    public void SafetyRiskOptionIsRejectedByScoringRank()
    {
        var risky = new DecisionOptionDraft("X", "Test", "desc", "{}", ["güvenlik riski"], false, false, null);
        var safe = new DecisionOptionDraft("B", "Safe", "desc", "{}", [], false, false, null);
        var ranked = DecisionScoringEngine.ScoreAndRank([risky, safe], ProductionManagerDecisionTypes.OrderDelay);

        Assert.Equal("B", ranked[0].OptionCode);
    }

    [Fact]
    public void OrderDelayOptionBRanksHighest()
    {
        var state = new FactoryStateSnapshot { MachineStateJson = """{"stopped":0}""" };
        var policy = new ProductionManagerPolicy { ProhibitedDecisionsJson = "[]" };
        var ranked = DecisionScoringEngine.ScoreAndRank(
            DecisionOptionGenerator.Generate(ProductionManagerDecisionTypes.OrderDelay, state, policy),
            ProductionManagerDecisionTypes.OrderDelay);

        Assert.Equal("B", ranked[0].OptionCode);
    }
}

public sealed class WhatIfSimulatorTests
{
    [Fact]
    public void SimulateReturnsComparisonRows()
    {
        var state = new FactoryStateSnapshot { Id = Guid.NewGuid() };
        var result = WhatIfSimulator.Simulate(
            state,
            new SimulationRequest("machine-stop", "IM-07", null, null, null, 3, null));

        Assert.Equal("machine-stop", result.ScenarioType);
        Assert.True(result.ComparisonRows.Count >= 3);
        Assert.Contains(result.ComparisonRows, r => r.OptionCode == "current");
    }
}

public sealed class AutonomyGuardEngineTests
{
    [Fact]
    public void ObserveModeNeverExecutesAutonomously()
    {
        var policy = new ProductionManagerPolicy { AutonomyMode = ProductionManagerAutonomyModes.Observe };
        var decision = SampleDecision(0.9m);
        var option = SampleOption(false);
        var state = FreshState();

        Assert.False(AutonomyGuardEngine.CanExecuteAutonomously(policy, decision, option, state));
        Assert.True(AutonomyGuardEngine.RequiresApproval(policy, decision, option, state));
    }

    [Fact]
    public void LowConfidenceBlocksFullAutonomy()
    {
        var policy = new ProductionManagerPolicy
        {
            AutonomyMode = ProductionManagerAutonomyModes.Full,
            LowConfidenceThreshold = 0.45m,
            StaleDataMaxMinutes = 60,
        };
        var decision = SampleDecision(0.3m);
        var option = SampleOption(true);

        Assert.False(AutonomyGuardEngine.CanExecuteAutonomously(policy, decision, option, FreshState()));
    }

    [Fact]
    public void StaleDataBlocksAutonomy()
    {
        var policy = new ProductionManagerPolicy
        {
            AutonomyMode = ProductionManagerAutonomyModes.Full,
            LowConfidenceThreshold = 0.3m,
            StaleDataMaxMinutes = 5,
        };
        var state = new FactoryStateSnapshot
        {
            CapturedAtUtc = DateTimeOffset.UtcNow.AddHours(-2),
            FreshnessJson = "{}",
        };

        Assert.False(AutonomyGuardEngine.CanExecuteAutonomously(policy, SampleDecision(0.9m), SampleOption(false), state));
    }

    private static FactoryDecision SampleDecision(decimal confidence) => new()
    {
        ConfidenceScore = confidence,
        Status = ProductionManagerDecisionStatuses.Proposed,
    };

    private static FactoryDecisionOption SampleOption(bool requiresApproval) => new()
    {
        RequiresApproval = requiresApproval,
    };

    private static FactoryStateSnapshot FreshState() => new()
    {
        CapturedAtUtc = DateTimeOffset.UtcNow,
        FreshnessJson = "{}",
    };
}

public sealed class ExpectedVsActualEngineTests
{
    [Fact]
    public void CompareMarksSuccessfulWhenActualMeetsExpected()
    {
        var decision = new FactoryDecision
        {
            Id = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            ExpectedOutcomeJson = """{"onTimeProbability":0.8}""",
        };

        var outcome = ExpectedVsActualEngine.Compare(decision, """{"onTimeProbability":1.0}""");

        Assert.Equal("successful", outcome.Result);
        Assert.True(outcome.EffectivenessScore >= 0.7m);
    }
}

public sealed class DecisionMemoryEngineTests
{
    [Fact]
    public void FindSimilarReturnsHighEffectivenessMemories()
    {
        var memories = new List<FactoryDecisionMemory>
        {
            new() { DecisionType = ProductionManagerDecisionTypes.OrderDelay, EffectivenessScore = 0.9m },
            new() { DecisionType = ProductionManagerDecisionTypes.OrderDelay, EffectivenessScore = 0.5m },
        };

        var similar = DecisionMemoryEngine.FindSimilar(memories, ProductionManagerDecisionTypes.OrderDelay, "{}");

        Assert.Single(similar);
        Assert.Equal(0.9m, similar[0].EffectivenessScore);
    }

    [Fact]
    public void RevalidateRejectsWhenMachineMissing()
    {
        var memory = new FactoryDecisionMemory { ConditionsJson = """{"machine":"IM-07","machine:IM-07":true}""" };
        var state = new FactoryStateSnapshot { MachineStateJson = """{"running":4}""" };

        Assert.False(DecisionMemoryEngine.RevalidateConditions(memory, state));
    }
}

public sealed class BottleneckDetectorTests
{
    [Fact]
    public void DetectsCapacityBottleneck()
    {
        var state = new FactoryStateSnapshot
        {
            CapacityStateJson = """{"bottleneck":"capacity","utilization":0.95}""",
            MaterialStateJson = """{"shortageSignals":0}""",
        };

        var bottlenecks = BottleneckDetector.Detect(state);

        Assert.Contains(bottlenecks, b => b.Domain == "capacity");
    }
}
