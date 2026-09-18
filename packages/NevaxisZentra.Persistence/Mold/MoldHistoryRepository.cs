using System.Text.Json;
using NevaxisZentra.Persistence.Audit;
using NevaxisZentra.Persistence.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace NevaxisZentra.Persistence.Mold;

public interface IMoldHistoryRepository
{
    Task<IReadOnlyList<MoldMaintenanceReadModel>> ListMaintenanceAsync(
        MoldOperationContext context,
        Guid moldId,
        MoldHistoryListFilters filters,
        CancellationToken cancellationToken = default);

    Task<MoldMaintenanceReadModel?> GetMaintenanceAsync(
        MoldOperationContext context,
        Guid moldId,
        Guid maintenanceId,
        CancellationToken cancellationToken = default);

    Task<MoldWriteResult> CreateMaintenanceAsync(
        MoldOperationContext context,
        CreateMoldMaintenanceCommand command,
        CancellationToken cancellationToken = default);

    Task<MoldWriteResult> UpdateMaintenanceAsync(
        MoldOperationContext context,
        UpdateMoldMaintenanceCommand command,
        CancellationToken cancellationToken = default);

    Task<MoldWriteResult> CompleteMaintenanceAsync(
        MoldOperationContext context,
        CompleteMoldMaintenanceCommand command,
        CancellationToken cancellationToken = default);

