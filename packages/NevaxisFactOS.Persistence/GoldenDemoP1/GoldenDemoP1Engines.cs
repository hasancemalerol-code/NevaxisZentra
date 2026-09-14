using System.Text.Json;

namespace NevaxisFactOS.Persistence.GoldenDemoP1;

public sealed record CyclePhaseDuration(string PhaseCode, string Label, decimal ReferenceSeconds, decimal CurrentSeconds)
{
    public decimal LossSeconds => CurrentSeconds - ReferenceSeconds;
    public bool IsLoss => LossSeconds > 0;
    public bool IsImprovement => LossSeconds < 0;
}

public sealed record CycleLossDecompositionResult(
    bool HasBaseline,
    string? BaselineFailureReason,
    decimal ReferenceCycleSeconds,
    decimal CurrentCycleSeconds,
    decimal CycleLossSeconds,
    decimal MicroStopEquivalentLossSeconds,
    IReadOnlyList<CyclePhaseDuration> Phases,
    IReadOnlyList<CyclePhaseDuration> Losses,
    IReadOnlyList<CyclePhaseDuration> Improvements);

public sealed record RootContributorResult(
    int Rank,
    string PhaseCode,
    string Label,
    string ContributorKind,
    decimal MeasuredDeviationSeconds,
    decimal ContributionToCycleLossSeconds,
    string Confidence,
    string SuggestedNextStep,
    string EvidenceWindow);

public sealed record ProductionRecoveryResult(
    int CurrentExpectedShiftGoodParts,
    int RecoveredExpectedShiftGoodParts,
    int ProductionRecoveryGoodParts,
    decimal ProductionRecoveryPercent,
    decimal RecoverableCycleSeconds,
    decimal RemainingShiftMinutes,
    string ConfidenceLevel,
    IReadOnlyList<string> Assumptions,
    string SafetyMessage);

public sealed record WhatIfScenarioResult(
    string ScenarioCode,
    string Title,
    int ProjectedEndOfShiftGoodParts,
    int ProductionGapGoodParts,
    decimal ContinuedCycleLossSeconds,
    decimal? InterventionDurationMinutes,
    decimal? RecoveredProductionOpportunity,
    string RiskExposure,
    bool IsRecommended,
    string RecommendationReason);

public sealed record OutcomeVerificationResult(
    string Result,
    string Summary,
    IReadOnlyList<VerificationCriterionResult> Criteria,
    decimal RecoveredGoodPartsPerShift);

public sealed record VerificationCriterionResult(
    string Code,
    string Label,
    bool Passed,
    string? Detail);

public sealed record BeforeAfterComparisonResult(
    decimal BeforeCycleSeconds,
    decimal AfterCycleSeconds,
    decimal BeforeGoodPartsPerHour,
    decimal AfterGoodPartsPerHour,
    int BeforeMicroStopCount,
    int AfterMicroStopCount,
    decimal BeforeRejectRatePercent,
    decimal AfterRejectRatePercent,
    decimal? BeforeKwhPerGoodPart,
    decimal? AfterKwhPerGoodPart,
    int RecoveredGoodPartsPerShift,
    string OutcomeLabel);

public static class GoldenDemoP1PhaseCatalog
{
    public static readonly IReadOnlyList<(string Code, string Label)> Phases =
    [
        (GoldenDemoCyclePhaseCodes.Injection, "Enjeksiyon"),
        (GoldenDemoCyclePhaseCodes.Hold, "Bekletme"),
        (GoldenDemoCyclePhaseCodes.Cooling, "Soğutma"),
        (GoldenDemoCyclePhaseCodes.ScrewRecovery, "Vida Geri Çekme"),
        (GoldenDemoCyclePhaseCodes.MoldMovement, "Kalıp Hareketi"),
        (GoldenDemoCyclePhaseCodes.OtherProcess, "Diğer Proses"),
    ];
}

