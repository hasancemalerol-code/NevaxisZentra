using Microsoft.EntityFrameworkCore;

namespace NevaxisFactOS.Persistence.MixedProduction;

public interface IMixedProductionService
{
    Task<IReadOnlyList<MixedProductionLineSummary>> ListLinesAsync(Guid facilityId, CancellationToken cancellationToken = default);
    Task<MixedProductionLineDetailSnapshot> GetLineDetailAsync(Guid facilityId, Guid lineId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MixedProductionTimelineEntry>> GetTimelineAsync(
        Guid facilityId, Guid lineId, DateTimeOffset? fromUtc, DateTimeOffset? toUtc, CancellationToken cancellationToken = default);
    Task<MixedProductionPlaybackSnapshot> GetPlaybackAsync(
        Guid facilityId, Guid lineId, DateTimeOffset fromUtc, DateTimeOffset toUtc, CancellationToken cancellationToken = default);
    Task<CanonicalProductionEvent> RecordEventAsync(
        RecordProductionEventRequest request, Guid tenantId, CancellationToken cancellationToken = default);
    Task<OperatorStationSession> PerformOperatorActionAsync(
        OperatorActionRequest request, Guid tenantId, CancellationToken cancellationToken = default);
    Task<MixedProductionSeedResult> SeedMixLine01Async(Guid facilityId, Guid tenantId, CancellationToken cancellationToken = default);
}

public sealed class MixedProductionService : IMixedProductionService
{
    private readonly NevaxisFactOSDbContext db;

    public MixedProductionService(NevaxisFactOSDbContext db)
    {
        this.db = db;
    }

    public async Task<IReadOnlyList<MixedProductionLineSummary>> ListLinesAsync(
        Guid facilityId, CancellationToken cancellationToken = default)
    {
        var lines = await db.ProductionLines
            .Where(l => l.FacilityId == facilityId && l.IsActive)
            .OrderBy(l => l.Code)
            .ToListAsync(cancellationToken);

        var summaries = new List<MixedProductionLineSummary>();
        foreach (var line in lines)
        {
            var events = await db.CanonicalProductionEvents
                .Where(e => e.LineId == line.Id)
                .OrderByDescending(e => e.OccurredAtUtc)
                .Take(50)
                .ToListAsync(cancellationToken);

            var today = DateTimeOffset.UtcNow.Date;
            summaries.Add(new MixedProductionLineSummary(
                line.Id,
                line.Code,
                line.Name,
                line.CurrentOrderReference,
                line.CurrentProductCode,
                line.CurrentShiftCode,
                MixedProductionLineStateEngine.DeriveLineState(events, []),
                events.Count(e => e.EventType == MixedProductionEventTypes.Good && e.OccurredAtUtc.Date == today),
                events.Count(e => e.EventType == MixedProductionEventTypes.Reject && e.OccurredAtUtc.Date == today)));
        }

        return summaries;
    }

