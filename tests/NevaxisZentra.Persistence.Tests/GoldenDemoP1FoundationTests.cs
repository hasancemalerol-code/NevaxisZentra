using NevaxisZentra.Persistence.Authorization;
using NevaxisZentra.Persistence.GoldenDemoP1;

namespace NevaxisZentra.Persistence.Tests;

public sealed class GoldenDemoP1FoundationTests
{
    [Fact]
    public void PermissionKeysFollowGoldenDemoConvention()
    {
        Assert.Equal("golden-demo-p1.read", GoldenDemoP1PermissionKeys.Read);
        Assert.Equal("golden-demo-p1.manage", GoldenDemoP1PermissionKeys.Manage);
        Assert.Equal("golden-demo-p1.approve", GoldenDemoP1PermissionKeys.Approve);
        Assert.Equal("golden-demo-p1.execute", GoldenDemoP1PermissionKeys.Execute);
        Assert.Equal("golden-demo-p1.verify", GoldenDemoP1PermissionKeys.Verify);
    }

    [Fact]
    public void CycleLossDecompositionUsesReferenceComparison()
    {
        var baseline = CreateBaseline();
        var observation = CreateObservation(totalCycle: 31.2m, cooling: 13.5m, screw: 5.3m, mold: 4.7m);

        var result = CycleLossDecompositionEngine.Decompose(baseline, observation);

        Assert.True(result.HasBaseline);
        Assert.Equal(28.0m, result.ReferenceCycleSeconds);
        Assert.Equal(31.2m, result.CurrentCycleSeconds);
        Assert.Equal(3.2m, result.CycleLossSeconds);
        Assert.Equal(120m, result.MicroStopEquivalentLossSeconds);
        Assert.Contains(result.Losses, loss => loss.PhaseCode == GoldenDemoCyclePhaseCodes.Cooling);
    }

    [Fact]
    public void CycleLossReturnsInsufficientBaselineWhenMissing()
    {
        var observation = CreateObservation(totalCycle: 31.2m, cooling: 13.5m, screw: 5.3m, mold: 4.7m);
        var result = CycleLossDecompositionEngine.Decompose(null, observation);

        Assert.False(result.HasBaseline);
        Assert.Contains("INSUFFICIENT BASELINE", result.BaselineFailureReason, StringComparison.Ordinal);
    }

    [Fact]
    public void CycleLossRepresentsFavorableVarianceSeparately()
    {
        var baseline = CreateBaseline();
        var phases = new Dictionary<string, decimal>(CycleLossDecompositionEngine.ParsePhaseMap(baseline.PhaseDurationsJson));
        phases[GoldenDemoCyclePhaseCodes.Injection] = 2.0m;
        var observation = CreateObservation(
            totalCycle: phases.Values.Sum(),
            cooling: 12.0m,
            screw: 4.5m,
            mold: 4.0m,
            injection: 2.0m);

        var result = CycleLossDecompositionEngine.Decompose(baseline, observation);

        Assert.Contains(result.Improvements, improvement => improvement.PhaseCode == GoldenDemoCyclePhaseCodes.Injection);
        Assert.DoesNotContain(result.Losses, loss => loss.PhaseCode == GoldenDemoCyclePhaseCodes.Injection);
    }