public static class CycleLossDecompositionEngine
{
    public static CycleLossDecompositionResult Decompose(
        GoldenDemoCycleBaseline? baseline,
        GoldenDemoCycleObservation observation)
    {
        if (baseline is null || baseline.ReferenceSource == GoldenDemoReferenceSources.Insufficient)
        {
            return new CycleLossDecompositionResult(
                false,
                "INSUFFICIENT BASELINE — reliable historical reference not available for this machine/product/mold combination.",
                0,
                observation.TotalCycleSeconds,
                0,
                observation.MicroStopDurationSeconds,
                [],
                [],
                []);
        }

        var referencePhases = ParsePhaseMap(baseline.PhaseDurationsJson);
        var currentPhases = ParsePhaseMap(observation.PhaseDurationsJson);
        var phases = GoldenDemoP1PhaseCatalog.Phases
            .Select(p =>
            {
                referencePhases.TryGetValue(p.Code, out var reference);
                currentPhases.TryGetValue(p.Code, out var current);
                return new CyclePhaseDuration(p.Code, p.Label, reference, current);
            })
            .ToList();

        var losses = phases.Where(p => p.IsLoss).OrderByDescending(p => p.LossSeconds).ToList();
        var improvements = phases.Where(p => p.IsImprovement).OrderByDescending(p => Math.Abs(p.LossSeconds)).ToList();
        var cycleLoss = observation.TotalCycleSeconds - baseline.TotalCycleSeconds;

        return new CycleLossDecompositionResult(
            true,
            null,
            baseline.TotalCycleSeconds,
            observation.TotalCycleSeconds,
            cycleLoss,
            observation.MicroStopDurationSeconds,
            phases,
            losses,
            improvements);
    }

    public static IReadOnlyDictionary<string, decimal> ParsePhaseMap(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new Dictionary<string, decimal>();
        }

        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, decimal>>(json) ?? new Dictionary<string, decimal>();
        }
        catch (JsonException)
        {
            return new Dictionary<string, decimal>();
        }
    }

    public static string SerializePhaseMap(IReadOnlyDictionary<string, decimal> phases) =>
        JsonSerializer.Serialize(phases);
}

public static class RootContributorAnalysisEngine
{
    public static IReadOnlyList<RootContributorResult> Analyze(CycleLossDecompositionResult decomposition)
    {
        if (!decomposition.HasBaseline)
        {
            return [];
        }

        var rank = 1;
        return decomposition.Losses.Select(loss =>
        {
            var confidence = loss.LossSeconds >= 1.5m
                ? GoldenDemoContributorConfidence.High
                : loss.LossSeconds >= 0.5m
                    ? GoldenDemoContributorConfidence.Medium
                    : GoldenDemoContributorConfidence.Low;
            var kind = confidence == GoldenDemoContributorConfidence.High
                ? GoldenDemoContributorKinds.ProbableContributor
                : GoldenDemoContributorKinds.LikelyContributor;

            return new RootContributorResult(
                rank++,
                loss.PhaseCode,
                loss.Label,
                kind,
                loss.LossSeconds,
                loss.LossSeconds,
                confidence,
                SuggestNextStep(loss.PhaseCode),
                "Current shift observation window");
        }).ToList();
    }

    private static string SuggestNextStep(string phaseCode) => phaseCode switch
    {
        GoldenDemoCyclePhaseCodes.Cooling => "Soğutma devresi ve mold sıcaklık stabilitesi kontrol edilmeli.",
        GoldenDemoCyclePhaseCodes.ScrewRecovery => "Vida geri çekme profili ve basınç sensörleri gözden geçirilmeli.",
        GoldenDemoCyclePhaseCodes.MoldMovement => "Kalıp aç/kapa mekanizması ve yağlama kontrol edilmeli.",
        GoldenDemoCyclePhaseCodes.Injection => "Enjeksiyon basıncı ve hız profili doğrulanmalı.",
        GoldenDemoCyclePhaseCodes.Hold => "Bekletme basıncı ve süresi referans bandı ile karşılaştırılmalı.",
        _ => "Proses parametreleri referans bandı ile karşılaştırılmalı.",
    };
}