    public async Task<MixedProductionLineDetailSnapshot> GetLineDetailAsync(
        Guid facilityId, Guid lineId, CancellationToken cancellationToken = default)
    {
        var line = await db.ProductionLines.FirstOrDefaultAsync(
            l => l.FacilityId == facilityId && l.Id == lineId, cancellationToken)
            ?? throw new MixedProductionException(MixedProductionFailureCodes.NotFound, "Line not found.");

        var stations = await db.ProductionStations
            .Where(s => s.LineId == lineId)
            .OrderBy(s => s.SequenceOrder)
            .ToListAsync(cancellationToken);

        var resources = await db.ManufacturingResources
            .Where(r => stations.Select(s => s.Id).Contains(r.StationId))
            .ToListAsync(cancellationToken);

        var events = await db.CanonicalProductionEvents
            .Where(e => e.LineId == lineId)
            .OrderByDescending(e => e.OccurredAtUtc)
            .Take(200)
            .ToListAsync(cancellationToken);

        var buffers = await db.BufferResourceStates
            .Where(b => resources.Select(r => r.Id).Contains(b.ResourceId))
            .ToListAsync(cancellationToken);

        var sessions = await db.OperatorStationSessions
            .Where(s => stations.Select(st => st.Id).Contains(s.StationId) && s.Status == "in-progress")
            .ToListAsync(cancellationToken);

        var stationsById = stations.ToDictionary(s => s.Id);
        var resourcesById = resources.ToDictionary(r => r.Id);
        var resourcesByStation = resources.GroupBy(r => r.StationId).ToDictionary(g => g.Key, g => g.First());

        var stationSnapshots = stations.Select(station =>
        {
            var resource = resourcesByStation.GetValueOrDefault(station.Id);
            var stationEvents = events.Where(e => e.StationId == station.Id).ToList();
            var session = sessions.FirstOrDefault(s => s.StationId == station.Id);
            BufferResourceState? buffer = null;
            if (resource is not null)
            {
                buffer = buffers.FirstOrDefault(b => b.ResourceId == resource.Id);
            }

            return new MixedProductionStationSnapshot(
                station.Id,
                station.Code,
                station.Name,
                station.StationKind,
                station.SequenceOrder,
                MixedProductionLineStateEngine.DeriveStationState(station.Id, stationEvents, session),
                resource?.Code,
                resource?.ResourceType,
                stationEvents.FirstOrDefault()?.OperationCode,
                session?.OperatorCode,
                buffer?.CurrentQuantity,
                buffer?.Capacity,
                stationEvents.Count(e => e.EventType == MixedProductionEventTypes.Good),
                stationEvents.Count(e => e.EventType == MixedProductionEventTypes.Reject));
        }).ToList();

        var today = DateTimeOffset.UtcNow.Date;
        var summary = new MixedProductionLineSummary(
            line.Id,
            line.Code,
            line.Name,
            line.CurrentOrderReference,
            line.CurrentProductCode,
            line.CurrentShiftCode,
            MixedProductionLineStateEngine.DeriveLineState(events, buffers),
            events.Count(e => e.EventType == MixedProductionEventTypes.Good && e.OccurredAtUtc.Date == today),
            events.Count(e => e.EventType == MixedProductionEventTypes.Reject && e.OccurredAtUtc.Date == today));

        var timeline = MixedProductionTimelineEngine.Build(events.OrderBy(e => e.OccurredAtUtc).ToList(), stationsById, resourcesById);

        var playbackWindowLabel = timeline.Count == 0
            ? "—"
            : $"{timeline[0].OccurredAtUtc:HH:mm} – {timeline[^1].OccurredAtUtc:HH:mm}";

        return new MixedProductionLineDetailSnapshot(
            summary,
            stationSnapshots,
            timeline,
            playbackWindowLabel);
    }

    public async Task<IReadOnlyList<MixedProductionTimelineEntry>> GetTimelineAsync(
        Guid facilityId,
        Guid lineId,
        DateTimeOffset? fromUtc,
        DateTimeOffset? toUtc,
        CancellationToken cancellationToken = default)
    {
        var query = db.CanonicalProductionEvents.Where(e => e.FacilityId == facilityId && e.LineId == lineId);
        if (fromUtc.HasValue) query = query.Where(e => e.OccurredAtUtc >= fromUtc.Value);
        if (toUtc.HasValue) query = query.Where(e => e.OccurredAtUtc <= toUtc.Value);

        var events = await query.OrderBy(e => e.OccurredAtUtc).ToListAsync(cancellationToken);
        var stations = await db.ProductionStations.Where(s => s.LineId == lineId).ToListAsync(cancellationToken);
        var resources = await db.ManufacturingResources
            .Where(r => stations.Select(s => s.Id).Contains(r.StationId))
            .ToListAsync(cancellationToken);

        return MixedProductionTimelineEngine.Build(
            events,
            stations.ToDictionary(s => s.Id),
            resources.ToDictionary(r => r.Id));
    }

    public async Task<MixedProductionPlaybackSnapshot> GetPlaybackAsync(
        Guid facilityId,
        Guid lineId,
        DateTimeOffset fromUtc,
        DateTimeOffset toUtc,
        CancellationToken cancellationToken = default)
    {
        var line = await db.ProductionLines.FirstOrDefaultAsync(
            l => l.FacilityId == facilityId && l.Id == lineId, cancellationToken)
            ?? throw new MixedProductionException(MixedProductionFailureCodes.NotFound, "Line not found.");

        var stations = await db.ProductionStations.Where(s => s.LineId == lineId).OrderBy(s => s.SequenceOrder).ToListAsync(cancellationToken);
        var resources = await db.ManufacturingResources.Where(r => stations.Select(s => s.Id).Contains(r.StationId)).ToListAsync(cancellationToken);
        var events = await db.CanonicalProductionEvents
            .Where(e => e.LineId == lineId && e.OccurredAtUtc >= fromUtc && e.OccurredAtUtc <= toUtc)
            .OrderBy(e => e.OccurredAtUtc)
            .ToListAsync(cancellationToken);
        var buffers = await db.BufferResourceStates
            .Where(b => resources.Select(r => r.Id).Contains(b.ResourceId))
            .ToDictionaryAsync(b => b.ResourceId, cancellationToken);

        return MixedProductionPlaybackEngine.Build(
            line,
            fromUtc,
            toUtc,
            events,
            stations,
            buffers,
            resources.GroupBy(r => r.StationId).ToDictionary(g => g.Key, g => g.First()));
    }

