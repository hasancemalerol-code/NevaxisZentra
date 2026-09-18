using Microsoft.EntityFrameworkCore;
using NevaxisZentra.Persistence.Tenancy;

namespace NevaxisZentra.Persistence.MixedProduction;

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
    Task<MixedProductionResourceWorkspacePayload> GetResourceWorkspaceAsync(
        Guid facilityId, CancellationToken cancellationToken = default);
}

public sealed class MixedProductionService : IMixedProductionService
{
    private readonly NevaxisZentraDbContext db;
    private readonly TenantDatabaseSession tenantDatabaseSession;

    public MixedProductionService(
        NevaxisZentraDbContext db,
        TenantDatabaseSession tenantDatabaseSession)
    {
        this.db = db;
        this.tenantDatabaseSession = tenantDatabaseSession;
    }

    public Task<IReadOnlyList<MixedProductionLineSummary>> ListLinesAsync(
        Guid facilityId, CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(
            ct => ListLinesCoreAsync(facilityId, ct),
            cancellationToken);

    private async Task<IReadOnlyList<MixedProductionLineSummary>> ListLinesCoreAsync(
        Guid facilityId, CancellationToken cancellationToken)
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

    public Task<MixedProductionLineDetailSnapshot> GetLineDetailAsync(
        Guid facilityId, Guid lineId, CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(
            ct => GetLineDetailCoreAsync(facilityId, lineId, ct),
            cancellationToken);

    private async Task<MixedProductionLineDetailSnapshot> GetLineDetailCoreAsync(
        Guid facilityId, Guid lineId, CancellationToken cancellationToken)
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

    public Task<IReadOnlyList<MixedProductionTimelineEntry>> GetTimelineAsync(
        Guid facilityId,
        Guid lineId,
        DateTimeOffset? fromUtc,
        DateTimeOffset? toUtc,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(
            ct => GetTimelineCoreAsync(facilityId, lineId, fromUtc, toUtc, ct),
            cancellationToken);

    private async Task<IReadOnlyList<MixedProductionTimelineEntry>> GetTimelineCoreAsync(
        Guid facilityId,
        Guid lineId,
        DateTimeOffset? fromUtc,
        DateTimeOffset? toUtc,
        CancellationToken cancellationToken)
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

    public Task<MixedProductionPlaybackSnapshot> GetPlaybackAsync(
        Guid facilityId,
        Guid lineId,
        DateTimeOffset fromUtc,
        DateTimeOffset toUtc,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(
            ct => GetPlaybackCoreAsync(facilityId, lineId, fromUtc, toUtc, ct),
            cancellationToken);

    private async Task<MixedProductionPlaybackSnapshot> GetPlaybackCoreAsync(
        Guid facilityId,
        Guid lineId,
        DateTimeOffset fromUtc,
        DateTimeOffset toUtc,
        CancellationToken cancellationToken)
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

    public Task<CanonicalProductionEvent> RecordEventAsync(
        RecordProductionEventRequest request,
        Guid tenantId,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteWriteAsync(
            ct => RecordEventCoreAsync(request, tenantId, ct),
            cancellationToken);

    private async Task<CanonicalProductionEvent> RecordEventCoreAsync(
        RecordProductionEventRequest request,
        Guid tenantId,
        CancellationToken cancellationToken)
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

    public Task<OperatorStationSession> PerformOperatorActionAsync(
        OperatorActionRequest request,
        Guid tenantId,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteWriteAsync(
            ct => PerformOperatorActionCoreAsync(request, tenantId, ct),
            cancellationToken);

    private async Task<OperatorStationSession> PerformOperatorActionCoreAsync(
        OperatorActionRequest request,
        Guid tenantId,
        CancellationToken cancellationToken)
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

            await RecordEventCoreAsync(new RecordProductionEventRequest(
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

            await RecordEventCoreAsync(new RecordProductionEventRequest(
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
                await RecordEventCoreAsync(new RecordProductionEventRequest(
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
        tenantDatabaseSession.ExecuteWriteAsync(
            ct => MixedProductionFixtureSeeder.SeedMixLine01Async(db, tenantId, facilityId, ct),
            cancellationToken);

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

    public Task<MixedProductionResourceWorkspacePayload> GetResourceWorkspaceAsync(
        Guid facilityId, CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(
            ct => GetResourceWorkspaceCoreAsync(facilityId, ct),
            cancellationToken);

    private async Task<MixedProductionResourceWorkspacePayload> GetResourceWorkspaceCoreAsync(
        Guid facilityId, CancellationToken cancellationToken)
    {
        var rows = await (
            from resource in db.ManufacturingResources.AsNoTracking()
            join station in db.ProductionStations.AsNoTracking() on resource.StationId equals station.Id
            join line in db.ProductionLines.AsNoTracking() on station.LineId equals line.Id
            join area in db.ManufacturingAreas.AsNoTracking() on line.AreaId equals area.Id
            where resource.FacilityId == facilityId
                && resource.IsActive
                && station.IsActive
                && line.IsActive
            orderby line.Code, station.SequenceOrder, resource.Code
            select new { resource, station, line, area }
        ).ToListAsync(cancellationToken);

        if (rows.Count == 0)
        {
            return new MixedProductionResourceWorkspacePayload(
                [],
                new MixedProductionResourceWorkspaceStatusSummary(0, 0, 0, 0, 0, 0, 0));
        }

        var lineIds = rows.Select(row => row.line.Id).Distinct().ToList();
        var stationIds = rows.Select(row => row.station.Id).Distinct().ToList();
        var resourceIds = rows.Select(row => row.resource.Id).Distinct().ToList();

        var events = await db.CanonicalProductionEvents.AsNoTracking()
            .Where(e => e.FacilityId == facilityId && e.LineId.HasValue && lineIds.Contains(e.LineId.Value))
            .OrderByDescending(e => e.OccurredAtUtc)
            .Take(1000)
            .ToListAsync(cancellationToken);

        var buffers = await db.BufferResourceStates.AsNoTracking()
            .Where(b => resourceIds.Contains(b.ResourceId))
            .ToDictionaryAsync(b => b.ResourceId, cancellationToken);

        var sessions = await db.OperatorStationSessions.AsNoTracking()
            .Where(s => stationIds.Contains(s.StationId) && s.Status == "in-progress")
            .ToListAsync(cancellationToken);

        var resources = rows.Select(row =>
        {
            var stationEvents = events.Where(e => e.StationId == row.station.Id).ToList();
            var session = sessions.FirstOrDefault(s => s.StationId == row.station.Id);
            buffers.TryGetValue(row.resource.Id, out var buffer);
            var state = MixedProductionLineStateEngine.DeriveStationState(row.station.Id, stationEvents, session);

            return new MixedProductionResourceWorkspaceRow(
                row.resource.Id,
                row.resource.Code,
                row.resource.Name,
                row.resource.ResourceType,
                row.resource.MachineRegistryCode,
                row.station.Id,
                row.station.Code,
                row.station.Name,
                row.station.StationKind,
                row.line.Id,
                row.line.Code,
                row.line.Name,
                row.area.Code,
                state,
                stationEvents.FirstOrDefault()?.OperationCode,
                session?.OperatorCode,
                row.line.CurrentOrderReference,
                stationEvents.Count(e => e.EventType == MixedProductionEventTypes.Good),
                stationEvents.Count(e => e.EventType == MixedProductionEventTypes.Reject),
                buffer?.CurrentQuantity,
                buffer?.Capacity ?? row.resource.BufferCapacity,
                buffer?.IsStarved ?? false,
                buffer?.IsBlocked ?? false);
        }).ToList();

        var status = new MixedProductionResourceWorkspaceStatusSummary(
            resources.Count,
            resources.Count(r => IsRunningState(r.State)),
            resources.Count(r => IsIdleState(r.State)),
            resources.Count(r => IsWaitingState(r.State)),
            resources.Count(r => IsStoppedState(r.State)),
            resources.Count(r => IsFaultState(r.State)),
            resources.Count(r => r.IsStarved || r.IsBlocked || IsFaultState(r.State)));

        return new MixedProductionResourceWorkspacePayload(resources, status);
    }

    private static bool IsRunningState(string state) =>
        state is MixedProductionLineStates.Running or "RUNNING" or "running";

    private static bool IsIdleState(string state) =>
        state is "IDLE" or "idle";

    private static bool IsWaitingState(string state) =>
        state is MixedProductionLineStates.Starved or MixedProductionLineStates.Blocked
            or "STARVED" or "BLOCKED" or "starved" or "blocked";

    private static bool IsStoppedState(string state) =>
        state is MixedProductionLineStates.Stopped or "STOPPED" or "stopped";

    private static bool IsFaultState(string state) =>
        state is MixedProductionLineStates.Faulted or "FAULT" or "FAULTED" or "fault" or "faulted";
}
