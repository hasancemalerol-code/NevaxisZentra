using Microsoft.EntityFrameworkCore;

namespace CrevaFactOS.Persistence.Edge;

public interface IEdgeIngestionService
{
    Task<EdgeIngestResult> IngestAsync(
        EdgeOperationContext context,
        EdgeIngestRequest request,
        CancellationToken cancellationToken);
}

/// <summary>
/// Processes incoming machine events. Enforces:
/// - Idempotency: duplicate IdempotencyKey returns the original event id.
/// - Sequence tracking: detects out-of-order events via SourceSequence.
/// - Runtime projection: persists last-seen/status so dashboards survive API restarts.
/// Does NOT issue write/control commands to machines.
/// </summary>
public sealed class EdgeIngestionService(
    CrevaFactOSDbContext dbContext,
    Tenancy.TenantDatabaseSession tenantDatabaseSession) : IEdgeIngestionService
{
    public Task<EdgeIngestResult> IngestAsync(
        EdgeOperationContext context,
        EdgeIngestRequest request,
        CancellationToken cancellationToken) =>
        tenantDatabaseSession.ExecuteWriteAsync(async ct =>
        {
            if (string.IsNullOrWhiteSpace(request.IdempotencyKey))
            {
                return EdgeIngestResult.Failure(EdgeFailureCodes.ValidationFailed, "IdempotencyKey is required.");
            }

            if (string.IsNullOrWhiteSpace(request.EventKind))
            {
                return EdgeIngestResult.Failure(EdgeFailureCodes.ValidationFailed, "EventKind is required.");
            }

            var facilityId = context.FacilityId;

            // Duplicate check — IdempotencyKey is globally unique per tenant/facility
            var duplicate = await dbContext.MachineEvents
                .Where(e => e.FacilityId == facilityId && e.IdempotencyKey == request.IdempotencyKey)
                .Select(e => new { e.Id })
                .FirstOrDefaultAsync(ct);

            if (duplicate is not null)
            {
                return EdgeIngestResult.Duplicate(duplicate.Id);
            }

            var receivedAt = DateTimeOffset.UtcNow;
            var eventId = Guid.NewGuid();

            // Out-of-order detection against persisted runtime
            var runtime = await dbContext.MachineRuntimes
                .FirstOrDefaultAsync(
                    r => r.FacilityId == facilityId && r.MachineId == request.MachineId,
                    ct);

            var isOutOfOrder = runtime?.LastSourceSequence.HasValue == true
                && request.SourceSequence <= runtime.LastSourceSequence!.Value;

            var machineEvent = new MachineEvent
            {
                Id = eventId,
                TenantId = context.TenantId,
                FacilityId = facilityId,
                MachineId = request.MachineId,
                GatewayId = request.GatewayId,
                EventKind = request.EventKind,
                SourceTimestampUtc = request.SourceTimestampUtc,
                ReceivedAtUtc = receivedAt,
                SourceSequence = request.SourceSequence,
                IdempotencyKey = request.IdempotencyKey,
                Payload = request.Payload,
                IsDuplicate = false,
                DuplicateOfIdempotencyKey = null,
            };
            dbContext.MachineEvents.Add(machineEvent);

            if (runtime is null)
            {
                runtime = new MachineRuntime
                {
                    Id = Guid.NewGuid(),
                    TenantId = context.TenantId,
                    FacilityId = facilityId,
                    MachineId = request.MachineId,
                    Status = MachineStatuses.Unknown,
                    IsReadyForProduction = false,
                };
                dbContext.MachineRuntimes.Add(runtime);
            }

            runtime.LastSeenAtUtc = receivedAt;
            runtime.LastEventId = eventId;
            runtime.LastSequenceOutOfOrder = isOutOfOrder;

            if (!isOutOfOrder)
            {
                runtime.LastSourceSequence = request.SourceSequence;
            }

            // Derive status from status-change events and record EDGE-2 state transitions
            string? newStatus = null;
            string? transitionReason = null;
            if (request.EventKind == MachineEventKinds.StatusChange && request.Payload is not null)
            {
                (newStatus, transitionReason) = TryParseStatusPayload(request.Payload);
                if (newStatus is not null && !isOutOfOrder)
                {
                    runtime.Status = newStatus;
                    runtime.IsReadyForProduction = newStatus == MachineStatuses.Running;
                }
            }

            runtime.UpdatedAtUtc = receivedAt;

            // EDGE-2: Record state transition when status changes (status-change event)
            if (request.EventKind == MachineEventKinds.StatusChange && newStatus is not null)
            {
                var fromStatus = runtime.Status == newStatus ? MachineStatuses.Unknown : runtime.Status;
                var transition = new MachineStateTransition
                {
                    Id = Guid.NewGuid(),
                    TenantId = context.TenantId,
                    FacilityId = facilityId,
                    MachineId = request.MachineId,
                    FromState = fromStatus,
                    ToState = newStatus,
                    Reason = transitionReason,
                    SourceTimestampUtc = request.SourceTimestampUtc,
                    ReceivedAtUtc = receivedAt,
                    SourceSequence = request.SourceSequence,
                    IsOutOfOrder = isOutOfOrder,
                    CreatedAtUtc = receivedAt,
                };
                dbContext.MachineStateTransitions.Add(transition);
            }

            // EDGE-2: Update signal projection connectivity/state
            await UpdateSignalProjectionStateAsync(context, request.MachineId, facilityId,
                runtime.Status, isOutOfOrder, receivedAt, ct);

            await dbContext.SaveChangesAsync(ct);

            return EdgeIngestResult.Success(eventId, isOutOfOrder);
        }, cancellationToken);

    private async Task UpdateSignalProjectionStateAsync(
        EdgeOperationContext context,
        Guid machineId,
        Guid facilityId,
        string currentStatus,
        bool isOutOfOrder,
        DateTimeOffset now,
        CancellationToken ct)
    {
        var signal = await dbContext.MachineSignalProjections
            .FirstOrDefaultAsync(s => s.FacilityId == facilityId && s.MachineId == machineId, ct);

        if (signal is null)
        {
            signal = new MachineSignalProjection
            {
                Id = Guid.NewGuid(),
                TenantId = context.TenantId,
                FacilityId = facilityId,
                MachineId = machineId,
            };
            dbContext.MachineSignalProjections.Add(signal);
        }

        signal.Connectivity = currentStatus is "offline" or "unknown" ? "offline" : "online";
        if (!isOutOfOrder)
            signal.ObservedState = currentStatus;
        signal.UpdatedAtUtc = now;
    }

    private static (string? Status, string? Reason) TryParseStatusPayload(string payload)
    {
        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(payload);
            string? status = null;
            string? reason = null;
            if (doc.RootElement.TryGetProperty("status", out var statusEl))
                status = statusEl.GetString();
            if (doc.RootElement.TryGetProperty("reason", out var reasonEl))
                reason = reasonEl.GetString();
            return (!string.IsNullOrWhiteSpace(status) ? status : null, reason);
        }
        catch (System.Text.Json.JsonException)
        {
            return (null, null);
        }
    }
}

public sealed class UnavailableEdgeIngestionService : IEdgeIngestionService
{
    public Task<EdgeIngestResult> IngestAsync(
        EdgeOperationContext context,
        EdgeIngestRequest request,
        CancellationToken cancellationToken) =>
        Task.FromResult(EdgeIngestResult.Failure(EdgeFailureCodes.InvalidScope, "Persistence is unavailable."));
}
