using System.Text.Json;
using NevaxisZentra.Persistence.Audit;
using NevaxisZentra.Persistence.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace NevaxisZentra.Persistence.Material;

public interface IMaterialRepository
{
    Task<MaterialWorkspaceSummaryReadModel> GetWorkspaceSummaryAsync(
        MaterialOperationContext context,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MaterialSummaryReadModel>> ListAsync(
        MaterialOperationContext context,
        MaterialListFilters filters,
        CancellationToken cancellationToken = default);

    Task<MaterialDetailReadModel?> GetByIdAsync(
        MaterialOperationContext context,
        Guid materialId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MaterialLifecycleEventReadModel>> ListLifecycleHistoryAsync(
        MaterialOperationContext context,
        Guid materialId,
        CancellationToken cancellationToken = default);

    Task<MaterialTechnicalSpecificationReadModel?> GetTechnicalSpecificationAsync(
        MaterialOperationContext context,
        Guid materialId,
        CancellationToken cancellationToken = default);

    Task<MaterialWriteResult> CreateAsync(
        MaterialOperationContext context,
        CreateMaterialCommand command,
        CancellationToken cancellationToken = default);

    Task<MaterialWriteResult> UpdateAsync(
        MaterialOperationContext context,
        UpdateMaterialCommand command,
        CancellationToken cancellationToken = default);

    Task<MaterialWriteResult> UpdateTechnicalSpecificationAsync(
        MaterialOperationContext context,
        UpdateMaterialTechnicalSpecificationCommand command,
        CancellationToken cancellationToken = default);

    Task<MaterialWriteResult> TransitionLifecycleAsync(
        MaterialOperationContext context,
        TransitionMaterialLifecycleCommand command,
        CancellationToken cancellationToken = default);
}

public sealed class MaterialRepository(
    NevaxisZentraDbContext dbContext,
    TenantDatabaseSession tenantDatabaseSession) : IMaterialRepository
{
    public async Task<MaterialWorkspaceSummaryReadModel> GetWorkspaceSummaryAsync(
        MaterialOperationContext context,
        CancellationToken cancellationToken = default)
    {
        return await tenantDatabaseSession.ExecuteReadAsync(
            async ct =>
            {
                var query = dbContext.Materials.AsNoTracking()
                    .Include(m => m.TechnicalSpecification)
                    .Where(m => m.TenantId == context.TenantId && m.FacilityId == context.FacilityId);

                var materials = await query.ToListAsync(ct);
                return new MaterialWorkspaceSummaryReadModel(
                    materials.Count,
                    materials.Count(m => m.LifecycleStatus == MaterialLifecycleStatus.Active),
                    materials.Count(m => m.LifecycleStatus == MaterialLifecycleStatus.Blocked),
                    materials.Count(m => m.LifecycleStatus == MaterialLifecycleStatus.Obsolete),
                    materials.Count(m => !MaterialTechnicalValidator.HasTechnicalData(m.TechnicalSpecification)));
            },
            cancellationToken);
    }

    public async Task<IReadOnlyList<MaterialSummaryReadModel>> ListAsync(
        MaterialOperationContext context,
        MaterialListFilters filters,
        CancellationToken cancellationToken = default)
    {
        var page = Math.Max(filters.Page, 1);
        var pageSize = Math.Clamp(filters.PageSize, 1, 100);

        return await tenantDatabaseSession.ExecuteReadAsync(
            async ct =>
            {
                var query = dbContext.Materials.AsNoTracking()
                    .Include(m => m.TechnicalSpecification)
                    .Where(m => m.TenantId == context.TenantId && m.FacilityId == context.FacilityId);

                if (!string.IsNullOrWhiteSpace(filters.Search))
                {
                    var search = filters.Search.Trim();
                    var normalized = search.ToUpperInvariant();
                    query = query.Where(m =>
                        m.Code.Contains(search) ||
                        m.CodeNormalized.Contains(normalized) ||
                        m.Name.Contains(search) ||
                        (m.Manufacturer != null && m.Manufacturer.Contains(search)) ||
                        (m.TradeNameOrGrade != null && m.TradeNameOrGrade.Contains(search)));
                }

                if (!string.IsNullOrWhiteSpace(filters.LifecycleStatus))
                {
                    var status = MaterialValueConversions.LifecycleFromStorage(filters.LifecycleStatus.Trim());
                    query = query.Where(m => m.LifecycleStatus == status);
                }

                if (!string.IsNullOrWhiteSpace(filters.MaterialCategory))
                {
                    var category = MaterialValueConversions.CategoryFromStorage(filters.MaterialCategory.Trim());
                    query = query.Where(m => m.MaterialCategory == category);
                }

                if (!string.IsNullOrWhiteSpace(filters.PolymerFamily))
                {
                    var polymer = MaterialValueConversions.PolymerFromStorage(filters.PolymerFamily.Trim());
                    query = query.Where(m => m.PolymerFamily == polymer);
                }

                if (filters.IsActive.HasValue)
                {
                    query = query.Where(m => m.IsActive == filters.IsActive.Value);
                }

                var materials = await query.ToListAsync(ct);

                return materials
                    .OrderBy(m => MaterialValueConversions.LifecycleSortPriority(m.LifecycleStatus))
                    .ThenBy(m => m.Name)
                    .ThenBy(m => m.CodeNormalized)
                    .ThenBy(m => m.Id)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(MapSummary)
                    .ToList();
            },
            cancellationToken);
    }

    public Task<MaterialDetailReadModel?> GetByIdAsync(
        MaterialOperationContext context,
        Guid materialId,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(
            async ct =>
            {
                var material = await dbContext.Materials.AsNoTracking()
                    .Include(m => m.TechnicalSpecification)
                    .SingleOrDefaultAsync(
                        m => m.TenantId == context.TenantId
                            && m.FacilityId == context.FacilityId
                            && m.Id == materialId,
                        ct);

                return material is null ? null : MapDetail(material);
            },
            cancellationToken);

    public Task<IReadOnlyList<MaterialLifecycleEventReadModel>> ListLifecycleHistoryAsync(
        MaterialOperationContext context,
        Guid materialId,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(
            async ct =>
            {
                var exists = await dbContext.Materials.AsNoTracking()
                    .AnyAsync(
                        m => m.TenantId == context.TenantId
                            && m.FacilityId == context.FacilityId
                            && m.Id == materialId,
                        ct);

                if (!exists)
                {
                    return (IReadOnlyList<MaterialLifecycleEventReadModel>)[];
                }

                return await dbContext.MaterialLifecycleEvents.AsNoTracking()
                    .Where(e => e.TenantId == context.TenantId && e.MaterialId == materialId)
                    .OrderByDescending(e => e.OccurredAtUtc)
                    .Select(e => new MaterialLifecycleEventReadModel(
                        e.Id,
                        MaterialValueConversions.ToStorage(e.PreviousStatus),
                        MaterialValueConversions.ToStorage(e.NewStatus),
                        e.Reason,
                        MaterialActorDisplayNames.FromSubjectId(e.ActorSubjectId),
                        e.OccurredAtUtc,
                        e.SourceReference,
                        e.Version))
                    .ToListAsync(ct);
            },
            cancellationToken);

    public Task<MaterialTechnicalSpecificationReadModel?> GetTechnicalSpecificationAsync(
        MaterialOperationContext context,
        Guid materialId,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(
            async ct =>
            {
                var material = await dbContext.Materials.AsNoTracking()
                    .Include(m => m.TechnicalSpecification)
                    .SingleOrDefaultAsync(
                        m => m.TenantId == context.TenantId
                            && m.FacilityId == context.FacilityId
                            && m.Id == materialId,
                        ct);

                if (material is null)
                {
                    return null;
                }

                return material.TechnicalSpecification is null
                    ? null
                    : MapTechnicalSpecification(material.TechnicalSpecification);
            },
            cancellationToken);

    public Task<MaterialWriteResult> CreateAsync(
        MaterialOperationContext context,
        CreateMaterialCommand command,
        CancellationToken cancellationToken = default) =>
        ExecuteWriteAsync(context, async ct =>
        {
            string? createFingerprint = null;
            if (!string.IsNullOrWhiteSpace(command.IdempotencyKey))
            {
                if (MaterialCodeNormalizer.TryNormalize(command.Code, out _, out var normalizedCodeForFingerprint, out _))
                {
                    createFingerprint = MaterialIdempotencyFingerprint.ComputeCreateFingerprint(
                        context,
                        command,
                        normalizedCodeForFingerprint);
                }

                var existing = await dbContext.Materials
                    .SingleOrDefaultAsync(
                        m => m.TenantId == context.TenantId
                            && m.CreateIdempotencyKey == command.IdempotencyKey.Trim(),
                        ct);

                if (existing is not null)
                {
                    return ResolveCreateIdempotency(existing, createFingerprint);
                }
            }

            if (!MaterialCodeNormalizer.TryNormalize(command.Code, out var displayCode, out var normalizedCode, out var codeFailure))
            {
                return MaterialWriteResult.Fail(MaterialFailureCodes.ValidationFailed, codeFailure!);
            }

            if (string.IsNullOrWhiteSpace(command.Name))
            {
                return MaterialWriteResult.Fail(MaterialFailureCodes.ValidationFailed, "Material name is required.");
            }

            if (string.IsNullOrWhiteSpace(command.UnitOfMeasure))
            {
                return MaterialWriteResult.Fail(MaterialFailureCodes.ValidationFailed, "Unit of measure is required.");
            }

            var now = DateTimeOffset.UtcNow;
            var actor = context.IdentitySubjectId.ToString();
            var materialId = Guid.NewGuid();
            createFingerprint ??= MaterialIdempotencyFingerprint.ComputeCreateFingerprint(context, command, normalizedCode);

            var material = new MaterialRegistryEntry
            {
                Id = materialId,
                TenantId = context.TenantId,
                FacilityId = context.FacilityId,
                Code = displayCode,
                CodeNormalized = normalizedCode,
                Name = command.Name.Trim(),
                Description = command.Description?.Trim(),
                MaterialCategory = MaterialValueConversions.CategoryFromStorage(command.MaterialCategory),
                PolymerFamily = MaterialValueConversions.PolymerFromStorage(command.PolymerFamily),
                Manufacturer = command.Manufacturer?.Trim(),
                TradeNameOrGrade = command.TradeNameOrGrade?.Trim(),
                ColorName = command.ColorName?.Trim(),
                ColorCode = command.ColorCode?.Trim(),
                PhysicalForm = MaterialValueConversions.PhysicalFormFromStorage(command.PhysicalForm),
                LifecycleStatus = MaterialLifecycleStatus.Draft,
                UnitOfMeasure = command.UnitOfMeasure.Trim(),
                IsActive = command.IsActive,
                SourceType = MaterialValueConversions.SourceFromStorage(command.SourceType),
                SourceSystem = command.SourceSystem?.Trim(),
                SourceReference = command.SourceReference?.Trim(),
                ExternalMaterialCode = command.ExternalMaterialCode?.Trim(),
                DataRecordedAtUtc = command.DataRecordedAtUtc,
                CreateIdempotencyKey = command.IdempotencyKey?.Trim(),
                CreateIdempotencyPayloadFingerprint = createFingerprint,
                CreatedAtUtc = now,
                UpdatedAtUtc = now,
                CreatedBySubjectId = actor,
                UpdatedBySubjectId = actor,
                Version = 1,
            };

            dbContext.Materials.Add(material);
            AddAudit(context, MaterialAuditActions.Created, MaterialAuditEventTypes.Created, "material.registry", materialId, now,
                new { materialId, material.Code, material.LifecycleStatus });

            try
            {
                await dbContext.SaveChangesAsync(ct);
            }
            catch (DbUpdateException ex) when (IsUniqueViolation(ex))
            {
                DetachFailedCreate(materialId, material);

                if (!string.IsNullOrWhiteSpace(command.IdempotencyKey))
                {
                    var raced = await dbContext.Materials
                        .SingleOrDefaultAsync(
                            m => m.TenantId == context.TenantId
                                && m.CreateIdempotencyKey == command.IdempotencyKey!.Trim(),
                            ct);

                    if (raced is not null)
                    {
                        return ResolveCreateIdempotency(raced, createFingerprint);
                    }
                }

                var duplicateCode = await dbContext.Materials.AsNoTracking()
                    .AnyAsync(
                        m => m.TenantId == context.TenantId && m.CodeNormalized == normalizedCode,
                        ct);

                if (duplicateCode)
                {
                    return MaterialWriteResult.Fail(
                        MaterialFailureCodes.DuplicateCode,
                        "A material with this code already exists for the tenant.");
                }

                throw;
            }

            return MaterialWriteResult.Success(materialId, material.Version);
        }, cancellationToken);

    public Task<MaterialWriteResult> UpdateAsync(
        MaterialOperationContext context,
        UpdateMaterialCommand command,
        CancellationToken cancellationToken = default) =>
        ExecuteWriteAsync(context, async ct =>
        {
            var material = await dbContext.Materials
                .SingleOrDefaultAsync(
                    m => m.TenantId == context.TenantId
                        && m.FacilityId == context.FacilityId
                        && m.Id == command.MaterialId,
                    ct);

            if (material is null)
            {
                return MaterialWriteResult.Fail(MaterialFailureCodes.NotFound, "Material not found.");
            }

            if (material.Version != command.ExpectedVersion)
            {
                return MaterialWriteResult.Fail(
                    MaterialFailureCodes.ConcurrencyConflict,
                    "The material record was modified. Please reload and try again.");
            }

            if (MaterialLifecycleStateMachine.IsTerminal(material.LifecycleStatus))
            {
                return MaterialWriteResult.Fail(
                    MaterialFailureCodes.ValidationFailed,
                    "Obsolete materials cannot be updated.");
            }

            if (string.IsNullOrWhiteSpace(command.Name))
            {
                return MaterialWriteResult.Fail(MaterialFailureCodes.ValidationFailed, "Material name is required.");
            }

            if (string.IsNullOrWhiteSpace(command.UnitOfMeasure))
            {
                return MaterialWriteResult.Fail(MaterialFailureCodes.ValidationFailed, "Unit of measure is required.");
            }

            var now = DateTimeOffset.UtcNow;
            var actor = context.IdentitySubjectId.ToString();

            material.Name = command.Name.Trim();
            material.Description = command.Description?.Trim();
            material.MaterialCategory = MaterialValueConversions.CategoryFromStorage(command.MaterialCategory);
            material.PolymerFamily = MaterialValueConversions.PolymerFromStorage(command.PolymerFamily);
            material.Manufacturer = command.Manufacturer?.Trim();
            material.TradeNameOrGrade = command.TradeNameOrGrade?.Trim();
            material.ColorName = command.ColorName?.Trim();
            material.ColorCode = command.ColorCode?.Trim();
            material.PhysicalForm = MaterialValueConversions.PhysicalFormFromStorage(command.PhysicalForm);
            material.UnitOfMeasure = command.UnitOfMeasure.Trim();
            material.IsActive = command.IsActive;
            material.SourceSystem = command.SourceSystem?.Trim();
            material.SourceReference = command.SourceReference?.Trim();
            material.ExternalMaterialCode = command.ExternalMaterialCode?.Trim();
            material.DataRecordedAtUtc = command.DataRecordedAtUtc;
            material.UpdatedAtUtc = now;
            material.UpdatedBySubjectId = actor;
            material.Version += 1;

            AddAudit(context, MaterialAuditActions.Updated, MaterialAuditEventTypes.Updated, "material.registry", material.Id, now,
                new { materialId = material.Id, material.Code });

            await dbContext.SaveChangesAsync(ct);
            return MaterialWriteResult.Success(material.Id, material.Version);
        }, cancellationToken);

    public Task<MaterialWriteResult> UpdateTechnicalSpecificationAsync(
        MaterialOperationContext context,
        UpdateMaterialTechnicalSpecificationCommand command,
        CancellationToken cancellationToken = default) =>
        ExecuteWriteAsync(context, async ct =>
        {
            var material = await dbContext.Materials
                .Include(m => m.TechnicalSpecification)
                .SingleOrDefaultAsync(
                    m => m.TenantId == context.TenantId
                        && m.FacilityId == context.FacilityId
                        && m.Id == command.MaterialId,
                    ct);

            if (material is null)
            {
                return MaterialWriteResult.Fail(MaterialFailureCodes.NotFound, "Material not found.");
            }

            if (material.Version != command.ExpectedVersion)
            {
                return MaterialWriteResult.Fail(
                    MaterialFailureCodes.ConcurrencyConflict,
                    "The material record was modified. Please reload and try again.");
            }

            var validationFailure = MaterialTechnicalValidator.ValidateTechnicalSpecification(
                command.DensityGPerCm3,
                command.MeltFlowRate,
                command.MeltFlowTestTemperatureC,
                command.MeltFlowTestLoadKg,
                command.MoistureLimitPercent,
                command.DryingRequired,
                command.DryingTemperatureMinC,
                command.DryingTemperatureMaxC,
                command.DryingTimeMinHours,
                command.DryingTimeMaxHours,
                command.MeltTemperatureMinC,
                command.MeltTemperatureMaxC,
                command.MoldTemperatureMinC,
                command.MoldTemperatureMaxC,
                command.StorageTemperatureMinC,
                command.StorageTemperatureMaxC,
                command.MaximumRegrindPercent);

            if (validationFailure is not null)
            {
                return MaterialWriteResult.Fail(MaterialFailureCodes.ValidationFailed, validationFailure);
            }

            var now = DateTimeOffset.UtcNow;
            var actor = context.IdentitySubjectId.ToString();
            var isNewSpec = material.TechnicalSpecification is null;

            if (isNewSpec)
            {
                material.TechnicalSpecification = new MaterialTechnicalSpecification
                {
                    Id = Guid.NewGuid(),
                    TenantId = context.TenantId,
                    MaterialId = material.Id,
                    CreatedAtUtc = now,
                    CreatedBySubjectId = actor,
                    UpdatedAtUtc = now,
                    UpdatedBySubjectId = actor,
                    Version = 1,
                };
                dbContext.MaterialTechnicalSpecifications.Add(material.TechnicalSpecification);
            }

            var spec = material.TechnicalSpecification!;
            spec.DensityGPerCm3 = command.DensityGPerCm3;
            spec.MeltFlowRate = command.MeltFlowRate;
            spec.MeltFlowUnit = command.MeltFlowUnit?.Trim();
            spec.MeltFlowTestTemperatureC = command.MeltFlowTestTemperatureC;
            spec.MeltFlowTestLoadKg = command.MeltFlowTestLoadKg;
            spec.MoistureLimitPercent = command.MoistureLimitPercent;
            spec.DryingRequired = command.DryingRequired;
            spec.DryingTemperatureMinC = command.DryingTemperatureMinC;
            spec.DryingTemperatureMaxC = command.DryingTemperatureMaxC;
            spec.DryingTimeMinHours = command.DryingTimeMinHours;
            spec.DryingTimeMaxHours = command.DryingTimeMaxHours;
            spec.MeltTemperatureMinC = command.MeltTemperatureMinC;
            spec.MeltTemperatureMaxC = command.MeltTemperatureMaxC;
            spec.MoldTemperatureMinC = command.MoldTemperatureMinC;
            spec.MoldTemperatureMaxC = command.MoldTemperatureMaxC;
            spec.StorageTemperatureMinC = command.StorageTemperatureMinC;
            spec.StorageTemperatureMaxC = command.StorageTemperatureMaxC;
            spec.MaximumRegrindPercent = command.MaximumRegrindPercent;
            spec.TechnicalNotes = command.TechnicalNotes?.Trim();
            spec.SafetyDataSheetReference = command.SafetyDataSheetReference?.Trim();
            spec.TechnicalDataSheetReference = command.TechnicalDataSheetReference?.Trim();
            spec.SpecificationSource = MaterialValueConversions.SpecificationSourceFromStorage(command.SpecificationSource);
            spec.SpecificationSourceReference = command.SpecificationSourceReference?.Trim();
            spec.SpecificationRecordedAtUtc = command.SpecificationRecordedAtUtc;
            spec.UpdatedAtUtc = now;
            spec.UpdatedBySubjectId = actor;
            if (!isNewSpec)
            {
                spec.Version += 1;
            }

            material.UpdatedAtUtc = now;
            material.UpdatedBySubjectId = actor;
            material.Version += 1;

            AddAudit(context, MaterialAuditActions.TechnicalSpecificationUpdated,
                MaterialAuditEventTypes.TechnicalSpecificationUpdated, "material.registry", material.Id, now,
                new { materialId = material.Id });

            await dbContext.SaveChangesAsync(ct);
            return MaterialWriteResult.Success(material.Id, material.Version);
        }, cancellationToken);

    public Task<MaterialWriteResult> TransitionLifecycleAsync(
        MaterialOperationContext context,
        TransitionMaterialLifecycleCommand command,
        CancellationToken cancellationToken = default) =>
        ExecuteWriteAsync(context, async ct =>
        {
            if (string.IsNullOrWhiteSpace(command.Reason))
            {
                return MaterialWriteResult.Fail(MaterialFailureCodes.ValidationFailed, "Transition reason is required.");
            }

            if (string.IsNullOrWhiteSpace(command.IdempotencyKey))
            {
                return MaterialWriteResult.Fail(MaterialFailureCodes.ValidationFailed, "Idempotency key is required.");
            }

            var idempotencyKey = command.IdempotencyKey.Trim();
            var existingEvent = await dbContext.MaterialLifecycleEvents
                .SingleOrDefaultAsync(
                    e => e.TenantId == context.TenantId
                        && e.MaterialId == command.MaterialId
                        && e.IdempotencyKey == idempotencyKey,
                    ct);

            if (existingEvent is not null)
            {
                if (!MaterialIdempotencyFingerprint.TransitionPayloadMatches(existingEvent, command))
                {
                    return IdempotencyConflictResult();
                }

                var replayMaterial = await dbContext.Materials
                    .SingleAsync(m => m.Id == command.MaterialId && m.TenantId == context.TenantId, ct);
                return MaterialWriteResult.Success(replayMaterial.Id, replayMaterial.Version, idempotentReplay: true);
            }

            var material = await dbContext.Materials
                .SingleOrDefaultAsync(
                    m => m.TenantId == context.TenantId
                        && m.FacilityId == context.FacilityId
                        && m.Id == command.MaterialId,
                    ct);

            if (material is null)
            {
                return MaterialWriteResult.Fail(MaterialFailureCodes.NotFound, "Material not found.");
            }

            if (material.Version != command.ExpectedVersion)
            {
                return MaterialWriteResult.Fail(
                    MaterialFailureCodes.ConcurrencyConflict,
                    "The material record was modified. Please reload and try again.");
            }

            var targetStatus = MaterialValueConversions.LifecycleFromStorage(command.TargetStatus);
            if (!MaterialLifecycleStateMachine.CanTransition(material.LifecycleStatus, targetStatus))
            {
                return MaterialWriteResult.Fail(
                    MaterialFailureCodes.InvalidTransition,
                    "This lifecycle transition is not allowed.");
            }

            var now = DateTimeOffset.UtcNow;
            var actor = context.IdentitySubjectId.ToString();
            var previousStatus = material.LifecycleStatus;
            material.LifecycleStatus = targetStatus;
            material.IsActive = targetStatus == MaterialLifecycleStatus.Active;
            material.UpdatedAtUtc = now;
            material.UpdatedBySubjectId = actor;
            material.Version += 1;

            dbContext.MaterialLifecycleEvents.Add(new MaterialLifecycleEvent
            {
                Id = Guid.NewGuid(),
                TenantId = context.TenantId,
                FacilityId = context.FacilityId,
                MaterialId = material.Id,
                PreviousStatus = previousStatus,
                NewStatus = targetStatus,
                Reason = command.Reason.Trim(),
                ActorSubjectId = actor,
                OccurredAtUtc = now,
                IdempotencyKey = idempotencyKey,
                SourceReference = command.SourceReference?.Trim(),
                Version = 1,
            });

            AddAudit(context, MaterialAuditActions.LifecycleTransitioned,
                MaterialAuditEventTypes.LifecycleTransitioned, "material.registry", material.Id, now,
                new { materialId = material.Id, previousStatus, targetStatus });

            try
            {
                await dbContext.SaveChangesAsync(ct);
            }
            catch (DbUpdateException ex) when (IsUniqueViolation(ex))
            {
                var racedEvent = await dbContext.MaterialLifecycleEvents
                    .SingleOrDefaultAsync(
                        e => e.TenantId == context.TenantId
                            && e.MaterialId == command.MaterialId
                            && e.IdempotencyKey == idempotencyKey,
                        ct);

                if (racedEvent is null)
                {
                    throw;
                }

                if (!MaterialIdempotencyFingerprint.TransitionPayloadMatches(racedEvent, command))
                {
                    return IdempotencyConflictResult();
                }

                await dbContext.Entry(material).ReloadAsync(ct);
                return MaterialWriteResult.Success(material.Id, material.Version, idempotentReplay: true);
            }

            return MaterialWriteResult.Success(material.Id, material.Version);
        }, cancellationToken);

    private async Task<MaterialWriteResult> ExecuteWriteAsync(
        MaterialOperationContext context,
        Func<CancellationToken, Task<MaterialWriteResult>> action,
        CancellationToken cancellationToken)
    {
        if (context.TenantId == Guid.Empty || context.FacilityId == Guid.Empty)
        {
            return MaterialWriteResult.Fail(MaterialFailureCodes.InvalidScope, "Tenant and facility are required.");
        }

        try
        {
            return await tenantDatabaseSession.ExecuteWriteAsync(action, cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return MaterialWriteResult.Fail(
                MaterialFailureCodes.ConcurrencyConflict,
                "The record was modified by another user. Please reload and try again.");
        }
        catch (Exception) when (cancellationToken.IsCancellationRequested == false)
        {
            return MaterialWriteResult.Fail(MaterialFailureCodes.SourceUnavailable, "Material service is unavailable.");
        }
    }

    private static MaterialWriteResult IdempotencyConflictResult() =>
        MaterialWriteResult.Fail(
            MaterialFailureCodes.IdempotencyConflict,
            "The idempotency key was already used with a different command payload.");

    private static MaterialWriteResult IdempotencyLegacyUnverifiableResult() =>
        MaterialWriteResult.Fail(
            MaterialFailureCodes.IdempotencyLegacyUnverifiable,
            "The idempotency key belongs to a legacy create that cannot be verified safely.");

    private static MaterialWriteResult ResolveCreateIdempotency(
        MaterialRegistryEntry existing,
        string? createFingerprint)
    {
        if (string.IsNullOrEmpty(existing.CreateIdempotencyPayloadFingerprint))
        {
            return IdempotencyLegacyUnverifiableResult();
        }

        if (!string.IsNullOrEmpty(createFingerprint)
            && existing.CreateIdempotencyPayloadFingerprint == createFingerprint)
        {
            return MaterialWriteResult.Success(existing.Id, existing.Version, idempotentReplay: true);
        }

        return IdempotencyConflictResult();
    }

    private static MaterialSummaryReadModel MapSummary(MaterialRegistryEntry material) =>
        new(
            material.Id,
            material.Code,
            material.Name,
            MaterialValueConversions.ToStorage(material.MaterialCategory),
            MaterialValueConversions.ToStorage(material.PolymerFamily),
            material.Manufacturer,
            material.TradeNameOrGrade,
            material.ColorName,
            MaterialValueConversions.ToStorage(material.LifecycleStatus),
            material.IsActive,
            MaterialTechnicalValidator.HasTechnicalData(material.TechnicalSpecification),
            material.FacilityId,
            material.UpdatedAtUtc,
            material.Version);

    private static MaterialDetailReadModel MapDetail(MaterialRegistryEntry material) =>
        new(
            material.Id,
            material.FacilityId,
            material.Code,
            material.Name,
            material.Description,
            MaterialValueConversions.ToStorage(material.MaterialCategory),
            MaterialValueConversions.ToStorage(material.PolymerFamily),
            material.Manufacturer,
            material.TradeNameOrGrade,
            material.ColorName,
            material.ColorCode,
            MaterialValueConversions.ToStorage(material.PhysicalForm),
            MaterialValueConversions.ToStorage(material.LifecycleStatus),
            material.UnitOfMeasure,
            material.IsActive,
            MaterialValueConversions.ToStorage(material.SourceType),
            material.SourceSystem,
            material.SourceReference,
            material.ExternalMaterialCode,
            material.DataRecordedAtUtc,
            material.TechnicalSpecification is null
                ? null
                : MapTechnicalSpecification(material.TechnicalSpecification),
            material.CreatedAtUtc,
            material.UpdatedAtUtc,
            material.Version);

    private static MaterialTechnicalSpecificationReadModel MapTechnicalSpecification(
        MaterialTechnicalSpecification spec) =>
        new(
            spec.DensityGPerCm3,
            spec.MeltFlowRate,
            spec.MeltFlowUnit,
            spec.MeltFlowTestTemperatureC,
            spec.MeltFlowTestLoadKg,
            spec.MoistureLimitPercent,
            spec.DryingRequired,
            spec.DryingTemperatureMinC,
            spec.DryingTemperatureMaxC,
            spec.DryingTimeMinHours,
            spec.DryingTimeMaxHours,
            spec.MeltTemperatureMinC,
            spec.MeltTemperatureMaxC,
            spec.MoldTemperatureMinC,
            spec.MoldTemperatureMaxC,
            spec.StorageTemperatureMinC,
            spec.StorageTemperatureMaxC,
            spec.MaximumRegrindPercent,
            spec.TechnicalNotes,
            spec.SafetyDataSheetReference,
            spec.TechnicalDataSheetReference,
            MaterialValueConversions.ToStorage(spec.SpecificationSource),
            spec.SpecificationSourceReference,
            spec.SpecificationRecordedAtUtc,
            MaterialTechnicalValidator.HasTechnicalData(spec),
            spec.Version);

    private void DetachFailedCreate(Guid materialId, MaterialRegistryEntry material)
    {
        dbContext.Entry(material).State = EntityState.Detached;

        foreach (var auditEntry in dbContext.ChangeTracker.Entries<AuditRecord>()
                     .Where(entry => entry.State == EntityState.Added
                         && entry.Entity.ResourceId == materialId.ToString())
                     .ToList())
        {
            auditEntry.State = EntityState.Detached;
        }
    }

    private void AddAudit(
        MaterialOperationContext context,
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
            IdentitySubjectId = context.IdentitySubjectId == Guid.Empty ? null : context.IdentitySubjectId,
            TenantMembershipId = context.MembershipId == Guid.Empty ? null : context.MembershipId,
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

public sealed class UnavailableMaterialRepository : IMaterialRepository
{
    private static MaterialWriteResult Unavailable() =>
        MaterialWriteResult.Fail(MaterialFailureCodes.SourceUnavailable, "Material service is unavailable.");

    public Task<MaterialWorkspaceSummaryReadModel> GetWorkspaceSummaryAsync(
        MaterialOperationContext context,
        CancellationToken cancellationToken = default) =>
        throw new InvalidOperationException("Material repository unavailable.");

    public Task<IReadOnlyList<MaterialSummaryReadModel>> ListAsync(
        MaterialOperationContext context,
        MaterialListFilters filters,
        CancellationToken cancellationToken = default) =>
        throw new InvalidOperationException("Material repository unavailable.");

    public Task<MaterialDetailReadModel?> GetByIdAsync(
        MaterialOperationContext context,
        Guid materialId,
        CancellationToken cancellationToken = default) =>
        throw new InvalidOperationException("Material repository unavailable.");

    public Task<IReadOnlyList<MaterialLifecycleEventReadModel>> ListLifecycleHistoryAsync(
        MaterialOperationContext context,
        Guid materialId,
        CancellationToken cancellationToken = default) =>
        throw new InvalidOperationException("Material repository unavailable.");

    public Task<MaterialTechnicalSpecificationReadModel?> GetTechnicalSpecificationAsync(
        MaterialOperationContext context,
        Guid materialId,
        CancellationToken cancellationToken = default) =>
        throw new InvalidOperationException("Material repository unavailable.");

    public Task<MaterialWriteResult> CreateAsync(
        MaterialOperationContext context,
        CreateMaterialCommand command,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Unavailable());

    public Task<MaterialWriteResult> UpdateAsync(
        MaterialOperationContext context,
        UpdateMaterialCommand command,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Unavailable());

    public Task<MaterialWriteResult> UpdateTechnicalSpecificationAsync(
        MaterialOperationContext context,
        UpdateMaterialTechnicalSpecificationCommand command,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Unavailable());

    public Task<MaterialWriteResult> TransitionLifecycleAsync(
        MaterialOperationContext context,
        TransitionMaterialLifecycleCommand command,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Unavailable());
}
