using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace NevaxisFactOS.Persistence.Maintenance;

public sealed class ConditionEvaluationService : IConditionEvaluationService
{
    private static readonly TimeSpan StaleThreshold = TimeSpan.FromMinutes(30);

    public ConditionAssetEvaluationResult EvaluateAsset(ConditionAssetEvaluationInput input)
    {
        var metricResults = new List<ConditionMetricEvaluationResult>();
        var allAnomalies = new List<ConditionAnomalyCandidate>();
        var allSignals = new List<ConditionSignalCandidate>();

        foreach (var metric in input.Metrics)
        {
            var metricResult = EvaluateMetric(metric, input.EvaluatedAtUtc, input.SourceIsStale);
            metricResults.Add(metricResult);
            allAnomalies.AddRange(metricResult.Anomalies);
            allSignals.AddRange(metricResult.Signals);
        }

        var cycleResult = EvaluateCycleDrift(input.CycleSamples, input.EvaluatedAtUtc, input.SourceIsStale);
        if (cycleResult is { } drift)
        {
            allAnomalies.Add(drift.Anomaly);
            allSignals.Add(drift.Signal);
        }

        ApplyAlarmContext(allSignals, allAnomalies, input.AlarmContext, input.EvaluatedAtUtc, input.AssetReference, input.MachineId);
        ApplyDowntimeContext(allSignals, input.DowntimeContext, input.AssetReference);

        var dataFreshness = ResolveDataFreshness(input.SourceIsStale, metricResults);
        var dataSufficiency = ResolveOverallDataSufficiency(metricResults);
        var trendState = ResolveOverallTrend(metricResults, cycleResult?.TrendState);
        var highestSeverity = ResolveHighestSeverity(allAnomalies);
        var activeAnomalies = allAnomalies.Where(a => a.Severity != MaintenanceAnomalySeverities.Info).ToList();
        var conditionStatus = ResolveConditionStatus(dataFreshness, dataSufficiency, activeAnomalies);
        var overallScore = ComputeOverallScore(conditionStatus, activeAnomalies);
        var evaluationIdentity = BuildEvaluationIdentity(input, allAnomalies);

        return new ConditionAssetEvaluationResult(
            input.AssetReference,
            input.MachineId,
            conditionStatus,
            overallScore,
            dataFreshness,
            dataSufficiency,
            trendState,
            activeAnomalies.Count,
            highestSeverity,
            evaluationIdentity,
            metricResults,
            allAnomalies,
            allSignals);
    }

