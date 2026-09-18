using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using NevaxisZentra.Persistence.Edge;

namespace NevaxisZentra.Persistence.ProcessOee;

public static class ProcessOeeStabilityCalculator
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public static ProcessOeeStabilityAnalysisResult Analyze(
        ProcessOeeEvaluationWindow window,
        ProcessOeeEvidenceBundle evidence,
        ProcessOeeCalculationResult calculation,
        ProcessOeeStabilityDataBundle stabilityData)
    {
        var drivers = new List<ProcessOeeStabilityDriverDraft>();
        var breakdownRows = new List<ProcessOeeDowntimeBreakdownDraft>();
        var freshness = ResolveFreshness(window, evidence, calculation, stabilityData);

        if (calculation.Conflicts.Count > 0 || stabilityData.Conflicts.Count > 0)
        {
            drivers.Add(DraftDriver(
                ProcessOeeStabilityReasonCodes.SourceConflict,
                ProcessOeeStabilityCategories.SourceConflict,
                ProcessOeeStabilityStates.ReviewRequired,
                ProcessOeeStabilitySeverities.High,
                90,
                null,
                null,
                "count",
                0,
                window.WindowStartUtc,
                window.WindowEndUtc,
                ["edge-downtime-episodes", "production-order"],
                freshness,
                "source-evidence-conflict",
                calculation.Conflicts.Concat(stabilityData.Conflicts).Distinct().ToList()));
        }

        var cycleAnalysis = AnalyzeCycles(window, stabilityData, evidence, calculation, freshness, drivers);
        var microStopAnalysis = AnalyzeMicroStops(window, stabilityData, freshness, drivers);
        var paretoAnalysis = AnalyzeDowntimePareto(window, stabilityData, breakdownRows);

        var resultState = ResolveOverallState(cycleAnalysis, drivers, calculation, freshness);

        var fingerprint = ComputeFingerprint(
            window,
            cycleAnalysis,
            microStopAnalysis,
            paretoAnalysis,
            drivers);

        return new ProcessOeeStabilityAnalysisResult(
            ProcessOeeStabilityConstants.RuleSetVersion,
            resultState,
            cycleAnalysis.MedianCycleMs,
            cycleAnalysis.MadCycleMs,
            cycleAnalysis.SampleCount,
            cycleAnalysis.DriftDirection,
            microStopAnalysis.Count,
            microStopAnalysis.TotalDurationSeconds,
            microStopAnalysis.MedianDurationSeconds,
            paretoAnalysis.ClassifiedDurationSeconds,
            paretoAnalysis.UnclassifiedDurationSeconds,
            fingerprint,
            drivers,
            breakdownRows);
    }

    public static ProcessOeeShiftComparisonResult CompareShifts(
        ProcessOeeOperationContext context,
        DateTimeOffset periodStartUtc,
        DateTimeOffset periodEndUtc,
        IReadOnlyList<ProcessOeeShiftComparisonInput> inputs)
    {
        if (inputs.Count == 0)
        {
            return new ProcessOeeShiftComparisonResult(
                ProcessOeeStabilityConstants.RuleSetVersion,
                periodStartUtc,
                periodEndUtc,
                false,
                "no-shift-windows",
                [],
                null);
        }

        var facilityIds = inputs.Select(i => i.FacilityId).Distinct().ToList();
        var comparable = facilityIds.Count == 1 && inputs.All(i => i.IsDataSufficient);

        var rows = inputs
            .Select(i => new ProcessOeeShiftComparisonRowDraft(
                i.AssessmentId,
                i.ShiftCode,
                i.ShiftLabel,
                i.WindowStartUtc,
                i.WindowEndUtc,
                i.PlannedProductionSeconds,
                i.OperatingSeconds,
                i.AvailabilityPercent,
                i.PerformancePercent,
                i.QualityPercent,
                i.OeePercent,
                i.StabilityResultState,
                i.DowntimeSeconds,
                i.MicroStopCount,
                i.DataSufficiency,
                i.FreshnessState,
                comparable && i.IsDataSufficient && !i.IsStale))
            .ToList();

        ProcessOeeShiftComparisonSummaryDraft? summary = null;
        if (comparable && rows.Count(r => r.IsComparable) >= 2)
        {
            var supported = rows.Where(r => r.IsComparable).ToList();
            summary = new ProcessOeeShiftComparisonSummaryDraft(
                supported.MaxBy(r => r.OeePercent ?? decimal.MinValue)?.ShiftLabel,
                supported.MinBy(r => r.AvailabilityPercent ?? decimal.MaxValue)?.ShiftLabel,
                supported.MaxBy(r => r.DowntimeSeconds ?? 0)?.ShiftLabel,
                supported
                    .Where(r => r.StabilityResultState is ProcessOeeStabilityStates.Variable or ProcessOeeStabilityStates.Drifting)
                    .OrderByDescending(r => r.StabilityResultState == ProcessOeeStabilityStates.Drifting ? 1 : 0)
                    .FirstOrDefault()?.ShiftLabel);
        }

        return new ProcessOeeShiftComparisonResult(
            ProcessOeeStabilityConstants.RuleSetVersion,
            periodStartUtc,
            periodEndUtc,
            comparable,
            comparable ? null : "facility-or-evidence-incomparable",
            rows,
            summary);
    }

    private static CycleAnalysisResult AnalyzeCycles(
        ProcessOeeEvaluationWindow window,
        ProcessOeeStabilityDataBundle stabilityData,
        ProcessOeeEvidenceBundle evidence,
        ProcessOeeCalculationResult calculation,
        string freshness,
        List<ProcessOeeStabilityDriverDraft> drivers)
    {
        var excludedDuplicates = stabilityData.RawCycles.Count(c => c.IsDuplicate);
        var excludedBadQuality = stabilityData.RawCycles.Count(c =>
            !c.IsDuplicate && !IsGoodQuality(c.Quality));
        var excludedInvalid = stabilityData.RawCycles.Count(c =>
            !c.IsDuplicate && IsGoodQuality(c.Quality) && c.CycleDurationMs <= 0);

        if (excludedDuplicates > 0)
        {
            drivers.Add(DraftDriver(
                ProcessOeeStabilityReasonCodes.ExcludedDuplicates,
                ProcessOeeStabilityCategories.InsufficientData,
                ProcessOeeStabilityStates.ReviewRequired,
                ProcessOeeStabilitySeverities.Informational,
                10,
                excludedDuplicates,
                null,
                "count",
                excludedDuplicates,
                window.WindowStartUtc,
                window.WindowEndUtc,
                ["edge-observed-cycles"],
                freshness,
                "duplicate-cycles-excluded",
                [$"Excluded {excludedDuplicates} duplicate cycle observations."]));
        }

        if (excludedBadQuality > 0)
        {
            drivers.Add(DraftDriver(
                ProcessOeeStabilityReasonCodes.ExcludedBadQuality,
                ProcessOeeStabilityCategories.InsufficientData,
                ProcessOeeStabilityStates.ReviewRequired,
                ProcessOeeStabilitySeverities.Medium,
                20,
                excludedBadQuality,
                null,
                "count",
                excludedBadQuality,
                window.WindowStartUtc,
                window.WindowEndUtc,
                ["edge-observed-cycles"],
                freshness,
                "bad-quality-cycles-excluded",
                [$"Excluded {excludedBadQuality} non-good quality cycle observations."]));
        }

        if (excludedInvalid > 0)
        {
            drivers.Add(DraftDriver(
                ProcessOeeStabilityReasonCodes.ExcludedInvalidDuration,
                ProcessOeeStabilityCategories.InsufficientData,
                ProcessOeeStabilityStates.ReviewRequired,
                ProcessOeeStabilitySeverities.Medium,
                25,
                excludedInvalid,
                null,
                "count",
                excludedInvalid,
                window.WindowStartUtc,
                window.WindowEndUtc,
                ["edge-observed-cycles"],
                freshness,
                "invalid-cycle-duration-excluded",
                [$"Excluded {excludedInvalid} cycles with invalid or negative duration."]));
        }

        var goodCycles = stabilityData.RawCycles
            .Where(c => !c.IsDuplicate && IsGoodQuality(c.Quality) && c.CycleDurationMs > 0)
            .OrderBy(c => c.SourceTimestampUtc)
            .ToList();

        var freshnessUtc = stabilityData.SourceFreshnessUtc ?? evidence.SourceFreshnessUtc;
        if (freshnessUtc.HasValue
            && freshnessUtc.Value < DateTimeOffset.UtcNow.AddHours(-ProcessOeeStabilityConstants.StaleEvidenceThresholdHours))
        {
            drivers.Add(DraftDriver(
                ProcessOeeStabilityReasonCodes.StaleCycles,
                ProcessOeeStabilityCategories.InsufficientData,
                ProcessOeeStabilityStates.InsufficientData,
                ProcessOeeStabilitySeverities.Medium,
                40,
                null,
                null,
                "ms",
                goodCycles.Count,
                window.WindowStartUtc,
                window.WindowEndUtc,
                [evidence.SourceSystem ?? "edge-observed-cycles"],
                freshness,
                "stale-cycle-evidence",
                ["Cycle evidence is older than the stale threshold relative to the evaluation window."]));
        }

        if (goodCycles.Count < ProcessOeeStabilityConstants.MinimumSampleCount)
        {
            drivers.Add(DraftDriver(
                ProcessOeeStabilityReasonCodes.InsufficientSamples,
                ProcessOeeStabilityCategories.InsufficientData,
                ProcessOeeStabilityStates.InsufficientData,
                ProcessOeeStabilitySeverities.High,
                80,
                goodCycles.Count,
                ProcessOeeStabilityConstants.MinimumSampleCount,
                "count",
                goodCycles.Count,
                window.WindowStartUtc,
                window.WindowEndUtc,
                ["edge-observed-cycles"],
                freshness,
                "insufficient-cycle-samples",
                [$"Requires at least {ProcessOeeStabilityConstants.MinimumSampleCount} good-quality cycles; observed {goodCycles.Count}."]));

            return new CycleAnalysisResult(null, null, goodCycles.Count, ProcessOeeStabilityDriftDirections.None);
        }

        var durations = goodCycles.Select(c => c.CycleDurationMs).ToList();
        var median = Median(durations);
        var mad = MedianAbsoluteDeviation(durations, median);
        var madRatio = median > 0 ? (decimal)mad / median : 0m;

        var driftDirection = AnalyzeDrift(goodCycles, median, window, freshness, drivers, stabilityData.IdealCycleTimeSeconds);

        if (madRatio < ProcessOeeStabilityConstants.StableMadRatioThreshold
            && driftDirection == ProcessOeeStabilityDriftDirections.None)
        {
            drivers.Add(DraftDriver(
                ProcessOeeStabilityReasonCodes.StableCycles,
                ProcessOeeStabilityCategories.CycleVariability,
                ProcessOeeStabilityStates.Stable,
                ProcessOeeStabilitySeverities.Informational,
                10,
                madRatio,
                ProcessOeeStabilityConstants.StableMadRatioThreshold,
                "ratio",
                goodCycles.Count,
                goodCycles[0].SourceTimestampUtc,
                goodCycles[^1].SourceTimestampUtc,
                ["edge-observed-cycles"],
                freshness,
                "robust-dispersion-stable",
                [$"Median cycle {median} ms; MAD/median ratio {madRatio:0.####} below stable threshold."]));
        }
        else if (madRatio >= ProcessOeeStabilityConstants.VariableMadRatioThreshold)
        {
            drivers.Add(DraftDriver(
                ProcessOeeStabilityReasonCodes.VariableCycles,
                ProcessOeeStabilityCategories.CycleVariability,
                ProcessOeeStabilityStates.Variable,
                ProcessOeeStabilitySeverities.Medium,
                60,
                madRatio,
                ProcessOeeStabilityConstants.VariableMadRatioThreshold,
                "ratio",
                goodCycles.Count,
                goodCycles[0].SourceTimestampUtc,
                goodCycles[^1].SourceTimestampUtc,
                ["edge-observed-cycles"],
                freshness,
                "robust-dispersion-variable",
                [$"Median cycle {median} ms; MAD/median ratio {madRatio:0.####} indicates high variability."]));
        }

        if (calculation.DerivedShotCount.HasValue && calculation.DerivedShotCount > 0
            && (!calculation.ObservedCycleCount.HasValue || calculation.ObservedCycleCount == 0))
        {
            drivers.Add(DraftDriver(
                ProcessOeeStabilityReasonCodes.DerivedCycleEvidence,
                ProcessOeeStabilityCategories.InsufficientData,
                ProcessOeeStabilityStates.ReviewRequired,
                ProcessOeeStabilitySeverities.Low,
                15,
                calculation.DerivedShotCount,
                null,
                "count",
                goodCycles.Count,
                window.WindowStartUtc,
                window.WindowEndUtc,
                ["derived-shot-evidence"],
                freshness,
                "derived-cycle-evidence-used",
                ["Some cycle evidence is derived rather than directly observed."]));
        }

        return new CycleAnalysisResult(median, mad, goodCycles.Count, driftDirection);
    }

    private static string AnalyzeDrift(
        List<ProcessOeeCycleObservation> goodCycles,
        long overallMedian,
        ProcessOeeEvaluationWindow window,
        string freshness,
        List<ProcessOeeStabilityDriverDraft> drivers,
        decimal? idealCycleSeconds)
    {
        var segmentSize = Math.Max(2, (int)Math.Ceiling(goodCycles.Count * ProcessOeeStabilityConstants.DriftWindowFraction));
        var early = goodCycles.Take(segmentSize).Select(c => c.CycleDurationMs).ToList();
        var late = goodCycles.Skip(goodCycles.Count - segmentSize).Select(c => c.CycleDurationMs).ToList();
        var earlyMedian = Median(early);
        var lateMedian = Median(late);

        if (earlyMedian <= 0)
        {
            return ProcessOeeStabilityDriftDirections.None;
        }

        var relativeChange = (decimal)(lateMedian - earlyMedian) / earlyMedian;
        var referenceValue = idealCycleSeconds.HasValue
            ? (decimal?)(idealCycleSeconds.Value * 1000m)
            : overallMedian;

        if (relativeChange >= ProcessOeeStabilityConstants.DriftRatioThreshold)
        {
            drivers.Add(DraftDriver(
                ProcessOeeStabilityReasonCodes.DriftIncreasing,
                ProcessOeeStabilityCategories.CycleDrift,
                ProcessOeeStabilityStates.Drifting,
                ProcessOeeStabilitySeverities.Medium,
                70,
                relativeChange,
                ProcessOeeStabilityConstants.DriftRatioThreshold,
                "ratio",
                goodCycles.Count,
                goodCycles[0].SourceTimestampUtc,
                goodCycles[^1].SourceTimestampUtc,
                ["edge-observed-cycles"],
                freshness,
                "early-late-median-increase",
                [$"Late-window median {lateMedian} ms vs early-window {earlyMedian} ms ({relativeChange:0.####} increase)."],
                referenceValue,
                "ms"));
            return ProcessOeeStabilityDriftDirections.Increasing;
        }

        if (relativeChange <= -ProcessOeeStabilityConstants.DriftRatioThreshold)
        {
            drivers.Add(DraftDriver(
                ProcessOeeStabilityReasonCodes.DriftDecreasing,
                ProcessOeeStabilityCategories.CycleDrift,
                ProcessOeeStabilityStates.Drifting,
                ProcessOeeStabilitySeverities.Medium,
                70,
                relativeChange,
                -ProcessOeeStabilityConstants.DriftRatioThreshold,
                "ratio",
                goodCycles.Count,
                goodCycles[0].SourceTimestampUtc,
                goodCycles[^1].SourceTimestampUtc,
                ["edge-observed-cycles"],
                freshness,
                "early-late-median-decrease",
                [$"Late-window median {lateMedian} ms vs early-window {earlyMedian} ms ({relativeChange:0.####} decrease)."],
                referenceValue,
                "ms"));
            return ProcessOeeStabilityDriftDirections.Decreasing;
        }

        return ProcessOeeStabilityDriftDirections.None;
    }

    private static MicroStopAnalysisResult AnalyzeMicroStops(
        ProcessOeeEvaluationWindow window,
        ProcessOeeStabilityDataBundle stabilityData,
        string freshness,
        List<ProcessOeeStabilityDriverDraft> drivers)
    {
        var microEpisodes = stabilityData.DowntimeEpisodes
            .Where(e => e.IsMicroStop || e.ClippedDurationMs <= ProcessOeeStabilityConstants.MicroStopBoundaryMs)
            .ToList();

        if (microEpisodes.Count == 0)
        {
            return new MicroStopAnalysisResult(0, 0, 0);
        }

        var merged = MergeMicroStopDurations(microEpisodes);
        var durationsSeconds = merged.Select(ms => (int)Math.Round(ms / 1000.0)).Where(s => s > 0).OrderBy(s => s).ToList();
        var totalSeconds = durationsSeconds.Sum();
        var medianSeconds = durationsSeconds.Count > 0 ? MedianInt(durationsSeconds) : 0;

        drivers.Add(DraftDriver(
            ProcessOeeStabilityReasonCodes.MicroStopSummary,
            ProcessOeeStabilityCategories.MicroStop,
            ProcessOeeStabilityStates.ReviewRequired,
            ProcessOeeStabilitySeverities.Medium,
            50,
            microEpisodes.Count,
            ProcessOeeStabilityConstants.MicroStopBoundaryMs,
            "count",
            microEpisodes.Count,
            window.WindowStartUtc,
            window.WindowEndUtc,
            ["edge-downtime-episodes"],
            freshness,
            "micro-stop-episodes-detected",
            [
                $"Micro-stop boundary {ProcessOeeStabilityConstants.MicroStopBoundaryMs} ms.",
                $"Count {microEpisodes.Count}; total duration {totalSeconds} s; median {medianSeconds} s.",
            ]));

        return new MicroStopAnalysisResult(microEpisodes.Count, totalSeconds, medianSeconds);
    }

    private static ParetoAnalysisResult AnalyzeDowntimePareto(
        ProcessOeeEvaluationWindow window,
        ProcessOeeStabilityDataBundle stabilityData,
        List<ProcessOeeDowntimeBreakdownDraft> breakdownRows)
    {
        var longDowntime = stabilityData.DowntimeEpisodes
            .Where(e => !e.IsMicroStop && e.ClippedDurationMs > ProcessOeeStabilityConstants.MicroStopBoundaryMs)
            .ToList();

        var mergedIntervals = MergeDowntimeByReason(longDowntime);
        var grouped = mergedIntervals
            .GroupBy(i => NormalizeReasonCode(i.ReasonCode))
            .Select(g => new
            {
                ReasonCode = g.Key,
                DurationSeconds = (int)Math.Round(g.Sum(x => x.DurationMs) / 1000.0),
                OccurrenceCount = g.Count(),
                IsClassified = g.Key != ProcessOeeDowntimeReasonCodes.Unknown,
            })
            .OrderByDescending(g => g.DurationSeconds)
            .ThenByDescending(g => g.OccurrenceCount)
            .ThenBy(g => g.ReasonCode, StringComparer.Ordinal)
            .ToList();

        var rank = 1;
        foreach (var group in grouped)
        {
            breakdownRows.Add(new ProcessOeeDowntimeBreakdownDraft(
                group.ReasonCode,
                group.DurationSeconds,
                group.OccurrenceCount,
                rank++,
                group.IsClassified));
        }

        var classified = breakdownRows.Where(r => r.IsClassified).Sum(r => r.DurationSeconds);
        var unclassified = breakdownRows.Where(r => !r.IsClassified).Sum(r => r.DurationSeconds);

        return new ParetoAnalysisResult(classified, unclassified);
    }

    private static string ResolveOverallState(
        CycleAnalysisResult cycleAnalysis,
        IReadOnlyList<ProcessOeeStabilityDriverDraft> drivers,
        ProcessOeeCalculationResult calculation,
        string freshness)
    {
        if (drivers.Any(d => d.ResultState == ProcessOeeStabilityStates.InsufficientData
                             && d.ReasonCode == ProcessOeeStabilityReasonCodes.InsufficientSamples))
        {
            return ProcessOeeStabilityStates.InsufficientData;
        }

        if (freshness == ProcessOeeStabilityFreshnessStates.Stale
            || calculation.Status == ProcessOeeAssessmentStatuses.StaleData)
        {
            return ProcessOeeStabilityStates.InsufficientData;
        }

        if (drivers.Any(d => d.Category == ProcessOeeStabilityCategories.SourceConflict))
        {
            return ProcessOeeStabilityStates.ReviewRequired;
        }

        if (drivers.Any(d => d.ResultState == ProcessOeeStabilityStates.Drifting))
        {
            return ProcessOeeStabilityStates.Drifting;
        }

        if (drivers.Any(d => d.ResultState == ProcessOeeStabilityStates.Variable))
        {
            return ProcessOeeStabilityStates.Variable;
        }

        if (cycleAnalysis.SampleCount >= ProcessOeeStabilityConstants.MinimumSampleCount
            && drivers.Any(d => d.ResultState == ProcessOeeStabilityStates.Stable))
        {
            return ProcessOeeStabilityStates.Stable;
        }

        return ProcessOeeStabilityStates.ReviewRequired;
    }

    private static string ResolveFreshness(
        ProcessOeeEvaluationWindow window,
        ProcessOeeEvidenceBundle evidence,
        ProcessOeeCalculationResult calculation,
        ProcessOeeStabilityDataBundle stabilityData)
    {
        var freshnessUtc = stabilityData.SourceFreshnessUtc ?? evidence.SourceFreshnessUtc;
        if (freshnessUtc.HasValue
            && freshnessUtc.Value < DateTimeOffset.UtcNow.AddHours(-ProcessOeeStabilityConstants.StaleEvidenceThresholdHours))
        {
            return ProcessOeeStabilityFreshnessStates.Stale;
        }

        if (calculation.Status == ProcessOeeAssessmentStatuses.StaleData)
        {
            return ProcessOeeStabilityFreshnessStates.Stale;
        }

        return freshnessUtc.HasValue
            ? ProcessOeeStabilityFreshnessStates.Fresh
            : ProcessOeeStabilityFreshnessStates.Unknown;
    }

    private static bool IsGoodQuality(string quality) =>
        string.Equals(quality, TelemetryQualities.Good, StringComparison.OrdinalIgnoreCase);

    private static long Median(List<long> values)
    {
        if (values.Count == 0)
        {
            return 0;
        }

        var sorted = values.OrderBy(v => v).ToList();
        var mid = sorted.Count / 2;
        return sorted.Count % 2 == 0
            ? (sorted[mid - 1] + sorted[mid]) / 2
            : sorted[mid];
    }

    private static int MedianInt(List<int> values)
    {
        if (values.Count == 0)
        {
            return 0;
        }

        var sorted = values.OrderBy(v => v).ToList();
        var mid = sorted.Count / 2;
        return sorted.Count % 2 == 0
            ? (sorted[mid - 1] + sorted[mid]) / 2
            : sorted[mid];
    }

    private static long MedianAbsoluteDeviation(List<long> values, long median)
    {
        var deviations = values.Select(v => Math.Abs(v - median)).ToList();
        return Median(deviations);
    }

    private static List<long> MergeMicroStopDurations(List<ProcessOeeDowntimeObservation> episodes) =>
        episodes.Select(e => e.ClippedDurationMs).ToList();

    private sealed record MergedDowntimeInterval(string? ReasonCode, long DurationMs);

    private static List<MergedDowntimeInterval> MergeDowntimeByReason(
        List<ProcessOeeDowntimeObservation> episodes)
    {
        return episodes
            .Select(e => new MergedDowntimeInterval(e.ReasonCode, e.ClippedDurationMs))
            .ToList();
    }

    private static string NormalizeReasonCode(string? reasonCode) =>
        string.IsNullOrWhiteSpace(reasonCode)
            ? ProcessOeeDowntimeReasonCodes.Unknown
            : reasonCode.Trim().ToLowerInvariant();

    private static string ComputeFingerprint(
        ProcessOeeEvaluationWindow window,
        CycleAnalysisResult cycle,
        MicroStopAnalysisResult micro,
        ParetoAnalysisResult pareto,
        IReadOnlyList<ProcessOeeStabilityDriverDraft> drivers)
    {
        var payload = JsonSerializer.Serialize(new
        {
            windowId = window.Id,
            windowStart = window.WindowStartUtc,
            windowEnd = window.WindowEndUtc,
            cycle.MedianCycleMs,
            cycle.MadCycleMs,
            cycle.SampleCount,
            cycle.DriftDirection,
            micro.Count,
            micro.TotalDurationSeconds,
            pareto.ClassifiedDurationSeconds,
            pareto.UnclassifiedDurationSeconds,
            driverCodes = drivers.Select(d => d.ReasonCode).OrderBy(c => c).ToList(),
            ruleSet = ProcessOeeStabilityConstants.RuleSetVersion,
        }, JsonOptions);

        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static ProcessOeeStabilityDriverDraft DraftDriver(
        string reasonCode,
        string category,
        string resultState,
        string severity,
        int priority,
        decimal? observedValue,
        decimal? referenceValue,
        string unit,
        int sampleCount,
        DateTimeOffset rangeStart,
        DateTimeOffset rangeEnd,
        IReadOnlyList<string> sourceReferences,
        string freshness,
        string explanationCode,
        IReadOnlyList<string> limitations,
        decimal? referenceValueAlt = null,
        string? referenceUnit = null)
    {
        var refValue = referenceValueAlt ?? referenceValue;
        return new ProcessOeeStabilityDriverDraft(
            reasonCode,
            category,
            resultState,
            severity,
            priority,
            observedValue,
            refValue,
            referenceUnit ?? unit,
            sampleCount,
            rangeStart,
            rangeEnd,
            sourceReferences,
            freshness,
            explanationCode,
            limitations);
    }

    private sealed record CycleAnalysisResult(long? MedianCycleMs, long? MadCycleMs, int SampleCount, string DriftDirection);
    private sealed record MicroStopAnalysisResult(int Count, int TotalDurationSeconds, int MedianDurationSeconds);
    private sealed record ParetoAnalysisResult(int ClassifiedDurationSeconds, int UnclassifiedDurationSeconds);
}

