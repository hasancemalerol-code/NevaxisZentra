using NevaxisZentra.Persistence.MixedProduction;

namespace NevaxisZentra.Persistence.ThroughputIntelligence;

public static class ThroughputMeasurementEngine
{
    public static ThroughputMeasurementResult Compute(
        IReadOnlyList<CanonicalProductionEvent> events,
        DateTimeOffset windowStartUtc,
        DateTimeOffset windowEndUtc,
        decimal? targetGoodUnitsPerHour,
        string windowLabel)
    {
        var windowEvents = events
            .Where(e => e.OccurredAtUtc >= windowStartUtc && e.OccurredAtUtc <= windowEndUtc)
            .ToList();

        var goodCount = windowEvents.Count(e => e.EventType == MixedProductionEventTypes.Good);
        var rejectCount = windowEvents.Count(e => e.EventType == MixedProductionEventTypes.Reject);
        var reworkCount = windowEvents.Count(e => e.EventType == MixedProductionEventTypes.Rework);
        var grossCount = goodCount + rejectCount + reworkCount;

        var windowHours = Math.Max((windowEndUtc - windowStartUtc).TotalHours, 1.0 / 3600.0);
        var goodPerHour = (decimal)(goodCount / windowHours);
        var grossPerHour = (decimal)(grossCount / windowHours);
        var rejectPerHour = (decimal)(rejectCount / windowHours);
        var reworkPerHour = (decimal)(reworkCount / windowHours);

        var dataQuality = windowEvents.Count >= 5
            ? ThroughputIntelligenceDataQualityStates.Good
            : windowEvents.Count > 0
                ? ThroughputIntelligenceDataQualityStates.Partial
                : ThroughputIntelligenceDataQualityStates.Insufficient;

        var gap = targetGoodUnitsPerHour.HasValue
            ? targetGoodUnitsPerHour.Value - goodPerHour
            : (decimal?)null;

        return new ThroughputMeasurementResult(
            goodPerHour,
            grossPerHour,
            rejectPerHour,
            reworkPerHour,
            targetGoodUnitsPerHour,
            goodPerHour,
            gap,
            goodCount,
            grossCount,
            rejectCount,
            reworkCount,
            windowHours,
            dataQuality,
            windowLabel);
    }
}

public static class TaktDemandEngine
{
    public static TaktDemandResult Compute(LineThroughputTarget? target, decimal? actualEffectiveCycleSeconds)
    {
        if (target is null)
        {
            return new TaktDemandResult(
                null, null, actualEffectiveCycleSeconds, null, null,
                ThroughputIntelligenceDataQualityStates.Insufficient,
                "No throughput target configured for line.");
        }

        if (!target.RequiredGoodQuantity.HasValue || !target.AvailableProductionHours.HasValue
            || target.AvailableProductionHours.Value <= 0)
        {
            return new TaktDemandResult(
                null,
                target.TargetCycleSeconds,
                actualEffectiveCycleSeconds,
                target.RequiredGoodQuantity,
                target.AvailableProductionHours,
                ThroughputIntelligenceDataQualityStates.Insufficient,
                "Required good quantity or available production time not configured.");
        }

        var availableSeconds = target.AvailableProductionHours.Value * 3600m;
        var takt = availableSeconds / target.RequiredGoodQuantity.Value;

        return new TaktDemandResult(
            takt,
            target.TargetCycleSeconds,
            actualEffectiveCycleSeconds,
            target.RequiredGoodQuantity,
            target.AvailableProductionHours,
            ThroughputIntelligenceDataQualityStates.Good,
            null);
    }
}

public static class StationCycleTimeEngine
{
    public static IReadOnlyList<StationCycleTimeResult> ComputeAll(
        ThroughputAnalysisContext context,
        IReadOnlyDictionary<Guid, string> stationCodes)
    {
        return context.Stations
            .Where(s => s.StationKind != MixedProductionStationKinds.Buffer)
            .Select(station =>
            {
                var stationEvents = context.Events
                    .Where(e => e.StationId == station.Id
                        && e.OccurredAtUtc >= context.WindowStartUtc
                        && e.OccurredAtUtc <= context.WindowEndUtc)
                    .OrderBy(e => e.OccurredAtUtc)
                    .ToList();

                var cycles = ExtractCycleDurations(stationEvents);
                var target = context.Target?.TargetCycleSeconds;

                if (cycles.Count == 0)
                {
                    return new StationCycleTimeResult(
                        station.Id,
                        stationCodes.GetValueOrDefault(station.Id, station.Code),
                        station.Name,
                        null, null, target, null, 0,
                        context.AnalysisWindow,
                        "stale",
                        ThroughputIntelligenceDataQualityStates.Insufficient);
                }

                var median = Median(cycles);
                var variation = cycles.Count > 1 ? StdDev(cycles) : 0m;
                var gap = target.HasValue ? median - target.Value : (decimal?)null;

                return new StationCycleTimeResult(
                    station.Id,
                    stationCodes.GetValueOrDefault(station.Id, station.Code),
                    station.Name,
                    median,
                    variation,
                    target,
                    gap,
                    cycles.Count,
                    context.AnalysisWindow,
                    "fresh",
                    cycles.Count >= 3
                        ? ThroughputIntelligenceDataQualityStates.Good
                        : ThroughputIntelligenceDataQualityStates.Partial);
            })
            .ToList();
    }

    private static List<decimal> ExtractCycleDurations(List<CanonicalProductionEvent> stationEvents)
    {
        var durations = new List<decimal>();

        for (var i = 0; i < stationEvents.Count - 1; i++)
        {
            var current = stationEvents[i];
            var next = stationEvents[i + 1];

            if (current.EventType == MixedProductionEventTypes.CycleStart
                && next.EventType == MixedProductionEventTypes.CycleEnd)
            {
                durations.Add((decimal)(next.OccurredAtUtc - current.OccurredAtUtc).TotalSeconds);
                continue;
            }

            if (current.EventType == MixedProductionEventTypes.OperatorStart
                && next.EventType == MixedProductionEventTypes.OperatorComplete)
            {
                durations.Add((decimal)(next.OccurredAtUtc - current.OccurredAtUtc).TotalSeconds);
            }
        }

        return durations;
    }

