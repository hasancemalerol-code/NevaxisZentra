namespace NevaxisZentra.Persistence.ProcessOee;

public static class ProcessOeeOperatingTimeCalculator
{
    public sealed record TimeInterval(DateTimeOffset StartUtc, DateTimeOffset EndUtc);

    public static int ComputeWindowDurationSeconds(DateTimeOffset startUtc, DateTimeOffset endUtc) =>
        Math.Max(0, (int)Math.Floor((endUtc - startUtc).TotalSeconds));

    public static IReadOnlyList<TimeInterval> ClipIntervalsToWindow(
        IEnumerable<TimeInterval> intervals,
        DateTimeOffset windowStartUtc,
        DateTimeOffset windowEndUtc)
    {
        var clipped = new List<TimeInterval>();
        foreach (var interval in intervals)
        {
            var start = interval.StartUtc < windowStartUtc ? windowStartUtc : interval.StartUtc;
            var end = interval.EndUtc > windowEndUtc ? windowEndUtc : interval.EndUtc;
            if (end > start)
            {
                clipped.Add(new TimeInterval(start, end));
            }
        }

        return clipped;
    }

    public static IReadOnlyList<TimeInterval> MergeOverlappingIntervals(IReadOnlyList<TimeInterval> intervals)
    {
        if (intervals.Count == 0)
        {
            return intervals;
        }

        var ordered = intervals.OrderBy(i => i.StartUtc).ThenBy(i => i.EndUtc).ToList();
        var merged = new List<TimeInterval> { ordered[0] };

        for (var index = 1; index < ordered.Count; index++)
        {
            var current = ordered[index];
            var last = merged[^1];
            if (current.StartUtc <= last.EndUtc)
            {
                merged[^1] = new TimeInterval(last.StartUtc, current.EndUtc > last.EndUtc ? current.EndUtc : last.EndUtc);
            }
            else
            {
                merged.Add(current);
            }
        }

        return merged;
    }

    public static int SumIntervalSeconds(IEnumerable<TimeInterval> intervals) =>
        intervals.Sum(i => Math.Max(0, (int)Math.Floor((i.EndUtc - i.StartUtc).TotalSeconds)));

    public static (int DowntimeSeconds, int UnknownSeconds, int OperatingSeconds) ComputeOperatingBreakdown(
        int windowDurationSeconds,
        int? plannedProductionSeconds,
        IReadOnlyList<TimeInterval> downtimeIntervals)
    {
        var downtimeSeconds = SumIntervalSeconds(downtimeIntervals);
        var planned = plannedProductionSeconds ?? windowDurationSeconds;
        planned = Math.Min(planned, windowDurationSeconds);
        var unknownSeconds = Math.Max(0, windowDurationSeconds - planned);
        var operatingSeconds = plannedProductionSeconds.HasValue
            ? Math.Max(0, plannedProductionSeconds.Value - downtimeSeconds)
            : Math.Max(0, windowDurationSeconds - downtimeSeconds - unknownSeconds);

        return (downtimeSeconds, unknownSeconds, operatingSeconds);
    }
}