public sealed record ProcessOeeStabilityDriverDraft(
    string ReasonCode,
    string Category,
    string ResultState,
    string Severity,
    int OperationalPriority,
    decimal? ObservedValue,
    decimal? ReferenceValue,
    string Unit,
    int SampleCount,
    DateTimeOffset TimeRangeStartUtc,
    DateTimeOffset TimeRangeEndUtc,
    IReadOnlyList<string> SourceReferences,
    string FreshnessState,
    string ExplanationCode,
    IReadOnlyList<string> DataLimitations);

public sealed record ProcessOeeDowntimeBreakdownDraft(
    string ReasonCode,
    int DurationSeconds,
    int OccurrenceCount,
    int SortRank,
    bool IsClassified);

public sealed record ProcessOeeStabilityAnalysisResult(
    string RuleSetVersion,
    string ResultState,
    long? MedianCycleMs,
    long? MadCycleMs,
    int SampleCount,
    string DriftDirection,
    int MicroStopCount,
    int MicroStopTotalSeconds,
    int MicroStopMedianSeconds,
    int ClassifiedDowntimeSeconds,
    int UnclassifiedDowntimeSeconds,
    string EvidenceFingerprint,
    IReadOnlyList<ProcessOeeStabilityDriverDraft> Drivers,
    IReadOnlyList<ProcessOeeDowntimeBreakdownDraft> BreakdownRows);