    internal static ConditionMetricEvaluationResult EvaluateMetric(
        ConditionMetricEvaluationInput metric,
        DateTimeOffset evaluatedAtUtc,
        bool sourceIsStale)
    {
        var validSamples = metric.Samples
            .Where(s => IsUsableQuality(s.Quality))
            .OrderBy(s => s.SourceTimestampUtc)
            .ToList();

        var windowStart = evaluatedAtUtc.AddHours(-metric.EvaluationWindowHours);
        var windowSamples = validSamples
            .Where(s => s.SourceTimestampUtc >= windowStart && s.SourceTimestampUtc <= evaluatedAtUtc)
            .ToList();

        var dataFreshness = ResolveMetricFreshness(sourceIsStale, metric.LatestSampleAtUtc, evaluatedAtUtc);
        if (windowSamples.Count < metric.MinimumSamples)
        {
            return new ConditionMetricEvaluationResult(
                metric.MetricKey,
                false,
                MaintenanceConditionDataSufficiencyLevels.Insufficient,
                dataFreshness,
                MaintenanceConditionTrendStates.Insufficient,
                null,
                null,
                null,
                [],
                []);
        }

        var baselineSamples = windowSamples.Take(windowSamples.Count / 2).ToList();
        var recentSamples = windowSamples.Skip(windowSamples.Count / 2).ToList();
        if (baselineSamples.Count == 0 || recentSamples.Count == 0)
        {
            baselineSamples = windowSamples.Take(Math.Max(1, windowSamples.Count - 1)).ToList();
            recentSamples = windowSamples.Skip(baselineSamples.Count).ToList();
        }

        var baseline = CalculateBaseline(baselineSamples.Select(s => s.Value).ToList(), metric.BaselineMethod);
        var observed = CalculateBaseline(recentSamples.Select(s => s.Value).ToList(), metric.BaselineMethod);
        var deviationPercent = baseline == 0 ? 0 : Math.Abs((observed - baseline) / baseline * 100m);
        var trendState = EvaluateTrend(baselineSamples, recentSamples, metric.TrendThresholdPercent);
        var volatilityPercent = CalculateVolatilityPercent(windowSamples.Select(s => s.Value).ToList());
        var dataSufficiency = ResolveSampleSufficiency(windowSamples.Count, metric.MinimumSamples);

        var anomalies = new List<ConditionAnomalyCandidate>();
        var signals = new List<ConditionSignalCandidate>();

        if (dataFreshness != MaintenanceConditionDataFreshnessLevels.Stale &&
            deviationPercent >= metric.DeviationThresholdPercent)
        {
            var direction = observed > baseline
                ? MaintenanceAnomalyDirections.High
                : MaintenanceAnomalyDirections.Low;
            var severity = ResolveDeviationSeverity(deviationPercent, metric.DeviationThresholdPercent);
            var evidence = BuildDeviationEvidence(metric, baseline, observed, deviationPercent, recentSamples.Count);
            var identity = BuildMetricEvaluationIdentity(
                metric.MetricKey,
                windowStart,
                evaluatedAtUtc,
                observed,
                baseline,
                direction);

            anomalies.Add(new ConditionAnomalyCandidate(
                metric.MetricKey,
                windowStart,
                evaluatedAtUtc,
                observed,
                baseline,
                deviationPercent,
                severity,
                direction,
                evidence,
                dataFreshness,
                "good",
                identity));

            signals.Add(new ConditionSignalCandidate(
                $"{metric.MetricKey}_above_baseline",
                metric.MetricKey,
                severity,
                "Normal davranıştan sapma algılandı."));
        }

        if (metric.VolatilityThresholdPercent is decimal volatilityThreshold &&
            volatilityPercent >= volatilityThreshold)
        {
            var evidence =
                $"Son {windowSamples.Count} örneğin değişkenliği %{volatilityPercent.ToString("0.0", CultureInfo.InvariantCulture)}; eşik %{volatilityThreshold.ToString("0.0", CultureInfo.InvariantCulture)}.";
            var identity = BuildMetricEvaluationIdentity(
                metric.MetricKey,
                windowStart,
                evaluatedAtUtc,
                volatilityPercent,
                volatilityThreshold,
                MaintenanceAnomalyDirections.Volatile);

            anomalies.Add(new ConditionAnomalyCandidate(
                metric.MetricKey,
                windowStart,
                evaluatedAtUtc,
                volatilityPercent,
                volatilityThreshold,
                Math.Abs(volatilityPercent - volatilityThreshold),
                ResolveVolatilitySeverity(volatilityPercent, volatilityThreshold),
                MaintenanceAnomalyDirections.Volatile,
                evidence,
                dataFreshness,
                "good",
                identity));

            signals.Add(new ConditionSignalCandidate(
                $"{metric.MetricKey}_variability",
                metric.MetricKey,
                MaintenanceAnomalySeverities.Warning,
                "Metrik değerlerinde dalgalanma algılandı."));
        }

        return new ConditionMetricEvaluationResult(
            metric.MetricKey,
            true,
            dataSufficiency,
            dataFreshness,
            trendState,
            baseline,
            observed,
            deviationPercent,
            anomalies,
            signals);
    }

