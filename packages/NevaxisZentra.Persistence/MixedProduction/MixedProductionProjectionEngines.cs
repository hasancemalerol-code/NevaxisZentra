namespace NevaxisZentra.Persistence.MixedProduction;

public static class MixedProductionLineStateEngine
{
    public static string DeriveLineState(
        IReadOnlyList<CanonicalProductionEvent> recentEvents,
        IReadOnlyList<BufferResourceState> buffers)
    {
        if (recentEvents.Any(e => e.EventType == MixedProductionEventTypes.Fault))
        {
            return MixedProductionLineStates.Faulted;
        }

        if (buffers.Any(b => b.IsBlocked))
        {
            return MixedProductionLineStates.Blocked;
        }

        if (buffers.Any(b => b.IsStarved))
        {
            return MixedProductionLineStates.Starved;
        }

        var latest = recentEvents.OrderByDescending(e => e.OccurredAtUtc).FirstOrDefault();
        if (latest is null)
        {
            return MixedProductionLineStates.Stopped;
        }

        return latest.EventType switch
        {
            MixedProductionEventTypes.Run or MixedProductionEventTypes.CycleStart or MixedProductionEventTypes.Good
                or MixedProductionEventTypes.OperatorComplete => MixedProductionLineStates.Running,
            MixedProductionEventTypes.Stop => MixedProductionLineStates.Stopped,
            MixedProductionEventTypes.Starved => MixedProductionLineStates.Starved,
            MixedProductionEventTypes.Blocked => MixedProductionLineStates.Blocked,
            MixedProductionEventTypes.Fault => MixedProductionLineStates.Faulted,
            _ => MixedProductionLineStates.MixedDegraded,
        };
    }

    public static string DeriveStationState(
        Guid stationId,
        IReadOnlyList<CanonicalProductionEvent> stationEvents,
        OperatorStationSession? activeSession)
    {
        if (stationEvents.Any(e => e.EventType == MixedProductionEventTypes.Fault))
        {
            return MixedProductionEventTypes.Fault;
        }

        if (activeSession is { Status: "in-progress" })
        {
            return "OPERATING";
        }

        var latest = stationEvents.OrderByDescending(e => e.OccurredAtUtc).FirstOrDefault();
        return latest?.EventType ?? MixedProductionEventTypes.Idle;
    }
}

public static class MixedProductionTimelineEngine
{
    public static IReadOnlyList<MixedProductionTimelineEntry> Build(
        IReadOnlyList<CanonicalProductionEvent> events,
        IReadOnlyDictionary<Guid, ProductionStation> stationsById,
        IReadOnlyDictionary<Guid, ManufacturingResource> resourcesById)
    {
        return events
            .OrderBy(e => e.OccurredAtUtc)
            .Select(e =>
            {
                var stationCode = e.StationId.HasValue && stationsById.TryGetValue(e.StationId.Value, out var station)
                    ? station.Code
                    : "—";
                var resourceCode = e.ResourceId.HasValue && resourcesById.TryGetValue(e.ResourceId.Value, out var resource)
                    ? resource.Code
                    : null;

                return new MixedProductionTimelineEntry(
                    e.Id,
                    e.OccurredAtUtc,
                    e.EventType,
                    Categorize(e),
                    stationCode,
                    resourceCode,
                    e.OperatorCode,
                    BuildSummary(e, stationCode, resourceCode),
                    e.Quantity,
                    e.CorrelationKey);
            })
            .ToList();
    }

    private static string Categorize(CanonicalProductionEvent eventRow) =>
        eventRow.EventType switch
        {
            MixedProductionEventTypes.OperatorStart or MixedProductionEventTypes.OperatorComplete
                or MixedProductionEventTypes.OperatorWait => "operator",
            MixedProductionEventTypes.BufferChange => "buffer",
            MixedProductionEventTypes.Good or MixedProductionEventTypes.Reject or MixedProductionEventTypes.Rework
                or MixedProductionEventTypes.QualityResult => "quality",
            MixedProductionEventTypes.MaterialWait => "material",
            _ => "production",
        };

    private static string BuildSummary(
        CanonicalProductionEvent eventRow,
        string stationCode,
        string? resourceCode)
    {
        var resource = resourceCode is null ? string.Empty : $" · {resourceCode}";
        var qty = eventRow.Quantity.HasValue ? $" ({eventRow.Quantity} {eventRow.Unit ?? "pcs"})" : string.Empty;
        return $"{stationCode}{resource}: {eventRow.EventType}{qty}";
    }
}

public static class MixedProductionPlaybackEngine
{
    public static MixedProductionPlaybackSnapshot Build(
        ProductionLine line,
        DateTimeOffset windowStartUtc,
        DateTimeOffset windowEndUtc,
        IReadOnlyList<CanonicalProductionEvent> events,
        IReadOnlyList<ProductionStation> stations,
        IReadOnlyDictionary<Guid, BufferResourceState> bufferByResourceId,
        IReadOnlyDictionary<Guid, ManufacturingResource> resourcesByStationId)
    {
        var ordered = events
            .Where(e => e.OccurredAtUtc >= windowStartUtc && e.OccurredAtUtc <= windowEndUtc)
            .OrderBy(e => e.OccurredAtUtc)
            .ToList();

        var frames = new List<MixedProductionPlaybackFrame>();
        var cursor = windowStartUtc;
        while (cursor <= windowEndUtc)
        {
            var slice = ordered.Where(e => e.OccurredAtUtc <= cursor).ToList();
            var lineState = MixedProductionLineStateEngine.DeriveLineState(
                slice,
                bufferByResourceId.Values.ToList());

            var stationStates = stations
                .OrderBy(s => s.SequenceOrder)
                .Select(station =>
                {
                    var stationEvents = slice.Where(e => e.StationId == station.Id).ToList();
                    var resource = resourcesByStationId.GetValueOrDefault(station.Id);
                    BufferResourceState? buffer = null;
                    if (resource is not null)
                    {
                        bufferByResourceId.TryGetValue(resource.Id, out buffer);
                    }

                    var latest = stationEvents.LastOrDefault();
                    return new MixedProductionStationPlaybackState(
                        station.Code,
                        MixedProductionLineStateEngine.DeriveStationState(station.Id, stationEvents, null),
                        buffer?.CurrentQuantity,
                        latest?.OperatorCode,
                        latest?.EventType);
                })
                .ToList();

            frames.Add(new MixedProductionPlaybackFrame(cursor, lineState, stationStates));
            cursor = cursor.AddMinutes(1);
        }

        return new MixedProductionPlaybackSnapshot(
            line.Id,
            line.Code,
            windowStartUtc,
            windowEndUtc,
            frames);
    }
}
