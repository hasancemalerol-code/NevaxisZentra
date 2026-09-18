using NevaxisZentra.Persistence.FactoryIntelligence;
using NevaxisZentra.Persistence.Integration.FactoryEventSpine;

namespace NevaxisZentra.Persistence.Tests;

public sealed class FactoryIntelligenceBaselineEngineTests
{
    [Fact]
    public void StableSignalProducesNoMeaningfulDeviation()
    {
        var result = FactoryIntelligenceBaselineEngine.Compute(
            FactoryIntelligenceSignalTypes.CycleTimeDrift,
            27.8m,
            27.5m,
            [27.6m, 27.7m, 27.8m],
            FactoryIntelligenceDataQualityStates.Good);

        Assert.True(Math.Abs(result.DeviationPercent) < 3m);
        Assert.Equal("stable", result.Trend);
    }

    [Fact]
    public void InsufficientDataDoesNotProduceConfidentBaseline()
    {
        var result = FactoryIntelligenceBaselineEngine.Compute(
            FactoryIntelligenceSignalTypes.CycleTimeDrift,
            30m,
            27.5m,
            [],
            FactoryIntelligenceDataQualityStates.Insufficient);

        Assert.Equal(FactoryIntelligenceDataQualityStates.Insufficient, result.DataQuality);
    }
}