    internal static (ConditionAnomalyCandidate Anomaly, ConditionSignalCandidate Signal, string TrendState)? EvaluateCycleDrift(
        IReadOnlyList<ConditionCycleSampleInput> cycles,
        DateTimeOffset evaluatedAtUtc,
        bool sourceIsStale)
    {
        if (cycles.Count < 6)
        {
            return null;
        }

        var ordered = cycles.OrderBy(c => c.ObservedAtUtc).ToList();
        var baselineCycles = ordered.Take(ordered.Count / 2).Select(c => c.DurationMs).ToList();
        var recentCycles = ordered.Skip(ordered.Count / 2).Select(c => c.DurationMs).ToList();
        var baseline = CalculateBaseline(baselineCycles, MaintenanceConditionBaselineMethods.Median);
        var recent = CalculateBaseline(recentCycles, MaintenanceConditionBaselineMethods.Median);
        if (baseline <= 0)
        {
            return null;
        }

        var driftPercent = (recent - baseline) / baseline * 100m;
        const decimal cycleDriftThreshold = 8m;
        var trendState = driftPercent >= cycleDriftThreshold
            ? MaintenanceConditionTrendStates.Increasing
            : driftPercent <= -cycleDriftThreshold
                ? MaintenanceConditionTrendStates.Decreasing
                : MaintenanceConditionTrendStates.Stable;

        if (Math.Abs(driftPercent) < cycleDriftThreshold)
        {
            return null;
        }

        var dataFreshness = sourceIsStale
            ? MaintenanceConditionDataFreshnessLevels.Stale
            : MaintenanceConditionDataFreshnessLevels.Fresh;
        var direction = driftPercent > 0
            ? MaintenanceAnomalyDirections.TrendUp
            : MaintenanceAnomalyDirections.TrendDown;
        var evidence =
            $"Referans çevrim süresi {FormatNumber(baseline)} ms; son medyan {FormatNumber(recent)} ms; sapma {FormatSignedPercent(driftPercent)}.";
        var identity = BuildMetricEvaluationIdentity(
            "cycle_time_drift",
            ordered.First().ObservedAtUtc,
            evaluatedAtUtc,
            recent,
            baseline,
            direction);

        var anomaly = new ConditionAnomalyCandidate(
            "cycle_time_drift",
            ordered.First().ObservedAtUtc,
            evaluatedAtUtc,
            recent,
            baseline,
            Math.Abs(driftPercent),
            ResolveDeviationSeverity(Math.Abs(driftPercent), cycleDriftThreshold),
            direction,
            evidence,
            dataFreshness,
            "good",
            identity);

        var signal = new ConditionSignalCandidate(
            "cycle_time_drift",
            "cycle_time_drift",
            anomaly.Severity,
            "Çevrim süresinde kayma algılandı.");

        return (anomaly, signal, trendState);
    }

    private static void ApplyAlarmContext(
        List<ConditionSignalCandidate> signals,
        List<ConditionAnomalyCandidate> anomalies,
        ConditionAlarmContextInput alarmContext,
        DateTimeOffset evaluatedAtUtc,
        string assetReference,
        Guid machineId)
    {
        if (alarmContext.RecentAlarmCount24h < 3)
        {
            return;
        }

        var summary =
            $"Son 24 saatte {alarmContext.RecentAlarmCount24h} tekrarlayan alarm gözlemi destekleyici kanıt olarak değerlendirildi.";
        signals.Add(new ConditionSignalCandidate(
            "repeated_fault_pattern",
            "alarm_context",
            MaintenanceAnomalySeverities.Warning,
            summary));

        if (alarmContext.HighestSeverity24h is "critical" or "high")
        {
            anomalies.Add(new ConditionAnomalyCandidate(
                "alarm_context",
                evaluatedAtUtc.AddHours(-24),
                evaluatedAtUtc,
                alarmContext.RecentAlarmCount24h,
                2,
                (alarmContext.RecentAlarmCount24h - 2) * 10m,
                MaintenanceAnomalySeverities.Warning,
                MaintenanceAnomalyDirections.High,
                summary,
                MaintenanceConditionDataFreshnessLevels.Fresh,
                "good",
                BuildMetricEvaluationIdentity(
                    "alarm_context",
                    evaluatedAtUtc.AddHours(-24),
                    evaluatedAtUtc,
                    alarmContext.RecentAlarmCount24h,
                    2,
                    MaintenanceAnomalyDirections.High)));
        }

        _ = assetReference;
        _ = machineId;
    }