public static class ProductionRecoveryEngine
{
    public static ProductionRecoveryResult Calculate(
        CycleLossDecompositionResult decomposition,
        GoldenDemoCycleObservation observation,
        GoldenDemoProductionEvent productionEvent,
        DateTimeOffset nowUtc)
    {
        if (!decomposition.HasBaseline)
        {
            return new ProductionRecoveryResult(
                0, 0, 0, 0, 0, 0, "insufficient-data",
                ["Baseline unavailable — recovery opportunity cannot be calculated."],
                SafeLanguage.NoUnsafeSpeedMessage);
        }

        var remainingSeconds = Math.Max(0, (productionEvent.ShiftEndUtc - nowUtc).TotalSeconds);
        var remainingMinutes = (decimal)(remainingSeconds / 60.0);
        var yieldFactor = observation.GoodParts + observation.RejectParts > 0
            ? (decimal)observation.GoodParts / (observation.GoodParts + observation.RejectParts)
            : 1m;
        var availabilityFactor = Math.Clamp(observation.AvailabilityPercent / 100m, 0m, 1m);
        var recoverableCycleLoss = decomposition.Losses.Sum(l => l.LossSeconds);
        var currentCycle = Math.Max(0.1m, observation.TotalCycleSeconds);
        var recoveredCycle = Math.Max(decomposition.ReferenceCycleSeconds, currentCycle - recoverableCycleLoss);

        var currentExpected = EstimateGoodParts(remainingSeconds, currentCycle, yieldFactor, availabilityFactor);
        var recoveredExpected = EstimateGoodParts(remainingSeconds, recoveredCycle, yieldFactor, availabilityFactor);
        var recoveryGoodParts = Math.Max(0, recoveredExpected - currentExpected);
        var recoveryPercent = currentExpected > 0
            ? Math.Round((decimal)recoveryGoodParts / currentExpected * 100m, 1)
            : 0m;

        var confidence = recoverableCycleLoss >= 2m && decomposition.Losses.Count >= 2
            ? "high"
            : recoverableCycleLoss > 0
                ? "medium"
                : "low";

        return new ProductionRecoveryResult(
            currentExpected,
            recoveredExpected,
            recoveryGoodParts,
            recoveryPercent,
            recoverableCycleLoss,
            remainingMinutes,
            confidence,
            [
                "Recovery assumes restoring validated stable operating band, not unsafe acceleration.",
                "Micro-stop equivalent loss is tracked separately from phase loss.",
                $"Yield factor {yieldFactor:P1} and availability {observation.AvailabilityPercent:F1}% applied.",
            ],
            SafeLanguage.RecoveryOpportunityMessage(recoveryGoodParts));
    }

    private static int EstimateGoodParts(double remainingSeconds, decimal cycleSeconds, decimal yieldFactor, decimal availabilityFactor)
    {
        if (cycleSeconds <= 0)
        {
            return 0;
        }

        var cycles = (decimal)(remainingSeconds / (double)cycleSeconds) * availabilityFactor;
        return (int)Math.Floor(cycles * yieldFactor);
    }
}

public static class SafeLanguage
{
    public const string NoUnsafeSpeedMessage =
        "Current losses indicate an opportunity to return the machine to its validated stable operating band.";

    public static string RecoveryOpportunityMessage(int recoveryGoodParts) =>
        recoveryGoodParts > 0
            ? $"If the identified recoverable losses are reduced, the system estimates {recoveryGoodParts} additional good parts per shift within the validated operating range."
            : NoUnsafeSpeedMessage;
}

