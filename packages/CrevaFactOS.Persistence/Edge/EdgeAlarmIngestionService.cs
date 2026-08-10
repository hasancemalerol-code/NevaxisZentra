using Microsoft.EntityFrameworkCore;

namespace CrevaFactOS.Persistence.Edge;

public interface IEdgeAlarmIngestionService
{
    Task<OperationalIngestResult> IngestAlarmRaisedAsync(
        EdgeOperationContext context,
        AlarmRaiseIngestRequest request,
        CancellationToken cancellationToken);

    Task<OperationalIngestResult> IngestAlarmClearedAsync(
        EdgeOperationContext context,
        AlarmClearIngestRequest request,
        CancellationToken cancellationToken);
}

/// <summary>
/// Processes machine.alarm.raised / machine.alarm.cleared observations.
/// No Maintenance WO, no PLC acknowledge, no Production state change.
/// </summary>
public sealed class EdgeAlarmIngestionService(
    CrevaFactOSDbContext dbContext,
    Tenancy.TenantDatabaseSession tenantDatabaseSession) : IEdgeAlarmIngestionService
{
    private static readonly HashSet<string> ValidSeverities =
    [
        AlarmSeverities.Info,
        AlarmSeverities.Warning,
        AlarmSeverities.Error,
        AlarmSeverities.Critical,
        AlarmSeverities.Unknown,
    ];

    public Task<OperationalIngestResult> IngestAlarmRaisedAsync(
        EdgeOperationContext context,
        AlarmRaiseIngestRequest request,
        CancellationToken cancellationToken) =>
        tenantDatabaseSession.ExecuteWriteAsync(async ct =>
        {
            if (string.IsNullOrWhiteSpace(request.IdempotencyKey))
                return OperationalIngestResult.Failure(EdgeFailureCodes.AlarmInvalid, "IdempotencyKey is required.");
            if (string.IsNullOrWhiteSpace(request.AlarmIdentity))
                return OperationalIngestResult.Failure(EdgeFailureCodes.AlarmInvalid, "AlarmIdentity is required.");
            if (string.IsNullOrWhiteSpace(request.ExternalAlarmCode))
                return OperationalIngestResult.Failure(EdgeFailureCodes.AlarmInvalid, "ExternalAlarmCode is required.");

            var severity = string.IsNullOrWhiteSpace(request.Severity)
                ? AlarmSeverities.Unknown
                : request.Severity.Trim().ToLowerInvariant();
            if (!ValidSeverities.Contains(severity))
                return OperationalIngestResult.Failure(EdgeFailureCodes.AlarmInvalid, "Invalid severity.");

            var facilityId = context.FacilityId;

            var duplicate = await dbContext.MachineAlarmObservations
                .Where(o => o.FacilityId == facilityId && o.IdempotencyKey == request.IdempotencyKey)
                .Select(o => new { o.Id })
                .FirstOrDefaultAsync(ct);
            if (duplicate is not null)
                return OperationalIngestResult.Duplicate(duplicate.Id);

            var receivedAt = DateTimeOffset.UtcNow;
            var observationId = Guid.NewGuid();
            var projection = await EnsureAlarmProjectionAsync(context, request.MachineId, facilityId, receivedAt, ct);
            var signal = await EnsureSignalAsync(context, request.MachineId, facilityId, receivedAt, ct);

            var isOutOfOrder = projection.LastSourceSequence.HasValue
                && request.SourceSequence.HasValue
                && request.SourceSequence.Value <= projection.LastSourceSequence.Value;

            var observation = new MachineAlarmObservation
            {
                Id = observationId,
                TenantId = context.TenantId,
                FacilityId = facilityId,
                MachineId = request.MachineId,
                GatewayId = request.GatewayId,
                AlarmIdentity = request.AlarmIdentity,
                ExternalAlarmCode = request.ExternalAlarmCode,
                AlarmText = request.AlarmText,
                Severity = severity,
                EventKind = MachineEventKinds.AlarmRaised,
                SourceTimestampUtc = request.SourceTimestampUtc,
                ReceivedAtUtc = receivedAt,
                SourceSequence = request.SourceSequence,
                Quality = NormalizeQuality(request.Quality),
                Transport = request.Transport ?? TelemetryTransports.Simulator,
                IdempotencyKey = request.IdempotencyKey,
                IsOutOfOrder = isOutOfOrder,
                CreatedAtUtc = receivedAt,
            };
            dbContext.MachineAlarmObservations.Add(observation);

            if (!isOutOfOrder)
            {
                var existingActive = await dbContext.MachineActiveAlarms
                    .FirstOrDefaultAsync(a =>
                        a.FacilityId == facilityId
                        && a.MachineId == request.MachineId
                        && a.AlarmIdentity == request.AlarmIdentity, ct);

                if (existingActive is null)
                {
                    dbContext.MachineActiveAlarms.Add(new MachineActiveAlarm
                    {
                        Id = Guid.NewGuid(),
                        TenantId = context.TenantId,
                        FacilityId = facilityId,
                        MachineId = request.MachineId,
                        AlarmIdentity = request.AlarmIdentity,
                        ExternalAlarmCode = request.ExternalAlarmCode,
                        AlarmText = request.AlarmText,
                        Severity = severity,
                        RaisedAtSourceUtc = request.SourceTimestampUtc,
                        RaisedAtReceivedUtc = receivedAt,
                        SourceSequence = request.SourceSequence,
                        Quality = NormalizeQuality(request.Quality),
                        RaisedObservationId = observationId,
                    });
                }
                else
                {
                    existingActive.ExternalAlarmCode = request.ExternalAlarmCode;
                    existingActive.AlarmText = request.AlarmText;
                    existingActive.Severity = severity;
                    existingActive.RaisedAtSourceUtc = request.SourceTimestampUtc;
                    existingActive.RaisedAtReceivedUtc = receivedAt;
                    existingActive.SourceSequence = request.SourceSequence;
                    existingActive.Quality = NormalizeQuality(request.Quality);
                    existingActive.RaisedObservationId = observationId;
                }

                await dbContext.SaveChangesAsync(ct);
                await RefreshAlarmProjectionAsync(projection, facilityId, request.MachineId, receivedAt, ct);
                if (request.SourceSequence.HasValue)
                {
                    projection.LastSourceSequence = request.SourceSequence.Value;
                    signal.LastOperationalSourceSequence = request.SourceSequence.Value;
                }

                signal.ActiveAlarmCount = projection.ActiveAlarmCount;
                signal.HighestAlarmSeverity = projection.HighestSeverity;
            }

            projection.UpdatedAtUtc = receivedAt;
            signal.UpdatedAtUtc = receivedAt;
            await dbContext.SaveChangesAsync(ct);
            return OperationalIngestResult.Success(observationId, isOutOfOrder);
        }, cancellationToken);

    public Task<OperationalIngestResult> IngestAlarmClearedAsync(
        EdgeOperationContext context,
        AlarmClearIngestRequest request,
        CancellationToken cancellationToken) =>
        tenantDatabaseSession.ExecuteWriteAsync(async ct =>
        {
            if (string.IsNullOrWhiteSpace(request.IdempotencyKey))
                return OperationalIngestResult.Failure(EdgeFailureCodes.AlarmInvalid, "IdempotencyKey is required.");
            if (string.IsNullOrWhiteSpace(request.AlarmIdentity))
                return OperationalIngestResult.Failure(EdgeFailureCodes.AlarmInvalid, "AlarmIdentity is required.");

            var facilityId = context.FacilityId;

            var duplicate = await dbContext.MachineAlarmObservations
                .Where(o => o.FacilityId == facilityId && o.IdempotencyKey == request.IdempotencyKey)
                .Select(o => new { o.Id })
                .FirstOrDefaultAsync(ct);
            if (duplicate is not null)
                return OperationalIngestResult.Duplicate(duplicate.Id);

            var receivedAt = DateTimeOffset.UtcNow;
            var observationId = Guid.NewGuid();
            var projection = await EnsureAlarmProjectionAsync(context, request.MachineId, facilityId, receivedAt, ct);
            var signal = await EnsureSignalAsync(context, request.MachineId, facilityId, receivedAt, ct);

            var isOutOfOrder = projection.LastSourceSequence.HasValue
                && request.SourceSequence.HasValue
                && request.SourceSequence.Value <= projection.LastSourceSequence.Value;

            var active = await dbContext.MachineActiveAlarms
                .FirstOrDefaultAsync(a =>
                    a.FacilityId == facilityId
                    && a.MachineId == request.MachineId
                    && a.AlarmIdentity == request.AlarmIdentity, ct);

            var observation = new MachineAlarmObservation
            {
                Id = observationId,
                TenantId = context.TenantId,
                FacilityId = facilityId,
                MachineId = request.MachineId,
                GatewayId = request.GatewayId,
                AlarmIdentity = request.AlarmIdentity,
                ExternalAlarmCode = request.ExternalAlarmCode ?? active?.ExternalAlarmCode ?? request.AlarmIdentity,
                AlarmText = active?.AlarmText,
                Severity = active?.Severity ?? AlarmSeverities.Unknown,
                EventKind = MachineEventKinds.AlarmCleared,
                SourceTimestampUtc = request.SourceTimestampUtc,
                ReceivedAtUtc = receivedAt,
                SourceSequence = request.SourceSequence,
                Quality = NormalizeQuality(request.Quality),
                Transport = request.Transport ?? TelemetryTransports.Simulator,
                IdempotencyKey = request.IdempotencyKey,
                IsOutOfOrder = isOutOfOrder,
                IsUnmatchedClear = active is null,
                CreatedAtUtc = receivedAt,
            };
            dbContext.MachineAlarmObservations.Add(observation);

            if (active is null)
            {
                await dbContext.SaveChangesAsync(ct);
                return OperationalIngestResult.Unmatched(
                    EdgeFailureCodes.AlarmUnmatchedClear,
                    observationId,
                    "No matching active alarm observation.");
            }

            if (!isOutOfOrder)
            {
                dbContext.MachineActiveAlarms.Remove(active);
                await dbContext.SaveChangesAsync(ct);
                await RefreshAlarmProjectionAsync(projection, facilityId, request.MachineId, receivedAt, ct);
                if (request.SourceSequence.HasValue)
                {
                    projection.LastSourceSequence = request.SourceSequence.Value;
                    signal.LastOperationalSourceSequence = request.SourceSequence.Value;
                }

                signal.ActiveAlarmCount = projection.ActiveAlarmCount;
                signal.HighestAlarmSeverity = projection.HighestSeverity;
            }
            // Out-of-order clear: history kept; active projection must not incorrectly reactivate later older raises

            projection.UpdatedAtUtc = receivedAt;
            signal.UpdatedAtUtc = receivedAt;
            await dbContext.SaveChangesAsync(ct);
            return OperationalIngestResult.Success(observationId, isOutOfOrder);
        }, cancellationToken);

    private async Task RefreshAlarmProjectionAsync(
        MachineAlarmProjection projection,
        Guid facilityId,
        Guid machineId,
        DateTimeOffset now,
        CancellationToken ct)
    {
        var remaining = await dbContext.MachineActiveAlarms
            .Where(a => a.FacilityId == facilityId && a.MachineId == machineId)
            .ToListAsync(ct);

        projection.ActiveAlarmCount = remaining.Count;
        if (remaining.Count == 0)
        {
            projection.HighestSeverity = null;
            projection.LatestAlarmCode = null;
            projection.LatestAlarmText = null;
            projection.LatestRaisedAtUtc = null;
        }
        else
        {
            string? highest = null;
            MachineActiveAlarm? latest = null;
            foreach (var a in remaining)
            {
                highest = highest is null ? a.Severity : AlarmSeverities.Higher(highest, a.Severity);
                if (latest is null || a.RaisedAtSourceUtc > latest.RaisedAtSourceUtc)
                    latest = a;
            }

            projection.HighestSeverity = highest;
            projection.LatestAlarmCode = latest?.ExternalAlarmCode;
            projection.LatestAlarmText = latest?.AlarmText;
            projection.LatestRaisedAtUtc = latest?.RaisedAtSourceUtc;
        }

        projection.UpdatedAtUtc = now;
    }

    private async Task<MachineAlarmProjection> EnsureAlarmProjectionAsync(
        EdgeOperationContext context,
        Guid machineId,
        Guid facilityId,
        DateTimeOffset now,
        CancellationToken ct)
    {
        var projection = await dbContext.MachineAlarmProjections
            .FirstOrDefaultAsync(p => p.FacilityId == facilityId && p.MachineId == machineId, ct);
        if (projection is null)
        {
            projection = new MachineAlarmProjection
            {
                Id = Guid.NewGuid(),
                TenantId = context.TenantId,
                FacilityId = facilityId,
                MachineId = machineId,
                UpdatedAtUtc = now,
            };
            dbContext.MachineAlarmProjections.Add(projection);
        }

        return projection;
    }

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

public sealed class UnavailableEdgeAlarmIngestionService : IEdgeAlarmIngestionService
{
    public Task<OperationalIngestResult> IngestAlarmRaisedAsync(
        EdgeOperationContext context, AlarmRaiseIngestRequest request, CancellationToken cancellationToken) =>
        Task.FromResult(OperationalIngestResult.Failure(EdgeFailureCodes.InvalidScope, "Persistence is unavailable."));

    public Task<OperationalIngestResult> IngestAlarmClearedAsync(
        EdgeOperationContext context, AlarmClearIngestRequest request, CancellationToken cancellationToken) =>
        Task.FromResult(OperationalIngestResult.Failure(EdgeFailureCodes.InvalidScope, "Persistence is unavailable."));
}