    private static decimal Median(List<decimal> values)
    {
        var ordered = values.OrderBy(v => v).ToList();
        var mid = ordered.Count / 2;
        return ordered.Count % 2 == 0
            ? (ordered[mid - 1] + ordered[mid]) / 2m
            : ordered[mid];
    }

    private static decimal StdDev(List<decimal> values)
    {
        if (values.Count < 2) return 0;
        var avg = values.Average();
        var variance = values.Select(v => (v - avg) * (v - avg)).Average();
        return (decimal)Math.Sqrt((double)variance);
    }
}

public static class CapacityModelEngine
{
    public static IReadOnlyList<StationCapacityResult> ComputeAll(
        ThroughputAnalysisContext context,
        IReadOnlyList<StationCycleTimeResult> cycles,
        IReadOnlyDictionary<Guid, SimulationStationState> simulationStates)
    {
        var windowHours = Math.Max((context.WindowEndUtc - context.WindowStartUtc).TotalHours, 1.0 / 3600.0);
        var results = new List<StationCapacityResult>();

        foreach (var station in context.Stations.Where(s => s.StationKind != MixedProductionStationKinds.Buffer))
        {
            var resource = context.ResourcesByStationId.GetValueOrDefault(station.Id);
            var cycle = cycles.FirstOrDefault(c => c.StationId == station.Id);
            var sim = simulationStates.GetValueOrDefault(station.Id);
            var effectiveCycle = sim?.EffectiveCycleSeconds ?? cycle?.MedianCycleSeconds ?? 0m;

            if (effectiveCycle <= 0)
            {
                results.Add(new StationCapacityResult(
                    station.Id, station.Code, station.Name, ResolveCapacityKind(resource?.ResourceType),
                    0, 0, 0, 0,
                    "Insufficient cycle samples for capacity estimation.",
                    ThroughputIntelligenceDataQualityStates.Insufficient));
                continue;
            }

            var operatorCount = Math.Max(sim?.OperatorCount ?? 1, 1);
            var theoretical = 3600m / effectiveCycle * operatorCount;
            var yield = (sim?.YieldPercent ?? 100m) / 100m;

            var downtimeSeconds = (sim?.MicrostopLossSecondsPerHour ?? 0m)
                + (sim?.OperatorWaitSecondsPerHour ?? 0m)
                + (sim?.MaterialWaitSecondsPerHour ?? 0m)
                + (sim?.StarvedSecondsPerHour ?? 0m)
                + (sim?.BlockedSecondsPerHour ?? 0m);

            var availabilityFactor = Math.Max(0m, 1m - downtimeSeconds / 3600m);
            var available = theoretical * availabilityFactor;
            var effective = available * yield;

            var actualGood = context.Events.Count(e =>
                e.StationId == station.Id
                && e.EventType == MixedProductionEventTypes.Good
                && e.OccurredAtUtc >= context.WindowStartUtc
                && e.OccurredAtUtc <= context.WindowEndUtc);
            var actualRate = (decimal)(actualGood / windowHours);
            var utilization = effective > 0 ? Math.Min(100m, actualRate / effective * 100m) : 0m;

            results.Add(new StationCapacityResult(
                station.Id,
                station.Code,
                station.Name,
                ResolveCapacityKind(resource?.ResourceType),
                theoretical,
                available,
                effective,
                utilization,
                $"Theoretical from {effectiveCycle:F1}s cycle × {operatorCount} operator(s); availability {availabilityFactor:P0}; yield {yield:P0}.",
                cycle?.DataQuality ?? ThroughputIntelligenceDataQualityStates.Partial));
        }

        return results;
    }

    private static string ResolveCapacityKind(string? resourceType) =>
        resourceType switch
        {
            MixedProductionResourceTypes.Human => "HUMAN_CAPACITY",
            MixedProductionResourceTypes.Machine or MixedProductionResourceTypes.Robot => "MACHINE_CAPACITY",
            _ => "COMBINED_CAPACITY",
        };
}

public static class ConstraintDetectionEngine
{
    public static IReadOnlyList<ConstraintResult> Detect(
        ThroughputAnalysisContext context,
        IReadOnlyList<StationCapacityResult> capacities,
        ThroughputMeasurementResult measurement,
        IReadOnlyDictionary<Guid, SimulationStationState> simulationStates)
    {
        var targetRate = measurement.TargetGoodUnitsPerHour ?? measurement.GoodUnitsPerHour;
        if (targetRate <= 0)
        {
            return [];
        }

        var scored = capacities
            .Select(cap =>
            {
                var sim = simulationStates.GetValueOrDefault(cap.StationId);
                var deficit = targetRate - cap.EffectiveGoodUnitsPerHour;
                var utilization = cap.EffectiveGoodUnitsPerHour > 0
                    ? measurement.GoodUnitsPerHour / cap.EffectiveGoodUnitsPerHour * 100m
                    : 100m;

                var constraintType = ResolveConstraintType(context, cap.StationId, sim);
                var impact = Math.Max(0m, deficit);
                var evidence = BuildEvidence(context, cap, sim, utilization);

                return new
                {
                    Cap = cap,
                    Deficit = deficit,
                    Utilization = utilization,
                    ConstraintType = constraintType,
                    Impact = impact,
                    Evidence = evidence,
                    Score = utilization + (deficit > 0 ? deficit / targetRate * 50m : 0m),
                };
            })
            .Where(x => x.Cap.EffectiveGoodUnitsPerHour > 0 || x.Impact > 0)
            .OrderByDescending(x => x.Score)
            .ToList();

        if (scored.Count == 0)
        {
            return [];
        }

        var results = new List<ConstraintResult>();
        var roles = new[]
        {
            ThroughputIntelligenceConstraintRoles.Current,
            ThroughputIntelligenceConstraintRoles.Secondary,
            ThroughputIntelligenceConstraintRoles.Emerging,
        };

        for (var i = 0; i < Math.Min(roles.Length, scored.Count); i++)
        {
            var item = scored[i];
            results.Add(new ConstraintResult(
                roles[i],
                item.Cap.StationId,
                item.Cap.StationCode,
                item.Cap.StationName,
                null,
                null,
                item.ConstraintType,
                item.Evidence,
                Math.Round(item.Impact, 2),
                i == 0 ? "high" : "medium",
                item.Cap.DataQuality,
                BuildExplanation(item.Cap, item.ConstraintType, item.Utilization, item.Impact)));
        }

        return results;
    }