public sealed record ProcessOeeShiftComparisonInput(
    Guid FacilityId,
    Guid AssessmentId,
    string ShiftCode,
    string ShiftLabel,
    DateTimeOffset WindowStartUtc,
    DateTimeOffset WindowEndUtc,
    int? PlannedProductionSeconds,
    int? OperatingSeconds,
    decimal? AvailabilityPercent,
    decimal? PerformancePercent,
    decimal? QualityPercent,
    decimal? OeePercent,
    string? StabilityResultState,
    int? DowntimeSeconds,
    int MicroStopCount,
    string DataSufficiency,
    string FreshnessState,
    bool IsDataSufficient,
    bool IsStale);

public sealed record ProcessOeeShiftComparisonRowDraft(
    Guid AssessmentId,
    string ShiftCode,
    string ShiftLabel,
    DateTimeOffset WindowStartUtc,
    DateTimeOffset WindowEndUtc,
    int? PlannedProductionSeconds,
    int? OperatingSeconds,
    decimal? AvailabilityPercent,
    decimal? PerformancePercent,
    decimal? QualityPercent,
    decimal? OeePercent,
    string? StabilityResultState,
    int? DowntimeSeconds,
    int MicroStopCount,
    string DataSufficiency,
    string FreshnessState,
    bool IsComparable);

public sealed record ProcessOeeShiftComparisonSummaryDraft(
    string? HighestOeeShiftLabel,
    string? LowestAvailabilityShiftLabel,
    string? HighestDowntimeShiftLabel,
    string? HighestVariabilityShiftLabel);

public sealed record ProcessOeeShiftComparisonResult(
    string RuleSetVersion,
    DateTimeOffset PeriodStartUtc,
    DateTimeOffset PeriodEndUtc,
    bool IsComparable,
    string? ComparabilityWarningCode,
    IReadOnlyList<ProcessOeeShiftComparisonRowDraft> Rows,
    ProcessOeeShiftComparisonSummaryDraft? Summary);
