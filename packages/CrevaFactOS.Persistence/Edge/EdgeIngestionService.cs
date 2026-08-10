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

            // Derive status from status-change events
            if (request.EventKind == MachineEventKinds.StatusChange && request.Payload is not null)
            {
                TryApplyStatusFromPayload(runtime, request.Payload);
            }

            runtime.UpdatedAtUtc = receivedAt;

            await dbContext.SaveChangesAsync(ct);

            return EdgeIngestResult.Success(eventId, isOutOfOrder);
        }, cancellationToken);

    private static void TryApplyStatusFromPayload(MachineRuntime runtime, string payload)
    {
        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(payload);
            if (doc.RootElement.TryGetProperty("status", out var statusEl))
            {
                var status = statusEl.GetString();
                if (!string.IsNullOrWhiteSpace(status))
                {
                    runtime.Status = status;
                    runtime.IsReadyForProduction = status == MachineStatuses.Running;
                }
            }
        }
        catch (System.Text.Json.JsonException)
        {
            // Malformed payload — status unchanged
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