    private static string ResolveConstraintType(
        ThroughputAnalysisContext context,
        Guid stationId,
        SimulationStationState? sim)
    {
        if (sim is { MicrostopLossSecondsPerHour: > 60 })
        {
            return ThroughputIntelligenceConstraintTypes.Microstop;
        }

        if (sim is { OperatorWaitSecondsPerHour: > 30 })
        {
            return ThroughputIntelligenceConstraintTypes.OperatorWait;
        }

        if (sim is { MaterialWaitSecondsPerHour: > 30 })
        {
            return ThroughputIntelligenceConstraintTypes.MaterialWait;
        }

        if (sim is { StarvedSecondsPerHour: > 30 })
        {
            return ThroughputIntelligenceConstraintTypes.Starvation;
        }

        if (sim is { BlockedSecondsPerHour: > 30 })
        {
            return ThroughputIntelligenceConstraintTypes.Blocking;
        }

        if (sim is { YieldPercent: < 95 })
        {
            return ThroughputIntelligenceConstraintTypes.QualityYield;
        }

        var resource = context.ResourcesByStationId.GetValueOrDefault(stationId);
        return resource?.ResourceType == MixedProductionResourceTypes.Human
            ? ThroughputIntelligenceConstraintTypes.CycleTime
            : ThroughputIntelligenceConstraintTypes.Capacity;
    }

    private static List<ConstraintEvidence> BuildEvidence(
        ThroughputAnalysisContext context,
        StationCapacityResult cap,
        SimulationStationState? sim,
        decimal utilization)
    {
        var evidence = new List<ConstraintEvidence>
        {
            new("effective-capacity", "Effective good capacity", cap.EffectiveGoodUnitsPerHour, "units/hour"),
            new("utilization", "Utilization vs capacity", utilization, "percent"),
        };

        if (sim is not null)
        {
            if (sim.MicrostopLossSecondsPerHour > 0)
            {
                evidence.Add(new("microstop-loss", "Microstop loss time", sim.MicrostopLossSecondsPerHour, "sec/hour"));
            }

            if (sim.OperatorWaitSecondsPerHour > 0)
            {
                evidence.Add(new("operator-wait", "Operator wait time", sim.OperatorWaitSecondsPerHour, "sec/hour"));
            }
        }

        return evidence;
    }

    private static string BuildExplanation(
        StationCapacityResult cap,
        string constraintType,
        decimal utilization,
        decimal impact) =>
        $"{cap.StationCode} limits verified good throughput at {utilization:F0}% utilization ({constraintType}); estimated gap impact {impact:F1} GOOD/hour.";
}

public static class LossTreeEngine
{
    public static LossTreeResult Compute(
        ThroughputAnalysisContext context,
        ThroughputMeasurementResult measurement,
        IReadOnlyDictionary<Guid, SimulationStationState> simulationStates,
        IReadOnlyDictionary<Guid, string> stationCodes)
    {
        var gap = measurement.ThroughputGapPerHour ?? 0m;
        if (gap <= 0)
        {
            return new LossTreeResult([], gap, 0, gap,
                ThroughputIntelligenceAttributionQuality.Measured,
                measurement.DataQuality);
        }

        var windowHours = Math.Max(measurement.WindowHours, 1.0 / 3600.0);
        var nodes = new List<LossTreeNode>();

        foreach (var station in context.Stations)
        {
            var code = stationCodes.GetValueOrDefault(station.Id, station.Code);
            var sim = simulationStates.GetValueOrDefault(station.Id);
            if (sim is null) continue;

            AddLossNode(nodes, ThroughputIntelligenceLossCategories.OperatorWait, "Operator wait",
                sim.OperatorWaitSecondsPerHour, windowHours, station.Id, code);
            AddLossNode(nodes, ThroughputIntelligenceLossCategories.MaterialWait, "Material wait",
                sim.MaterialWaitSecondsPerHour, windowHours, station.Id, code);
            AddLossNode(nodes, ThroughputIntelligenceLossCategories.Microstop, "Microstops",
                sim.MicrostopLossSecondsPerHour, windowHours, station.Id, code);
            AddLossNode(nodes, ThroughputIntelligenceLossCategories.Starved, "Starved",
                sim.StarvedSecondsPerHour, windowHours, station.Id, code);
            AddLossNode(nodes, ThroughputIntelligenceLossCategories.Blocked, "Blocked",
                sim.BlockedSecondsPerHour, windowHours, station.Id, code);
        }

        var rejectEvents = context.Events.Count(e =>
            e.EventType == MixedProductionEventTypes.Reject
            && e.OccurredAtUtc >= context.WindowStartUtc
            && e.OccurredAtUtc <= context.WindowEndUtc);
        var rejectLoss = (decimal)(rejectEvents / windowHours);
        if (rejectLoss > 0)
        {
            nodes.Add(new LossTreeNode(
                ThroughputIntelligenceLossCategories.QualityReject,
                "Quality reject",
                rejectLoss,
                ThroughputIntelligenceAttributionQuality.Measured,
                null, null,
                "Reject events reduce verified good throughput."));
        }

        var reworkEvents = context.Events.Count(e =>
            e.EventType == MixedProductionEventTypes.Rework
            && e.OccurredAtUtc >= context.WindowStartUtc
            && e.OccurredAtUtc <= context.WindowEndUtc);
        var reworkLoss = (decimal)(reworkEvents / windowHours);
        if (reworkLoss > 0)
        {
            nodes.Add(new LossTreeNode(
                ThroughputIntelligenceLossCategories.Rework,
                "Rework",
                reworkLoss,
                ThroughputIntelligenceAttributionQuality.Measured,
                null, null,
                "Rework diverts capacity from net good output."));
        }

        var cycleVariationLoss = ComputeCycleVariationLoss(context, simulationStates, stationCodes, windowHours);
        nodes.AddRange(cycleVariationLoss);

        var attributed = nodes.Sum(n => n.LostGoodUnitsPerHour);
        var reconciliation = attributed >= gap * 0.7m
            ? ThroughputIntelligenceAttributionQuality.Measured
            : attributed > 0
                ? ThroughputIntelligenceAttributionQuality.Partial
                : ThroughputIntelligenceAttributionQuality.InsufficientData;

        return new LossTreeResult(
            nodes.OrderByDescending(n => n.LostGoodUnitsPerHour).ToList(),
            gap,
            attributed,
            Math.Max(0, gap - attributed),
            reconciliation,
            measurement.DataQuality);
    }

