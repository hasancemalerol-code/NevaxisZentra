using Microsoft.EntityFrameworkCore;

namespace NevaxisFactOS.Persistence.Edge;

public interface IEdgeCycleIngestionService
{
    Task<CycleIngestResult> IngestCycleAsync(
        EdgeOperationContext context,
        CycleIngestRequest request,
        CancellationToken cancellationToken);
}

/// <summary>
/// Processes machine.cycle.completed events.
/// Enforces:
/// - Idempotency: duplicate does NOT increment ObservedCycleCount.
/// - Duration validation: > 0, bounded sane max (10 hours = 36,000,000 ms).
/// - Out-of-order: stored historically but latest cycle projection not regressed.
/// - ObservedCycleCount is NOT official Production quantity.
/// - No Production mutation.
/// </summary>
public sealed class EdgeCycleIngestionService(
    NevaxisFactOSDbContext dbContext,
    Tenancy.TenantDatabaseSession tenantDatabaseSession) : IEdgeCycleIngestionService
{
    private const long MaxSaneCycleDurationMs = 36_000_000L; // 10 hours

    public Task<CycleIngestResult> IngestCycleAsync(
        EdgeOperationContext context,
        CycleIngestRequest request,
        CancellationToken cancellationToken) =>
        tenantDatabaseSession.ExecuteWriteAsync(async ct =>
        {
            if (string.IsNullOrWhiteSpace(request.IdempotencyKey))
                return CycleIngestResult.Failure(EdgeFailureCodes.CycleInvalid, "IdempotencyKey is required.");

            if (string.IsNullOrWhiteSpace(request.CycleIdentity))
                return CycleIngestResult.Failure(EdgeFailureCodes.CycleInvalid, "CycleIdentity is required.");

            if (request.CycleDurationMs <= 0)
                return CycleIngestResult.Failure(EdgeFailureCodes.CycleInvalid, "CycleDurationMs must be > 0.");

            if (request.CycleDurationMs > MaxSaneCycleDurationMs)
                return CycleIngestResult.Failure(EdgeFailureCodes.CycleInvalid,
                    $"CycleDurationMs exceeds sane maximum of {MaxSaneCycleDurationMs} ms.");

            var facilityId = context.FacilityId;

            // Duplicate check
            var duplicate = await dbContext.MachineObservedCycles
                .Where(c => c.FacilityId == facilityId && c.IdempotencyKey == request.IdempotencyKey)
                .Select(c => new { c.Id })
                .FirstOrDefaultAsync(ct);

            if (duplicate is not null)
                return CycleIngestResult.Duplicate(duplicate.Id);

            var receivedAt = DateTimeOffset.UtcNow;
            var cycleId = Guid.NewGuid();

            // Out-of-order detection against projection
            var projection = await dbContext.MachineCycleProjections
                .FirstOrDefaultAsync(p => p.FacilityId == facilityId && p.MachineId == request.MachineId, ct);

            var isOutOfOrder = projection?.LastSourceSequence.HasValue == true
                && request.SourceSequence.HasValue
                && request.SourceSequence.Value <= projection.LastSourceSequence!.Value;

            if (!isOutOfOrder && projection?.LastCycleAtUtc.HasValue == true
                && projection.LastCycleAtUtc.Value > request.SourceTimestampUtc)
                isOutOfOrder = true;

            var cycle = new MachineObservedCycle
            {
                Id = cycleId,
                TenantId = context.TenantId,
                FacilityId = facilityId,
                MachineId = request.MachineId,
                GatewayId = request.GatewayId,
                CycleIdentity = request.CycleIdentity,
                CycleDurationMs = request.CycleDurationMs,
                ObservedCounter = request.ObservedCounter,
                Quality = string.IsNullOrWhiteSpace(request.Quality) ? TelemetryQualities.Unknown : request.Quality,
                SourceTimestampUtc = request.SourceTimestampUtc,
                ReceivedAtUtc = receivedAt,
                SourceSequence = request.SourceSequence,
                IdempotencyKey = request.IdempotencyKey,
                IsDuplicate = false,
                IsOutOfOrder = isOutOfOrder,
                CreatedAtUtc = receivedAt,
            };
            dbContext.MachineObservedCycles.Add(cycle);

            // Update projection — only advance if in-order (no OEE, no Production mutation)
            if (projection is null)
            {
                projection = new MachineCycleProjection
                {
                    Id = Guid.NewGuid(),
                    TenantId = context.TenantId,
                    FacilityId = facilityId,
                    MachineId = request.MachineId,
                };
                dbContext.MachineCycleProjections.Add(projection);
            }

            if (!isOutOfOrder)
            {
                projection.ObservedCycleCount++;
                projection.LastCycleAtUtc = request.SourceTimestampUtc;
                projection.LastCycleDurationMs = request.CycleDurationMs;
                if (request.ObservedCounter.HasValue)
                    projection.LatestObservedCounter = request.ObservedCounter.Value;
                if (request.SourceSequence.HasValue)
                    projection.LastSourceSequence = request.SourceSequence.Value;

                // Rolling average of last cycle duration (simple exponential approximation)
                projection.AverageRecentCycleDurationMs = projection.AverageRecentCycleDurationMs is null
                    ? request.CycleDurationMs
                    : (projection.AverageRecentCycleDurationMs * 3 + request.CycleDurationMs) / 4;
            }

            projection.UpdatedAtUtc = receivedAt;

            // Update signal projection
            await UpdateSignalProjectionCycleAsync(context, request.MachineId, facilityId, projection, receivedAt, isOutOfOrder, ct);

            await dbContext.SaveChangesAsync(ct);
            return CycleIngestResult.Success(cycleId, isOutOfOrder);
        }, cancellationToken);

    private async Task UpdateSignalProjectionCycleAsync(
        EdgeOperationContext context,
        Guid machineId,
        Guid facilityId,
        MachineCycleProjection proj,
        DateTimeOffset now,
        bool isOutOfOrder,
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

        if (!isOutOfOrder)
        {
            signal.LastCycleAtUtc = proj.LastCycleAtUtc;
            signal.LastCycleDurationMs = proj.LastCycleDurationMs;
            signal.ObservedCycleCount = proj.ObservedCycleCount;
            if (proj.LatestObservedCounter.HasValue)
                signal.LatestCounter = proj.LatestObservedCounter.Value;
        }

        signal.UpdatedAtUtc = now;
    }
}

public sealed class UnavailableEdgeCycleIngestionService : IEdgeCycleIngestionService
{
    public Task<CycleIngestResult> IngestCycleAsync(
        EdgeOperationContext context,
        CycleIngestRequest request,
        CancellationToken cancellationToken) =>
        Task.FromResult(CycleIngestResult.Failure(EdgeFailureCodes.InvalidScope, "Persistence is unavailable."));
}