public static class WhatIfDecisionEngine
{
    public static IReadOnlyList<WhatIfScenarioResult> BuildScenarios(
        ProductionRecoveryResult recovery,
        CycleLossDecompositionResult decomposition,
        GoldenDemoProductionEvent productionEvent,
        GoldenDemoCycleObservation observation)
    {
        if (!decomposition.HasBaseline)
        {
            return [];
        }

        var currentProjected = recovery.CurrentExpectedShiftGoodParts + observation.GoodParts;
        var targetGap = Math.Max(0, productionEvent.ShiftTargetGoodParts - currentProjected);
        var laterProjected = currentProjected;
        var recoveredProjected = recovery.RecoveredExpectedShiftGoodParts + observation.GoodParts;

        var continueScenario = new WhatIfScenarioResult(
            GoldenDemoWhatIfScenarioCodes.ContinueCurrent,
            "Mevcut Operasyonla Devam",
            currentProjected,
            targetGap,
            decomposition.CycleLossSeconds,
            null,
            null,
            "Cycle loss continues; shift target gap persists.",
            false,
            string.Empty);

        var laterScenario = new WhatIfScenarioResult(
            GoldenDemoWhatIfScenarioCodes.LaterIntervention,
            "Vardiya Sonunda Müdahale",
            laterProjected,
            targetGap,
            decomposition.CycleLossSeconds,
            null,
            0,
            "Losses accumulate until end-of-shift intervention.",
            false,
            string.Empty);

        var nowScenario = new WhatIfScenarioResult(
            GoldenDemoWhatIfScenarioCodes.ControlledInterventionNow,
            "Kontrollü Müdahale Şimdi",
            recoveredProjected,
            Math.Max(0, productionEvent.ShiftTargetGoodParts - recoveredProjected),
            Math.Max(0, decomposition.CycleLossSeconds - recovery.RecoverableCycleSeconds),
            25,
            recovery.ProductionRecoveryGoodParts,
            "Controlled technical intervention with validation run.",
            recovery.ProductionRecoveryGoodParts > 0 && recovery.ConfidenceLevel != "low",
            recovery.ProductionRecoveryGoodParts > 0
                ? "Evidence supports recoverable cycle loss within validated operating band."
                : string.Empty);

        if (nowScenario.IsRecommended)
        {
            return [nowScenario, continueScenario, laterScenario];
        }

        return [continueScenario, laterScenario, nowScenario];
    }
}

public static class OutcomeVerificationEngine
{
    public static OutcomeVerificationResult Verify(
        GoldenDemoCycleObservation before,
        GoldenDemoCycleObservation? after,
        ProductionRecoveryResult recovery)
    {
        if (after is null)
        {
            return new OutcomeVerificationResult(
                GoldenDemoVerificationResults.InsufficientData,
                "Post-intervention observation window not available.",
                [],
                0);
        }

        var criteria = new List<VerificationCriterionResult>
        {
            Check("cycle-time", "Cycle time improved", after.TotalCycleSeconds <= before.TotalCycleSeconds,
                $"Before {before.TotalCycleSeconds:F1}s → After {after.TotalCycleSeconds:F1}s"),
            Check("throughput", "Good parts/hour improved", GoodPartsPerHour(after) >= GoodPartsPerHour(before),
                $"Before {GoodPartsPerHour(before):F1} → After {GoodPartsPerHour(after):F1}"),
            Check("micro-stops", "Micro-stops decreased", after.MicroStopCount <= before.MicroStopCount,
                $"Before {before.MicroStopCount} → After {after.MicroStopCount}"),
            Check("reject-rate", "Reject rate stable or improved", RejectRate(after) <= RejectRate(before) + 0.5m,
                $"Before {RejectRate(before):F1}% → After {RejectRate(after):F1}%"),
            Check("availability", "Availability stable or improved", after.AvailabilityPercent >= before.AvailabilityPercent - 1m,
                $"Before {before.AvailabilityPercent:F1}% → After {after.AvailabilityPercent:F1}%"),
        };

        if (before.KwhPerGoodPart.HasValue && after.KwhPerGoodPart.HasValue)
        {
            criteria.Add(Check(
                "energy-intensity",
                "kWh/good part stable or improved",
                after.KwhPerGoodPart <= before.KwhPerGoodPart + 0.02m,
                $"Before {before.KwhPerGoodPart:F3} → After {after.KwhPerGoodPart:F3}"));
        }

        var passed = criteria.Count(c => c.Passed);
        var result = passed == criteria.Count
            ? GoldenDemoVerificationResults.Verified
            : passed >= criteria.Count - 1
                ? GoldenDemoVerificationResults.PartiallyVerified
                : GoldenDemoVerificationResults.NotVerified;

        var recovered = Math.Max(0, GoodPartsPerHour(after) - GoodPartsPerHour(before));
        var recoveredShift = (int)Math.Round(recovered * (decimal)(after.WindowEndUtc - after.WindowStartUtc).TotalHours);

        return new OutcomeVerificationResult(
            result,
            result switch
            {
                GoldenDemoVerificationResults.Verified =>
                    "PRODUCTION RECOVERED — improvement persisted across validation window.",
                GoldenDemoVerificationResults.PartiallyVerified =>
                    "Partial improvement observed; continue monitoring validation window.",
                _ => "Expected production recovery not fully verified.",
            },
            criteria,
            recoveredShift);
    }