    private static void AddLossNode(
        List<LossTreeNode> nodes,
        string category,
        string label,
        decimal lossSecondsPerHour,
        double windowHours,
        Guid stationId,
        string stationCode)
    {
        if (lossSecondsPerHour <= 0) return;

        var simRate = 3600m / Math.Max(lossSecondsPerHour, 1m);
        var lostUnits = Math.Min(lossSecondsPerHour / 3600m * simRate, lossSecondsPerHour / 60m);

        nodes.Add(new LossTreeNode(
            category,
            $"{label} · {stationCode}",
            Math.Round(lostUnits, 2),
            ThroughputIntelligenceAttributionQuality.Estimated,
            stationId,
            stationCode,
            $"{label} estimated from event durations."));
    }

    private static List<LossTreeNode> ComputeCycleVariationLoss(
        ThroughputAnalysisContext context,
        IReadOnlyDictionary<Guid, SimulationStationState> simulationStates,
        IReadOnlyDictionary<Guid, string> stationCodes,
        double windowHours)
    {
        var nodes = new List<LossTreeNode>();
        foreach (var station in context.Stations)
        {
            var sim = simulationStates.GetValueOrDefault(station.Id);
            if (sim is null || sim.TargetCycleSeconds <= 0) continue;

            var excess = sim.EffectiveCycleSeconds - sim.TargetCycleSeconds;
            if (excess <= 0) continue;

            var baseRate = 3600m / sim.EffectiveCycleSeconds;
            var targetRate = 3600m / sim.TargetCycleSeconds;
            var loss = Math.Max(0, targetRate - baseRate);

            if (loss > 0)
            {
                nodes.Add(new LossTreeNode(
                    ThroughputIntelligenceLossCategories.CycleVariation,
                    $"Cycle variation · {stationCodes.GetValueOrDefault(station.Id, station.Code)}",
                    Math.Round(loss, 2),
                    ThroughputIntelligenceAttributionQuality.Estimated,
                    station.Id,
                    station.Code,
                    $"Effective cycle {sim.EffectiveCycleSeconds:F1}s exceeds target {sim.TargetCycleSeconds:F1}s."));
            }
        }

        return nodes;
    }
}

public static class MicrostopAnalysisEngine
{
    public static IReadOnlyList<MicrostopSummary> Compute(
        ThroughputAnalysisContext context,
        decimal thresholdSeconds)
    {
        var summaries = new Dictionary<Guid, MicrostopSummary>();

        foreach (var station in context.Stations)
        {
            var stops = context.Events
                .Where(e => e.StationId == station.Id
                    && e.EventType == MixedProductionEventTypes.Stop
                    && e.OccurredAtUtc >= context.WindowStartUtc
                    && e.OccurredAtUtc <= context.WindowEndUtc)
                .OrderBy(e => e.OccurredAtUtc)
                .ToList();

            var totalSeconds = 0.0;
            for (var i = 0; i < stops.Count; i++)
            {
                var stop = stops[i];
                var next = context.Events
                    .Where(e => e.StationId == station.Id && e.OccurredAtUtc > stop.OccurredAtUtc)
                    .OrderBy(e => e.OccurredAtUtc)
                    .FirstOrDefault();

                if (next is null) continue;
                var duration = (next.OccurredAtUtc - stop.OccurredAtUtc).TotalSeconds;
                if (duration > 0 && duration <= (double)thresholdSeconds)
                {
                    totalSeconds += duration;
                }
            }

            if (stops.Count == 0 && totalSeconds == 0) continue;

            var windowHours = Math.Max((context.WindowEndUtc - context.WindowStartUtc).TotalHours, 1.0 / 3600.0);
            var impact = (decimal)(totalSeconds / windowHours / 60.0);

            summaries[station.Id] = new MicrostopSummary(
                station.Id,
                station.Code,
                stops.Count,
                totalSeconds,
                Math.Round(impact, 2),
                totalSeconds > 0
                    ? ThroughputIntelligenceDataQualityStates.Good
                    : ThroughputIntelligenceDataQualityStates.Partial);
        }

        return summaries.Values.OrderByDescending(s => s.TotalLostSeconds).ToList();
    }
}

