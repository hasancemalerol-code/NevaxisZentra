using Microsoft.EntityFrameworkCore;

namespace CrevaFactOS.Persistence.Edge;

public interface IEdgeDowntimeIngestionService
{
    Task<OperationalIngestResult> IngestDowntimeStartAsync(
        EdgeOperationContext context,
        DowntimeStartIngestRequest request,
        CancellationToken cancellationToken);

    Task<OperationalIngestResult> IngestDowntimeEndAsync(
        EdgeOperationContext context,
        DowntimeEndIngestRequest request,
        CancellationToken cancellationToken);
}

/// <summary>
/// Processes machine.downtime.started / machine.downtime.ended observations.
/// Observed-only — no Production lifecycle mutation, no Maintenance request.
/// </summary>
public sealed class EdgeDowntimeIngestionService(
    CrevaFactOSDbContext dbContext,
    Tenancy.TenantDatabaseSession tenantDatabaseSession) : IEdgeDowntimeIngestionService
{
    public Task<OperationalIngestResult> IngestDowntimeStartAsync(
        EdgeOperationContext context,
        DowntimeStartIngestRequest request,
        CancellationToken cancellationToken) =>
        tenantDatabaseSession.ExecuteWriteAsync(async ct =>
        {
            if (string.IsNullOrWhiteSpace(request.IdempotencyKey))
                return OperationalIngestResult.Failure(EdgeFailureCodes.DowntimeInvalid, "IdempotencyKey is required.");

            var facilityId = context.FacilityId;

            var duplicate = await dbContext.MachineDowntimeObservations
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

            // Older start must not reopen a closed/current downtime state
            if (!isOutOfOrder && signal.IsInObservedDowntime
                && signal.CurrentDowntimeStartedAtUtc.HasValue
                && request.SourceTimestampUtc < signal.CurrentDowntimeStartedAtUtc.Value)
                isOutOfOrder = true;

            var observation = new MachineDowntimeObservation
            {
                Id = observationId,
                TenantId = context.TenantId,
                FacilityId = facilityId,
                MachineId = request.MachineId,
                GatewayId = request.GatewayId,
                EpisodeId = episodeId,
                EventKind = MachineEventKinds.DowntimeStarted,
                ExternalEpisodeId = request.ExternalEpisodeId,
                ReasonCode = request.ReasonCode,
                ReasonCategory = NormalizeCategory(request.ReasonCategory),
                ReasonText = request.ReasonText,
                Quality = NormalizeQuality(request.Quality),
                Transport = request.Transport ?? TelemetryTransports.Simulator,
                SourceTimestampUtc = request.SourceTimestampUtc,
                ReceivedAtUtc = receivedAt,
                SourceSequence = request.SourceSequence,
                IdempotencyKey = request.IdempotencyKey,
                IsOutOfOrder = isOutOfOrder,
                CreatedAtUtc = receivedAt,
            };
            dbContext.MachineDowntimeObservations.Add(observation);

            var episode = new MachineDowntimeEpisode
            {
                Id = episodeId,
                TenantId = context.TenantId,
                FacilityId = facilityId,
                MachineId = request.MachineId,
                GatewayId = request.GatewayId,
                ExternalEpisodeId = request.ExternalEpisodeId,
                StartedAtSourceUtc = request.SourceTimestampUtc,
                StartedAtReceivedUtc = receivedAt,
                ReasonCode = request.ReasonCode,
                ReasonCategory = NormalizeCategory(request.ReasonCategory),
                ReasonText = request.ReasonText,
                Quality = NormalizeQuality(request.Quality),
                Transport = request.Transport ?? TelemetryTransports.Simulator,
                StartSourceSequence = request.SourceSequence,
                StartIdempotencyKey = request.IdempotencyKey,
                IsClosed = false,
                IsOutOfOrder = isOutOfOrder,
                CreatedAtUtc = receivedAt,
                UpdatedAtUtc = receivedAt,
            };
            dbContext.MachineDowntimeEpisodes.Add(episode);

            if (!isOutOfOrder)
            {
                signal.IsInObservedDowntime = true;
                signal.CurrentDowntimeStartedAtUtc = request.SourceTimestampUtc;
                signal.ObservedDowntimeDurationMs = null;
                if (request.SourceSequence.HasValue)
                    signal.LastOperationalSourceSequence = request.SourceSequence.Value;
            }

            signal.UpdatedAtUtc = receivedAt;
            await dbContext.SaveChangesAsync(ct);
            return OperationalIngestResult.Success(observationId, isOutOfOrder, episodeId);
        }, cancellationToken);

    public Task<OperationalIngestResult> IngestDowntimeEndAsync(
        EdgeOperationContext context,
        DowntimeEndIngestRequest request,
        CancellationToken cancellationToken) =>
        tenantDatabaseSession.ExecuteWriteAsync(async ct =>
        {
            if (string.IsNullOrWhiteSpace(request.IdempotencyKey))
                return OperationalIngestResult.Failure(EdgeFailureCodes.DowntimeInvalid, "IdempotencyKey is required.");

            var facilityId = context.FacilityId;

            var duplicate = await dbContext.MachineDowntimeObservations
                .Where(o => o.FacilityId == facilityId && o.IdempotencyKey == request.IdempotencyKey)
                .Select(o => new { o.Id })
                .FirstOrDefaultAsync(ct);
            if (duplicate is not null)
                return OperationalIngestResult.Duplicate(duplicate.Id);

            // Duplicate end via EndIdempotencyKey on already-closed episode
            if (!string.IsNullOrWhiteSpace(request.IdempotencyKey))
            {
                var closedDup = await dbContext.MachineDowntimeEpisodes
                    .Where(e => e.FacilityId == facilityId
                        && e.EndIdempotencyKey == request.IdempotencyKey)
                    .Select(e => new { e.Id })
                    .FirstOrDefaultAsync(ct);
                if (closedDup is not null)
                    return OperationalIngestResult.Duplicate(closedDup.Id);
            }

            var receivedAt = DateTimeOffset.UtcNow;
            var observationId = Guid.NewGuid();
            var signal = await EnsureSignalAsync(context, request.MachineId, facilityId, receivedAt, ct);

            MachineDowntimeEpisode? openEpisode = null;
            if (!string.IsNullOrWhiteSpace(request.ExternalEpisodeId))
            {
                openEpisode = await dbContext.MachineDowntimeEpisodes
                    .Where(e => e.FacilityId == facilityId
                        && e.MachineId == request.MachineId
                        && e.ExternalEpisodeId == request.ExternalEpisodeId
                        && !e.IsClosed
                        && !e.IsOutOfOrder)
                    .OrderByDescending(e => e.StartedAtSourceUtc)
                    .FirstOrDefaultAsync(ct);
            }

            openEpisode ??= await dbContext.MachineDowntimeEpisodes
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
                var unmatched = new MachineDowntimeObservation
                {
                    Id = observationId,
                    TenantId = context.TenantId,
                    FacilityId = facilityId,
                    MachineId = request.MachineId,
                    GatewayId = request.GatewayId,
                    EventKind = MachineEventKinds.DowntimeEnded,
                    ExternalEpisodeId = request.ExternalEpisodeId,
                    ReasonCode = request.ReasonCode,
                    ReasonCategory = NormalizeCategory(request.ReasonCategory),
                    ReasonText = request.ReasonText,
                    Quality = NormalizeQuality(request.Quality),
                    Transport = request.Transport ?? TelemetryTransports.Simulator,
                    SourceTimestampUtc = request.SourceTimestampUtc,
                    ReceivedAtUtc = receivedAt,
                    SourceSequence = request.SourceSequence,
                    IdempotencyKey = request.IdempotencyKey,
                    IsOutOfOrder = isOutOfOrder,
                    IsUnmatchedEnd = true,
                    CreatedAtUtc = receivedAt,
                };
                dbContext.MachineDowntimeObservations.Add(unmatched);
                await dbContext.SaveChangesAsync(ct);
                return OperationalIngestResult.Unmatched(
                    EdgeFailureCodes.DowntimeUnmatchedEnd,
                    observationId,
                    "No matching open observed downtime episode.");
            }

            // Do not close twice
            if (openEpisode.IsClosed)
                return OperationalIngestResult.Failure(EdgeFailureCodes.DowntimeInvalid, "Episode already closed.");

            long? durationMs = null;
            if (request.SourceTimestampUtc >= openEpisode.StartedAtSourceUtc)
                durationMs = (long)(request.SourceTimestampUtc - openEpisode.StartedAtSourceUtc).TotalMilliseconds;

            var isMicroStop = durationMs.HasValue
                && durationMs.Value >= 0
                && durationMs.Value <= EdgeObservationDefaults.MicroStopThresholdMs;

            var observation = new MachineDowntimeObservation
            {
                Id = observationId,
                TenantId = context.TenantId,
                FacilityId = facilityId,
                MachineId = request.MachineId,
                GatewayId = request.GatewayId,
                EpisodeId = openEpisode.Id,
                EventKind = MachineEventKinds.DowntimeEnded,
                ExternalEpisodeId = request.ExternalEpisodeId ?? openEpisode.ExternalEpisodeId,
                ReasonCode = request.ReasonCode ?? openEpisode.ReasonCode,
                ReasonCategory = NormalizeCategory(request.ReasonCategory ?? openEpisode.ReasonCategory),
                ReasonText = request.ReasonText ?? openEpisode.ReasonText,
                Quality = NormalizeQuality(request.Quality),
                Transport = request.Transport ?? TelemetryTransports.Simulator,
                SourceTimestampUtc = request.SourceTimestampUtc,
                ReceivedAtUtc = receivedAt,
                SourceSequence = request.SourceSequence,
                IdempotencyKey = request.IdempotencyKey,
                IsOutOfOrder = isOutOfOrder,
                CreatedAtUtc = receivedAt,
            };
            dbContext.MachineDowntimeObservations.Add(observation);

            if (!isOutOfOrder)
            {
                openEpisode.EndedAtSourceUtc = request.SourceTimestampUtc;
                openEpisode.EndedAtReceivedUtc = receivedAt;
                openEpisode.DurationMs = durationMs;
                openEpisode.IsMicroStop = isMicroStop;
                openEpisode.EndSourceSequence = request.SourceSequence;
                openEpisode.EndIdempotencyKey = request.IdempotencyKey;
                openEpisode.IsClosed = true;
                if (!string.IsNullOrWhiteSpace(request.ReasonCode))
                    openEpisode.ReasonCode = request.ReasonCode;
                if (!string.IsNullOrWhiteSpace(request.ReasonCategory))
                    openEpisode.ReasonCategory = NormalizeCategory(request.ReasonCategory);
                if (!string.IsNullOrWhiteSpace(request.ReasonText))
                    openEpisode.ReasonText = request.ReasonText;
                openEpisode.UpdatedAtUtc = receivedAt;

                signal.IsInObservedDowntime = false;
                signal.CurrentDowntimeStartedAtUtc = null;
                signal.ObservedDowntimeDurationMs = durationMs;
                if (request.SourceSequence.HasValue)
                    signal.LastOperationalSourceSequence = request.SourceSequence.Value;
            }
            else
            {
                // Historical end stored; current downtime state must not regress incorrectly
                openEpisode.IsOutOfOrder = true;
                openEpisode.UpdatedAtUtc = receivedAt;
            }

            signal.UpdatedAtUtc = receivedAt;
            await dbContext.SaveChangesAsync(ct);
            return OperationalIngestResult.Success(
                observationId, isOutOfOrder, openEpisode.Id, isMicroStop, durationMs);
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

    private static string? NormalizeCategory(string? category)
    {
        if (string.IsNullOrWhiteSpace(category)) return DowntimeReasonCategories.Unknown;
        return category.Trim().ToLowerInvariant() switch
        {
            DowntimeReasonCategories.Unknown => DowntimeReasonCategories.Unknown,
            DowntimeReasonCategories.Machine => DowntimeReasonCategories.Machine,
            DowntimeReasonCategories.Material => DowntimeReasonCategories.Material,
            DowntimeReasonCategories.Operator => DowntimeReasonCategories.Operator,
            DowntimeReasonCategories.Quality => DowntimeReasonCategories.Quality,
            DowntimeReasonCategories.Setup => DowntimeReasonCategories.Setup,
            DowntimeReasonCategories.External => DowntimeReasonCategories.External,
            DowntimeReasonCategories.Planned => DowntimeReasonCategories.Planned,
            DowntimeReasonCategories.Other => DowntimeReasonCategories.Other,
            _ => DowntimeReasonCategories.Other,
        };
    }
}

public sealed class UnavailableEdgeDowntimeIngestionService : IEdgeDowntimeIngestionService
{
    public Task<OperationalIngestResult> IngestDowntimeStartAsync(
        EdgeOperationContext context, DowntimeStartIngestRequest request, CancellationToken cancellationToken) =>
        Task.FromResult(OperationalIngestResult.Failure(EdgeFailureCodes.InvalidScope, "Persistence is unavailable."));

    public Task<OperationalIngestResult> IngestDowntimeEndAsync(
        EdgeOperationContext context, DowntimeEndIngestRequest request, CancellationToken cancellationToken) =>
        Task.FromResult(OperationalIngestResult.Failure(EdgeFailureCodes.InvalidScope, "Persistence is unavailable."));
}