    private static VerificationCriterionResult Check(string code, string label, bool passed, string detail) =>
        new(code, label, passed, detail);

    private static decimal GoodPartsPerHour(GoldenDemoCycleObservation observation)
    {
        var hours = (decimal)(observation.WindowEndUtc - observation.WindowStartUtc).TotalHours;
        return hours <= 0 ? 0 : observation.GoodParts / hours;
    }

    private static decimal RejectRate(GoldenDemoCycleObservation observation)
    {
        var total = observation.GoodParts + observation.RejectParts;
        return total <= 0 ? 0 : Math.Round((decimal)observation.RejectParts / total * 100m, 1);
    }
}

public static class BeforeAfterComparisonEngine
{
    public static BeforeAfterComparisonResult Compare(
        GoldenDemoCycleObservation before,
        GoldenDemoCycleObservation after,
        OutcomeVerificationResult verification) =>
        new(
            before.TotalCycleSeconds,
            after.TotalCycleSeconds,
            GoodPartsPerHour(before),
            GoodPartsPerHour(after),
            before.MicroStopCount,
            after.MicroStopCount,
            RejectRate(before),
            RejectRate(after),
            before.KwhPerGoodPart,
            after.KwhPerGoodPart,
            (int)verification.RecoveredGoodPartsPerShift,
            verification.Result == GoldenDemoVerificationResults.Verified ? "PRODUCTION RECOVERED" : verification.Result);

    private static decimal GoodPartsPerHour(GoldenDemoCycleObservation observation)
    {
        var hours = (decimal)(observation.WindowEndUtc - observation.WindowStartUtc).TotalHours;
        return hours <= 0 ? 0 : observation.GoodParts / hours;
    }

    private static decimal RejectRate(GoldenDemoCycleObservation observation)
    {
        var total = observation.GoodParts + observation.RejectParts;
        return total <= 0 ? 0 : Math.Round((decimal)observation.RejectParts / total * 100m, 1);
    }
}

