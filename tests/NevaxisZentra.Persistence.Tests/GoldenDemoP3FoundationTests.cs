using System.Globalization;
using NevaxisZentra.Persistence.GoldenDemoP1;
using NevaxisZentra.Persistence.GoldenDemoP3;

namespace NevaxisZentra.Persistence.Tests;

public sealed class GoldenDemoP3FoundationTests
{
    [Fact]
    public void OutcomeProjectionUsesCalculatedRecoveredProductionAfterVerification()
    {
        var workspace = CreateWorkspace(includePostIntervention: true, verified: true);
        var outcome = GoldenDemoP3OutcomeProjectionEngine.Build(workspace);

        Assert.True(outcome.Hero.RecoveredGoodPartsPerShift > 0);
        Assert.Equal(GoldenDemoImpactClassifications.Calculated, outcome.Hero.RecoveredGoodPartsClassification);
        Assert.Equal(GoldenDemoVerificationResults.Verified, outcome.OutcomeStatus);
        Assert.True(outcome.Hero.ShowSuccessHero);
    }

    [Fact]
    public void OutcomeProjectionReturnsInsufficientDataWithoutValidationWindow()
    {
        var workspace = CreateWorkspace(includePostIntervention: false, verified: false);
        var outcome = GoldenDemoP3OutcomeProjectionEngine.Build(workspace);

        Assert.Equal(GoldenDemoVerificationResults.InsufficientData, outcome.OutcomeStatus);
        Assert.False(outcome.Hero.ShowSuccessHero);
        Assert.Contains(outcome.InsufficientEvidenceReasons, reason => reason.Contains("validation window", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void OutcomeProjectionSurfacesNotVerifiedWhenChecksFail()
    {
        var workspace = CreateWorkspace(includePostIntervention: true, verified: false, degradePostIntervention: true);
        var outcome = GoldenDemoP3OutcomeProjectionEngine.Build(workspace);

        Assert.Equal(GoldenDemoVerificationResults.NotVerified, outcome.OutcomeStatus);
        Assert.Equal("OUTCOME NOT VERIFIED", outcome.Hero.Headline);
        Assert.Contains(outcome.VerificationChecks, check => check.Passed == false);
    }

    [Fact]
    public void ValidationWindowMarksReadyForVerificationWhenPostWindowExists()
    {
        var workspace = CreateWorkspace(includePostIntervention: true, verified: false);
        workspace.Event.Status = GoldenDemoP1EventStatuses.ValidationRun;

        var outcome = GoldenDemoP3OutcomeProjectionEngine.Build(workspace);

        Assert.Equal(GoldenDemoP3ValidationStatuses.ReadyForVerification, outcome.ValidationWindow.Status);
        Assert.True(outcome.ValidationWindow.ReadyForVerification);
    }

    [Fact]
    public void EnergyImpactReportsInsufficientDataWhenTelemetryMissing()
    {
        var workspace = CreateWorkspace(includePostIntervention: true, verified: false, includeEnergy: false);
        var outcome = GoldenDemoP3OutcomeProjectionEngine.Build(workspace);

        Assert.False(outcome.EnergyImpact.HasData);
        Assert.Contains("Insufficient", outcome.EnergyImpact.Summary, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void EnergyImpactUsesMeasuredKwhPerGoodPartWhenAvailable()
    {
        var workspace = CreateWorkspace(includePostIntervention: true, verified: true);
        var outcome = GoldenDemoP3OutcomeProjectionEngine.Build(workspace);

        Assert.True(outcome.EnergyImpact.HasData);
        Assert.Equal(GoldenDemoImpactClassifications.Measured, outcome.EnergyImpact.Classification);
        Assert.NotNull(outcome.EnergyImpact.BeforeKwhPerGoodPart);
        Assert.NotNull(outcome.EnergyImpact.AfterKwhPerGoodPart);
    }

    [Fact]
    public void BusinessImpactNeverMarksFinancialValuesAsVerified()
    {
        var workspace = CreateWorkspace(includePostIntervention: true, verified: true);
        var outcome = GoldenDemoP3OutcomeProjectionEngine.Build(workspace);

        Assert.Equal(GoldenDemoImpactClassifications.Estimated, outcome.BusinessImpact.EstimatedBusinessClassification);
        Assert.Equal(GoldenDemoImpactClassifications.Estimated, outcome.BusinessImpact.ModeledVerifiedBusinessClassification);
        Assert.NotEqual(GoldenDemoImpactClassifications.Verified, outcome.BusinessImpact.EstimatedBusinessClassification);
    }

    [Fact]
    public void InterventionCompletedDoesNotImplyOutcomeVerified()
    {
        var workspace = CreateWorkspace(includePostIntervention: true, verified: false, degradePostIntervention: true);
        workspace.Plan!.Status = GoldenDemoPlanActionStatuses.Completed;
        workspace.Event.Status = GoldenDemoP1EventStatuses.ValidationRun;

        var outcome = GoldenDemoP3OutcomeProjectionEngine.Build(workspace);

        Assert.Equal("completed", outcome.InterventionStatus);
        Assert.NotEqual(GoldenDemoVerificationResults.Verified, outcome.OutcomeStatus);
    }

    [Fact]
    public void MetricComparisonsUseRuntimeBeforeAfterValues()
    {
        var workspace = CreateWorkspace(includePostIntervention: true, verified: true);
        var outcome = GoldenDemoP3OutcomeProjectionEngine.Build(workspace);
        var cycle = outcome.MetricComparisons.Single(m => m.MetricCode == "cycle-time");

        Assert.Equal("31.2", cycle.BeforeValue);
        Assert.True(decimal.Parse(cycle.AfterValue!, CultureInfo.InvariantCulture) <
            decimal.Parse(cycle.BeforeValue!, CultureInfo.InvariantCulture));
    }

    private static GoldenDemoWorkspaceSnapshot CreateWorkspace(
        bool includePostIntervention,
        bool verified,
        bool degradePostIntervention = false,
        bool includeEnergy = true)
    {
        var baseline = CreateBaseline();
        var current = CreateObservation(totalCycle: 31.2m, cooling: 13.5m, screw: 5.3m, mold: 4.7m, kwhPerGoodPart: includeEnergy ? 0.42m : null);
        current.MicroStopCount = 8;
        current.MicroStopDurationSeconds = 120m;
        current.GoodParts = 168;
        current.RejectParts = 8;
        current.AvailabilityPercent = 88m;

        var decomposition = CycleLossDecompositionEngine.Decompose(baseline, current);
        var productionEvent = new GoldenDemoProductionEvent
        {
            Id = Guid.NewGuid(),
            MachineCode = "INJ-001",
            MoldReference = "MOLD-PP-240",
            ShiftTargetGoodParts = 480,
            ShiftStartUtc = DateTimeOffset.UtcNow.AddHours(-4),
            ShiftEndUtc = DateTimeOffset.UtcNow.AddHours(4),
            Status = verified ? GoldenDemoP1EventStatuses.Verified : GoldenDemoP1EventStatuses.ValidationRun,
            UpdatedAtUtc = DateTimeOffset.UtcNow,
        };
        var recovery = ProductionRecoveryEngine.Calculate(decomposition, current, productionEvent, DateTimeOffset.UtcNow);

        GoldenDemoCycleObservation? post = null;
        if (includePostIntervention)
        {
            post = CreateObservation(
                totalCycle: degradePostIntervention ? 31.5m : 29.0m,
                cooling: degradePostIntervention ? 13.5m : 12.5m,
                screw: 4.8m,
                mold: 4.2m,
                kwhPerGoodPart: includeEnergy ? 0.38m : null);
            post.ObservationKind = "post-intervention";
            post.MicroStopCount = degradePostIntervention ? 8 : 4;
            post.GoodParts = degradePostIntervention ? 150 : 220;
            post.RejectParts = degradePostIntervention ? 10 : 6;
            post.AvailabilityPercent = degradePostIntervention ? 87m : 92m;
            post.WindowStartUtc = DateTimeOffset.UtcNow.AddMinutes(-30);
            post.WindowEndUtc = DateTimeOffset.UtcNow;
        }

        var verificationResult = OutcomeVerificationEngine.Verify(current, post, recovery);
        GoldenDemoVerificationRun? verification = verified
            ? new GoldenDemoVerificationRun
            {
                Id = Guid.NewGuid(),
                Result = verificationResult.Result,
                Summary = verificationResult.Summary,
                RecoveredGoodPartsPerShift = verificationResult.RecoveredGoodPartsPerShift,
                VerifiedAtUtc = DateTimeOffset.UtcNow,
                CriteriaJson = System.Text.Json.JsonSerializer.Serialize(verificationResult.Criteria),
            }
            : null;

        BeforeAfterComparisonResult? beforeAfter = post is null
            ? null
            : BeforeAfterComparisonEngine.Compare(current, post, verificationResult);

        var plan = new GoldenDemoRecoveryPlan
        {
            Id = Guid.NewGuid(),
            RequiresApproval = true,
            ApprovedAtUtc = DateTimeOffset.UtcNow.AddHours(-2),
            Status = includePostIntervention ? GoldenDemoPlanActionStatuses.Completed : GoldenDemoPlanActionStatuses.Planned,
        };

        var metrics = new MachineIntelligenceMetrics(
            480, 168, 300, 35m, 8, 4.5m, 112m, "running-degraded", 31.2m, 28.0m, 88m, 8, 120m,
            "MOLD-PP-240", includeEnergy ? 0.42m : null, 0.38m, includeEnergy ? 10.5m : null,
            new Dictionary<string, decimal?>(), new Dictionary<string, string>());

        var business = new BusinessImpactSummary(
            recovery.ProductionRecoveryGoodParts, 8.8m, includeEnergy ? -10.5m : null, 2.2m,
            verified ? verificationResult.RecoveredGoodPartsPerShift * 125m : null,
            recovery.ProductionRecoveryGoodParts * 125m);

        return new GoldenDemoWorkspaceSnapshot(
            productionEvent, baseline, current, post, decomposition, [],
            recovery, [], plan, [], [], verification, beforeAfter, metrics, business);
    }

    private static GoldenDemoCycleBaseline CreateBaseline() =>
        new()
        {
            ReferenceSource = GoldenDemoReferenceSources.StableHistoricalWindow,
            ReferenceSourceLabel = "Stable historical window",
            TotalCycleSeconds = 28.0m,
            PhaseDurationsJson = CycleLossDecompositionEngine.SerializePhaseMap(new Dictionary<string, decimal>
            {
                [GoldenDemoCyclePhaseCodes.Cooling] = 12.0m,
                [GoldenDemoCyclePhaseCodes.ScrewRecovery] = 4.5m,
                [GoldenDemoCyclePhaseCodes.MoldMovement] = 4.0m,
                [GoldenDemoCyclePhaseCodes.Injection] = 2.5m,
                [GoldenDemoCyclePhaseCodes.Hold] = 3.0m,
                [GoldenDemoCyclePhaseCodes.OtherProcess] = 2.0m,
            }),
            ReferenceKwhPerGoodPart = 0.38m,
        };

    private static GoldenDemoCycleObservation CreateObservation(
        decimal totalCycle,
        decimal cooling,
        decimal screw,
        decimal mold,
        decimal injection = 2.5m,
        decimal? kwhPerGoodPart = 0.42m)
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
            KwhPerGoodPart = kwhPerGoodPart,
            WindowStartUtc = DateTimeOffset.UtcNow.AddMinutes(-90),
            WindowEndUtc = DateTimeOffset.UtcNow,
        };
    }
}
