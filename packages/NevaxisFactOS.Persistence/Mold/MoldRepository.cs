using System.Text.Json;
using NevaxisFactOS.Persistence.Audit;
using NevaxisFactOS.Persistence.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace NevaxisFactOS.Persistence.Mold;

public interface IMoldRepository
{
    Task<MoldWorkspaceSummaryReadModel> GetWorkspaceSummaryAsync(
        MoldOperationContext context,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MoldSummaryReadModel>> ListAsync(
        MoldOperationContext context,
        MoldListFilters filters,
        CancellationToken cancellationToken = default);

    Task<MoldDetailReadModel?> GetByIdAsync(
        MoldOperationContext context,
        Guid moldId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MoldLifecycleEventReadModel>> ListLifecycleHistoryAsync(
        MoldOperationContext context,
        Guid moldId,
        CancellationToken cancellationToken = default);

    Task<MoldWriteResult> CreateAsync(
        MoldOperationContext context,
        CreateMoldCommand command,
        CancellationToken cancellationToken = default);

    Task<MoldWriteResult> UpdateAsync(
        MoldOperationContext context,
        UpdateMoldCommand command,
        CancellationToken cancellationToken = default);

    Task<MoldWriteResult> RecordCounterSnapshotAsync(
        MoldOperationContext context,
        RecordMoldCounterSnapshotCommand command,
        CancellationToken cancellationToken = default);

    Task<MoldWriteResult> TransitionLifecycleAsync(
        MoldOperationContext context,
        TransitionMoldLifecycleCommand command,
        CancellationToken cancellationToken = default);
}

public sealed class MoldRepository(
    NevaxisFactOSDbContext dbContext,
    TenantDatabaseSession tenantDatabaseSession) : IMoldRepository
{
    public async Task<MoldWorkspaceSummaryReadModel> GetWorkspaceSummaryAsync(
        MoldOperationContext context,
        CancellationToken cancellationToken = default)
    {
        var counts = await tenantDatabaseSession.ExecuteReadAsync(
            async ct =>
            {
                var query = dbContext.Molds.AsNoTracking()
                    .Where(m => m.TenantId == context.TenantId && m.FacilityId == context.FacilityId);

                return new MoldWorkspaceSummaryReadModel(
                    await query.CountAsync(ct),
                    await query.CountAsync(m => m.LifecycleState == MoldLifecycleState.Draft, ct),
                    await query.CountAsync(m => m.LifecycleState == MoldLifecycleState.Active, ct),
                    await query.CountAsync(m => m.LifecycleState == MoldLifecycleState.Maintenance, ct),
                    await query.CountAsync(m => m.LifecycleState == MoldLifecycleState.Trial, ct),
                    await query.CountAsync(m => m.LifecycleState == MoldLifecycleState.Quarantined, ct),
                    await query.CountAsync(m => m.LifecycleState == MoldLifecycleState.Retired, ct),
                    await query.CountAsync(m => m.LifecycleState == MoldLifecycleState.Scrapped, ct));
            },
            cancellationToken);

        return counts;
    }

    public async Task<IReadOnlyList<MoldSummaryReadModel>> ListAsync(
        MoldOperationContext context,
        MoldListFilters filters,
        CancellationToken cancellationToken = default)
    {
        var page = Math.Max(filters.Page, 1);
        var pageSize = Math.Clamp(filters.PageSize, 1, 100);

        return await tenantDatabaseSession.ExecuteReadAsync(
            async ct =>
            {
                var query = dbContext.Molds.AsNoTracking()
                    .Where(m => m.TenantId == context.TenantId && m.FacilityId == context.FacilityId);

                if (!string.IsNullOrWhiteSpace(filters.Search))
                {
                    var search = filters.Search.Trim();
                    var normalized = search.ToUpperInvariant();
                    query = query.Where(m =>
                        m.Code.Contains(search) ||
                        m.CodeNormalized.Contains(normalized) ||
                        m.Name.Contains(search));
                }

                if (!string.IsNullOrWhiteSpace(filters.LifecycleState))
                {
                    var state = MoldValueConversions.LifecycleFromStorage(filters.LifecycleState.Trim());
                    query = query.Where(m => m.LifecycleState == state);
                }

                if (!string.IsNullOrWhiteSpace(filters.OwnershipType))
                {
                    var ownership = MoldValueConversions.OwnershipFromStorage(filters.OwnershipType.Trim());
                    query = query.Where(m => m.OwnershipType == ownership);
                }

                if (!string.IsNullOrWhiteSpace(filters.LocationType))
                {
                    var location = MoldValueConversions.LocationFromStorage(filters.LocationType.Trim());
                    query = query.Where(m => m.LocationType == location);
                }

                if (!string.IsNullOrWhiteSpace(filters.SourceType))
                {
                    var source = MoldValueConversions.SourceFromStorage(filters.SourceType.Trim());
                    query = query.Where(m => m.SourceType == source);
                }

                return await query
                    .OrderBy(m => m.CodeNormalized)
                    .ThenBy(m => m.Name)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(m => new MoldSummaryReadModel(
                        m.Id,
                        m.Code,
                        m.Name,
                        MoldValueConversions.ToStorage(m.LifecycleState),
                        MoldValueConversions.ToStorage(m.OwnershipType),
                        MoldValueConversions.ToStorage(m.LocationType),
                        m.LocationLabel,
                        m.FacilityId,
                        m.CavityCount,
                        m.CurrentShotCount,
                        m.CounterRecordedAtUtc,
                        MoldValueConversions.ToStorage(m.SourceType),
                        m.UpdatedAtUtc,
                        m.Version))
                    .ToListAsync(ct);
            },
            cancellationToken);
    }

    public async Task<MoldDetailReadModel?> GetByIdAsync(
        MoldOperationContext context,
        Guid moldId,
        CancellationToken cancellationToken = default)
    {
        var mold = await tenantDatabaseSession.ExecuteReadAsync(
            ct => dbContext.Molds.AsNoTracking()
                .SingleOrDefaultAsync(
                    m => m.TenantId == context.TenantId
                        && m.FacilityId == context.FacilityId
                        && m.Id == moldId,
                    ct),
            cancellationToken);

        return mold is null ? null : MapDetail(mold);
    }

    public async Task<IReadOnlyList<MoldLifecycleEventReadModel>> ListLifecycleHistoryAsync(
        MoldOperationContext context,
        Guid moldId,
        CancellationToken cancellationToken = default)
    {
        return await tenantDatabaseSession.ExecuteReadAsync(
            async ct =>
            {
                var exists = await dbContext.Molds.AsNoTracking()
                    .AnyAsync(
                        m => m.TenantId == context.TenantId
                            && m.FacilityId == context.FacilityId
                            && m.Id == moldId,
                        ct);

                if (!exists)
                {
                    return [];
                }

                return await dbContext.MoldLifecycleEvents.AsNoTracking()
                    .Where(e => e.TenantId == context.TenantId
                        && e.FacilityId == context.FacilityId
                        && e.MoldId == moldId)
                    .OrderByDescending(e => e.OccurredAtUtc)
                    .ThenByDescending(e => e.Id)
                    .Select(e => new MoldLifecycleEventReadModel(
                        e.Id,
                        MoldValueConversions.ToStorage(e.PreviousState),
                        MoldValueConversions.ToStorage(e.NewState),
                        e.Reason,
                        MoldActorDisplayNames.FromSubjectId(e.ActorSubjectId),
                        e.OccurredAtUtc,
                        e.SourceReference,
                        e.Version))
                    .ToListAsync(ct);
            },
            cancellationToken);
    }

    public Task<MoldWriteResult> CreateAsync(
        MoldOperationContext context,
        CreateMoldCommand command,
        CancellationToken cancellationToken = default) =>
        ExecuteWriteAsync(context, async ct =>
        {
            string? createFingerprint = null;
            if (!string.IsNullOrWhiteSpace(command.IdempotencyKey))
            {
                if (!MoldCodeNormalizer.TryNormalize(command.Code, out _, out var normalizedCodeForFingerprint, out _))
                {
                    // Validation below will return the proper code failure.
                }
                else
                {
                    createFingerprint = MoldIdempotencyFingerprint.ComputeCreateFingerprint(
                        context,
                        command,
                        normalizedCodeForFingerprint);
                }

                var existing = await dbContext.Molds
                    .SingleOrDefaultAsync(
                        m => m.TenantId == context.TenantId
                            && m.CreateIdempotencyKey == command.IdempotencyKey.Trim(),
                        ct);

                if (existing is not null)
                {
                    return ResolveCreateIdempotency(existing, createFingerprint);
                }
            }

            if (!MoldCodeNormalizer.TryNormalize(command.Code, out var displayCode, out var normalizedCode, out var codeFailure))
            {
                return MoldWriteResult.Fail(MoldFailureCodes.ValidationFailed, codeFailure!);
            }

            if (string.IsNullOrWhiteSpace(command.Name))
            {
                return MoldWriteResult.Fail(MoldFailureCodes.ValidationFailed, "Mold name is required.");
            }

            var technicalFailure = MoldTechnicalValidator.ValidateTechnicalProfile(
                command.CavityCount,
                command.NominalShotLife,
                command.CurrentShotCount,
                command.MoldWeightKg,
                command.DimensionLengthMm,
                command.DimensionWidthMm,
                command.DimensionHeightMm,
                command.ManufactureYear,
                command.ManufactureDate,
                command.CommissioningDate);

            if (technicalFailure is not null)
            {
                return MoldWriteResult.Fail(MoldFailureCodes.ValidationFailed, technicalFailure);
            }

            var now = DateTimeOffset.UtcNow;
            var actor = context.IdentitySubjectId.ToString();
            var moldId = Guid.NewGuid();
            createFingerprint ??= MoldIdempotencyFingerprint.ComputeCreateFingerprint(context, command, normalizedCode);
            var mold = new MoldRegistryEntry
            {
                Id = moldId,
                TenantId = context.TenantId,
                FacilityId = context.FacilityId,
                Code = displayCode,
                CodeNormalized = normalizedCode,
                Name = command.Name.Trim(),
                Description = command.Description?.Trim(),
                SerialNumber = command.SerialNumber?.Trim(),
                Manufacturer = command.Manufacturer?.Trim(),
                ManufactureYear = command.ManufactureYear,
                ManufactureDate = command.ManufactureDate,
                CommissioningDate = command.CommissioningDate,
                SourceType = MoldValueConversions.SourceFromStorage(command.SourceType),
                ExternalSourceReference = command.ExternalSourceReference?.Trim(),
                OwnershipType = MoldValueConversions.OwnershipFromStorage(command.OwnershipType),
                OwnerReference = command.OwnerReference?.Trim(),
                LocationType = MoldValueConversions.LocationFromStorage(command.LocationType),
                LocationLabel = command.LocationLabel?.Trim(),
                LocationChangedAtUtc = now,
                LocationChangedBySubjectId = actor,
                LifecycleState = MoldLifecycleState.Draft,
                MoldType = command.MoldType?.Trim(),
                CavityCount = command.CavityCount,
                NominalShotLife = command.NominalShotLife,
                CurrentShotCount = command.CurrentShotCount,
                PrimaryProductReference = command.PrimaryProductReference?.Trim(),
                MaterialFamilyReference = command.MaterialFamilyReference?.Trim(),
                RunnerType = command.RunnerType?.Trim(),
                HotRunnerPresent = command.HotRunnerPresent,
                MoldWeightKg = command.MoldWeightKg,
                DimensionLengthMm = command.DimensionLengthMm,
                DimensionWidthMm = command.DimensionWidthMm,
                DimensionHeightMm = command.DimensionHeightMm,
                Notes = command.Notes?.Trim(),
                CreateIdempotencyKey = command.IdempotencyKey?.Trim(),
                CreateIdempotencyPayloadFingerprint = createFingerprint,
                CreatedAtUtc = now,
                UpdatedAtUtc = now,
                CreatedBySubjectId = actor,
                UpdatedBySubjectId = actor,
                Version = 1,
            };

            dbContext.Molds.Add(mold);
            AddAudit(context, MoldAuditActions.Created, MoldAuditEventTypes.Created, "mold.registry", moldId, now,
                new { moldId, mold.Code, mold.LifecycleState });

            try
            {
                await dbContext.SaveChangesAsync(ct);
            }
            catch (DbUpdateException ex) when (IsUniqueViolation(ex))
            {
                if (!string.IsNullOrWhiteSpace(command.IdempotencyKey))
                {
                    var raced = await dbContext.Molds
                        .SingleOrDefaultAsync(
                            m => m.TenantId == context.TenantId
                                && m.CreateIdempotencyKey == command.IdempotencyKey!.Trim(),
                            ct);

                    if (raced is not null)
                    {
                        return ResolveCreateIdempotency(raced, createFingerprint);
                    }
                }

                var duplicateCode = await dbContext.Molds.AsNoTracking()
                    .AnyAsync(
                        m => m.TenantId == context.TenantId && m.CodeNormalized == normalizedCode,
                        ct);

                if (duplicateCode)
                {
                    return MoldWriteResult.Fail(
                        MoldFailureCodes.DuplicateCode,
                        "A mold with this code already exists for the tenant.");
                }

                throw;
            }

            return MoldWriteResult.Success(moldId, mold.Version);
        }, cancellationToken);

    public Task<MoldWriteResult> UpdateAsync(
        MoldOperationContext context,
        UpdateMoldCommand command,
        CancellationToken cancellationToken = default) =>
        ExecuteWriteAsync(context, async ct =>
        {
            var mold = await dbContext.Molds
                .SingleOrDefaultAsync(
                    m => m.TenantId == context.TenantId
                        && m.FacilityId == context.FacilityId
                        && m.Id == command.MoldId,
                    ct);

            if (mold is null)
            {
                return MoldWriteResult.Fail(MoldFailureCodes.NotFound, "Mold not found.");
            }

            if (mold.Version != command.ExpectedVersion)
            {
                return MoldWriteResult.Fail(
                    MoldFailureCodes.ConcurrencyConflict,
                    "The mold record was modified. Please reload and try again.");
            }

            if (string.IsNullOrWhiteSpace(command.Name))
            {
                return MoldWriteResult.Fail(MoldFailureCodes.ValidationFailed, "Mold name is required.");
            }

            var technicalFailure = MoldTechnicalValidator.ValidateTechnicalProfile(
                command.CavityCount,
                command.NominalShotLife,
                mold.CurrentShotCount,
                command.MoldWeightKg,
                command.DimensionLengthMm,
                command.DimensionWidthMm,
                command.DimensionHeightMm,
                command.ManufactureYear,
                command.ManufactureDate,
                command.CommissioningDate);

            if (technicalFailure is not null)
            {
                return MoldWriteResult.Fail(MoldFailureCodes.ValidationFailed, technicalFailure);
            }

            var now = DateTimeOffset.UtcNow;
            var actor = context.IdentitySubjectId.ToString();
            var locationType = MoldValueConversions.LocationFromStorage(command.LocationType);
            if (mold.LocationType != locationType || !string.Equals(mold.LocationLabel, command.LocationLabel?.Trim(), StringComparison.Ordinal))
            {
                mold.LocationChangedAtUtc = now;
                mold.LocationChangedBySubjectId = actor;
            }

            mold.Name = command.Name.Trim();
            mold.Description = command.Description?.Trim();
            mold.SerialNumber = command.SerialNumber?.Trim();
            mold.Manufacturer = command.Manufacturer?.Trim();
            mold.ManufactureYear = command.ManufactureYear;
            mold.ManufactureDate = command.ManufactureDate;
            mold.CommissioningDate = command.CommissioningDate;
            mold.ExternalSourceReference = command.ExternalSourceReference?.Trim();
            mold.OwnershipType = MoldValueConversions.OwnershipFromStorage(command.OwnershipType);
            mold.OwnerReference = command.OwnerReference?.Trim();
            mold.LocationType = locationType;
            mold.LocationLabel = command.LocationLabel?.Trim();
            mold.MoldType = command.MoldType?.Trim();
            mold.CavityCount = command.CavityCount;
            mold.NominalShotLife = command.NominalShotLife;
            mold.PrimaryProductReference = command.PrimaryProductReference?.Trim();
            mold.MaterialFamilyReference = command.MaterialFamilyReference?.Trim();
            mold.RunnerType = command.RunnerType?.Trim();
            mold.HotRunnerPresent = command.HotRunnerPresent;
            mold.MoldWeightKg = command.MoldWeightKg;
            mold.DimensionLengthMm = command.DimensionLengthMm;
            mold.DimensionWidthMm = command.DimensionWidthMm;
            mold.DimensionHeightMm = command.DimensionHeightMm;
            mold.Notes = command.Notes?.Trim();
            mold.UpdatedAtUtc = now;
            mold.UpdatedBySubjectId = actor;
            mold.Version += 1;

            AddAudit(context, MoldAuditActions.Updated, MoldAuditEventTypes.Updated, "mold.registry", mold.Id, now,
                new { moldId = mold.Id, mold.Code, mold.Version });

            await dbContext.SaveChangesAsync(ct);
            return MoldWriteResult.Success(mold.Id, mold.Version);
        }, cancellationToken);

    public Task<MoldWriteResult> RecordCounterSnapshotAsync(
        MoldOperationContext context,
        RecordMoldCounterSnapshotCommand command,
        CancellationToken cancellationToken = default) =>
        ExecuteWriteAsync(context, async ct =>
        {
            if (string.IsNullOrWhiteSpace(command.IdempotencyKey))
            {
                return MoldWriteResult.Fail(MoldFailureCodes.ValidationFailed, "Idempotency key is required.");
            }

            var idempotencyKey = command.IdempotencyKey.Trim();
            var existingSnapshot = await dbContext.MoldCounterSnapshots
                .SingleOrDefaultAsync(
                    s => s.TenantId == context.TenantId
                        && s.MoldId == command.MoldId
                        && s.IdempotencyKey == idempotencyKey,
                    ct);

            if (existingSnapshot is not null)
            {
                if (!MoldIdempotencyFingerprint.CounterPayloadMatches(existingSnapshot, command))
                {
                    return IdempotencyConflictResult();
                }

                var replayMold = await dbContext.Molds
                    .SingleAsync(m => m.Id == command.MoldId && m.TenantId == context.TenantId, ct);
                return MoldWriteResult.Success(replayMold.Id, replayMold.Version, idempotentReplay: true);
            }

            var mold = await dbContext.Molds
                .SingleOrDefaultAsync(
                    m => m.TenantId == context.TenantId
                        && m.FacilityId == context.FacilityId
                        && m.Id == command.MoldId,
                    ct);

            if (mold is null)
            {
                return MoldWriteResult.Fail(MoldFailureCodes.NotFound, "Mold not found.");
            }

            if (mold.Version != command.ExpectedVersion)
            {
                return MoldWriteResult.Fail(
                    MoldFailureCodes.ConcurrencyConflict,
                    "The mold record was modified. Please reload and try again.");
            }

            if (command.ShotCount < 0 || command.ShotCount > MoldTechnicalValidator.MaxShotCount)
            {
                return MoldWriteResult.Fail(MoldFailureCodes.ValidationFailed, "Shot count is invalid.");
            }

            var now = DateTimeOffset.UtcNow;
            var actor = context.IdentitySubjectId.ToString();
            var sourceType = MoldValueConversions.CounterSourceFromStorage(command.SourceType);
            var snapshot = new MoldCounterSnapshot
            {
                Id = Guid.NewGuid(),
                TenantId = context.TenantId,
                FacilityId = context.FacilityId,
                MoldId = mold.Id,
                ShotCount = command.ShotCount,
                RecordedAtUtc = now,
                SourceType = sourceType,
                SourceReference = command.SourceReference?.Trim(),
                RecordedBySubjectId = sourceType == MoldCounterSourceType.Manual ? actor : null,
                Note = command.Note?.Trim(),
                IdempotencyKey = idempotencyKey,
                Version = 1,
            };

            mold.CurrentShotCount = command.ShotCount;
            mold.CounterRecordedAtUtc = now;
            mold.CounterSource = sourceType;
            mold.CounterSourceReference = command.SourceReference?.Trim();
            mold.CounterRecordedBySubjectId = sourceType == MoldCounterSourceType.Manual ? actor : null;
            mold.CounterNote = command.Note?.Trim();
            mold.UpdatedAtUtc = now;
            mold.UpdatedBySubjectId = actor;
            mold.Version += 1;

            dbContext.MoldCounterSnapshots.Add(snapshot);
            AddAudit(context, MoldAuditActions.CounterRecorded, MoldAuditEventTypes.CounterRecorded, "mold.registry", mold.Id, now,
                new { moldId = mold.Id, command.ShotCount, sourceType = command.SourceType });

            try
            {
                await dbContext.SaveChangesAsync(ct);
            }
            catch (DbUpdateException ex) when (IsUniqueViolation(ex))
            {
                var racedSnapshot = await dbContext.MoldCounterSnapshots
                    .SingleOrDefaultAsync(
                        s => s.TenantId == context.TenantId
                            && s.MoldId == command.MoldId
                            && s.IdempotencyKey == idempotencyKey,
                        ct);

                if (racedSnapshot is null)
                {
                    throw;
                }

                if (!MoldIdempotencyFingerprint.CounterPayloadMatches(racedSnapshot, command))
                {
                    return IdempotencyConflictResult();
                }

                await dbContext.Entry(mold).ReloadAsync(ct);
                return MoldWriteResult.Success(mold.Id, mold.Version, idempotentReplay: true);
            }

            return MoldWriteResult.Success(mold.Id, mold.Version);
        }, cancellationToken);

    public Task<MoldWriteResult> TransitionLifecycleAsync(
        MoldOperationContext context,
        TransitionMoldLifecycleCommand command,
        CancellationToken cancellationToken = default) =>
        ExecuteWriteAsync(context, async ct =>
        {
            if (string.IsNullOrWhiteSpace(command.Reason))
            {
                return MoldWriteResult.Fail(MoldFailureCodes.ValidationFailed, "Transition reason is required.");
            }

            if (string.IsNullOrWhiteSpace(command.IdempotencyKey))
            {
                return MoldWriteResult.Fail(MoldFailureCodes.ValidationFailed, "Idempotency key is required.");
            }

            var idempotencyKey = command.IdempotencyKey.Trim();
            var existingEvent = await dbContext.MoldLifecycleEvents
                .SingleOrDefaultAsync(
                    e => e.TenantId == context.TenantId
                        && e.MoldId == command.MoldId
                        && e.IdempotencyKey == idempotencyKey,
                    ct);

            if (existingEvent is not null)
            {
                if (!MoldIdempotencyFingerprint.TransitionPayloadMatches(existingEvent, command))
                {
                    return IdempotencyConflictResult();
                }

                var replayMold = await dbContext.Molds
                    .SingleAsync(m => m.Id == command.MoldId && m.TenantId == context.TenantId, ct);
                return MoldWriteResult.Success(replayMold.Id, replayMold.Version, idempotentReplay: true);
            }

            var mold = await dbContext.Molds
                .SingleOrDefaultAsync(
                    m => m.TenantId == context.TenantId
                        && m.FacilityId == context.FacilityId
                        && m.Id == command.MoldId,
                    ct);

            if (mold is null)
            {
                return MoldWriteResult.Fail(MoldFailureCodes.NotFound, "Mold not found.");
            }

            if (mold.Version != command.ExpectedVersion)
            {
                return MoldWriteResult.Fail(
                    MoldFailureCodes.ConcurrencyConflict,
                    "The mold record was modified. Please reload and try again.");
            }

            var targetState = MoldValueConversions.LifecycleFromStorage(command.TargetState);
            if (!MoldLifecycleStateMachine.CanTransition(mold.LifecycleState, targetState))
            {
                return MoldWriteResult.Fail(
                    MoldFailureCodes.InvalidTransition,
                    "This lifecycle transition is not allowed.");
            }

            var now = DateTimeOffset.UtcNow;
            var actor = context.IdentitySubjectId.ToString();
            var previousState = mold.LifecycleState;
            mold.LifecycleState = targetState;
            mold.UpdatedAtUtc = now;
            mold.UpdatedBySubjectId = actor;
            mold.Version += 1;

            dbContext.MoldLifecycleEvents.Add(new MoldLifecycleEvent
            {
                Id = Guid.NewGuid(),
                TenantId = context.TenantId,
                FacilityId = context.FacilityId,
                MoldId = mold.Id,
                PreviousState = previousState,
                NewState = targetState,
                Reason = command.Reason.Trim(),
                ActorSubjectId = actor,
                OccurredAtUtc = now,
                IdempotencyKey = idempotencyKey,
                SourceReference = command.SourceReference?.Trim(),
                Version = 1,
            });

            AddAudit(context, MoldAuditActions.LifecycleTransitioned, MoldAuditEventTypes.LifecycleTransitioned,
                "mold.registry", mold.Id, now,
                new
                {
                    moldId = mold.Id,
                    previousState = MoldValueConversions.ToStorage(previousState),
                    newState = MoldValueConversions.ToStorage(targetState),
                    command.Reason,
                });

            try
            {
                await dbContext.SaveChangesAsync(ct);
            }
            catch (DbUpdateException ex) when (IsUniqueViolation(ex))
            {
                var racedEvent = await dbContext.MoldLifecycleEvents
                    .SingleOrDefaultAsync(
                        e => e.TenantId == context.TenantId
                            && e.MoldId == command.MoldId
                            && e.IdempotencyKey == idempotencyKey,
                        ct);

                if (racedEvent is null)
                {
                    throw;
                }

                if (!MoldIdempotencyFingerprint.TransitionPayloadMatches(racedEvent, command))
                {
                    return IdempotencyConflictResult();
                }

                await dbContext.Entry(mold).ReloadAsync(ct);
                return MoldWriteResult.Success(mold.Id, mold.Version, idempotentReplay: true);
            }

            return MoldWriteResult.Success(mold.Id, mold.Version);
        }, cancellationToken);

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

    private static MoldWriteResult IdempotencyConflictResult() =>
        MoldWriteResult.Fail(
            MoldFailureCodes.IdempotencyConflict,
            "The idempotency key was already used with a different command payload.");

    private static MoldWriteResult IdempotencyLegacyUnverifiableResult() =>
        MoldWriteResult.Fail(
            MoldFailureCodes.IdempotencyLegacyUnverifiable,
            "The idempotency key belongs to a legacy create that cannot be verified safely.");

    private static MoldWriteResult ResolveCreateIdempotency(
        MoldRegistryEntry existing,
        string? createFingerprint)
    {
        if (string.IsNullOrEmpty(existing.CreateIdempotencyPayloadFingerprint))
        {
            return IdempotencyLegacyUnverifiableResult();
        }

        if (!string.IsNullOrEmpty(createFingerprint)
            && existing.CreateIdempotencyPayloadFingerprint == createFingerprint)
        {
            return MoldWriteResult.Success(existing.Id, existing.Version, idempotentReplay: true);
        }

        return IdempotencyConflictResult();
    }

    private static MoldDetailReadModel MapDetail(MoldRegistryEntry mold) =>
        new(
            mold.Id,
            mold.FacilityId,
            mold.Code,
            mold.Name,
            mold.Description,
            mold.SerialNumber,
            mold.Manufacturer,
            mold.ManufactureYear,
            mold.ManufactureDate,
            mold.CommissioningDate,
            MoldValueConversions.ToStorage(mold.SourceType),
            mold.ExternalSourceReference,
            MoldValueConversions.ToStorage(mold.OwnershipType),
            mold.OwnerReference,
            MoldValueConversions.ToStorage(mold.LocationType),
            mold.LocationLabel,
            mold.LocationChangedAtUtc,
            MoldValueConversions.ToStorage(mold.LifecycleState),
            mold.MoldType,
            mold.CavityCount,
            mold.NominalShotLife,
            mold.CurrentShotCount,
            mold.CounterRecordedAtUtc,
            mold.CounterSource.HasValue ? MoldValueConversions.ToStorage(mold.CounterSource.Value) : null,
            mold.CounterSourceReference,
            mold.CounterNote,
            mold.PrimaryProductReference,
            mold.MaterialFamilyReference,
            mold.RunnerType,
            mold.HotRunnerPresent,
            mold.MoldWeightKg,
            mold.DimensionLengthMm,
            mold.DimensionWidthMm,
            mold.DimensionHeightMm,
            mold.Notes,
            mold.CreatedAtUtc,
            mold.UpdatedAtUtc,
            mold.Version);

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

public static class MoldActorDisplayNames
{
    public static string FromSubjectId(string subjectId)
    {
        if (Guid.TryParse(subjectId, out _))
        {
            return "Operasyon Kullanıcısı";
        }

        return string.IsNullOrWhiteSpace(subjectId) ? "Bilinmiyor" : subjectId;
    }
}

public sealed class UnavailableMoldRepository : IMoldRepository
{
    public Task<MoldWorkspaceSummaryReadModel> GetWorkspaceSummaryAsync(
        MoldOperationContext context,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(new MoldWorkspaceSummaryReadModel(0, 0, 0, 0, 0, 0, 0, 0));

    public Task<IReadOnlyList<MoldSummaryReadModel>> ListAsync(
        MoldOperationContext context,
        MoldListFilters filters,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<MoldSummaryReadModel>>([]);

    public Task<MoldDetailReadModel?> GetByIdAsync(
        MoldOperationContext context,
        Guid moldId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<MoldDetailReadModel?>(null);

    public Task<IReadOnlyList<MoldLifecycleEventReadModel>> ListLifecycleHistoryAsync(
        MoldOperationContext context,
        Guid moldId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<MoldLifecycleEventReadModel>>([]);

    public Task<MoldWriteResult> CreateAsync(
        MoldOperationContext context,
        CreateMoldCommand command,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(MoldWriteResult.Fail(MoldFailureCodes.SourceUnavailable, "Mold service is unavailable."));

    public Task<MoldWriteResult> UpdateAsync(
        MoldOperationContext context,
        UpdateMoldCommand command,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(MoldWriteResult.Fail(MoldFailureCodes.SourceUnavailable, "Mold service is unavailable."));

    public Task<MoldWriteResult> RecordCounterSnapshotAsync(
        MoldOperationContext context,
        RecordMoldCounterSnapshotCommand command,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(MoldWriteResult.Fail(MoldFailureCodes.SourceUnavailable, "Mold service is unavailable."));

    public Task<MoldWriteResult> TransitionLifecycleAsync(
        MoldOperationContext context,
        TransitionMoldLifecycleCommand command,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(MoldWriteResult.Fail(MoldFailureCodes.SourceUnavailable, "Mold service is unavailable."));
}