public static class GoldenDemoAgentOrchestrationEngine
{
    public static IReadOnlyList<GoldenDemoAgentFinding> BuildFindings(
        Guid tenantId,
        Guid facilityId,
        Guid eventId,
        CycleLossDecompositionResult decomposition,
        IReadOnlyList<RootContributorResult> contributors,
        ProductionRecoveryResult recovery,
        GoldenDemoCycleObservation observation,
        GoldenDemoCycleBaseline? baseline,
        DateTimeOffset nowUtc)
    {
        var findings = new List<GoldenDemoAgentFinding>();

        void Add(string agentCode, string workerCode, string summary, object detail, string classification)
        {
            findings.Add(new GoldenDemoAgentFinding
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                FacilityId = facilityId,
                ProductionEventId = eventId,
                AgentCode = agentCode,
                WorkerCode = workerCode,
                Summary = summary,
                DetailJson = JsonSerializer.Serialize(detail),
                EvidenceClassification = classification,
                GeneratedAtUtc = nowUtc,
            });
        }

        Add(
            GoldenDemoAgentCodes.ProductionPerformance,
            DigitalWorkforce.DigitalWorkforceWorkerCodes.ProductionRecovery,
            $"Shift gap analysis: +{recovery.ProductionRecoveryGoodParts} recoverable good parts estimated.",
            new { recovery.CurrentExpectedShiftGoodParts, recovery.RecoveredExpectedShiftGoodParts, recovery.ProductionRecoveryGoodParts },
            GoldenDemoImpactClassifications.Calculated);

        Add(
            GoldenDemoAgentCodes.MachinePerformance,
            DigitalWorkforce.DigitalWorkforceWorkerCodes.MaintenanceResolution,
            decomposition.HasBaseline
                ? $"Cycle loss {decomposition.CycleLossSeconds:F1}s vs reference {decomposition.ReferenceCycleSeconds:F1}s."
                : "Insufficient baseline for machine cycle comparison.",
            new { decomposition.ReferenceCycleSeconds, decomposition.CurrentCycleSeconds, decomposition.CycleLossSeconds },
            GoldenDemoImpactClassifications.Calculated);

        var topContributor = contributors.Count > 0 ? contributors[0] : null;
        Add(
            GoldenDemoAgentCodes.ProcessOptimization,
            DigitalWorkforce.DigitalWorkforceWorkerCodes.ProductionRecovery,
            topContributor is not null
                ? $"Top process contributor: {topContributor.Label} ({topContributor.MeasuredDeviationSeconds:F1}s)."
                : "No dominant process phase loss detected.",
            contributors.Take(3),
            GoldenDemoImpactClassifications.Calculated);

        Add(
            GoldenDemoAgentCodes.MoldHealth,
            DigitalWorkforce.DigitalWorkforceWorkerCodes.MoldLifecycle,
            "Mold/tool cycle and maintenance context included in recovery assessment.",
            new { baseline?.ReferenceSourceLabel, observation.MachineState },
            GoldenDemoImpactClassifications.Calculated);

        Add(
            GoldenDemoAgentCodes.Maintenance,
            DigitalWorkforce.DigitalWorkforceWorkerCodes.MaintenanceResolution,
            "Technical recovery actions mapped to evidence-backed inspection steps.",
            new { microStops = observation.MicroStopCount, microStopSeconds = observation.MicroStopDurationSeconds },
            GoldenDemoImpactClassifications.Calculated);

        if (observation.KwhPerGoodPart.HasValue)
        {
            Add(
                GoldenDemoAgentCodes.Energy,
                DigitalWorkforce.DigitalWorkforceWorkerCodes.ProductionRecovery,
                $"Energy intensity {observation.KwhPerGoodPart:F3} kWh/good part observed.",
                new { observation.KwhPerGoodPart, baseline?.ReferenceKwhPerGoodPart },
                GoldenDemoImpactClassifications.Measured);
        }

        Add(
            GoldenDemoAgentCodes.Orchestrator,
            DigitalWorkforce.DigitalWorkforceWorkerCodes.FactoryCoordinator,
            SafeLanguage.RecoveryOpportunityMessage(recovery.ProductionRecoveryGoodParts),
            new { recovery.SafetyMessage, recovery.Assumptions },
            GoldenDemoImpactClassifications.Calculated);

        return findings;
    }
}
