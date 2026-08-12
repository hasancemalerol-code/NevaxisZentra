using System.Globalization;
using System.Text.Json;

namespace CrevaFactOS.Persistence.ProcessOee;

public static class ProcessOeeLossCalculator
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public static ProcessOeeLossAnalysisResult Analyze(
        ProcessOeeEvaluationWindow window,
        ProcessOeeEvidenceBundle evidence,
        ProcessOeeCalculationResult calculation)
    {
        var drivers = new List<ProcessOeeLossDriverDraft>();
        var freshness = ResolveFreshness(window, evidence, calculation);

        if (calculation.Conflicts.Count > 0)
        {
            drivers.Add(Draft(
                ProcessOeeLossReasonCodes.SourceEvidenceConflict,
                ProcessOeeLossCategories.SourceConflict,
                ProcessOeeLossSeverities.High,
                90,
                null,
                null,
                null,
                null,
                null,
                ["edge-downtime-episodes", "production-order"],
                evidence.SourceFreshnessUtc,
                freshness,
                "source-evidence-conflict",
                ["Conflicting source values preserved; no automatic reconciliation applied."],
                null));
        }

        if (calculation.MissingFields.Count > 0)
        {
            drivers.Add(Draft(
                ProcessOeeLossReasonCodes.MissingRequiredFields,
                ProcessOeeLossCategories.InsufficientData,
                ProcessOeeLossSeverities.Medium,
                50,
                null,
                null,
                null,
                null,
                null,
                ["process-oee-evidence"],
                evidence.SourceFreshnessUtc,
                freshness,
                "missing-required-evidence",
                calculation.MissingFields.Select(f => $"missing:{f}").ToList(),
                null));
        }

        if (calculation.Status == ProcessOeeAssessmentStatuses.StaleData)
        {
            drivers.Add(Draft(
                ProcessOeeLossReasonCodes.StaleEvidence,
                ProcessOeeLossCategories.InsufficientData,
                ProcessOeeLossSeverities.Medium,
                40,
                null,
                null,
                null,
                null,
                null,
                [evidence.SourceSystem ?? "unknown"],
                evidence.SourceFreshnessUtc,
                ProcessOeeLossFreshnessStates.Stale,
                "stale-evidence",
                ["Source evidence is older than the evaluation window freshness threshold."],
                null));
        }

        AnalyzeAvailability(evidence, calculation, drivers, freshness);
        AnalyzePerformance(evidence, calculation, drivers, freshness);
        AnalyzeQuality(evidence, calculation, drivers, freshness);

        ApplyLeadingLoss(drivers, calculation);

        return new ProcessOeeLossAnalysisResult(
            ProcessOeeLossConstants.RuleSetVersion,
            ProcessOeeLossConstants.LeadingLossMethod,
            drivers);
    }

    private static void AnalyzeAvailability(
        ProcessOeeEvidenceBundle evidence,
        ProcessOeeCalculationResult calculation,
        List<ProcessOeeLossDriverDraft> drivers,
        string freshness)
    {
        if (!evidence.PlannedProductionSeconds.HasValue || evidence.PlannedProductionSeconds.Value <= 0)
        {
            drivers.Add(Draft(
                ProcessOeeLossReasonCodes.PlannedTimeMissing,
                ProcessOeeLossCategories.InsufficientData,
                ProcessOeeLossSeverities.High,
                80,
                null,
                null,
                null,
                null,
                null,
                ["process-oee-window"],
                evidence.SourceFreshnessUtc,
                freshness,
                "planned-production-time-missing",
                ["Planned production time is required for availability loss analysis."],
                null));
            return;
        }

        if (!evidence.DowntimeSeconds.HasValue && !evidence.OperatingSeconds.HasValue)
        {
            drivers.Add(Draft(
                ProcessOeeLossReasonCodes.MissingDowntimeEvidence,
                ProcessOeeLossCategories.InsufficientData,
                ProcessOeeLossSeverities.High,
                75,
                null,
                null,
                null,
                null,
                null,
                ["edge-downtime-episodes"],
                evidence.SourceFreshnessUtc,
                freshness,
                "downtime-evidence-missing",
                ["Missing downtime evidence does not imply zero downtime."],
                null));
            return;
        }

        var planned = evidence.PlannedProductionSeconds.Value;
        var operating = evidence.OperatingSeconds ?? 0;
        var downtime = evidence.DowntimeSeconds ?? Math.Max(0, planned - operating);
        var availabilityLossSeconds = Math.Max(0, planned - operating);

        if (availabilityLossSeconds <= 0)
        {
            return;
        }

        drivers.Add(Draft(
            ProcessOeeLossReasonCodes.DowntimeDuration,
            ProcessOeeLossCategories.AvailabilityLoss,
            ProcessOeeLossSeverities.High,
            100,
            operating,
            planned,
            availabilityLossSeconds,
            availabilityLossSeconds,
            null,
            ["edge-downtime-episodes", "production-order"],
            evidence.SourceFreshnessUtc,
            freshness,
            "downtime-reduces-operating-time",
            [
                "Downtime intervals are clipped to the evaluation window and merged before summing.",
                "Availability loss duration reconciles with planned minus operating seconds.",
            ],
            null));
    }

    private static void AnalyzePerformance(
        ProcessOeeEvidenceBundle evidence,
        ProcessOeeCalculationResult calculation,
        List<ProcessOeeLossDriverDraft> drivers,
        string freshness)
    {
        if (calculation.PerformanceRawPercent is > 100m)
        {
            drivers.Add(Draft(
                ProcessOeeLossReasonCodes.PerformanceAbove100,
                ProcessOeeLossCategories.SourceConflict,
                ProcessOeeLossSeverities.High,
                95,
                calculation.PerformanceRawPercent,
                100m,
                calculation.PerformanceRawPercent - 100m,
                null,
                null,
                [evidence.ObservedCycleCount.HasValue ? "edge-observed-cycles" : "derived-shot-count"],
                evidence.SourceFreshnessUtc,
                freshness,
                "performance-above-100-review",
                ["Raw performance above 100% remains visible; review is required."],
                null));
        }

        var shotCount = evidence.ObservedCycleCount ?? evidence.DerivedShotCount;
        var usingDerived = !evidence.ObservedCycleCount.HasValue && evidence.DerivedShotCount.HasValue;

        if (!shotCount.HasValue || !evidence.IdealCycleTimeSeconds.HasValue || evidence.IdealCycleTimeSeconds <= 0
            || !evidence.OperatingSeconds.HasValue || evidence.OperatingSeconds <= 0)
        {
            if (calculation.MissingFields.Any(f => f is "observedCycleCount" or "idealCycleTimeSeconds" or "operatingSeconds"))
            {
                drivers.Add(Draft(
                    ProcessOeeLossReasonCodes.MissingPerformanceEvidence,
                    ProcessOeeLossCategories.InsufficientData,
                    ProcessOeeLossSeverities.Medium,
                    60,
                    null,
                    null,
                    null,
                    null,
                    null,
                    ["edge-observed-cycles", "mold-compatibility"],
                    evidence.SourceFreshnessUtc,
                    freshness,
                    "performance-evidence-insufficient",
                    ["Performance loss is not calculated without trusted cycle and operating time evidence."],
                    null));
            }

            return;
        }

        var idealRunSeconds = evidence.IdealCycleTimeSeconds.Value * shotCount.Value;
        var performanceLossSeconds = Math.Max(0, evidence.OperatingSeconds.Value - (int)Math.Ceiling(idealRunSeconds));

        if (performanceLossSeconds <= 0 && calculation.PerformancePercent is >= 100m)
        {
            return;
        }

        drivers.Add(Draft(
            usingDerived ? ProcessOeeLossReasonCodes.DerivedShotEvidence : ProcessOeeLossReasonCodes.SlowCycleTime,
            ProcessOeeLossCategories.PerformanceLoss,
            usingDerived ? ProcessOeeLossSeverities.Medium : ProcessOeeLossSeverities.High,
            usingDerived ? 70 : 85,
            calculation.PerformancePercent,
            100m,
            performanceLossSeconds,
            performanceLossSeconds,
            shotCount,
            usingDerived
                ? ["derived-shot-count", "mold-compatibility"]
                : ["edge-observed-cycles", "mold-compatibility"],
            evidence.SourceFreshnessUtc,
            freshness,
            usingDerived ? "performance-loss-from-derived-shots" : "performance-loss-slow-cycle",
            usingDerived
                ? ["Shot count was derived from total pieces and cavity count."]
                : ["Performance loss is based on ideal cycle time multiplied by observed shots."],
            null));
    }

    private static void AnalyzeQuality(
        ProcessOeeEvidenceBundle evidence,
        ProcessOeeCalculationResult calculation,
        List<ProcessOeeLossDriverDraft> drivers,
        string freshness)
    {
        if (!evidence.GoodPieceCount.HasValue || !evidence.TotalPieceCount.HasValue || evidence.TotalPieceCount <= 0)
        {
            drivers.Add(Draft(
                ProcessOeeLossReasonCodes.MissingQualityEvidence,
                ProcessOeeLossCategories.InsufficientData,
                ProcessOeeLossSeverities.Medium,
                55,
                null,
                null,
                null,
                null,
                null,
                ["production-order"],
                evidence.SourceFreshnessUtc,
                freshness,
                "quality-evidence-insufficient",
                ["Missing quality quantity evidence does not imply zero reject/scrap."],
                null));
            return;
        }

        var scrap = evidence.ScrapPieceCount ?? Math.Max(0, evidence.TotalPieceCount.Value - evidence.GoodPieceCount.Value);
        var reconciledTotal = evidence.GoodPieceCount.Value + scrap;

        if (reconciledTotal != evidence.TotalPieceCount.Value)
        {
            drivers.Add(Draft(
                ProcessOeeLossReasonCodes.QuantityReconciliationConflict,
                ProcessOeeLossCategories.SourceConflict,
                ProcessOeeLossSeverities.High,
                88,
                evidence.TotalPieceCount,
                reconciledTotal,
                Math.Abs(evidence.TotalPieceCount.Value - reconciledTotal),
                null,
                Math.Abs(evidence.TotalPieceCount.Value - reconciledTotal),
                ["production-order"],
                evidence.SourceFreshnessUtc,
                freshness,
                "quality-quantity-reconciliation-conflict",
                ["Good plus scrap does not reconcile with reported total pieces."],
                null));
            return;
        }

        if (scrap <= 0)
        {
            return;
        }

        drivers.Add(Draft(
            ProcessOeeLossReasonCodes.ScrapQuantity,
            ProcessOeeLossCategories.QualityLoss,
            ProcessOeeLossSeverities.High,
            82,
            scrap,
            0,
            scrap,
            null,
            scrap,
            ["production-order"],
            evidence.SourceFreshnessUtc,
            freshness,
            "scrap-reduces-quality",
            ["Quality loss quantity reconciles as total minus good pieces when scrap is not explicitly supplied."],
            null));
    }

    private static void ApplyLeadingLoss(
        List<ProcessOeeLossDriverDraft> drivers,
        ProcessOeeCalculationResult calculation)
    {
        var availability = calculation.AvailabilityPercent;
        var performance = calculation.PerformancePercent;
        var quality = calculation.QualityPercent;

        if (!availability.HasValue || !performance.HasValue || !quality.HasValue)
        {
            return;
        }

        var a = availability.Value / 100m;
        var p = performance.Value / 100m;
        var q = quality.Value / 100m;

        var impacts = new Dictionary<string, decimal>
        {
            [ProcessOeeLossCategories.AvailabilityLoss] = (100m - availability.Value) * p * q,
            [ProcessOeeLossCategories.PerformanceLoss] = a * (100m - performance.Value) * q,
            [ProcessOeeLossCategories.QualityLoss] = a * p * (100m - quality.Value),
        };

        var leadingCategory = impacts
            .Where(kvp => kvp.Value > 0m)
            .OrderByDescending(kvp => kvp.Value)
            .ThenBy(kvp => kvp.Key, StringComparer.Ordinal)
            .Select(kvp => kvp.Key)
            .FirstOrDefault();

        if (leadingCategory is null)
        {
            return;
        }

        var leadingImpact = impacts[leadingCategory];
        foreach (var driver in drivers)
        {
            if (driver.Category == leadingCategory && driver.AbsoluteLossValue is > 0)
            {
                driver.IsLeadingLoss = true;
                driver.NormalizedImpactPoints = ProcessOeeCalculator.RoundPercent(leadingImpact);
                break;
            }

            if (driver.Category == leadingCategory)
            {
                driver.IsLeadingLoss = true;
                driver.NormalizedImpactPoints = ProcessOeeCalculator.RoundPercent(leadingImpact);
                break;
            }
        }
    }

    private static string ResolveFreshness(
        ProcessOeeEvaluationWindow window,
        ProcessOeeEvidenceBundle evidence,
        ProcessOeeCalculationResult calculation)
    {
        if (calculation.Status == ProcessOeeAssessmentStatuses.StaleData)
        {
            return ProcessOeeLossFreshnessStates.Stale;
        }

        return evidence.SourceFreshnessUtc.HasValue
            ? ProcessOeeLossFreshnessStates.Fresh
            : ProcessOeeLossFreshnessStates.Unknown;
    }

    private static ProcessOeeLossDriverDraft Draft(
        string reasonCode,
        string category,
        string severity,
        int operationalPriority,
        decimal? observedValue,
        decimal? expectedValue,
        decimal? absoluteLossValue,
        int? affectedDurationSeconds,
        int? affectedQuantity,
        IReadOnlyList<string> sourceReferences,
        DateTimeOffset? evidenceTimestampUtc,
        string freshnessState,
        string explanationCode,
        IReadOnlyList<string> dataLimitations,
        decimal? normalizedImpactPoints) =>
        new(
            reasonCode,
            category,
            severity,
            operationalPriority,
            observedValue,
            expectedValue,
            absoluteLossValue,
            affectedDurationSeconds,
            affectedQuantity,
            JsonSerializer.Serialize(sourceReferences, JsonOptions),
            evidenceTimestampUtc,
            freshnessState,
            explanationCode,
            JsonSerializer.Serialize(dataLimitations, JsonOptions),
            false,
            normalizedImpactPoints);

    public static string SerializeSourceReferences(IReadOnlyList<string> references) =>
        JsonSerializer.Serialize(references, JsonOptions);
}