    private static void ApplyDowntimeContext(
        List<ConditionSignalCandidate> signals,
        ConditionDowntimeContextInput downtimeContext,
        string assetReference)
    {
        if (downtimeContext.EpisodeCount24h < 2)
        {
            return;
        }

        signals.Add(new ConditionSignalCandidate(
            "downtime_frequency_increase",
            "downtime_context",
            MaintenanceAnomalySeverities.Info,
            $"Son 24 saatte {downtimeContext.EpisodeCount24h} duruş gözlemi ({FormatNumber(downtimeContext.TotalDurationMinutes24h)} dk) destekleyici kanıt olarak kaydedildi."));
        _ = assetReference;
    }

    private static bool IsUsableQuality(string quality) =>
        !string.Equals(quality, "bad", StringComparison.OrdinalIgnoreCase);

    private static decimal CalculateBaseline(List<decimal> values, string method)
    {
        if (values.Count == 0)
        {
            return 0;
        }

        return method == MaintenanceConditionBaselineMethods.Mean
            ? values.Average()
            : CalculateMedian(values);
    }

    private static decimal CalculateMedian(List<decimal> values)
    {
        var ordered = values.OrderBy(v => v).ToList();
        var mid = ordered.Count / 2;
        return ordered.Count % 2 == 0
            ? (ordered[mid - 1] + ordered[mid]) / 2m
            : ordered[mid];
    }

    private static decimal CalculateVolatilityPercent(List<decimal> values)
    {
        if (values.Count < 2)
        {
            return 0;
        }

        var mean = values.Average();
        if (mean == 0)
        {
            return 0;
        }

        var variance = values.Select(v => (v - mean) * (v - mean)).Average();
        var stdDev = (decimal)Math.Sqrt((double)variance);
        return stdDev / Math.Abs(mean) * 100m;
    }

    private static string EvaluateTrend(
        List<ConditionTelemetrySampleInput> baselineSamples,
        List<ConditionTelemetrySampleInput> recentSamples,
        decimal trendThresholdPercent)
    {
        if (baselineSamples.Count == 0 || recentSamples.Count == 0)
        {
            return MaintenanceConditionTrendStates.Insufficient;
        }

        var baseline = CalculateBaseline(baselineSamples.Select(s => s.Value).ToList(), MaintenanceConditionBaselineMethods.Median);
        var recent = CalculateBaseline(recentSamples.Select(s => s.Value).ToList(), MaintenanceConditionBaselineMethods.Median);
        if (baseline == 0)
        {
            return MaintenanceConditionTrendStates.Stable;
        }

        var deltaPercent = (recent - baseline) / baseline * 100m;
        if (Math.Abs(deltaPercent) < trendThresholdPercent)
        {
            return MaintenanceConditionTrendStates.Stable;
        }

        return deltaPercent > 0
            ? MaintenanceConditionTrendStates.Increasing
            : MaintenanceConditionTrendStates.Decreasing;
    }

    private static string ResolveMetricFreshness(
        bool sourceIsStale,
        DateTimeOffset? latestSampleAtUtc,
        DateTimeOffset evaluatedAtUtc)
    {
        if (sourceIsStale)
        {
            return MaintenanceConditionDataFreshnessLevels.Stale;
        }

        if (latestSampleAtUtc is null)
        {
            return MaintenanceConditionDataFreshnessLevels.Unknown;
        }

        return evaluatedAtUtc - latestSampleAtUtc.Value > StaleThreshold
            ? MaintenanceConditionDataFreshnessLevels.Stale
            : MaintenanceConditionDataFreshnessLevels.Fresh;
    }

    private static string ResolveSampleSufficiency(int sampleCount, int minimumSamples)
    {
        if (sampleCount < minimumSamples)
        {
            return MaintenanceConditionDataSufficiencyLevels.Insufficient;
        }

        if (sampleCount >= minimumSamples * 2)
        {
            return MaintenanceConditionDataSufficiencyLevels.High;
        }

        return sampleCount >= minimumSamples + 2
            ? MaintenanceConditionDataSufficiencyLevels.Medium
            : MaintenanceConditionDataSufficiencyLevels.Low;
    }