    Task<MoldWriteResult> CancelMaintenanceAsync(
        MoldOperationContext context,
        CancelMoldMaintenanceCommand command,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MoldRevisionReadModel>> ListRevisionsAsync(
        MoldOperationContext context,
        Guid moldId,
        MoldHistoryListFilters filters,
        CancellationToken cancellationToken = default);

    Task<MoldRevisionReadModel?> GetRevisionAsync(
        MoldOperationContext context,
        Guid moldId,
        Guid revisionId,
        CancellationToken cancellationToken = default);

    Task<MoldWriteResult> AppendRevisionAsync(
        MoldOperationContext context,
        AppendMoldRevisionCommand command,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MoldTrialReadModel>> ListTrialsAsync(
        MoldOperationContext context,
        Guid moldId,
        MoldHistoryListFilters filters,
        CancellationToken cancellationToken = default);

    Task<MoldTrialReadModel?> GetTrialAsync(
        MoldOperationContext context,
        Guid moldId,
        Guid trialId,
        CancellationToken cancellationToken = default);

    Task<MoldWriteResult> AppendTrialAsync(
        MoldOperationContext context,
        AppendMoldTrialCommand command,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MoldTimelineEntryReadModel>> ListTimelineAsync(
        MoldOperationContext context,
        Guid moldId,
        MoldHistoryListFilters filters,
        CancellationToken cancellationToken = default);
}

public sealed class MoldHistoryRepository(
    NevaxisZentraDbContext dbContext,
    TenantDatabaseSession tenantDatabaseSession) : IMoldHistoryRepository
{
    public Task<IReadOnlyList<MoldMaintenanceReadModel>> ListMaintenanceAsync(
        MoldOperationContext context,
        Guid moldId,
        MoldHistoryListFilters filters,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(async ct =>
        {
            if (!await MoldExistsAsync(context, moldId, ct))
            {
                return (IReadOnlyList<MoldMaintenanceReadModel>)new List<MoldMaintenanceReadModel>();
            }

            var (page, pageSize, skip) = NormalizePaging(filters);
            var items = await dbContext.MoldMaintenanceRecords
                .AsNoTracking()
                .Where(r => r.TenantId == context.TenantId
                    && r.FacilityId == context.FacilityId
                    && r.MoldId == moldId)
                .OrderByDescending(r => r.OpenedAtUtc)
                .ThenByDescending(r => r.CreatedAtUtc)
                .ThenByDescending(r => r.Id)
                .Skip(skip)
                .Take(pageSize)
                .ToListAsync(ct);

            return (IReadOnlyList<MoldMaintenanceReadModel>)items.Select(MapMaintenance).ToList();
        }, cancellationToken);

    public Task<MoldMaintenanceReadModel?> GetMaintenanceAsync(
        MoldOperationContext context,
        Guid moldId,
        Guid maintenanceId,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(async ct =>
        {
            var item = await dbContext.MoldMaintenanceRecords
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    r => r.TenantId == context.TenantId
                        && r.FacilityId == context.FacilityId
                        && r.MoldId == moldId
                        && r.Id == maintenanceId,
                    ct);

            return item is null ? null : MapMaintenance(item);
        }, cancellationToken);

    public Task<MoldWriteResult> CreateMaintenanceAsync(
        MoldOperationContext context,
        CreateMoldMaintenanceCommand command,
        CancellationToken cancellationToken = default) =>
        ExecuteWriteAsync(context, async ct =>
        {
            var validationFailure = ValidateCreateMaintenance(command);
            if (validationFailure is not null)
            {
                return MoldWriteResult.Fail(MoldFailureCodes.ValidationFailed, validationFailure);
            }

            if (string.IsNullOrWhiteSpace(command.IdempotencyKey))
            {
                return MoldWriteResult.Fail(MoldFailureCodes.ValidationFailed, "Idempotency key is required.");
            }

            var idempotencyKey = command.IdempotencyKey.Trim();
            var fingerprint = MoldHistoryIdempotencyFingerprint.ComputeMaintenanceCreateFingerprint(context, command);
            var existing = await dbContext.MoldMaintenanceRecords
                .SingleOrDefaultAsync(
                    r => r.TenantId == context.TenantId
                        && r.MoldId == command.MoldId
                        && r.IdempotencyKey == idempotencyKey,
                    ct);

            if (existing is not null)
            {
                return ResolveHistoryIdempotency(existing.IdempotencyPayloadFingerprint, fingerprint, existing.Id, existing.Version);
            }

            if (!await MoldExistsAsync(context, command.MoldId, ct))
            {
                return MoldWriteResult.Fail(MoldFailureCodes.NotFound, "Mold not found.");
            }

            var now = DateTimeOffset.UtcNow;
            var actor = context.IdentitySubjectId.ToString();
            var maintenanceType = MoldHistoryValueConversions.MaintenanceTypeFromStorage(command.MaintenanceType);
            var status = MoldHistoryValueConversions.MaintenanceStatusFromStorage(command.Status);
            var completionFailure = MoldHistoryValidator.ValidateMaintenanceCompletion(status, command.OpenedAtUtc, null);
            if (completionFailure is not null)
            {
                return MoldWriteResult.Fail(MoldFailureCodes.ValidationFailed, completionFailure);
            }

            var record = new MoldMaintenanceRecord
            {
                Id = Guid.NewGuid(),
                TenantId = context.TenantId,
                FacilityId = context.FacilityId,
                MoldId = command.MoldId,
                MaintenanceType = maintenanceType,
                Status = status,
                OpenedAtUtc = command.OpenedAtUtc.ToUniversalTime(),
                Summary = command.Summary.Trim(),
                Findings = command.Findings?.Trim(),
                ActionsTaken = command.ActionsTaken?.Trim(),
                PerformedByDisplayName = command.PerformedByDisplayName?.Trim(),
                ExternalMaintenanceRequestReference = command.ExternalMaintenanceRequestReference?.Trim(),
                ExternalWorkOrderReference = command.ExternalWorkOrderReference?.Trim(),
                SourceType = MoldValueConversions.SourceFromStorage(command.SourceType),
                SourceReference = command.SourceReference?.Trim(),
                IdempotencyKey = idempotencyKey,
                IdempotencyPayloadFingerprint = fingerprint,
                CreatedAtUtc = now,
                UpdatedAtUtc = now,
                CreatedBySubjectId = actor,
                UpdatedBySubjectId = actor,
                Version = 1,
            };

            dbContext.MoldMaintenanceRecords.Add(record);
            AddAudit(context, MoldAuditActions.MaintenanceCreated, MoldAuditEventTypes.MaintenanceCreated,
                "mold.maintenance", record.Id, now, new { moldId = command.MoldId, record.Id });

            try
            {
                await dbContext.SaveChangesAsync(ct);
            }
            catch (DbUpdateException ex) when (IsUniqueViolation(ex))
            {
                var raced = await dbContext.MoldMaintenanceRecords
                    .SingleOrDefaultAsync(
                        r => r.TenantId == context.TenantId
                            && r.MoldId == command.MoldId
                            && r.IdempotencyKey == idempotencyKey,
                        ct);
                if (raced is null)
                {
                    throw;
                }

                return ResolveHistoryIdempotency(raced.IdempotencyPayloadFingerprint, fingerprint, raced.Id, raced.Version);
            }

            return MoldWriteResult.Success(record.Id, record.Version);
        }, cancellationToken);

    public Task<MoldWriteResult> UpdateMaintenanceAsync(
        MoldOperationContext context,
        UpdateMoldMaintenanceCommand command,
        CancellationToken cancellationToken = default) =>
        ExecuteWriteAsync(context, async ct =>
        {
            var record = await dbContext.MoldMaintenanceRecords
                .SingleOrDefaultAsync(
                    r => r.TenantId == context.TenantId
                        && r.FacilityId == context.FacilityId
                        && r.MoldId == command.MoldId
                        && r.Id == command.MaintenanceId,
                    ct);

            if (record is null)
            {
                return MoldWriteResult.Fail(MoldFailureCodes.NotFound, "Maintenance record not found.");
            }

            if (MoldHistoryValidator.IsTerminalMaintenanceStatus(record.Status))
            {
                return MoldWriteResult.Fail(
                    MoldHistoryFailureCodes.InvalidMaintenanceState,
                    "Completed or cancelled maintenance cannot be updated.");
            }

            if (record.Version != command.ExpectedVersion)
            {
                return MoldWriteResult.Fail(
                    MoldFailureCodes.ConcurrencyConflict,
                    "The maintenance record was modified. Please reload and try again.");
            }

            var summaryFailure = MoldHistoryValidator.ValidateRequiredSummary(command.Summary);
            if (summaryFailure is not null)
            {
                return MoldWriteResult.Fail(MoldFailureCodes.ValidationFailed, summaryFailure);
            }

            var status = MoldHistoryValueConversions.MaintenanceStatusFromStorage(command.Status);
            if (status is MoldMaintenanceStatus.Completed or MoldMaintenanceStatus.Cancelled)
            {
                return MoldWriteResult.Fail(
                    MoldHistoryFailureCodes.InvalidMaintenanceState,
                    "Use complete or cancel operations for terminal maintenance states.");
            }

            var completionFailure = MoldHistoryValidator.ValidateMaintenanceCompletion(
                status,
                command.OpenedAtUtc,
                record.CompletedAtUtc);
            if (completionFailure is not null)
            {
                return MoldWriteResult.Fail(MoldFailureCodes.ValidationFailed, completionFailure);
            }

            var now = DateTimeOffset.UtcNow;
            var actor = context.IdentitySubjectId.ToString();
            record.MaintenanceType = MoldHistoryValueConversions.MaintenanceTypeFromStorage(command.MaintenanceType);
            record.Status = status;
            record.OpenedAtUtc = command.OpenedAtUtc.ToUniversalTime();
            record.Summary = command.Summary.Trim();
            record.Findings = command.Findings?.Trim();
            record.ActionsTaken = command.ActionsTaken?.Trim();
            record.PerformedByDisplayName = command.PerformedByDisplayName?.Trim();
            record.ExternalMaintenanceRequestReference = command.ExternalMaintenanceRequestReference?.Trim();
            record.ExternalWorkOrderReference = command.ExternalWorkOrderReference?.Trim();
            record.SourceType = MoldValueConversions.SourceFromStorage(command.SourceType);
            record.SourceReference = command.SourceReference?.Trim();
            record.UpdatedAtUtc = now;
            record.UpdatedBySubjectId = actor;
            record.Version += 1;

            AddAudit(context, MoldAuditActions.MaintenanceUpdated, MoldAuditEventTypes.MaintenanceUpdated,
                "mold.maintenance", record.Id, now, new { moldId = command.MoldId, record.Id });

            await dbContext.SaveChangesAsync(ct);
            return MoldWriteResult.Success(record.Id, record.Version);
        }, cancellationToken);

    public Task<MoldWriteResult> CompleteMaintenanceAsync(
        MoldOperationContext context,
        CompleteMoldMaintenanceCommand command,
        CancellationToken cancellationToken = default) =>
        ExecuteWriteAsync(context, async ct =>
        {
            var record = await dbContext.MoldMaintenanceRecords
                .SingleOrDefaultAsync(
                    r => r.TenantId == context.TenantId
                        && r.FacilityId == context.FacilityId
                        && r.MoldId == command.MoldId
                        && r.Id == command.MaintenanceId,
                    ct);

            if (record is null)
            {
                return MoldWriteResult.Fail(MoldFailureCodes.NotFound, "Maintenance record not found.");
            }

            if (MoldHistoryValidator.IsTerminalMaintenanceStatus(record.Status))
            {
                return MoldWriteResult.Fail(
                    MoldHistoryFailureCodes.InvalidMaintenanceState,
                    "Maintenance is already in a terminal state.");
            }

            if (record.Version != command.ExpectedVersion)
            {
                return MoldWriteResult.Fail(
                    MoldFailureCodes.ConcurrencyConflict,
                    "The maintenance record was modified. Please reload and try again.");
            }

            var completedAt = command.CompletedAtUtc.ToUniversalTime();
            var completionFailure = MoldHistoryValidator.ValidateMaintenanceCompletion(
                MoldMaintenanceStatus.Completed,
                record.OpenedAtUtc,
                completedAt);
            if (completionFailure is not null)
            {
                return MoldWriteResult.Fail(MoldFailureCodes.ValidationFailed, completionFailure);
            }

            var now = DateTimeOffset.UtcNow;
            var actor = context.IdentitySubjectId.ToString();
            record.Status = MoldMaintenanceStatus.Completed;
            record.CompletedAtUtc = completedAt;
            record.Findings = command.Findings?.Trim() ?? record.Findings;
            record.ActionsTaken = command.ActionsTaken?.Trim() ?? record.ActionsTaken;
            record.PerformedByDisplayName = command.PerformedByDisplayName?.Trim() ?? record.PerformedByDisplayName;
            record.UpdatedAtUtc = now;
            record.UpdatedBySubjectId = actor;
            record.Version += 1;

            AddAudit(context, MoldAuditActions.MaintenanceCompleted, MoldAuditEventTypes.MaintenanceCompleted,
                "mold.maintenance", record.Id, now, new { moldId = command.MoldId, record.Id });

            await dbContext.SaveChangesAsync(ct);
            return MoldWriteResult.Success(record.Id, record.Version);
        }, cancellationToken);

    public Task<MoldWriteResult> CancelMaintenanceAsync(
        MoldOperationContext context,
        CancelMoldMaintenanceCommand command,
        CancellationToken cancellationToken = default) =>
        ExecuteWriteAsync(context, async ct =>
        {
            var record = await dbContext.MoldMaintenanceRecords
                .SingleOrDefaultAsync(
                    r => r.TenantId == context.TenantId
                        && r.FacilityId == context.FacilityId
                        && r.MoldId == command.MoldId
                        && r.Id == command.MaintenanceId,
                    ct);

            if (record is null)
            {
                return MoldWriteResult.Fail(MoldFailureCodes.NotFound, "Maintenance record not found.");
            }

            if (MoldHistoryValidator.IsTerminalMaintenanceStatus(record.Status))
            {
                return MoldWriteResult.Fail(
                    MoldHistoryFailureCodes.InvalidMaintenanceState,
                    "Maintenance is already in a terminal state.");
            }

            if (record.Version != command.ExpectedVersion)
            {
                return MoldWriteResult.Fail(
                    MoldFailureCodes.ConcurrencyConflict,
                    "The maintenance record was modified. Please reload and try again.");
            }

            var now = DateTimeOffset.UtcNow;
            var actor = context.IdentitySubjectId.ToString();
            record.Status = MoldMaintenanceStatus.Cancelled;
            record.Findings = command.Findings?.Trim() ?? record.Findings;
            record.UpdatedAtUtc = now;
            record.UpdatedBySubjectId = actor;
            record.Version += 1;

            AddAudit(context, MoldAuditActions.MaintenanceCancelled, MoldAuditEventTypes.MaintenanceCancelled,
                "mold.maintenance", record.Id, now, new { moldId = command.MoldId, record.Id });

            await dbContext.SaveChangesAsync(ct);
            return MoldWriteResult.Success(record.Id, record.Version);
        }, cancellationToken);

    public Task<IReadOnlyList<MoldRevisionReadModel>> ListRevisionsAsync(
        MoldOperationContext context,
        Guid moldId,
        MoldHistoryListFilters filters,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(async ct =>
        {
            if (!await MoldExistsAsync(context, moldId, ct))
            {
                return (IReadOnlyList<MoldRevisionReadModel>)new List<MoldRevisionReadModel>();
            }

            var (_, pageSize, skip) = NormalizePaging(filters);
            var items = await dbContext.MoldRevisionRecords
                .AsNoTracking()
                .Where(r => r.TenantId == context.TenantId
                    && r.FacilityId == context.FacilityId
                    && r.MoldId == moldId)
                .OrderByDescending(r => r.RevisionDate)
                .ThenByDescending(r => r.CreatedAtUtc)
                .ThenByDescending(r => r.Id)
                .Skip(skip)
                .Take(pageSize)
                .ToListAsync(ct);

            return (IReadOnlyList<MoldRevisionReadModel>)items.Select(MapRevision).ToList();
        }, cancellationToken);

    public Task<MoldRevisionReadModel?> GetRevisionAsync(
        MoldOperationContext context,
        Guid moldId,
        Guid revisionId,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(async ct =>
        {
            var item = await dbContext.MoldRevisionRecords
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    r => r.TenantId == context.TenantId
                        && r.FacilityId == context.FacilityId
                        && r.MoldId == moldId
                        && r.Id == revisionId,
                    ct);

            return item is null ? null : MapRevision(item);
        }, cancellationToken);

    public Task<MoldWriteResult> AppendRevisionAsync(
        MoldOperationContext context,
        AppendMoldRevisionCommand command,
        CancellationToken cancellationToken = default) =>
        ExecuteWriteAsync(context, async ct =>
        {
            var validationFailure = ValidateAppendRevision(command);
            if (validationFailure is not null)
            {
                return MoldWriteResult.Fail(MoldFailureCodes.ValidationFailed, validationFailure);
            }

            if (string.IsNullOrWhiteSpace(command.IdempotencyKey))
            {
                return MoldWriteResult.Fail(MoldFailureCodes.ValidationFailed, "Idempotency key is required.");
            }

            if (!MoldHistoryValidator.TryNormalizeNumber(
                    command.RevisionNumber,
                    out var revisionNumber,
                    out var revisionNumberNormalized,
                    out var numberFailure))
            {
                return MoldWriteResult.Fail(MoldFailureCodes.ValidationFailed, numberFailure!);
            }

            var idempotencyKey = command.IdempotencyKey.Trim();
            var fingerprint = MoldHistoryIdempotencyFingerprint.ComputeRevisionAppendFingerprint(
                context, command, revisionNumberNormalized);

            var existingByKey = await dbContext.MoldRevisionRecords
                .SingleOrDefaultAsync(
                    r => r.TenantId == context.TenantId
                        && r.MoldId == command.MoldId
                        && r.IdempotencyKey == idempotencyKey,
                    ct);

            if (existingByKey is not null)
            {
                return ResolveHistoryIdempotency(existingByKey.IdempotencyPayloadFingerprint, fingerprint, existingByKey.Id, 1);
            }

            if (!await MoldExistsAsync(context, command.MoldId, ct))
            {
                return MoldWriteResult.Fail(MoldFailureCodes.NotFound, "Mold not found.");
            }

            var duplicateNumber = await dbContext.MoldRevisionRecords
                .AnyAsync(
                    r => r.TenantId == context.TenantId
                        && r.MoldId == command.MoldId
                        && r.RevisionNumberNormalized == revisionNumberNormalized,
                    ct);

            if (duplicateNumber)
            {
                return MoldWriteResult.Fail(
                    MoldHistoryFailureCodes.DuplicateRevisionNumber,
                    "A revision with this number already exists for the mold.");
            }

            var now = DateTimeOffset.UtcNow;
            var actor = context.IdentitySubjectId.ToString();
            var record = new MoldRevisionRecord
            {
                Id = Guid.NewGuid(),
                TenantId = context.TenantId,
                FacilityId = context.FacilityId,
                MoldId = command.MoldId,
                RevisionNumber = revisionNumber,
                RevisionNumberNormalized = revisionNumberNormalized,
                RevisionType = MoldHistoryValueConversions.RevisionTypeFromStorage(command.RevisionType),
                RevisionDate = command.RevisionDate,
                Summary = command.Summary.Trim(),
                Reason = command.Reason?.Trim(),
                TechnicalChange = command.TechnicalChange?.Trim(),
                PreviousCavityCount = command.PreviousCavityCount,
                NewCavityCount = command.NewCavityCount,
                PerformedByDisplayName = command.PerformedByDisplayName?.Trim(),
                ExternalReference = command.ExternalReference?.Trim(),
                SourceType = MoldValueConversions.SourceFromStorage(command.SourceType),
                SourceReference = command.SourceReference?.Trim(),
                IdempotencyKey = idempotencyKey,
                IdempotencyPayloadFingerprint = fingerprint,
                CreatedAtUtc = now,
                CreatedBySubjectId = actor,
            };

            dbContext.MoldRevisionRecords.Add(record);
            AddAudit(context, MoldAuditActions.RevisionAppended, MoldAuditEventTypes.RevisionAppended,
                "mold.revision", record.Id, now, new { moldId = command.MoldId, record.Id, revisionNumber });

            try
            {
                await dbContext.SaveChangesAsync(ct);
            }
            catch (DbUpdateException ex) when (IsUniqueViolation(ex))
            {
                var raced = await dbContext.MoldRevisionRecords
                    .SingleOrDefaultAsync(
                        r => r.TenantId == context.TenantId
                            && r.MoldId == command.MoldId
                            && r.IdempotencyKey == idempotencyKey,
                        ct);
                if (raced is not null)
                {
                    return ResolveHistoryIdempotency(raced.IdempotencyPayloadFingerprint, fingerprint, raced.Id, 1);
                }

                return MoldWriteResult.Fail(
                    MoldHistoryFailureCodes.DuplicateRevisionNumber,
                    "A revision with this number already exists for the mold.");
            }

            return MoldWriteResult.Success(record.Id, 1);
        }, cancellationToken);

    public Task<IReadOnlyList<MoldTrialReadModel>> ListTrialsAsync(
        MoldOperationContext context,
        Guid moldId,
        MoldHistoryListFilters filters,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(async ct =>
        {
            if (!await MoldExistsAsync(context, moldId, ct))
            {
                return (IReadOnlyList<MoldTrialReadModel>)new List<MoldTrialReadModel>();
            }

            var (_, pageSize, skip) = NormalizePaging(filters);
            var items = await dbContext.MoldTrialRecords
                .AsNoTracking()
                .Where(r => r.TenantId == context.TenantId
                    && r.FacilityId == context.FacilityId
                    && r.MoldId == moldId)
                .OrderByDescending(r => r.TrialDate)
                .ThenByDescending(r => r.CreatedAtUtc)
                .ThenByDescending(r => r.Id)
                .Skip(skip)
                .Take(pageSize)
                .ToListAsync(ct);

            return (IReadOnlyList<MoldTrialReadModel>)items.Select(MapTrial).ToList();
        }, cancellationToken);

    public Task<MoldTrialReadModel?> GetTrialAsync(
        MoldOperationContext context,
        Guid moldId,
        Guid trialId,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(async ct =>
        {
            var item = await dbContext.MoldTrialRecords
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    r => r.TenantId == context.TenantId
                        && r.FacilityId == context.FacilityId
                        && r.MoldId == moldId
                        && r.Id == trialId,
                    ct);

            return item is null ? null : MapTrial(item);
        }, cancellationToken);

    public Task<MoldWriteResult> AppendTrialAsync(
        MoldOperationContext context,
        AppendMoldTrialCommand command,
        CancellationToken cancellationToken = default) =>
        ExecuteWriteAsync(context, async ct =>
        {
            var validationFailure = ValidateAppendTrial(command);
            if (validationFailure is not null)
            {
                return MoldWriteResult.Fail(MoldFailureCodes.ValidationFailed, validationFailure);
            }

            if (string.IsNullOrWhiteSpace(command.IdempotencyKey))
            {
                return MoldWriteResult.Fail(MoldFailureCodes.ValidationFailed, "Idempotency key is required.");
            }

            if (!MoldHistoryValidator.TryNormalizeNumber(
                    command.TrialNumber,
                    out var trialNumber,
                    out var trialNumberNormalized,
                    out var numberFailure))
            {
                return MoldWriteResult.Fail(MoldFailureCodes.ValidationFailed, numberFailure!);
            }

            var idempotencyKey = command.IdempotencyKey.Trim();
            var fingerprint = MoldHistoryIdempotencyFingerprint.ComputeTrialAppendFingerprint(
                context, command, trialNumberNormalized);

            var existingByKey = await dbContext.MoldTrialRecords
                .SingleOrDefaultAsync(
                    r => r.TenantId == context.TenantId
                        && r.MoldId == command.MoldId
                        && r.IdempotencyKey == idempotencyKey,
                    ct);

            if (existingByKey is not null)
            {
                return ResolveHistoryIdempotency(existingByKey.IdempotencyPayloadFingerprint, fingerprint, existingByKey.Id, 1);
            }

            if (!await MoldExistsAsync(context, command.MoldId, ct))
            {
                return MoldWriteResult.Fail(MoldFailureCodes.NotFound, "Mold not found.");
            }

            var duplicateNumber = await dbContext.MoldTrialRecords
                .AnyAsync(
                    r => r.TenantId == context.TenantId
                        && r.MoldId == command.MoldId
                        && r.TrialNumberNormalized == trialNumberNormalized,
                    ct);

            if (duplicateNumber)
            {
                return MoldWriteResult.Fail(
                    MoldHistoryFailureCodes.DuplicateTrialNumber,
                    "A trial with this number already exists for the mold.");
            }

            var now = DateTimeOffset.UtcNow;
            var actor = context.IdentitySubjectId.ToString();
            var record = new MoldTrialRecord
            {
                Id = Guid.NewGuid(),
                TenantId = context.TenantId,
                FacilityId = context.FacilityId,
                MoldId = command.MoldId,
                TrialNumber = trialNumber,
                TrialNumberNormalized = trialNumberNormalized,
                TrialDate = command.TrialDate,
                Result = MoldHistoryValueConversions.TrialResultFromStorage(command.Result),
                MachineReference = command.MachineReference?.Trim(),
                ProductionOrderReference = command.ProductionOrderReference?.Trim(),
                ProductReference = command.ProductReference?.Trim(),
                MaterialReference = command.MaterialReference?.Trim(),
                CycleTimeSeconds = command.CycleTimeSeconds,
                ShotCount = command.ShotCount,
                GoodPartCount = command.GoodPartCount,
                ScrapPartCount = command.ScrapPartCount,
                Findings = command.Findings?.Trim(),
                Decision = command.Decision?.Trim(),
                PerformedByDisplayName = command.PerformedByDisplayName?.Trim(),
                SourceType = MoldValueConversions.SourceFromStorage(command.SourceType),
                SourceReference = command.SourceReference?.Trim(),
                IdempotencyKey = idempotencyKey,
                IdempotencyPayloadFingerprint = fingerprint,
                CreatedAtUtc = now,
                CreatedBySubjectId = actor,
            };

            dbContext.MoldTrialRecords.Add(record);
            AddAudit(context, MoldAuditActions.TrialAppended, MoldAuditEventTypes.TrialAppended,
                "mold.trial", record.Id, now, new { moldId = command.MoldId, record.Id, trialNumber });

            try
            {
                await dbContext.SaveChangesAsync(ct);
            }
            catch (DbUpdateException ex) when (IsUniqueViolation(ex))
            {
                var raced = await dbContext.MoldTrialRecords
                    .SingleOrDefaultAsync(
                        r => r.TenantId == context.TenantId
                            && r.MoldId == command.MoldId
                            && r.IdempotencyKey == idempotencyKey,
                        ct);
                if (raced is not null)
                {
                    return ResolveHistoryIdempotency(raced.IdempotencyPayloadFingerprint, fingerprint, raced.Id, 1);
                }

                return MoldWriteResult.Fail(
                    MoldHistoryFailureCodes.DuplicateTrialNumber,
                    "A trial with this number already exists for the mold.");
            }

            return MoldWriteResult.Success(record.Id, 1);
        }, cancellationToken);

    public Task<IReadOnlyList<MoldTimelineEntryReadModel>> ListTimelineAsync(
        MoldOperationContext context,
        Guid moldId,
        MoldHistoryListFilters filters,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(async ct =>
        {
            if (!await MoldExistsAsync(context, moldId, ct))
            {
                return (IReadOnlyList<MoldTimelineEntryReadModel>)new List<MoldTimelineEntryReadModel>();
            }

            var (_, pageSize, skip) = NormalizePaging(filters);
            var fetchSize = Math.Min(pageSize + skip, 500);

            var lifecycle = await dbContext.MoldLifecycleEvents
                .AsNoTracking()
                .Where(e => e.TenantId == context.TenantId
                    && e.FacilityId == context.FacilityId
                    && e.MoldId == moldId)
                .OrderByDescending(e => e.OccurredAtUtc)
                .Take(fetchSize)
                .ToListAsync(ct);

            var counters = await dbContext.MoldCounterSnapshots
                .AsNoTracking()
                .Where(s => s.TenantId == context.TenantId
                    && s.FacilityId == context.FacilityId
                    && s.MoldId == moldId)
                .OrderByDescending(s => s.RecordedAtUtc)
                .Take(fetchSize)
                .ToListAsync(ct);

            var maintenance = await dbContext.MoldMaintenanceRecords
                .AsNoTracking()
                .Where(r => r.TenantId == context.TenantId
                    && r.FacilityId == context.FacilityId
                    && r.MoldId == moldId)
                .OrderByDescending(r => r.OpenedAtUtc)
                .Take(fetchSize)
                .ToListAsync(ct);

            var revisions = await dbContext.MoldRevisionRecords
                .AsNoTracking()
                .Where(r => r.TenantId == context.TenantId
                    && r.FacilityId == context.FacilityId
                    && r.MoldId == moldId)
                .OrderByDescending(r => r.RevisionDate)
                .Take(fetchSize)
                .ToListAsync(ct);

            var trials = await dbContext.MoldTrialRecords
                .AsNoTracking()
                .Where(r => r.TenantId == context.TenantId
                    && r.FacilityId == context.FacilityId
                    && r.MoldId == moldId)
                .OrderByDescending(r => r.TrialDate)
                .Take(fetchSize)
                .ToListAsync(ct);

            var entries = new List<MoldTimelineEntryReadModel>();

            entries.AddRange(lifecycle.Select(e => new MoldTimelineEntryReadModel(
                e.Id,
                "lifecycle",
                e.OccurredAtUtc,
                e.OccurredAtUtc,
                $"Yaşam döngüsü: {MoldValueConversions.ToStorage(e.PreviousState)} → {MoldValueConversions.ToStorage(e.NewState)}",
                e.Reason,
                MoldValueConversions.ToStorage(e.NewState),
                e.SourceReference,
                MoldActorDisplayNames.FromSubjectId(e.ActorSubjectId))));

            entries.AddRange(counters.Select(s => new MoldTimelineEntryReadModel(
                s.Id,
                "counter",
                s.RecordedAtUtc,
                s.RecordedAtUtc,
                "Sayaç kaydı",
                $"{s.ShotCount:N0} vuruş",
                MoldValueConversions.ToStorage(s.SourceType),
                s.SourceReference,
                MoldActorDisplayNames.FromSubjectId(s.RecordedBySubjectId ?? string.Empty))));

            entries.AddRange(maintenance.Select(r => new MoldTimelineEntryReadModel(
                r.Id,
                "maintenance",
                r.OpenedAtUtc,
                r.CreatedAtUtc,
                $"Bakım: {MoldHistoryValueConversions.ToApi(r.MaintenanceType)}",
                r.Summary,
                MoldHistoryValueConversions.ToApi(r.Status),
                r.ExternalWorkOrderReference ?? r.ExternalMaintenanceRequestReference,
                r.PerformedByDisplayName)));

            entries.AddRange(revisions.Select(r => new MoldTimelineEntryReadModel(
                r.Id,
                "revision",
                r.RevisionDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
                r.CreatedAtUtc,
                $"Revizyon {r.RevisionNumber}",
                r.Summary,
                MoldHistoryValueConversions.ToApi(r.RevisionType),
                r.ExternalReference,
                r.PerformedByDisplayName)));

            entries.AddRange(trials.Select(r => new MoldTimelineEntryReadModel(
                r.Id,
                "trial",
                r.TrialDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
                r.CreatedAtUtc,
                $"Deneme {r.TrialNumber}",
                r.Findings ?? r.Decision ?? "Deneme kaydı",
                MoldHistoryValueConversions.ToApi(r.Result),
                r.ProductionOrderReference ?? r.MachineReference,
                r.PerformedByDisplayName)));

            return (IReadOnlyList<MoldTimelineEntryReadModel>)entries
                .OrderByDescending(e => e.EventDateUtc)
                .ThenByDescending(e => e.CreatedAtUtc)
                .ThenByDescending(e => e.Id)
                .Skip(skip)
                .Take(pageSize)
                .ToList();
        }, cancellationToken);

    private async Task<bool> MoldExistsAsync(MoldOperationContext context, Guid moldId, CancellationToken ct) =>
        await dbContext.Molds.AnyAsync(
            m => m.TenantId == context.TenantId
                && m.FacilityId == context.FacilityId
                && m.Id == moldId,
            ct);

    private static (int Page, int PageSize, int Skip) NormalizePaging(MoldHistoryListFilters filters)
    {
        var page = Math.Clamp(filters.Page, 1, 100);
        var pageSize = Math.Clamp(filters.PageSize, 1, 100);
        return (page, pageSize, (page - 1) * pageSize);
    }

    private static string? ValidateCreateMaintenance(CreateMoldMaintenanceCommand command)
    {
        var summaryFailure = MoldHistoryValidator.ValidateRequiredSummary(command.Summary);
        if (summaryFailure is not null)
        {
            return summaryFailure;
        }

        foreach (var failure in new[]
                 {
                     MoldHistoryValidator.ValidateBoundedText(command.Findings, MoldHistoryValidator.MaxFindingsLength, "Findings"),
                     MoldHistoryValidator.ValidateBoundedText(command.ActionsTaken, MoldHistoryValidator.MaxActionsLength, "Actions taken"),
                     MoldHistoryValidator.ValidateBoundedText(command.PerformedByDisplayName, MoldHistoryValidator.MaxDisplayNameLength, "Performed by"),
                     MoldHistoryValidator.ValidateBoundedText(command.ExternalMaintenanceRequestReference, MoldHistoryValidator.MaxReferenceLength, "External maintenance request reference"),
                     MoldHistoryValidator.ValidateBoundedText(command.ExternalWorkOrderReference, MoldHistoryValidator.MaxReferenceLength, "External work order reference"),
                     MoldHistoryValidator.ValidateBoundedText(command.SourceReference, MoldHistoryValidator.MaxReferenceLength, "Source reference"),
                 })
        {
            if (failure is not null)
            {
                return failure;
            }
        }

        return null;
    }

    private static string? ValidateAppendRevision(AppendMoldRevisionCommand command)
    {
        var summaryFailure = MoldHistoryValidator.ValidateRequiredSummary(command.Summary);
        if (summaryFailure is not null)
        {
            return summaryFailure;
        }

        foreach (var failure in new[]
                 {
                     MoldHistoryValidator.ValidateBoundedText(command.Reason, MoldHistoryValidator.MaxReasonLength, "Reason"),
                     MoldHistoryValidator.ValidateBoundedText(command.TechnicalChange, MoldHistoryValidator.MaxTechnicalChangeLength, "Technical change"),
                     MoldHistoryValidator.ValidateBoundedText(command.PerformedByDisplayName, MoldHistoryValidator.MaxDisplayNameLength, "Performed by"),
                     MoldHistoryValidator.ValidateBoundedText(command.ExternalReference, MoldHistoryValidator.MaxReferenceLength, "External reference"),
                     MoldHistoryValidator.ValidateBoundedText(command.SourceReference, MoldHistoryValidator.MaxReferenceLength, "Source reference"),
                     MoldHistoryValidator.ValidateCavityCount(command.PreviousCavityCount),
                     MoldHistoryValidator.ValidateCavityCount(command.NewCavityCount),
                 })
        {
            if (failure is not null)
            {
                return failure;
            }
        }

        return null;
    }

    private static string? ValidateAppendTrial(AppendMoldTrialCommand command)
    {
        foreach (var failure in new[]
                 {
                     MoldHistoryValidator.ValidateCycleTime(command.CycleTimeSeconds),
                     MoldHistoryValidator.ValidateTrialCounts(command.ShotCount, command.GoodPartCount, command.ScrapPartCount),
                     MoldHistoryValidator.ValidateBoundedText(command.Findings, MoldHistoryValidator.MaxFindingsLength, "Findings"),
                     MoldHistoryValidator.ValidateBoundedText(command.Decision, MoldHistoryValidator.MaxDecisionLength, "Decision"),
                     MoldHistoryValidator.ValidateBoundedText(command.PerformedByDisplayName, MoldHistoryValidator.MaxDisplayNameLength, "Performed by"),
                     MoldHistoryValidator.ValidateBoundedText(command.MachineReference, MoldHistoryValidator.MaxReferenceLength, "Machine reference"),
                     MoldHistoryValidator.ValidateBoundedText(command.ProductionOrderReference, MoldHistoryValidator.MaxReferenceLength, "Production order reference"),
                     MoldHistoryValidator.ValidateBoundedText(command.ProductReference, MoldHistoryValidator.MaxReferenceLength, "Product reference"),
                     MoldHistoryValidator.ValidateBoundedText(command.MaterialReference, MoldHistoryValidator.MaxReferenceLength, "Material reference"),
                     MoldHistoryValidator.ValidateBoundedText(command.SourceReference, MoldHistoryValidator.MaxReferenceLength, "Source reference"),
                 })
        {
            if (failure is not null)
            {
                return failure;
            }
        }

        return null;
    }

    private static MoldMaintenanceReadModel MapMaintenance(MoldMaintenanceRecord record) =>
        new(
            record.Id,
            record.MoldId,
            MoldHistoryValueConversions.ToApi(record.MaintenanceType),
            MoldHistoryValueConversions.ToApi(record.Status),
            record.OpenedAtUtc,
            record.CompletedAtUtc,
            record.Summary,
            record.Findings,
            record.ActionsTaken,
            record.PerformedByDisplayName,
            record.ExternalMaintenanceRequestReference,
            record.ExternalWorkOrderReference,
            MoldValueConversions.ToStorage(record.SourceType),
            record.SourceReference,
            record.CreatedAtUtc,
            record.UpdatedAtUtc,
            record.Version);

    private static MoldRevisionReadModel MapRevision(MoldRevisionRecord record) =>
        new(
            record.Id,
            record.MoldId,
            record.RevisionNumber,
            MoldHistoryValueConversions.ToApi(record.RevisionType),
            record.RevisionDate,
            record.Summary,
            record.Reason,
            record.TechnicalChange,
            record.PreviousCavityCount,
            record.NewCavityCount,
            record.PerformedByDisplayName,
            record.ExternalReference,
            MoldValueConversions.ToStorage(record.SourceType),
            record.SourceReference,
            record.CreatedAtUtc);

    private static MoldTrialReadModel MapTrial(MoldTrialRecord record) =>
        new(
            record.Id,
            record.MoldId,
            record.TrialNumber,
            record.TrialDate,
            MoldHistoryValueConversions.ToApi(record.Result),
            record.MachineReference,
            record.ProductionOrderReference,
            record.ProductReference,
            record.MaterialReference,
            record.CycleTimeSeconds,
            record.ShotCount,
            record.GoodPartCount,
            record.ScrapPartCount,
            record.Findings,
            record.Decision,
            record.PerformedByDisplayName,
            MoldValueConversions.ToStorage(record.SourceType),
            record.SourceReference,
            record.CreatedAtUtc);

    private async Task<MoldWriteResult> ExecuteWriteAsync(
        MoldOperationContext context,
        Func<CancellationToken, Task<MoldWriteResult>> action,
        CancellationToken cancellationToken)
    {
        if (context.TenantId == Guid.Empty || context.FacilityId == Guid.Empty)
        {
            return MoldWriteResult.Fail(MoldFailureCodes.InvalidScope, "Tenant and facility are required.");
        }

        try
        {
            return await tenantDatabaseSession.ExecuteWriteAsync(action, cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return MoldWriteResult.Fail(
                MoldFailureCodes.ConcurrencyConflict,
                "The record was modified by another user. Please reload and try again.");
        }
        catch (Exception) when (cancellationToken.IsCancellationRequested == false)
        {
            return MoldWriteResult.Fail(MoldFailureCodes.SourceUnavailable, "Mold service is unavailable.");
        }
    }

    private static MoldWriteResult ResolveHistoryIdempotency(
        string? storedFingerprint,
        string fingerprint,
        Guid entityId,
        uint version)
    {
        if (string.IsNullOrEmpty(storedFingerprint))
        {
            return HistoryIdempotencyConflictResult();
        }

        if (storedFingerprint == fingerprint)
        {
            return MoldWriteResult.Success(entityId, version, idempotentReplay: true);
        }

        return HistoryIdempotencyConflictResult();
    }

    private static MoldWriteResult HistoryIdempotencyConflictResult() =>
        MoldWriteResult.Fail(
            MoldHistoryFailureCodes.HistoryIdempotencyConflict,
            "The idempotency key was already used with a different history payload.");

    private void AddAudit(
        MoldOperationContext context,
        string action,
        string eventType,
        string resourceType,
        Guid resourceId,
        DateTimeOffset now,
        object metadata)
    {
        dbContext.AuditRecords.Add(new AuditRecord
        {
            Id = Guid.NewGuid(),
            TenantId = context.TenantId,
            IdentitySubjectId = context.IdentitySubjectId,
            TenantMembershipId = context.MembershipId,
            EventType = eventType,
            Action = action,
            ResourceType = resourceType,
            ResourceId = resourceId.ToString(),
            Outcome = AuditOutcome.Success,
            PermissionKey = context.PermissionKey,
            CorrelationId = context.CorrelationId ?? Guid.NewGuid(),
            Source = AuditSource.Api,
            OccurredAtUtc = now,
            RecordedAtUtc = now,
            MetadataJson = JsonSerializer.Serialize(metadata),
        });
    }

    private static bool IsUniqueViolation(DbUpdateException ex) =>
        ex.InnerException?.Message.Contains("23505", StringComparison.OrdinalIgnoreCase) == true
        || ex.InnerException?.Message.Contains("unique constraint", StringComparison.OrdinalIgnoreCase) == true;
}

public sealed class UnavailableMoldHistoryRepository : IMoldHistoryRepository
{
    private static Task<T> Unavailable<T>() => Task.FromResult<T>(default!);

    public Task<IReadOnlyList<MoldMaintenanceReadModel>> ListMaintenanceAsync(
        MoldOperationContext context, Guid moldId, MoldHistoryListFilters filters, CancellationToken cancellationToken = default) =>
        Unavailable<IReadOnlyList<MoldMaintenanceReadModel>>();

    public Task<MoldMaintenanceReadModel?> GetMaintenanceAsync(
        MoldOperationContext context, Guid moldId, Guid maintenanceId, CancellationToken cancellationToken = default) =>
        Task.FromResult<MoldMaintenanceReadModel?>(null);

    public Task<MoldWriteResult> CreateMaintenanceAsync(
        MoldOperationContext context, CreateMoldMaintenanceCommand command, CancellationToken cancellationToken = default) =>
        Task.FromResult(MoldWriteResult.Fail(MoldFailureCodes.SourceUnavailable, "Mold history service is unavailable."));

    public Task<MoldWriteResult> UpdateMaintenanceAsync(
        MoldOperationContext context, UpdateMoldMaintenanceCommand command, CancellationToken cancellationToken = default) =>
        Task.FromResult(MoldWriteResult.Fail(MoldFailureCodes.SourceUnavailable, "Mold history service is unavailable."));

    public Task<MoldWriteResult> CompleteMaintenanceAsync(
        MoldOperationContext context, CompleteMoldMaintenanceCommand command, CancellationToken cancellationToken = default) =>
        Task.FromResult(MoldWriteResult.Fail(MoldFailureCodes.SourceUnavailable, "Mold history service is unavailable."));

    public Task<MoldWriteResult> CancelMaintenanceAsync(
        MoldOperationContext context, CancelMoldMaintenanceCommand command, CancellationToken cancellationToken = default) =>
        Task.FromResult(MoldWriteResult.Fail(MoldFailureCodes.SourceUnavailable, "Mold history service is unavailable."));

    public Task<IReadOnlyList<MoldRevisionReadModel>> ListRevisionsAsync(
        MoldOperationContext context, Guid moldId, MoldHistoryListFilters filters, CancellationToken cancellationToken = default) =>
        Unavailable<IReadOnlyList<MoldRevisionReadModel>>();

    public Task<MoldRevisionReadModel?> GetRevisionAsync(
        MoldOperationContext context, Guid moldId, Guid revisionId, CancellationToken cancellationToken = default) =>
        Task.FromResult<MoldRevisionReadModel?>(null);

    public Task<MoldWriteResult> AppendRevisionAsync(
        MoldOperationContext context, AppendMoldRevisionCommand command, CancellationToken cancellationToken = default) =>
        Task.FromResult(MoldWriteResult.Fail(MoldFailureCodes.SourceUnavailable, "Mold history service is unavailable."));

    public Task<IReadOnlyList<MoldTrialReadModel>> ListTrialsAsync(
        MoldOperationContext context, Guid moldId, MoldHistoryListFilters filters, CancellationToken cancellationToken = default) =>
        Unavailable<IReadOnlyList<MoldTrialReadModel>>();

    public Task<MoldTrialReadModel?> GetTrialAsync(
        MoldOperationContext context, Guid moldId, Guid trialId, CancellationToken cancellationToken = default) =>
        Task.FromResult<MoldTrialReadModel?>(null);

    public Task<MoldWriteResult> AppendTrialAsync(
        MoldOperationContext context, AppendMoldTrialCommand command, CancellationToken cancellationToken = default) =>
        Task.FromResult(MoldWriteResult.Fail(MoldFailureCodes.SourceUnavailable, "Mold history service is unavailable."));

    public Task<IReadOnlyList<MoldTimelineEntryReadModel>> ListTimelineAsync(
        MoldOperationContext context, Guid moldId, MoldHistoryListFilters filters, CancellationToken cancellationToken = default) =>
        Unavailable<IReadOnlyList<MoldTimelineEntryReadModel>>();
}
