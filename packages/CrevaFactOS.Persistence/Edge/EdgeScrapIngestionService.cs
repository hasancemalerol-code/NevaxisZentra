using Microsoft.EntityFrameworkCore;

namespace CrevaFactOS.Persistence.Edge;

public interface IEdgeScrapIngestionService
{
    Task<OperationalIngestResult> IngestScrapObservedAsync(
        EdgeOperationContext context,
        ScrapIngestRequest request,
        CancellationToken cancellationToken);
}

/// <summary>
/// Processes machine.scrap.observed.
/// Observed / machine-reported only — NOT official Production scrap, NOT Quality disposition.
/// Does not update Production quantity, create NCR, hold, inventory, or ERP movement.
/// </summary>
public sealed class EdgeScrapIngestionService(
    CrevaFactOSDbContext dbContext,
    Tenancy.TenantDatabaseSession tenantDatabaseSession) : IEdgeScrapIngestionService
{
    public Task<OperationalIngestResult> IngestScrapObservedAsync(
        EdgeOperationContext context,
        ScrapIngestRequest request,
        CancellationToken cancellationToken) =>
        tenantDatabaseSession.ExecuteWriteAsync(async ct =>
        {
            if (string.IsNullOrWhiteSpace(request.IdempotencyKey))
                return OperationalIngestResult.Failure(EdgeFailureCodes.ScrapInvalid, "IdempotencyKey is required.");
            if (string.IsNullOrWhiteSpace(request.ExternalObservationId))
                return OperationalIngestResult.Failure(EdgeFailureCodes.ScrapInvalid, "ExternalObservationId is required.");
            if (request.Quantity <= 0)
                return OperationalIngestResult.Failure(EdgeFailureCodes.ScrapInvalid, "Quantity must be > 0.");
            if (string.IsNullOrWhiteSpace(request.Unit))
                return OperationalIngestResult.Failure(EdgeFailureCodes.ScrapInvalid, "Unit is required.");

            var facilityId = context.FacilityId;

            var duplicate = await dbContext.MachineScrapObservations
                .Where(o => o.FacilityId == facilityId && o.IdempotencyKey == request.IdempotencyKey)
                .Select(o => new { o.Id })
                .FirstOrDefaultAsync(ct);
            if (duplicate is not null)
                return OperationalIngestResult.Duplicate(duplicate.Id);

            var receivedAt = DateTimeOffset.UtcNow;
            var observationId = Guid.NewGuid();
            var signal = await EnsureSignalAsync(context, request.MachineId, facilityId, receivedAt, ct);

            var isOutOfOrder = signal.LastOperationalSourceSequence.HasValue
                && request.SourceSequence.HasValue
                && request.SourceSequence.Value <= signal.LastOperationalSourceSequence.Value;

            if (!isOutOfOrder
                && signal.LastObservedScrapAtUtc.HasValue
                && request.SourceTimestampUtc < signal.LastObservedScrapAtUtc.Value)
                isOutOfOrder = true;

            dbContext.MachineScrapObservations.Add(new MachineScrapObservation
            {
                Id = observationId,
                TenantId = context.TenantId,
                FacilityId = facilityId,
                MachineId = request.MachineId,
                GatewayId = request.GatewayId,
                ExternalObservationId = request.ExternalObservationId,
                Quantity = request.Quantity,
                Unit = request.Unit.Trim(),
                ReasonCode = request.ReasonCode,
                ReasonCategory = request.ReasonCategory,
                ReasonText = request.ReasonText,
                Quality = string.IsNullOrWhiteSpace(request.Quality) ? TelemetryQualities.Unknown : request.Quality,
                Transport = request.Transport ?? TelemetryTransports.Simulator,
                SourceTimestampUtc = request.SourceTimestampUtc,
                ReceivedAtUtc = receivedAt,
                SourceSequence = request.SourceSequence,
                IdempotencyKey = request.IdempotencyKey,
                IsOutOfOrder = isOutOfOrder,
                CreatedAtUtc = receivedAt,
            });

            // Historical scrap retained; latest projection must not regress
            if (!isOutOfOrder)
            {
                signal.ObservedScrapCount++;
                signal.ObservedScrapQuantity += request.Quantity;
                signal.LastObservedScrapAtUtc = request.SourceTimestampUtc;
                if (request.SourceSequence.HasValue)
                    signal.LastOperationalSourceSequence = request.SourceSequence.Value;
            }

            signal.UpdatedAtUtc = receivedAt;
            await dbContext.SaveChangesAsync(ct);
            return OperationalIngestResult.Success(observationId, isOutOfOrder);
        }, cancellationToken);

    private async Task<MachineSignalProjection> EnsureSignalAsync(
        EdgeOperationContext context,
        Guid machineId,
        Guid facilityId,
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
                UpdatedAtUtc = now,
            };
            dbContext.MachineSignalProjections.Add(signal);
        }

        return signal;
    }
}

public sealed class UnavailableEdgeScrapIngestionService : IEdgeScrapIngestionService
{
    public Task<OperationalIngestResult> IngestScrapObservedAsync(
        EdgeOperationContext context, ScrapIngestRequest request, CancellationToken cancellationToken) =>
        Task.FromResult(OperationalIngestResult.Failure(EdgeFailureCodes.InvalidScope, "Persistence is unavailable."));
}