public static class StarvedBlockedAnalysisEngine
{
    public static IReadOnlyList<StarvedBlockedSummary> Compute(ThroughputAnalysisContext context)
    {
        var results = new List<StarvedBlockedSummary>();
        var orderedStations = context.Stations.OrderBy(s => s.SequenceOrder).ToList();

        foreach (var station in orderedStations)
        {
            var starvedEvents = context.Events.Where(e =>
                e.StationId == station.Id
                && e.EventType == MixedProductionEventTypes.Starved
                && e.OccurredAtUtc >= context.WindowStartUtc
                && e.OccurredAtUtc <= context.WindowEndUtc).ToList();

            var blockedEvents = context.Events.Where(e =>
                e.StationId == station.Id
                && e.EventType == MixedProductionEventTypes.Blocked
                && e.OccurredAtUtc >= context.WindowStartUtc
                && e.OccurredAtUtc <= context.WindowEndUtc).ToList();

            var upstream = orderedStations.LastOrDefault(s => s.SequenceOrder < station.SequenceOrder);
            var downstream = orderedStations.FirstOrDefault(s => s.SequenceOrder > station.SequenceOrder);

            foreach (var group in new[] { (starvedEvents, "STARVED", upstream?.Code), (blockedEvents, "BLOCKED", downstream?.Code) })
            {
                if (group.Item1.Count == 0) continue;

                var duration = group.Item1.Count * 15.0;
                results.Add(new StarvedBlockedSummary(
                    station.Id,
                    station.Code,
                    group.Item2,
                    group.Item3,
                    duration,
                    Math.Round((decimal)(duration / 3600.0 * 10), 2),
                    null,
                    $"{station.Code} {group.Item2} relative to {group.Item3 ?? "line"}.",
                    ThroughputIntelligenceDataQualityStates.Partial));
            }
        }

        return results;
    }
}

public static class BufferIntelligenceEngine
{
    public static IReadOnlyList<BufferIntelligenceResult> Compute(ThroughputAnalysisContext context)
    {
        var results = new List<BufferIntelligenceResult>();
        var orderedStations = context.Stations.OrderBy(s => s.SequenceOrder).ToList();

        foreach (var resource in context.ResourcesByStationId.Values.Where(r => r.ResourceType == MixedProductionResourceTypes.Buffer))
        {
            var buffer = context.BuffersByResourceId.GetValueOrDefault(resource.Id);
            if (buffer is null) continue;

            var station = context.Stations.First(s => s.Id == resource.StationId);
            var upstream = orderedStations.LastOrDefault(s => s.SequenceOrder < station.SequenceOrder);
            var downstream = orderedStations.FirstOrDefault(s => s.SequenceOrder > station.SequenceOrder);

            var bufferEvents = context.Events
                .Where(e => e.ResourceId == resource.Id && e.EventType == MixedProductionEventTypes.BufferChange)
                .Select(e => (int)(e.Quantity ?? 0))
                .ToList();

            var minObserved = bufferEvents.Count > 0 ? bufferEvents.Min() : buffer.CurrentQuantity;
            var maxObserved = bufferEvents.Count > 0 ? bufferEvents.Max() : buffer.CurrentQuantity;
            var utilization = buffer.Capacity > 0
                ? (decimal)buffer.CurrentQuantity / buffer.Capacity * 100m
                : 0m;

            var assessment = buffer.IsStarved ? "buffer chronically empty"
                : buffer.IsBlocked ? "buffer chronically full"
                : utilization < 20 ? "upstream/downstream imbalance (low WIP)"
                : utilization > 80 ? "downstream constraint exposure (high WIP)"
                : "balanced";

            results.Add(new BufferIntelligenceResult(
                resource.Id,
                resource.Code,
                buffer.Capacity,
                buffer.CurrentQuantity,
                minObserved,
                maxObserved,
                utilization,
                buffer.IsStarved ? "high" : "low",
                buffer.IsBlocked ? "high" : "low",
                utilization < 30 || utilization > 70 ? "imbalanced" : "balanced",
                upstream?.Code,
                downstream?.Code,
                assessment,
                ThroughputIntelligenceDataQualityStates.Good));
        }

        return results;
    }
}

public static class ProductionGapEngine
{
    public static ProductionGapResult Compute(
        ThroughputMeasurementResult measurement,
        LossTreeResult lossTree)
    {
        var required = measurement.TargetGoodUnitsPerHour ?? 0m;
        var current = measurement.GoodUnitsPerHour;
        var gap = Math.Max(0, required - current);

        return new ProductionGapResult(
            required,
            current,
            gap,
            lossTree.Nodes.Take(6).ToList(),
            lossTree.ReconciliationQuality,
            measurement.DataQuality);
    }
}

public static class OperatorSkillEngine
{
    public static IReadOnlyList<OperatorSkillEligibility> ComputeEligibility(
        ThroughputAnalysisContext context,
        IReadOnlyDictionary<Guid, string> stationCodes)
    {
        var now = DateTimeOffset.UtcNow;
        return context.OperatorSkills
            .Where(s => s.IsActive)
            .Select(skill =>
            {
                var quals = context.Qualifications
                    .Where(q => q.OperatorCode == skill.OperatorCode && q.IsActive)
                    .ToList();

                var eligibleStations = quals
                    .Where(q => q.ExpiresAtUtc is null || q.ExpiresAtUtc > now)
                    .Select(q => stationCodes.GetValueOrDefault(q.StationId, q.StationId.ToString()))
                    .Distinct()
                    .ToList();

                var expired = quals.Any(q => q.ExpiresAtUtc.HasValue && q.ExpiresAtUtc <= now);
                var isQualified = quals.Any(q => q.ExpiresAtUtc is null || q.ExpiresAtUtc > now);

                return new OperatorSkillEligibility(
                    skill.OperatorCode,
                    skill.DisplayName,
                    skill.SkillLevel,
                    isQualified,
                    expired,
                    eligibleStations,
                    quals.Where(q => q.ExpiresAtUtc is null || q.ExpiresAtUtc > now)
                        .Select(q => q.OperationCode).Distinct().ToList());
            })
            .ToList();
    }

    public static bool IsOperatorEligibleForStation(
        string operatorCode,
        Guid stationId,
        ThroughputAnalysisContext context)
    {
        var now = DateTimeOffset.UtcNow;
        return context.Qualifications.Any(q =>
            q.OperatorCode == operatorCode
            && q.StationId == stationId
            && q.IsActive
            && (q.ExpiresAtUtc is null || q.ExpiresAtUtc > now));
    }
}