public sealed class FactoryIntelligenceRiskDetectionEngineTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid FacilityId = Guid.NewGuid();

    [Fact]
    public void StableSignalsDoNotGenerateRisk()
    {
        var signals = DevelopmentFactoryIntelligenceSimulator.BuildWindow(0, TenantId, FacilityId, DateTimeOffset.UtcNow);
        var baselines = signals.Select(s => FactoryIntelligenceBaselineEngine.Compute(
            s.SignalType, s.MeasuredValue, 27.5m, [], s.DataQuality)).ToList();

        var detections = FactoryIntelligenceRiskDetectionEngine.Detect(new FactoryRiskDetectionInput(
            TenantId, FacilityId, DevelopmentFactoryIntelligenceSimulator.MachineCode,
            DevelopmentFactoryIntelligenceSimulator.OrderReference,
            DevelopmentFactoryIntelligenceSimulator.MaterialCode,
            signals, baselines, DateTimeOffset.UtcNow));

        Assert.Empty(detections);
    }

    [Fact]
    public void CycleDegradationGeneratesMachineRisk()
    {
        var signals = DevelopmentFactoryIntelligenceSimulator.BuildWindow(4, TenantId, FacilityId, DateTimeOffset.UtcNow);
        var baselines = signals.Select(s => FactoryIntelligenceBaselineEngine.Compute(
            s.SignalType, s.MeasuredValue, 27.5m, [28.1m, 28.5m], s.DataQuality)).ToList();

        var detections = FactoryIntelligenceRiskDetectionEngine.Detect(new FactoryRiskDetectionInput(
            TenantId, FacilityId, DevelopmentFactoryIntelligenceSimulator.MachineCode,
            DevelopmentFactoryIntelligenceSimulator.OrderReference,
            DevelopmentFactoryIntelligenceSimulator.MaterialCode,
            signals, baselines, DateTimeOffset.UtcNow));

        Assert.Contains(detections, d => d.RiskType == FactoryIntelligenceRiskTypes.MachineDegradation);
    }

    [Fact]
    public void MaterialCoverageRiskIsDetected()
    {
        var signal = new FactorySignalWindowInput(
            TenantId, FacilityId, "material", "PP", FactoryIntelligenceSignalTypes.MaterialCoverageHours,
            3.8m, DateTimeOffset.UtcNow);
        var baseline = FactoryIntelligenceBaselineEngine.Compute(
            signal.SignalType, signal.MeasuredValue, null, [], signal.DataQuality);

        var detections = FactoryIntelligenceRiskDetectionEngine.Detect(new FactoryRiskDetectionInput(
            TenantId, FacilityId, "INJ-006", "ORD-8841", "PP",
            [signal], [baseline], DateTimeOffset.UtcNow));

        Assert.Contains(detections, d => d.RiskType == FactoryIntelligenceRiskTypes.MaterialShortage);
    }

    [Fact]
    public void DeliveryProjectionRiskUsesExplainableGap()
    {
        var signals = DevelopmentFactoryIntelligenceSimulator.BuildWindow(5, TenantId, FacilityId, DateTimeOffset.UtcNow);
        var baselines = signals.Select(s => FactoryIntelligenceBaselineEngine.Compute(
            s.SignalType, s.MeasuredValue, 27.5m, [28.1m, 28.5m, 29m], s.DataQuality)).ToList();

        var detections = FactoryIntelligenceRiskDetectionEngine.Detect(new FactoryRiskDetectionInput(
            TenantId, FacilityId, DevelopmentFactoryIntelligenceSimulator.MachineCode,
            DevelopmentFactoryIntelligenceSimulator.OrderReference,
            DevelopmentFactoryIntelligenceSimulator.MaterialCode,
            signals, baselines, DateTimeOffset.UtcNow));

        Assert.Contains(detections, d => d.RiskType == FactoryIntelligenceRiskTypes.CriticalOrderDelivery);
        var delivery = detections.First(d => d.RiskType == FactoryIntelligenceRiskTypes.CriticalOrderDelivery);
        Assert.True(delivery.ProjectedGapQuantity > 0);
        Assert.Contains("PREDICTED", delivery.Explanation[1], StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void CompoundRiskEscalatesCorrelatedSignals()
    {
        var signals = DevelopmentFactoryIntelligenceSimulator.BuildWindow(5, TenantId, FacilityId, DateTimeOffset.UtcNow);
        var baselines = signals.Select(s => FactoryIntelligenceBaselineEngine.Compute(
            s.SignalType, s.MeasuredValue, 27.5m, [28.1m, 28.5m, 29m], s.DataQuality)).ToList();

        var detections = FactoryIntelligenceRiskDetectionEngine.Detect(new FactoryRiskDetectionInput(
            TenantId, FacilityId, DevelopmentFactoryIntelligenceSimulator.MachineCode,
            DevelopmentFactoryIntelligenceSimulator.OrderReference,
            DevelopmentFactoryIntelligenceSimulator.MaterialCode,
            signals, baselines, DateTimeOffset.UtcNow));

        Assert.Contains(detections, d => d.RiskType == FactoryIntelligenceRiskTypes.CompoundDelivery);
        Assert.Contains(detections, d => d.RiskType == FactoryIntelligenceRiskTypes.MachineDegradation);
        Assert.Contains(detections, d => d.RiskType == FactoryIntelligenceRiskTypes.MaterialShortage);
    }

    [Fact]
    public void StaleDataLowersConfidenceBand()
    {
        var signals = DevelopmentFactoryIntelligenceSimulator.BuildStaleWindow(TenantId, FacilityId, DateTimeOffset.UtcNow);
        var baselines = signals.Select(s => FactoryIntelligenceBaselineEngine.Compute(
            s.SignalType, s.MeasuredValue, 27.5m, [], s.DataQuality)).ToList();

        var detections = FactoryIntelligenceRiskDetectionEngine.Detect(new FactoryRiskDetectionInput(
            TenantId, FacilityId, DevelopmentFactoryIntelligenceSimulator.MachineCode,
            DevelopmentFactoryIntelligenceSimulator.OrderReference,
            DevelopmentFactoryIntelligenceSimulator.MaterialCode,
            signals, baselines, DateTimeOffset.UtcNow));

        foreach (var detection in detections)
        {
            Assert.Equal(FactoryIntelligenceConfidenceBands.Low, detection.ConfidenceBand);
        }
    }

    [Fact]
    public void MissingMachineTelemetryDoesNotFabricateDegradationRisk()
    {
        var signals = DevelopmentFactoryIntelligenceSimulator.BuildInsufficientTelemetryWindow(
            TenantId, FacilityId, DateTimeOffset.UtcNow);
        var baselines = signals.Select(s => FactoryIntelligenceBaselineEngine.Compute(
            s.SignalType, s.MeasuredValue, null, [], s.DataQuality)).ToList();

        var detections = FactoryIntelligenceRiskDetectionEngine.Detect(new FactoryRiskDetectionInput(
            TenantId, FacilityId, DevelopmentFactoryIntelligenceSimulator.MachineCode,
            DevelopmentFactoryIntelligenceSimulator.OrderReference,
            DevelopmentFactoryIntelligenceSimulator.MaterialCode,
            signals, baselines, DateTimeOffset.UtcNow));

        Assert.DoesNotContain(detections, d => d.RiskType == FactoryIntelligenceRiskTypes.MachineDegradation);
    }
}

public sealed class FactoryIntelligenceSuppressionEngineTests
{
    [Fact]
    public void DuplicateRiskWithinCooldownIsSuppressed()
    {
        var existing = new FactoryPredictedRisk
        {
            CorrelationKey = "machine-degradation:INJ-006",
            RiskState = FactoryIntelligenceRiskStates.Warning,
            UpdatedAtUtc = DateTimeOffset.UtcNow.AddMinutes(-5),
            IsActive = true,
        };
        var detection = new FactoryRiskDetectionResult(
            FactoryIntelligenceRiskTypes.MachineDegradation,
            FactoryIntelligenceRiskStates.Warning,
            "test",
            FactoryIntelligenceConfidenceBands.Medium,
            FactoryIntelligencePredictionHorizons.ShortTerm,
            "warning",
            "2h",
            null,
            [],
            [],
            [],
            FactoryIntelligenceDataQualityStates.Good,
            "machine-degradation:INJ-006");

        Assert.True(FactoryIntelligenceSuppressionEngine.ShouldSuppressDuplicate(
            existing, detection, DateTimeOffset.UtcNow));
    }

    [Fact]
    public void EscalationUpdatesSameRiskState()
    {
        var next = FactoryIntelligenceSuppressionEngine.ResolveEscalatedState(
            FactoryIntelligenceRiskStates.Watch,
            FactoryIntelligenceRiskStates.Critical);
        Assert.Equal(FactoryIntelligenceRiskStates.Critical, next);
    }

    [Fact]
    public void DeEscalationReducesRiskState()
    {
        var next = FactoryIntelligenceSuppressionEngine.ResolveDeEscalatedState(
            FactoryIntelligenceRiskStates.Critical,
            FactoryIntelligenceRiskStates.Warning);
        Assert.Equal(FactoryIntelligenceRiskStates.Warning, next);
    }

    [Fact]
    public void EarlyCaseOpensAtWarningThreshold()
    {
        Assert.True(FactoryIntelligenceSuppressionEngine.ShouldOpenEarlyCase(
            FactoryIntelligenceRiskStates.Warning));
        Assert.False(FactoryIntelligenceSuppressionEngine.ShouldOpenEarlyCase(
            FactoryIntelligenceRiskStates.Watch));
    }
}

public sealed class FactoryIntelligenceVersioningTests
{
    [Fact]
    public void EngineVersionsAreRecordedInConstants()
    {
        Assert.Equal("factory-risk-v1", FactoryIntelligenceEngineVersions.RiskEngine);
        Assert.Equal("factory-baseline-v1", FactoryIntelligenceEngineVersions.BaselineEngine);
        Assert.Equal("feature-window-v1", FactoryIntelligenceEngineVersions.FeatureWindow);
    }

    [Fact]
    public void DataClassificationsDistinguishObservedFromPredicted()
    {
        Assert.NotEqual(
            FactoryIntelligenceDataClassifications.Observed,
            FactoryIntelligenceDataClassifications.Predicted);
        Assert.NotEqual(
            FactoryIntelligenceDataClassifications.Predicted,
            FactoryIntelligenceDataClassifications.Verified);
    }
}

public sealed class FactoryIntelligenceEvaluationOutcomeTests
{
    [Fact]
    public void AvertedRiskOutcomeIsDistinctFromFalsePositive()
    {
        Assert.NotEqual(
            FactoryIntelligenceEvaluationOutcomes.RiskAvertedAfterIntervention,
            FactoryIntelligenceEvaluationOutcomes.FalsePositive);
    }

    [Fact]
    public void ConfirmedRiskOutcomeIsTruePositive()
    {
        Assert.Equal(
            FactoryIntelligenceEvaluationOutcomes.TruePositive,
            "true-positive");
    }
}

public sealed class FactoryIntelligencePermissionTests
{
    [Fact]
    public void PermissionKeysAreStable()
    {
        Assert.Equal("factory-intelligence.read", FactoryIntelligencePermissionKeys.Read);
        Assert.Equal("factory-intelligence.manage", FactoryIntelligencePermissionKeys.Manage);
        Assert.Equal("factory-intelligence.calibration", FactoryIntelligencePermissionKeys.Calibration);
    }
}

public sealed class FactoryIntelligenceSimulatorRegressionTests
{
    [Fact]
    public void Inj006SimulatorProducesGradualCycleDrift()
    {
        var tenantId = Guid.NewGuid();
        var facilityId = Guid.NewGuid();
        var stable = DevelopmentFactoryIntelligenceSimulator.BuildWindow(0, tenantId, facilityId, DateTimeOffset.UtcNow);
        var degraded = DevelopmentFactoryIntelligenceSimulator.BuildWindow(5, tenantId, facilityId, DateTimeOffset.UtcNow);

        var stableCycle = stable.First(s => s.SignalType == FactoryIntelligenceSignalTypes.CycleTimeDrift).MeasuredValue;
        var degradedCycle = degraded.First(s => s.SignalType == FactoryIntelligenceSignalTypes.CycleTimeDrift).MeasuredValue;
        Assert.True(degradedCycle > stableCycle);
    }

    [Fact]
    public void FalseAlertScenarioNormalizesAfterStepSix()
    {
        var tenantId = Guid.NewGuid();
        var facilityId = Guid.NewGuid();
        var normalized = DevelopmentFactoryIntelligenceSimulator.BuildWindow(
            6, tenantId, facilityId, DateTimeOffset.UtcNow, "false-alert");
        var cycle = normalized.First(s => s.SignalType == FactoryIntelligenceSignalTypes.CycleTimeDrift).MeasuredValue;
        Assert.True(cycle < 29m);
    }
}

public sealed class FactoryIntelligenceEventSpineRegressionTests
{
    [Fact]
    public void EventSpineConstantsRemainAvailableForRegression()
    {
        Assert.Equal("dead-letter", FactoryCanonicalEventStates.DeadLetter);
        Assert.NotNull(DevelopmentFactorySourceSimulator.CreateCycleDegradationEvent(Guid.NewGuid(), "x"));
    }
}
