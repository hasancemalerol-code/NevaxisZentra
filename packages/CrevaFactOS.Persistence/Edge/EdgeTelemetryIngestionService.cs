using Microsoft.EntityFrameworkCore;

namespace CrevaFactOS.Persistence.Edge;

public interface IEdgeTelemetryIngestionService
{
    Task<TelemetryIngestResult> IngestTelemetryAsync(
        EdgeOperationContext context,
        TelemetryIngestRequest request,
        CancellationToken cancellationToken);
}

/// <summary>
/// Processes machine.telemetry samples.
/// Enforces:
/// - Idempotency: duplicate IdempotencyKey returns original sample id.
/// - Out-of-order safety: out-of-order samples stored in history but do not regress latest projection.
/// - No write/control commands to machines. No Production mutation.
/// </summary>
public sealed class EdgeTelemetryIngestionService(
    CrevaFactOSDbContext dbContext,
    Tenancy.TenantDatabaseSession tenantDatabaseSession) : IEdgeTelemetryIngestionService
{
    public Task<TelemetryIngestResult> IngestTelemetryAsync(
        EdgeOperationContext context,
        TelemetryIngestRequest request,
        CancellationToken cancellationToken) =>
        tenantDatabaseSession.ExecuteWriteAsync(async ct =>
        {
            if (string.IsNullOrWhiteSpace(request.IdempotencyKey))
                return TelemetryIngestResult.Failure(EdgeFailureCodes.TelemetryInvalid, "IdempotencyKey is required.");

            if (string.IsNullOrWhiteSpace(request.MetricKey))
                return TelemetryIngestResult.Failure(EdgeFailureCodes.TelemetryInvalid, "MetricKey is required.");

            if (request.NumericValue is null && request.TextValue is null && request.BooleanValue is null)
                return TelemetryIngestResult.Failure(EdgeFailureCodes.TelemetryInvalid, "At least one value (Numeric/Text/Boolean) must be populated.");

            var facilityId = context.FacilityId;

            // Duplicate check
            var duplicate = await dbContext.MachineTelemetrySamples
                .Where(t => t.FacilityId == facilityId && t.IdempotencyKey == request.IdempotencyKey)
                .Select(t => new { t.Id })
                .FirstOrDefaultAsync(ct);

            if (duplicate is not null)
                return TelemetryIngestResult.Duplicate(duplicate.Id);

            var receivedAt = DateTimeOffset.UtcNow;
            var sampleId = Guid.NewGuid();

            // Out-of-order detection against latest projection
            var latestProjection = await dbContext.MachineLatestTelemetries
                .FirstOrDefaultAsync(
                    l => l.FacilityId == facilityId
                         && l.MachineId == request.MachineId
                         && l.MetricKey == request.MetricKey,
                    ct);

            var isOutOfOrder = latestProjection?.SourceSequence.HasValue == true
                && request.SourceSequence.HasValue
                && request.SourceSequence.Value <= latestProjection.SourceSequence!.Value;

            if (!isOutOfOrder && latestProjection?.SourceTimestampUtc > request.SourceTimestampUtc)
                isOutOfOrder = true;

            var sample = new MachineTelemetrySample
            {
                Id = sampleId,
                TenantId = context.TenantId,
                FacilityId = facilityId,
                MachineId = request.MachineId,
                GatewayId = request.GatewayId,
                MetricKey = request.MetricKey,
                NumericValue = request.NumericValue,
                TextValue = request.TextValue,
                BooleanValue = request.BooleanValue,
                Unit = request.Unit,
                Quality = string.IsNullOrWhiteSpace(request.Quality) ? TelemetryQualities.Unknown : request.Quality,
                Transport = request.Transport,
                SourceTimestampUtc = request.SourceTimestampUtc,
                ReceivedAtUtc = receivedAt,
                SourceSequence = request.SourceSequence,
                IdempotencyKey = request.IdempotencyKey,
                IsDuplicate = false,
                CreatedAtUtc = receivedAt,
            };
            dbContext.MachineTelemetrySamples.Add(sample);

            // Only advance latest projection if in-order
            if (!isOutOfOrder)
            {
                if (latestProjection is null)
                {
                    latestProjection = new MachineLatestTelemetry
                    {
                        Id = Guid.NewGuid(),
                        TenantId = context.TenantId,
                        FacilityId = facilityId,
                        MachineId = request.MachineId,
                        MetricKey = request.MetricKey,
                    };
                    dbContext.MachineLatestTelemetries.Add(latestProjection);
                }

                latestProjection.NumericValue = request.NumericValue;
                latestProjection.TextValue = request.TextValue;
                latestProjection.BooleanValue = request.BooleanValue;
                latestProjection.Unit = request.Unit;
                latestProjection.Quality = sample.Quality;
                latestProjection.SourceTimestampUtc = request.SourceTimestampUtc;
                latestProjection.ReceivedAtUtc = receivedAt;
                latestProjection.SourceSequence = request.SourceSequence;
                latestProjection.UpdatedAtUtc = receivedAt;
            }

            // Update signal projection last-telemetry timestamp
            await UpdateSignalProjectionTelemetryAsync(context, request.MachineId, facilityId, sample.Quality, receivedAt, ct);

            await dbContext.SaveChangesAsync(ct);
            return TelemetryIngestResult.Success(sampleId, isOutOfOrder);
        }, cancellationToken);

    private async Task UpdateSignalProjectionTelemetryAsync(
        EdgeOperationContext context,
        Guid machineId,
        Guid facilityId,
        string quality,
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

        signal.LastTelemetryAtUtc = now;
        signal.Quality = quality;
        signal.UpdatedAtUtc = now;
    }
}

public sealed class UnavailableEdgeTelemetryIngestionService : IEdgeTelemetryIngestionService
{
    public Task<TelemetryIngestResult> IngestTelemetryAsync(
        EdgeOperationContext context,
        TelemetryIngestRequest request,
        CancellationToken cancellationToken) =>
        Task.FromResult(TelemetryIngestResult.Failure(EdgeFailureCodes.InvalidScope, "Persistence is unavailable."));
}