public static class LineBalancingEngine
{
    public static IReadOnlyList<LineBalancingRecommendation> Compute(
        IReadOnlyList<StationCapacityResult> capacities,
        IReadOnlyList<OperatorSkillEligibility> operators,
        IReadOnlyList<ConstraintResult> constraints)
    {
        if (capacities.Count < 2) return [];

        var overloaded = capacities.OrderByDescending(c => c.UtilizationPercent).First();
        var underloaded = capacities.OrderBy(c => c.UtilizationPercent).First();
        var eligible = operators
            .Where(o => o.IsQualified && !o.IsExpired)
            .Select(o => o.OperatorCode)
            .ToList();

        var constraintStation = constraints.FirstOrDefault(c =>
            c.Role == ThroughputIntelligenceConstraintRoles.Current)?.StationCode ?? overloaded.StationCode;

        return
        [
            new LineBalancingRecommendation(
                constraintStation,
                underloaded.StationCode,
                eligible,
                "operator-redistribution",
                $"Overload at {constraintStation}; eligible operators may support {underloaded.StationCode} if qualified.",
                eligible.Count > 0),
        ];
    }
}

public static class SimulationStateBuilder
{
    public static IReadOnlyDictionary<Guid, SimulationStationState> BuildBaseline(
        ThroughputAnalysisContext context,
        IReadOnlyList<StationCycleTimeResult> cycles)
    {
        var windowHours = Math.Max((context.WindowEndUtc - context.WindowStartUtc).TotalHours, 1.0 / 3600.0);
        var states = new Dictionary<Guid, SimulationStationState>();

        foreach (var station in context.Stations)
        {
            var resource = context.ResourcesByStationId.GetValueOrDefault(station.Id);
            var cycle = cycles.FirstOrDefault(c => c.StationId == station.Id);
            var stationEvents = context.Events
                .Where(e => e.StationId == station.Id
                    && e.OccurredAtUtc >= context.WindowStartUtc
                    && e.OccurredAtUtc <= context.WindowEndUtc)
                .ToList();

            var effectiveCycle = cycle?.MedianCycleSeconds ?? context.Target?.TargetCycleSeconds ?? 6m;
            var targetCycle = context.Target?.TargetCycleSeconds ?? effectiveCycle;

            var operatorCount = stationEvents
                .Select(e => e.OperatorCode)
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .Distinct()
                .Count();
            if (operatorCount == 0 && resource?.ResourceType == MixedProductionResourceTypes.Human)
            {
                operatorCount = 1;
            }

            var bufferCapacity = resource?.BufferCapacity ?? 0;
            if (resource?.ResourceType == MixedProductionResourceTypes.Buffer)
            {
                bufferCapacity = context.BuffersByResourceId.GetValueOrDefault(resource.Id)?.Capacity ?? bufferCapacity;
            }

            var good = stationEvents.Count(e => e.EventType == MixedProductionEventTypes.Good);
            var reject = stationEvents.Count(e => e.EventType == MixedProductionEventTypes.Reject);
            var yieldPercent = good + reject > 0 ? (decimal)good / (good + reject) * 100m : 100m;

            states[station.Id] = new SimulationStationState(
                station.Id,
                station.Code,
                station.Name,
                resource?.ResourceType ?? "unknown",
                effectiveCycle,
                targetCycle,
                operatorCount,
                bufferCapacity,
                yieldPercent,
                SumEventDurations(stationEvents, MixedProductionEventTypes.Stop, context.MicrostopThresholdSeconds),
                SumEventDurations(stationEvents, MixedProductionEventTypes.OperatorWait, null),
                SumEventDurations(stationEvents, MixedProductionEventTypes.MaterialWait, null),
                SumEventDurations(stationEvents, MixedProductionEventTypes.Starved, null),
                SumEventDurations(stationEvents, MixedProductionEventTypes.Blocked, null));
        }

        return states;
    }

    private static decimal SumEventDurations(
        IReadOnlyList<CanonicalProductionEvent> stationEvents,
        string eventType,
        decimal? maxDurationSeconds)
    {
        var events = stationEvents.Where(e => e.EventType == eventType).ToList();
        if (events.Count == 0) return 0m;

        var windowHours = 1.0;
        return (decimal)(events.Count * 15.0 / windowHours);
    }
}

public static class ScenarioSimulationEngine
{
    public static ScenarioMetricSnapshot Simulate(
        ThroughputAnalysisContext context,
        IReadOnlyDictionary<Guid, SimulationStationState> baselineStates,
        IReadOnlyList<ScenarioIntervention> interventions,
        ThroughputMeasurementResult baselineMeasurement,
        IReadOnlyList<ConstraintResult> baselineConstraints,
        IReadOnlyList<OperatorSkillEligibility> operators)
    {
        var simulated = baselineStates.ToDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value with { });

        var eligibility = ThroughputIntelligenceScenarioEligibility.Eligible;
        var assumptions = new List<string> { "Simulation uses baseline projections; not verified operational truth." };
        var limitingFactors = new List<string>();

