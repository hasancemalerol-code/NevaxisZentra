using Microsoft.EntityFrameworkCore;

namespace NevaxisZentra.Persistence.Edge;

public interface IEdgeCounterIngestionService
{
    Task<CounterIngestResult> IngestCounterAsync(
        EdgeOperationContext context,
        CounterIngestRequest request,
        CancellationToken cancellationToken);
}

/// <summary>
/// Processes machine.counter.observed events.
/// Counter safety:
/// - Counters may increase, reset, wrap, or arrive out of order.
/// - CounterResetSuspected flagged when lower value follows trusted newer higher value.
/// - No negative-delta interpretation as negative production.
/// - No Production mutation.
/// </summary>
public sealed class EdgeCounterIngestionService(
    NevaxisZentraDbContext dbContext,
    Tenancy.TenantDatabaseSession tenantDatabaseSession) : IEdgeCounterIngestionService
{
    public Task<CounterIngestResult> IngestCounterAsync(
        EdgeOperationContext context,
        CounterIngestRequest request,
        CancellationToken cancellationToken) =>
        tenantDatabaseSession.ExecuteWriteAsync(async ct =>
        {
            if (string.IsNullOrWhiteSpace(request.IdempotencyKey))
                return CounterIngestResult.Failure(EdgeFailureCodes.CounterInvalid, "IdempotencyKey is required.");

            if (string.IsNullOrWhiteSpace(request.CounterKey))
                return CounterIngestResult.Failure(EdgeFailureCodes.CounterInvalid, "CounterKey is required.");

            var facilityId = context.FacilityId;
            var receivedAt = DateTimeOffset.UtcNow;

            // Find the last known counter value for this machine+key (by source timestamp)
            var lastObservation = await dbContext.MachineCounterObservations
                .Where(c => c.FacilityId == facilityId
                            && c.MachineId == request.MachineId
                            && c.CounterKey == request.CounterKey)
                .OrderByDescending(c => c.SourceTimestampUtc)
                .Select(c => new { c.CounterValue, c.SourceTimestampUtc })
                .FirstOrDefaultAsync(ct);

            // Counter reset suspicion: lower value after a trusted newer higher value
            var resetSuspected = lastObservation is not null
                && request.SourceTimestampUtc >= lastObservation.SourceTimestampUtc
                && request.CounterValue < lastObservation.CounterValue;

            var observation = new MachineCounterObservation
            {
                Id = Guid.NewGuid(),
                TenantId = context.TenantId,
                FacilityId = facilityId,
                MachineId = request.MachineId,
                GatewayId = request.GatewayId,
                CounterKey = request.CounterKey,
                CounterValue = request.CounterValue,
                Quality = string.IsNullOrWhiteSpace(request.Quality) ? TelemetryQualities.Unknown : request.Quality,
                CounterResetSuspected = resetSuspected,
                SourceTimestampUtc = request.SourceTimestampUtc,
                ReceivedAtUtc = receivedAt,
                SourceSequence = request.SourceSequence,
                IdempotencyKey = request.IdempotencyKey,
                CreatedAtUtc = receivedAt,
            };
            dbContext.MachineCounterObservations.Add(observation);

            // Update signal projection latest counter (only if not a reset-suspected decrease)
            if (!resetSuspected)
            {
                await UpdateSignalProjectionCounterAsync(context, request.MachineId, facilityId, request.CounterValue, receivedAt, ct);
            }

            await dbContext.SaveChangesAsync(ct);
            return CounterIngestResult.Success(observation.Id, resetSuspected);
        }, cancellationToken);

    private async Task UpdateSignalProjectionCounterAsync(
        EdgeOperationContext context,
        Guid machineId,
        Guid facilityId,
        long counterValue,
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

        signal.LatestCounter = counterValue;
        signal.UpdatedAtUtc = now;
    }
}

public sealed class UnavailableEdgeCounterIngestionService : IEdgeCounterIngestionService
{
    public Task<CounterIngestResult> IngestCounterAsync(
        EdgeOperationContext context,
        CounterIngestRequest request,
        CancellationToken cancellationToken) =>
        Task.FromResult(CounterIngestResult.Failure(EdgeFailureCodes.InvalidScope, "Persistence is unavailable."));
}
