namespace NevaxisFactOS.Persistence.FactoryIntelligence;

public static class FactoryIntelligenceBaselineEngine
{
    private const decimal MinReference = 0.0001m;

    public static FactoryBaselineResult Compute(
        string signalType,
        decimal measuredValue,
        decimal? configuredReference,
        IReadOnlyList<decimal> recentStableSamples,
        string dataQuality)
    {
        if (dataQuality is FactoryIntelligenceDataQualityStates.Insufficient
            or FactoryIntelligenceDataQualityStates.Stale)
        {
            return new FactoryBaselineResult(
                SignalType: signalType,
                Reference: configuredReference ?? measuredValue,
                Current: measuredValue,
                DeviationPercent: 0,
                Trend: "unknown",
                DataQuality: dataQuality,
                BaselineSource: "insufficient");
        }

        var reference = ResolveReference(configuredReference, recentStableSamples, measuredValue);
        var deviation = reference <= MinReference
            ? 0
            : ((measuredValue - reference) / reference) * 100m;
        var trend = ResolveTrend(recentStableSamples, measuredValue, signalType);
        var quality = DowngradeQualityIfSparse(dataQuality, recentStableSamples.Count);

        return new FactoryBaselineResult(
            SignalType: signalType,
            Reference: reference,
            Current: measuredValue,
            DeviationPercent: deviation,
            Trend: trend,
            DataQuality: quality,
            BaselineSource: configuredReference.HasValue ? "configured-reference" : "recent-stable-window");
    }

    private static decimal ResolveReference(decimal? configured, IReadOnlyList<decimal> samples, decimal fallback)
    {
        if (configured.HasValue && configured.Value > 0)
        {
            return configured.Value;
        }

        if (samples.Count >= 3)
        {
            return samples.Take(Math.Min(5, samples.Count)).Average();
        }

        return fallback;
    }

    private static string ResolveTrend(IReadOnlyList<decimal> samples, decimal current, string signalType)
    {
        if (samples.Count < 2)
        {
            return "stable";
        }

        var ordered = samples.Concat([current]).ToList();
        var first = ordered.First();
        var last = ordered.Last();
        if (first <= MinReference)
        {
            return "stable";
        }

        var delta = ((last - first) / first) * 100m;
        var isHigherWorse = signalType is FactoryIntelligenceSignalTypes.CycleTimeDrift
            or FactoryIntelligenceSignalTypes.MicroStopFrequency
            or FactoryIntelligenceSignalTypes.DowntimeFrequency
            or FactoryIntelligenceSignalTypes.ScrapRejectTrend;

        if (Math.Abs(delta) < 2m)
        {
            return "stable";
        }

        if (isHigherWorse)
        {
            return delta > 0 ? "worsening" : "improving";
        }

        return delta < 0 ? "worsening" : "improving";
    }

    private static string DowngradeQualityIfSparse(string dataQuality, int sampleCount)
    {
        if (sampleCount < 2)
        {
            return FactoryIntelligenceDataQualityStates.Partial;
        }

        return dataQuality;
    }
}