        foreach (var intervention in interventions)
        {
            var stationId = intervention.StationId
                ?? context.Stations.FirstOrDefault(s => s.Code == intervention.StationCode)?.Id;

            switch (intervention.InterventionType)
            {
                case ThroughputIntelligenceScenarioTypes.AddOperator:
                    if (stationId.HasValue && simulated.TryGetValue(stationId.Value, out var addState))
                    {
                        if (!string.IsNullOrWhiteSpace(intervention.OperatorCode)
                            && !OperatorSkillEngine.IsOperatorEligibleForStation(
                                intervention.OperatorCode, stationId.Value, context))
                        {
                            eligibility = ThroughputIntelligenceScenarioEligibility.NotEligible;
                            limitingFactors.Add($"Operator {intervention.OperatorCode} not qualified for station.");
                        }
                        else
                        {
                            simulated[stationId.Value] = addState with { OperatorCount = addState.OperatorCount + 1 };
                            assumptions.Add($"Added qualified operator at {addState.StationCode}; parallel capacity assumed.");
                        }
                    }
                    break;

                case ThroughputIntelligenceScenarioTypes.MoveOperator:
                    if (stationId.HasValue && !string.IsNullOrWhiteSpace(intervention.TargetStationCode))
                    {
                        var target = context.Stations.FirstOrDefault(s => s.Code == intervention.TargetStationCode);
                        if (target is not null
                            && !string.IsNullOrWhiteSpace(intervention.OperatorCode)
                            && !OperatorSkillEngine.IsOperatorEligibleForStation(
                                intervention.OperatorCode, target.Id, context))
                        {
                            eligibility = ThroughputIntelligenceScenarioEligibility.NotEligible;
                            limitingFactors.Add($"Operator {intervention.OperatorCode} not qualified for {intervention.TargetStationCode}.");
                        }
                        else if (simulated.TryGetValue(stationId.Value, out var fromState) && target is not null)
                        {
                            simulated[stationId.Value] = fromState with
                            {
                                OperatorCount = Math.Max(0, fromState.OperatorCount - 1),
                            };
                            if (simulated.TryGetValue(target.Id, out var toState))
                            {
                                simulated[target.Id] = toState with { OperatorCount = toState.OperatorCount + 1 };
                            }

                            assumptions.Add($"Moved operator from {fromState.StationCode} to {intervention.TargetStationCode}.");
                        }
                    }
                    break;

                case ThroughputIntelligenceScenarioTypes.ChangeBufferCapacity:
                    if (stationId.HasValue && intervention.BufferCapacityDelta.HasValue
                        && simulated.TryGetValue(stationId.Value, out var bufState))
                    {
                        simulated[stationId.Value] = bufState with
                        {
                            BufferCapacity = Math.Max(1, bufState.BufferCapacity + intervention.BufferCapacityDelta.Value),
                        };
                        assumptions.Add($"Buffer capacity adjusted by {intervention.BufferCapacityDelta} at {bufState.StationCode}.");
                    }
                    break;

                case ThroughputIntelligenceScenarioTypes.ChangeCycleTime:
                    if (stationId.HasValue && intervention.CycleTimeDeltaSeconds.HasValue
                        && simulated.TryGetValue(stationId.Value, out var cycleState))
                    {
                        var cycleDelta = intervention.CycleTimeDeltaSeconds.Value;
                        var yieldPenalty = cycleDelta < -1m ? Math.Abs(cycleDelta) * 2m : 0m;
                        simulated[stationId.Value] = cycleState with
                        {
                            EffectiveCycleSeconds = Math.Max(0.5m,
                                cycleState.EffectiveCycleSeconds + cycleDelta),
                            YieldPercent = Math.Max(50m, cycleState.YieldPercent - yieldPenalty),
                        };
                        assumptions.Add($"Cycle time delta {cycleDelta}s at {cycleState.StationCode}.");
                        if (yieldPenalty > 0)
                        {
                            assumptions.Add($"Aggressive cycle-time reduction may increase reject exposure (yield −{yieldPenalty:F1}%).");
                        }
                    }
                    break;

                case ThroughputIntelligenceScenarioTypes.RedistributeOperation:
                    if (stationId.HasValue && intervention.OperationRedistributionPercent.HasValue
                        && !string.IsNullOrWhiteSpace(intervention.TargetStationCode))
                    {
                        var target = context.Stations.FirstOrDefault(s => s.Code == intervention.TargetStationCode);
                        var pct = intervention.OperationRedistributionPercent.Value / 100m;
                        if (simulated.TryGetValue(stationId.Value, out var fromCycle) && target is not null
                            && simulated.TryGetValue(target.Id, out var toCycle))
                        {
                            simulated[stationId.Value] = fromCycle with
                            {
                                EffectiveCycleSeconds = fromCycle.EffectiveCycleSeconds * (1m + pct * 0.5m),
                            };
                            simulated[target.Id] = toCycle with
                            {
                                EffectiveCycleSeconds = Math.Max(0.5m, toCycle.EffectiveCycleSeconds * (1m - pct * 0.3m)),
                            };
                            assumptions.Add($"Redistributed {pct:P0} of operation from {fromCycle.StationCode} to {intervention.TargetStationCode}.");
                        }
                    }
                    break;
            }
        }

        var cycles = StationCycleTimeEngine.ComputeAll(context, context.Stations.ToDictionary(s => s.Id, s => s.Code));
        var capacities = CapacityModelEngine.ComputeAll(context, cycles, simulated);
        var predictedRate = capacities.Count > 0
            ? capacities.Min(c => c.EffectiveGoodUnitsPerHour)
            : baselineMeasurement.GoodUnitsPerHour;

        var baselineRate = baselineMeasurement.GoodUnitsPerHour;
        var delta = predictedRate - baselineRate;

        var afterConstraints = ConstraintDetectionEngine.Detect(
            context,
            capacities,
            baselineMeasurement with { GoodUnitsPerHour = predictedRate, ActualGoodUnitsPerHour = predictedRate },
            simulated);

        var baselineScrap = baselineMeasurement.GrossUnitsPerHour > 0
            ? baselineMeasurement.RejectUnitsPerHour / baselineMeasurement.GrossUnitsPerHour * 100m
            : 0m;

        var yieldDelta = simulated.Values.Sum(sim =>
        {
            var baseline = baselineStates.GetValueOrDefault(sim.StationId);
            if (baseline is null) return 0m;
            return Math.Max(0m, baseline.YieldPercent - sim.YieldPercent);
        });
        var predictedScrap = baselineScrap + yieldDelta;
        var qualityImpact = predictedScrap - baselineScrap;
        if (eligibility == ThroughputIntelligenceScenarioEligibility.Eligible)
        {
            if (qualityImpact > 5m)
            {
                eligibility = ThroughputIntelligenceScenarioEligibility.RequiresControlledApproval;
                limitingFactors.Add("Predicted reject rate increase exceeds quality guardrail.");
            }
            else if (delta > 0 && qualityImpact > 0 && predictedScrap > baselineScrap * 1.15m)
            {
                eligibility = ThroughputIntelligenceScenarioEligibility.RequiresControlledApproval;
                limitingFactors.Add("Gross throughput may rise but verified good throughput is constrained by quality yield.");
            }
        }