public sealed class ProcessOeeLossDriverDraft
{
    public ProcessOeeLossDriverDraft(
        string reasonCode,
        string category,
        string severity,
        int operationalPriority,
        decimal? observedValue,
        decimal? expectedValue,
        decimal? absoluteLossValue,
        int? affectedDurationSeconds,
        int? affectedQuantity,
        string sourceReferencesJson,
        DateTimeOffset? evidenceTimestampUtc,
        string freshnessState,
        string explanationCode,
        string dataLimitationsJson,
        bool isLeadingLoss,
        decimal? normalizedImpactPoints)
    {
        ReasonCode = reasonCode;
        Category = category;
        Severity = severity;
        OperationalPriority = operationalPriority;
        ObservedValue = observedValue;
        ExpectedValue = expectedValue;
        AbsoluteLossValue = absoluteLossValue;
        AffectedDurationSeconds = affectedDurationSeconds;
        AffectedQuantity = affectedQuantity;
        SourceReferencesJson = sourceReferencesJson;
        EvidenceTimestampUtc = evidenceTimestampUtc;
        FreshnessState = freshnessState;
        ExplanationCode = explanationCode;
        DataLimitationsJson = dataLimitationsJson;
        IsLeadingLoss = isLeadingLoss;
        NormalizedImpactPoints = normalizedImpactPoints;
    }

    public string ReasonCode { get; }
    public string Category { get; }
    public string Severity { get; }
    public int OperationalPriority { get; }
    public decimal? ObservedValue { get; }
    public decimal? ExpectedValue { get; }
    public decimal? AbsoluteLossValue { get; }
    public int? AffectedDurationSeconds { get; }
    public int? AffectedQuantity { get; }
    public string SourceReferencesJson { get; }
    public DateTimeOffset? EvidenceTimestampUtc { get; }
    public string FreshnessState { get; }
    public string ExplanationCode { get; }
    public string DataLimitationsJson { get; }
    public bool IsLeadingLoss { get; set; }
    public decimal? NormalizedImpactPoints { get; set; }
}

public sealed record ProcessOeeLossAnalysisResult(
    string RuleSetVersion,
    string LeadingLossMethod,
    IReadOnlyList<ProcessOeeLossDriverDraft> Drivers);