    public async Task<CanonicalProductionEvent> RecordEventAsync(
        RecordProductionEventRequest request,
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.IdempotencyKey))
        {
            throw new MixedProductionException(
                MixedProductionFailureCodes.ValidationFailed,
                "Idempotency key is required.");
        }

        var existing = await db.CanonicalProductionEvents.AsNoTracking()
            .FirstOrDefaultAsync(
                e => e.TenantId == tenantId && e.FacilityId == request.FacilityId && e.IdempotencyKey == request.IdempotencyKey,
                cancellationToken);
        if (existing is not null)
        {
            return existing;
        }

        var resourceType = request.ResourceId.HasValue
            ? await db.ManufacturingResources.AsNoTracking()
                .Where(r => r.Id == request.ResourceId.Value)
                .Select(r => r.ResourceType)
                .FirstOrDefaultAsync(cancellationToken)
            : null;

        var now = DateTimeOffset.UtcNow;
        var eventRow = new CanonicalProductionEvent
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            FacilityId = request.FacilityId,
            LineId = request.LineId,
            StationId = request.StationId,
            ResourceId = request.ResourceId,
            ResourceType = resourceType,
            EventType = request.EventType,
            OperationCode = request.OperationCode,
            OrderReference = request.OrderReference,
            ProductCode = request.ProductCode,
            OperatorCode = request.OperatorCode,
            ShiftCode = request.ShiftCode,
            Quantity = request.Quantity,
            Unit = request.Unit ?? "pcs",
            ReasonCode = request.ReasonCode,
            CorrelationKey = request.CorrelationKey,
            Source = request.Source ?? "mixed-production",
            SourceEventId = request.SourceEventId,
            IdempotencyKey = request.IdempotencyKey,
            OccurredAtUtc = request.OccurredAtUtc ?? now,
            ReceivedAtUtc = now,
        };

        db.CanonicalProductionEvents.Add(eventRow);
        await ApplyBufferSideEffectsAsync(eventRow, tenantId, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return eventRow;
    }

    public async Task<OperatorStationSession> PerformOperatorActionAsync(
        OperatorActionRequest request,
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        var station = await db.ProductionStations.FirstOrDefaultAsync(
            s => s.FacilityId == request.FacilityId && s.Id == request.StationId, cancellationToken)
            ?? throw new MixedProductionException(MixedProductionFailureCodes.NotFound, "Station not found.");

        var resource = await db.ManufacturingResources.FirstOrDefaultAsync(
            r => r.StationId == station.Id && r.ResourceType == MixedProductionResourceTypes.Human, cancellationToken)
            ?? await db.ManufacturingResources.FirstOrDefaultAsync(r => r.StationId == station.Id, cancellationToken)
            ?? throw new MixedProductionException(MixedProductionFailureCodes.NotFound, "Resource not found.");

        var now = DateTimeOffset.UtcNow;
        var session = await db.OperatorStationSessions
            .Where(s => s.StationId == station.Id && s.OperatorCode == request.OperatorCode && s.Status == "in-progress")
            .OrderByDescending(s => s.StartedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        if (request.Action.Equals("start", StringComparison.OrdinalIgnoreCase))
        {
            session = new OperatorStationSession
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                FacilityId = request.FacilityId,
                StationId = station.Id,
                ResourceId = resource.Id,
                OperatorCode = request.OperatorCode,
                OperationCode = request.OperationCode,
                OrderReference = request.OrderReference,
                ShiftCode = request.ShiftCode,
                Status = "in-progress",
                WorkInstructionId = request.WorkInstructionId,
                StartedAtUtc = now,
                CreatedAtUtc = now,
                UpdatedAtUtc = now,
            };
            db.OperatorStationSessions.Add(session);

            await RecordEventAsync(new RecordProductionEventRequest(
                request.FacilityId,
                station.LineId,
                station.Id,
                resource.Id,
                MixedProductionEventTypes.OperatorStart,
                request.OperationCode,
                request.OrderReference,
                null,
                request.OperatorCode,
                request.ShiftCode,
                null,
                null,
                null,
                request.OrderReference,
                "operator-action",
                $"op-start-{session.Id:N}",
                $"op-start-{session.Id:N}",
                now), tenantId, cancellationToken);
        }
        else if (request.Action.Equals("complete", StringComparison.OrdinalIgnoreCase))
        {
            if (session is null)
            {
                throw new MixedProductionException(MixedProductionFailureCodes.ValidationFailed, "No active operator session.");
            }

            session.Status = "completed";
            session.CompletedAtUtc = now;
            session.Outcome = request.Outcome ?? "good";
            session.ReasonCode = request.ReasonCode;
            session.UpdatedAtUtc = now;

            var outcomeEvent = request.Outcome?.ToLowerInvariant() switch
            {
                "reject" => MixedProductionEventTypes.Reject,
                "rework" => MixedProductionEventTypes.Rework,
                _ => MixedProductionEventTypes.Good,
            };

            await RecordEventAsync(new RecordProductionEventRequest(
                request.FacilityId,
                station.LineId,
                station.Id,
                resource.Id,
                MixedProductionEventTypes.OperatorComplete,
                request.OperationCode,
                request.OrderReference,
                null,
                request.OperatorCode,
                request.ShiftCode,
                1,
                "pcs",
                request.ReasonCode,
                request.OrderReference,
                "operator-action",
                $"op-complete-{session.Id:N}",
                $"op-complete-{session.Id:N}",
                now), tenantId, cancellationToken);

            if (outcomeEvent != MixedProductionEventTypes.OperatorComplete)
            {
                await RecordEventAsync(new RecordProductionEventRequest(
                    request.FacilityId,
                    station.LineId,
                    station.Id,
                    resource.Id,
                    outcomeEvent,
                    request.OperationCode,
                    request.OrderReference,
                    null,
                    request.OperatorCode,
                    request.ShiftCode,
                    1,
                    "pcs",
                    request.ReasonCode,
                    request.OrderReference,
                    "operator-action",
                    $"op-outcome-{session.Id:N}",
                    $"op-outcome-{session.Id:N}",
                    now), tenantId, cancellationToken);
            }
        }
        else
        {
            throw new MixedProductionException(MixedProductionFailureCodes.ValidationFailed, "Unsupported operator action.");
        }

        await db.SaveChangesAsync(cancellationToken);
        return session!;
    }

    public Task<MixedProductionSeedResult> SeedMixLine01Async(
        Guid facilityId, Guid tenantId, CancellationToken cancellationToken = default) =>
        MixedProductionFixtureSeeder.SeedMixLine01Async(db, tenantId, facilityId, cancellationToken);

    private async Task ApplyBufferSideEffectsAsync(
        CanonicalProductionEvent eventRow,
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        if (eventRow.ResourceId is null ||
            eventRow.EventType is not (MixedProductionEventTypes.BufferChange or MixedProductionEventTypes.Good))
        {
            return;
        }

        var resource = await db.ManufacturingResources.FirstOrDefaultAsync(
            r => r.Id == eventRow.ResourceId.Value, cancellationToken);
        if (resource?.ResourceType != MixedProductionResourceTypes.Buffer)
        {
            return;
        }

        var buffer = await db.BufferResourceStates.FirstOrDefaultAsync(
            b => b.ResourceId == resource.Id, cancellationToken);
        if (buffer is null)
        {
            buffer = new BufferResourceState
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                FacilityId = eventRow.FacilityId,
                ResourceId = resource.Id,
                Capacity = resource.BufferCapacity ?? 40,
                CurrentQuantity = 0,
                UpdatedAtUtc = DateTimeOffset.UtcNow,
            };
            db.BufferResourceStates.Add(buffer);
        }

        if (eventRow.EventType == MixedProductionEventTypes.BufferChange && eventRow.Quantity.HasValue)
        {
            buffer.CurrentQuantity = (int)eventRow.Quantity.Value;
        }
        else if (eventRow.EventType == MixedProductionEventTypes.Good)
        {
            buffer.CurrentQuantity = Math.Min(buffer.Capacity, buffer.CurrentQuantity + 1);
        }

        buffer.IsBlocked = buffer.CurrentQuantity >= buffer.Capacity;
        buffer.IsStarved = buffer.CurrentQuantity <= 0;
        buffer.UpdatedAtUtc = DateTimeOffset.UtcNow;
    }
}