        var riskScore = interventions.Count * 10m + (eligibility != ThroughputIntelligenceScenarioEligibility.Eligible ? 20m : 0m);
        var bufferIntel = BufferIntelligenceEngine.Compute(context);
        var baselineWip = bufferIntel.Count > 0 ? bufferIntel[0].CurrentWip : 0;

        return new ScenarioMetricSnapshot(
            Math.Round(predictedRate, 2),
            Math.Round(baselineRate, 2),
            Math.Round(delta, 2),
            Math.Round(predictedScrap, 2),
            Math.Round(baselineScrap, 2),
            baselineWip,
            baselineWip,
            capacities.Count > 0 ? capacities.Average(c => c.UtilizationPercent) : 0,
            baselineConstraints.FirstOrDefault(c => c.Role == ThroughputIntelligenceConstraintRoles.Current),
            afterConstraints.FirstOrDefault(c => c.Role == ThroughputIntelligenceConstraintRoles.Current),
            qualityImpact,
            riskScore,
            eligibility,
            baselineMeasurement.DataQuality,
            eligibility == ThroughputIntelligenceScenarioEligibility.Eligible ? "medium" : "low",
            limitingFactors,
            assumptions);
    }
}

public static class ThroughputIntelligenceOrchestrator
{
    public static ThroughputIntelligenceOverview Analyze(ThroughputAnalysisContext context)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var stationCodes = context.Stations.ToDictionary(s => s.Id, s => s.Code);
        var windowLabel = $"{context.AnalysisWindow}:{context.WindowStartUtc:O}..{context.WindowEndUtc:O}";

        var targetRate = context.Target?.TargetGoodUnitsPerHour;
        var measurement = ThroughputMeasurementEngine.Compute(
            context.Events, context.WindowStartUtc, context.WindowEndUtc, targetRate, windowLabel);

        var cycles = StationCycleTimeEngine.ComputeAll(context, stationCodes);
        var lineCycle = cycles.Where(c => c.MedianCycleSeconds.HasValue).Select(c => c.MedianCycleSeconds).Average();
        var takt = TaktDemandEngine.Compute(context.Target, lineCycle);

        var baselineStates = SimulationStateBuilder.BuildBaseline(context, cycles);
        var capacities = CapacityModelEngine.ComputeAll(context, cycles, baselineStates);
        var constraints = ConstraintDetectionEngine.Detect(context, capacities, measurement, baselineStates);
        var lossTree = LossTreeEngine.Compute(context, measurement, baselineStates, stationCodes);
        var productionGap = ProductionGapEngine.Compute(measurement, lossTree);
        var microstops = MicrostopAnalysisEngine.Compute(context, context.MicrostopThresholdSeconds);
        var starvedBlocked = StarvedBlockedAnalysisEngine.Compute(context);
        var buffers = BufferIntelligenceEngine.Compute(context);
        var operators = OperatorSkillEngine.ComputeEligibility(context, stationCodes);
        var balancing = LineBalancingEngine.Compute(capacities, operators, constraints);

        var currentConstraintId = constraints
            .FirstOrDefault(c => c.Role == ThroughputIntelligenceConstraintRoles.Current)?.StationId;
        var secondaryConstraintId = constraints
            .FirstOrDefault(c => c.Role == ThroughputIntelligenceConstraintRoles.Secondary)?.StationId;

        var topology = context.Stations.OrderBy(s => s.SequenceOrder).Select(station =>
        {
            var cycle = cycles.FirstOrDefault(c => c.StationId == station.Id);
            var cap = capacities.FirstOrDefault(c => c.StationId == station.Id);
            var buffer = context.ResourcesByStationId.GetValueOrDefault(station.Id);
            BufferResourceState? bufferState = null;
            if (buffer is not null)
            {
                bufferState = context.BuffersByResourceId.GetValueOrDefault(buffer.Id);
            }

            var stationEvents = context.Events.Where(e => e.StationId == station.Id).ToList();
            var taktRelation = takt.TaktTimeSeconds.HasValue && cycle?.MedianCycleSeconds is > 0
                ? cycle.MedianCycleSeconds / takt.TaktTimeSeconds * 100m
                : (decimal?)null;

            return new ThroughputTopologyStationSnapshot(
                station.Id,
                station.Code,
                station.Name,
                MixedProductionLineStateEngine.DeriveStationState(station.Id, stationEvents, null),
                cycle?.MedianCycleSeconds,
                cap?.EffectiveGoodUnitsPerHour,
                taktRelation,
                bufferState?.CurrentQuantity,
                stationEvents.Select(e => e.OperatorCode).FirstOrDefault(c => !string.IsNullOrWhiteSpace(c)),
                stationEvents.Count(e => e.EventType == MixedProductionEventTypes.Good),
                stationEvents.Count(e => e.EventType == MixedProductionEventTypes.Reject),
                station.Id == currentConstraintId,
                station.Id == secondaryConstraintId,
                constraints.FirstOrDefault(c => c.StationId == station.Id)?.ConstraintType);
        }).ToList();

        sw.Stop();

        var constraintQuality = constraints.Count > 0 ? constraints[0].DataQuality : null;
        var dataQuality = new[]
        {
            measurement.DataQuality,
            lossTree.DataQuality,
            constraintQuality,
        }.Distinct().Contains(ThroughputIntelligenceDataQualityStates.Insufficient)
            ? ThroughputIntelligenceDataQualityStates.Partial
            : ThroughputIntelligenceDataQualityStates.Good;

        return new ThroughputIntelligenceOverview(
            context.Line.Id,
            context.Line.Code,
            context.Line.Name,
            measurement,
            takt,
            productionGap,
            constraints,
            lossTree,
            cycles,
            capacities,
            buffers,
            microstops,
            starvedBlocked,
            topology,
            operators,
            balancing,
            dataQuality,
            sw.ElapsedMilliseconds,
            context.Events.Count,
            context.Stations.Count,
            ThroughputIntelligenceEngineVersions.Measurement);
    }
}