    private static string ResolveDataFreshness(bool sourceIsStale, List<ConditionMetricEvaluationResult> metrics)
    {
        if (sourceIsStale || metrics.Any(m => m.DataFreshness == MaintenanceConditionDataFreshnessLevels.Stale))
        {
            return MaintenanceConditionDataFreshnessLevels.Stale;
        }

        return metrics.Any(m => m.DataFreshness == MaintenanceConditionDataFreshnessLevels.Fresh)
            ? MaintenanceConditionDataFreshnessLevels.Fresh
            : MaintenanceConditionDataFreshnessLevels.Unknown;
    }

    private static string ResolveOverallDataSufficiency(List<ConditionMetricEvaluationResult> metrics)
    {
        if (metrics.Count == 0 || metrics.All(m => m.DataSufficiency == MaintenanceConditionDataSufficiencyLevels.Insufficient))
        {
            return MaintenanceConditionDataSufficiencyLevels.Insufficient;
        }

        if (metrics.Any(m => m.DataSufficiency == MaintenanceConditionDataSufficiencyLevels.High))
        {
            return MaintenanceConditionDataSufficiencyLevels.High;
        }

        if (metrics.Any(m => m.DataSufficiency == MaintenanceConditionDataSufficiencyLevels.Medium))
        {
            return MaintenanceConditionDataSufficiencyLevels.Medium;
        }

        return MaintenanceConditionDataSufficiencyLevels.Low;
    }

    private static string ResolveOverallTrend(
        List<ConditionMetricEvaluationResult> metrics,
        string? cycleTrend)
    {
        if (cycleTrend is MaintenanceConditionTrendStates.Increasing or MaintenanceConditionTrendStates.Decreasing)
        {
            return cycleTrend;
        }

        if (metrics.Any(m => m.TrendState == MaintenanceConditionTrendStates.Volatile))
        {
            return MaintenanceConditionTrendStates.Volatile;
        }

        if (metrics.Any(m => m.TrendState == MaintenanceConditionTrendStates.Increasing))
        {
            return MaintenanceConditionTrendStates.Increasing;
        }

        if (metrics.Any(m => m.TrendState == MaintenanceConditionTrendStates.Decreasing))
        {
            return MaintenanceConditionTrendStates.Decreasing;
        }

        return metrics.Any(m => m.TrendState == MaintenanceConditionTrendStates.Stable)
            ? MaintenanceConditionTrendStates.Stable
            : MaintenanceConditionTrendStates.Insufficient;
    }

    private static string? ResolveHighestSeverity(IReadOnlyList<ConditionAnomalyCandidate> anomalies)
    {
        var rank = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            [MaintenanceAnomalySeverities.Info] = 1,
            [MaintenanceAnomalySeverities.Warning] = 2,
            [MaintenanceAnomalySeverities.High] = 3,
            [MaintenanceAnomalySeverities.Critical] = 4,
        };