    [Fact]
    public void ProductionRecoveryCalculatesRemainingShiftGoodParts()
    {
        var baseline = CreateBaseline();
        var observation = CreateObservation(totalCycle: 31.2m, cooling: 13.5m, screw: 5.3m, mold: 4.7m);
        var decomposition = CycleLossDecompositionEngine.Decompose(baseline, observation);
        var productionEvent = new GoldenDemoProductionEvent
        {
            ShiftEndUtc = DateTimeOffset.UtcNow.AddHours(4),
            ShiftStartUtc = DateTimeOffset.UtcNow.AddHours(-4),
            ShiftTargetGoodParts = 480,
        };

        var recovery = ProductionRecoveryEngine.Calculate(decomposition, observation, productionEvent, DateTimeOffset.UtcNow);

        Assert.True(recovery.ProductionRecoveryGoodParts >= 0);
        Assert.True(recovery.RecoveredExpectedShiftGoodParts >= recovery.CurrentExpectedShiftGoodParts);
        Assert.Contains("validated stable operating band", recovery.Assumptions[0], StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ProductionRecoveryReturnsInsufficientDataWithoutBaseline()
    {
        var observation = CreateObservation(totalCycle: 31.2m, cooling: 13.5m, screw: 5.3m, mold: 4.7m);
        var decomposition = CycleLossDecompositionEngine.Decompose(null, observation);
        var productionEvent = new GoldenDemoProductionEvent { ShiftEndUtc = DateTimeOffset.UtcNow.AddHours(2) };

        var recovery = ProductionRecoveryEngine.Calculate(decomposition, observation, productionEvent, DateTimeOffset.UtcNow);

        Assert.Equal(0, recovery.ProductionRecoveryGoodParts);
        Assert.Equal("insufficient-data", recovery.ConfidenceLevel);
    }

    [Fact]
    public void WhatIfDoesNotRecommendWhenEvidenceInsufficient()
    {
        var baseline = CreateBaseline();
        var observation = CreateObservation(totalCycle: 28.0m, cooling: 12.0m, screw: 4.5m, mold: 4.0m);
        var decomposition = CycleLossDecompositionEngine.Decompose(baseline, observation);
        var productionEvent = new GoldenDemoProductionEvent
        {
            ShiftTargetGoodParts = 480,
            ShiftEndUtc = DateTimeOffset.UtcNow.AddHours(4),
        };
        var recovery = ProductionRecoveryEngine.Calculate(decomposition, observation, productionEvent, DateTimeOffset.UtcNow);

        var scenarios = WhatIfDecisionEngine.BuildScenarios(recovery, decomposition, productionEvent, observation);

        Assert.DoesNotContain(scenarios, scenario => scenario.IsRecommended);
    }

    [Fact]
    public void WhatIfRecommendsControlledInterventionWhenRecoverableLossExists()
    {
        var baseline = CreateBaseline();
        var observation = CreateObservation(totalCycle: 31.2m, cooling: 13.5m, screw: 5.3m, mold: 4.7m);
        var decomposition = CycleLossDecompositionEngine.Decompose(baseline, observation);
        var productionEvent = new GoldenDemoProductionEvent
        {
            ShiftTargetGoodParts = 480,
            ShiftEndUtc = DateTimeOffset.UtcNow.AddHours(4),
        };
        var recovery = ProductionRecoveryEngine.Calculate(decomposition, observation, productionEvent, DateTimeOffset.UtcNow);

        var scenarios = WhatIfDecisionEngine.BuildScenarios(recovery, decomposition, productionEvent, observation);
        var recommended = scenarios.SingleOrDefault(scenario => scenario.IsRecommended);

        Assert.NotNull(recommended);
        Assert.Equal(GoldenDemoWhatIfScenarioCodes.ControlledInterventionNow, recommended!.ScenarioCode);
    }

    [Fact]
    public void OutcomeVerificationReturnsVerifiedWhenCriteriaPass()
    {
        var before = CreateObservation(totalCycle: 31.2m, cooling: 13.5m, screw: 5.3m, mold: 4.7m);
        var after = CreateObservation(totalCycle: 29.0m, cooling: 12.5m, screw: 4.8m, mold: 4.2m);
        after.MicroStopCount = 4;
        after.GoodParts = 220;
        after.RejectParts = 6;
        var recovery = new ProductionRecoveryResult(100, 130, 30, 30m, 2.2m, 240m, "high", [], SafeLanguage.NoUnsafeSpeedMessage);

        var verification = OutcomeVerificationEngine.Verify(before, after, recovery);

        Assert.Equal(GoldenDemoVerificationResults.Verified, verification.Result);
        Assert.True(verification.RecoveredGoodPartsPerShift >= 0);
    }

    [Fact]
    public void OutcomeVerificationReturnsInsufficientDataWithoutAfterObservation()
    {
        var before = CreateObservation(totalCycle: 31.2m, cooling: 13.5m, screw: 5.3m, mold: 4.7m);
        var recovery = new ProductionRecoveryResult(100, 130, 30, 30m, 2.2m, 240m, "high", [], SafeLanguage.NoUnsafeSpeedMessage);

        var verification = OutcomeVerificationEngine.Verify(before, null, recovery);

        Assert.Equal(GoldenDemoVerificationResults.InsufficientData, verification.Result);
    }

    private static GoldenDemoCycleBaseline CreateBaseline() =>
        new()
        {
            ReferenceSource = GoldenDemoReferenceSources.StableHistoricalWindow,
            TotalCycleSeconds = 28.0m,
            PhaseDurationsJson = CycleLossDecompositionEngine.SerializePhaseMap(new Dictionary<string, decimal>
            {
                [GoldenDemoCyclePhaseCodes.Injection] = 2.5m,
                [GoldenDemoCyclePhaseCodes.Hold] = 3.0m,
                [GoldenDemoCyclePhaseCodes.Cooling] = 12.0m,
                [GoldenDemoCyclePhaseCodes.ScrewRecovery] = 4.5m,
                [GoldenDemoCyclePhaseCodes.MoldMovement] = 4.0m,
                [GoldenDemoCyclePhaseCodes.OtherProcess] = 2.0m,
            }),
        };

    private static GoldenDemoCycleObservation CreateObservation(
        decimal totalCycle,
        decimal cooling,
        decimal screw,
        decimal mold,
        decimal injection = 2.5m)
    {
        var phases = new Dictionary<string, decimal>
        {
            [GoldenDemoCyclePhaseCodes.Injection] = injection,
            [GoldenDemoCyclePhaseCodes.Hold] = 3.0m,
            [GoldenDemoCyclePhaseCodes.Cooling] = cooling,
            [GoldenDemoCyclePhaseCodes.ScrewRecovery] = screw,
            [GoldenDemoCyclePhaseCodes.MoldMovement] = mold,
            [GoldenDemoCyclePhaseCodes.OtherProcess] = 2.0m,
        };

        return new GoldenDemoCycleObservation
        {
            TotalCycleSeconds = totalCycle,
            PhaseDurationsJson = CycleLossDecompositionEngine.SerializePhaseMap(phases),
            MicroStopCount = 8,
            MicroStopDurationSeconds = 120m,
            GoodParts = 168,
            RejectParts = 8,
            AvailabilityPercent = 88m,
            WindowStartUtc = DateTimeOffset.UtcNow.AddMinutes(-90),
            WindowEndUtc = DateTimeOffset.UtcNow,
        };
    }
}
