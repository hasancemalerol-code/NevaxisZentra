using Microsoft.EntityFrameworkCore;

namespace NevaxisZentra.Persistence.Edge;

public interface IEdgeSetupIngestionService
{
    Task<OperationalIngestResult> IngestSetupStartedAsync(
        EdgeOperationContext context,
        SetupStartIngestRequest request,
        CancellationToken cancellationToken);

    Task<OperationalIngestResult> IngestSetupCompletedAsync(
        EdgeOperationContext context,
        SetupCompleteIngestRequest request,
        CancellationToken cancellationToken);
}

/// <summary>
/// Processes machine.setup.started / machine.setup.completed observations.
/// "Gözlenen Setup / Değişim" — not official Production changeover confirmation.
/// No Production / Planning / mold lifecycle mutation.
/// </summary>
public sealed class EdgeSetupIngestionService(
    NevaxisZentraDbContext dbContext,
    Tenancy.TenantDatabaseSession tenantDatabaseSession) : IEdgeSetupIngestionService
{
    public Task<OperationalIngestResult> IngestSetupStartedAsync(
        EdgeOperationContext context,
        SetupStartIngestRequest request,
        CancellationToken cancellationToken) =>
        tenantDatabaseSession.ExecuteWriteAsync(async ct =>
        {
            if (string.IsNullOrWhiteSpace(request.IdempotencyKey))
                return OperationalIngestResult.Failure(EdgeFailureCodes.SetupInvalid, "IdempotencyKey is required.");

            var facilityId = context.FacilityId;

            var duplicate = await dbContext.MachineSetupObservations
                .Where(o => o.FacilityId == facilityId && o.IdempotencyKey == request.IdempotencyKey)
                .Select(o => new { o.Id })
                .FirstOrDefaultAsync(ct);
            if (duplicate is not null)
                return OperationalIngestResult.Duplicate(duplicate.Id);

            var receivedAt = DateTimeOffset.UtcNow;
            var observationId = Guid.NewGuid();
            var episodeId = Guid.NewGuid();
            var signal = await EnsureSignalAsync(context, request.MachineId, facilityId, receivedAt, ct);

            var isOutOfOrder = signal.LastOperationalSourceSequence.HasValue
                && request.SourceSequence.HasValue
                && request.SourceSequence.Value <= signal.LastOperationalSourceSequence.Value;

            if (!isOutOfOrder && signal.IsInObservedSetup
                && signal.ObservedSetupStartedAtUtc.HasValue
                && request.SourceTimestampUtc < signal.ObservedSetupStartedAtUtc.Value)
                isOutOfOrder = true;

            var observation = new MachineSetupObservation
            {
                Id = observationId,
                TenantId = context.TenantId,
                FacilityId = facilityId,
                MachineId = request.MachineId,
                GatewayId = request.GatewayId,
                EpisodeId = episodeId,
                EventKind = MachineEventKinds.SetupStarted,
                ExternalEpisodeId = request.ExternalEpisodeId,
                SetupType = request.SetupType,
                FromReference = request.FromReference,
                ToReference = request.ToReference,
                Quality = NormalizeQuality(request.Quality),
                Transport = request.Transport ?? TelemetryTransports.Simulator,
                SourceTimestampUtc = request.SourceTimestampUtc,
                ReceivedAtUtc = receivedAt,
                SourceSequence = request.SourceSequence,
                IdempotencyKey = request.IdempotencyKey,
                IsOutOfOrder = isOutOfOrder,
                CreatedAtUtc = receivedAt,
            };
            dbContext.MachineSetupObservations.Add(observation);

            dbContext.MachineSetupEpisodes.Add(new MachineSetupEpisode
            {
                Id = episodeId,
                TenantId = context.TenantId,
                FacilityId = facilityId,
                MachineId = request.MachineId,
                GatewayId = request.GatewayId,
                ExternalEpisodeId = request.ExternalEpisodeId,
                SetupType = request.SetupType,
                FromReference = request.FromReference,
                ToReference = request.ToReference,
                StartedAtSourceUtc = request.SourceTimestampUtc,
                StartedAtReceivedUtc = receivedAt,
                Quality = NormalizeQuality(request.Quality),
                Transport = request.Transport ?? TelemetryTransports.Simulator,
                StartSourceSequence = request.SourceSequence,
                StartIdempotencyKey = request.IdempotencyKey,
                IsClosed = false,
                IsOutOfOrder = isOutOfOrder,
                CreatedAtUtc = receivedAt,
                UpdatedAtUtc = receivedAt,
            });

            if (!isOutOfOrder)
            {
                signal.IsInObservedSetup = true;
                signal.ObservedSetupStartedAtUtc = request.SourceTimestampUtc;
                if (request.SourceSequence.HasValue)
                    signal.LastOperationalSourceSequence = request.SourceSequence.Value;
            }

            signal.UpdatedAtUtc = receivedAt;
            await dbContext.SaveChangesAsync(ct);
            return OperationalIngestResult.Success(observationId, isOutOfOrder, episodeId);
        }, cancellationToken);

    public Task<OperationalIngestResult> IngestSetupCompletedAsync(
        EdgeOperationContext context,
        SetupCompleteIngestRequest request,
        CancellationToken cancellationToken) =>
        tenantDatabaseSession.ExecuteWriteAsync(async ct =>
        {
            if (string.IsNullOrWhiteSpace(request.IdempotencyKey))
                return OperationalIngestResult.Failure(EdgeFailureCodes.SetupInvalid, "IdempotencyKey is required.");

            var facilityId = context.FacilityId;

            var duplicate = await dbContext.MachineSetupObservations
                .Where(o => o.FacilityId == facilityId && o.IdempotencyKey == request.IdempotencyKey)
                .Select(o => new { o.Id })
                .FirstOrDefaultAsync(ct);
            if (duplicate is not null)
                return OperationalIngestResult.Duplicate(duplicate.Id);

            var closedDup = await dbContext.MachineSetupEpisodes
                .Where(e => e.FacilityId == facilityId && e.CompleteIdempotencyKey == request.IdempotencyKey)
                .Select(e => new { e.Id })
                .FirstOrDefaultAsync(ct);
            if (closedDup is not null)
                return OperationalIngestResult.Duplicate(closedDup.Id);

            var receivedAt = DateTimeOffset.UtcNow;
            var observationId = Guid.NewGuid();
            var signal = await EnsureSignalAsync(context, request.MachineId, facilityId, receivedAt, ct);

            MachineSetupEpisode? openEpisode = null;
            if (!string.IsNullOrWhiteSpace(request.ExternalEpisodeId))
            {
                openEpisode = await dbContext.MachineSetupEpisodes
                    .Where(e => e.FacilityId == facilityId
                        && e.MachineId == request.MachineId
                        && e.ExternalEpisodeId == request.ExternalEpisodeId
                        && !e.IsClosed
                        && !e.IsOutOfOrder)
                    .OrderByDescending(e => e.StartedAtSourceUtc)
                    .FirstOrDefaultAsync(ct);
            }

            openEpisode ??= await dbContext.MachineSetupEpisodes
                .Where(e => e.FacilityId == facilityId
                    && e.MachineId == request.MachineId
                    && !e.IsClosed
                    && !e.IsOutOfOrder)
                .OrderByDescending(e => e.StartedAtSourceUtc)
                .FirstOrDefaultAsync(ct);

            var isOutOfOrder = signal.LastOperationalSourceSequence.HasValue
                && request.SourceSequence.HasValue
                && request.SourceSequence.Value <= signal.LastOperationalSourceSequence.Value;

            if (openEpisode is null)
            {
                dbContext.MachineSetupObservations.Add(new MachineSetupObservation
                {
                    Id = observationId,
                    TenantId = context.TenantId,
                    FacilityId = facilityId,
                    MachineId = request.MachineId,
                    GatewayId = request.GatewayId,
                    EventKind = MachineEventKinds.SetupCompleted,
                    ExternalEpisodeId = request.ExternalEpisodeId,
                    Quality = NormalizeQuality(request.Quality),
                    Transport = request.Transport ?? TelemetryTransports.Simulator,
                    SourceTimestampUtc = request.SourceTimestampUtc,
                    ReceivedAtUtc = receivedAt,
                    SourceSequence = request.SourceSequence,
                    IdempotencyKey = request.IdempotencyKey,
                    IsOutOfOrder = isOutOfOrder,
                    IsUnmatchedComplete = true,
                    CreatedAtUtc = receivedAt,
                });
                await dbContext.SaveChangesAsync(ct);
                return OperationalIngestResult.Unmatched(
                    EdgeFailureCodes.SetupUnmatchedComplete,
                    observationId,
                    "No matching open observed setup episode.");
            }

            if (openEpisode.IsClosed)
                return OperationalIngestResult.Failure(EdgeFailureCodes.SetupInvalid, "Setup episode already completed.");

            long? durationMs = null;
            if (request.SourceTimestampUtc >= openEpisode.StartedAtSourceUtc)
                durationMs = (long)(request.SourceTimestampUtc - openEpisode.StartedAtSourceUtc).TotalMilliseconds;

            dbContext.MachineSetupObservations.Add(new MachineSetupObservation
            {
                Id = observationId,
                TenantId = context.TenantId,
                FacilityId = facilityId,
                MachineId = request.MachineId,
                GatewayId = request.GatewayId,
                EpisodeId = openEpisode.Id,
                EventKind = MachineEventKinds.SetupCompleted,
                ExternalEpisodeId = request.ExternalEpisodeId ?? openEpisode.ExternalEpisodeId,
                SetupType = openEpisode.SetupType,
                FromReference = openEpisode.FromReference,
                ToReference = openEpisode.ToReference,
                Quality = NormalizeQuality(request.Quality),
                Transport = request.Transport ?? TelemetryTransports.Simulator,
                SourceTimestampUtc = request.SourceTimestampUtc,
                ReceivedAtUtc = receivedAt,
                SourceSequence = request.SourceSequence,
                IdempotencyKey = request.IdempotencyKey,
                IsOutOfOrder = isOutOfOrder,
                CreatedAtUtc = receivedAt,
            });

            if (!isOutOfOrder)
            {
                openEpisode.CompletedAtSourceUtc = request.SourceTimestampUtc;
                openEpisode.CompletedAtReceivedUtc = receivedAt;
                openEpisode.DurationMs = durationMs;
                openEpisode.CompleteSourceSequence = request.SourceSequence;
                openEpisode.CompleteIdempotencyKey = request.IdempotencyKey;
                openEpisode.IsClosed = true;
                openEpisode.UpdatedAtUtc = receivedAt;

                signal.IsInObservedSetup = false;
                signal.ObservedSetupStartedAtUtc = null;
                if (request.SourceSequence.HasValue)
                    signal.LastOperationalSourceSequence = request.SourceSequence.Value;
            }
            else
            {
                openEpisode.IsOutOfOrder = true;
                openEpisode.UpdatedAtUtc = receivedAt;
            }

            signal.UpdatedAtUtc = receivedAt;
            await dbContext.SaveChangesAsync(ct);
            return OperationalIngestResult.Success(observationId, isOutOfOrder, openEpisode.Id, durationMs: durationMs);
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

    private static string NormalizeQuality(string? quality) =>
        string.IsNullOrWhiteSpace(quality) ? TelemetryQualities.Unknown : quality;
}

public sealed class UnavailableEdgeSetupIngestionService : IEdgeSetupIngestionService
{
    public Task<OperationalIngestResult> IngestSetupStartedAsync(
        EdgeOperationContext context, SetupStartIngestRequest request, CancellationToken cancellationToken) =>
        Task.FromResult(OperationalIngestResult.Failure(EdgeFailureCodes.InvalidScope, "Persistence is unavailable."));

    public Task<OperationalIngestResult> IngestSetupCompletedAsync(
        EdgeOperationContext context, SetupCompleteIngestRequest request, CancellationToken cancellationToken) =>
        Task.FromResult(OperationalIngestResult.Failure(EdgeFailureCodes.InvalidScope, "Persistence is unavailable."));
}