        return anomalies
            .OrderByDescending(a => rank.GetValueOrDefault(a.Severity, 0))
            .Select(a => a.Severity)
            .FirstOrDefault();
    }

    private static string ResolveConditionStatus(
        string dataFreshness,
        string dataSufficiency,
        IReadOnlyList<ConditionAnomalyCandidate> activeAnomalies)
    {
        if (dataFreshness == MaintenanceConditionDataFreshnessLevels.Stale ||
            dataSufficiency == MaintenanceConditionDataSufficiencyLevels.Insufficient)
        {
            return MaintenanceConditionStatuses.Unknown;
        }

        var highest = ResolveHighestSeverity(activeAnomalies);
        return highest switch
        {
            MaintenanceAnomalySeverities.Critical => MaintenanceConditionStatuses.Critical,
            MaintenanceAnomalySeverities.High => MaintenanceConditionStatuses.Degraded,
            MaintenanceAnomalySeverities.Warning => MaintenanceConditionStatuses.Watch,
            MaintenanceAnomalySeverities.Info => MaintenanceConditionStatuses.Normal,
            _ => MaintenanceConditionStatuses.Normal,
        };
    }

    private static decimal? ComputeOverallScore(string conditionStatus, IReadOnlyList<ConditionAnomalyCandidate> activeAnomalies)
    {
        decimal? baseScore = conditionStatus switch
        {
            MaintenanceConditionStatuses.Critical => 25m,
            MaintenanceConditionStatuses.Degraded => 45m,
            MaintenanceConditionStatuses.Watch => 65m,
            MaintenanceConditionStatuses.Normal => 90m,
            _ => null,
        };

        if (baseScore is null)
        {
            return null;
        }

        var penalty = activeAnomalies.Sum(a => a.Severity switch
        {
            MaintenanceAnomalySeverities.Critical => 8m,
            MaintenanceAnomalySeverities.High => 5m,
            MaintenanceAnomalySeverities.Warning => 2m,
            _ => 0m,
        });

        return Math.Max(0m, Math.Min(100m, baseScore.Value - penalty));
    }

    private static string ResolveDeviationSeverity(decimal deviationPercent, decimal thresholdPercent)
    {
        if (deviationPercent >= thresholdPercent * 2.5m)
        {
            return MaintenanceAnomalySeverities.Critical;
        }

        if (deviationPercent >= thresholdPercent * 1.75m)
        {
            return MaintenanceAnomalySeverities.High;
        }

        if (deviationPercent >= thresholdPercent * 1.25m)
        {
            return MaintenanceAnomalySeverities.Warning;
        }

        return MaintenanceAnomalySeverities.Info;
    }

    private static string ResolveVolatilitySeverity(decimal volatilityPercent, decimal thresholdPercent)
    {
        if (volatilityPercent >= thresholdPercent * 2m)
        {
            return MaintenanceAnomalySeverities.High;
        }

        return volatilityPercent >= thresholdPercent * 1.35m
            ? MaintenanceAnomalySeverities.Warning
            : MaintenanceAnomalySeverities.Info;
    }

    private static string BuildDeviationEvidence(
        ConditionMetricEvaluationInput metric,
        decimal baseline,
        decimal observed,
        decimal deviationPercent,
        int recentSampleCount)
    {
        var unit = string.IsNullOrWhiteSpace(metric.ExpectedUnit) ? "" : $" {metric.ExpectedUnit}";
        return
            $"Son {recentSampleCount} örneğin medyan {metric.DisplayName.ToLowerInvariant()} değeri {FormatNumber(observed)}{unit}; referans medyan {FormatNumber(baseline)}{unit}; sapma {FormatSignedPercent(deviationPercent)}.";
    }

    private static string BuildEvaluationIdentity(
        ConditionAssetEvaluationInput input,
        IReadOnlyList<ConditionAnomalyCandidate> anomalies)
    {
        var builder = new StringBuilder();
        builder.Append(input.AssetReference);
        builder.Append('|');
        builder.Append(input.MachineId);
        builder.Append('|');
        builder.Append(input.EvaluatedAtUtc.ToUnixTimeSeconds());
        foreach (var anomaly in anomalies.OrderBy(a => a.MetricKey))
        {
            builder.Append('|');
            builder.Append(anomaly.EvaluationIdentity);
        }

        return HashIdentity(builder.ToString());
    }

    internal static string BuildMetricEvaluationIdentity(
        string metricKey,
        DateTimeOffset windowStartUtc,
        DateTimeOffset windowEndUtc,
        decimal observed,
        decimal baseline,
        string direction) =>
        HashIdentity($"{metricKey}|{windowStartUtc.ToUnixTimeSeconds()}|{windowEndUtc.ToUnixTimeSeconds()}|{Math.Round(observed, 2)}|{Math.Round(baseline, 2)}|{direction}");

    private static string HashIdentity(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes);
    }

    private static string FormatNumber(decimal value) =>
        value.ToString("0.##", CultureInfo.InvariantCulture);

    private static string FormatSignedPercent(decimal value) =>
        (value >= 0 ? "+" : "") + value.ToString("0.#", CultureInfo.InvariantCulture) + "%";
}
