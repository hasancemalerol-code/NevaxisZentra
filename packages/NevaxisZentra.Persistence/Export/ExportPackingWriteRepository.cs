using Microsoft.EntityFrameworkCore;
using Npgsql;
using NevaxisZentra.Persistence.Tenancy;

namespace NevaxisZentra.Persistence.Export;

public sealed class ExportPackingWriteRepository(
    NevaxisZentraDbContext dbContext,
    TenantDatabaseSession tenantDatabaseSession)
    : IExportPackingWriteRepository
{
    public async Task<ExportPackingUnitWriteResult> CreateUnitAsync(
        ExportOperationContext context,
        CreateExportPackingUnitCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(command);

        if (context.TenantId == Guid.Empty || context.FacilityId == Guid.Empty)
        {
            return ExportPackingUnitWriteResult.Fail(
                ExportFailureCodes.InvalidScope, "Tenant and facility identifiers are required.");
        }

        var unitCode = (command.UnitCode ?? string.Empty).Trim();
        if (unitCode.Length == 0)
        {
            return ExportPackingUnitWriteResult.Fail(
                ExportFailureCodes.ValidationFailed, "Unit code is required.");
        }

        if (!ExportPackingValidator.IsValidUnitType(command.UnitType))
        {
            return ExportPackingUnitWriteResult.Fail(
                Export2LFailureCodes.InvalidUnitType,
                $"Unit type '{command.UnitType}' is not recognized.");
        }

        try
        {
            return await tenantDatabaseSession.ExecuteWriteAsync(
                async ct =>
                {
                    var exportCase = await LoadCaseAsync(context, command.ExportCaseId, ct);
                    if (exportCase is null)
                    {
                        return ExportPackingUnitWriteResult.Fail(
                            ExportFailureCodes.NotFound, "Export case was not found.");
                    }

                    if (exportCase.Status == ExportCaseStatus.Closed)
                    {
                        return ExportPackingUnitWriteResult.Fail(
                            ExportFailureCodes.CaseClosed, "Cannot add packing units to a closed export case.");
                    }

                    var now = DateTimeOffset.UtcNow;
                    var correlationId = context.CorrelationId ?? Guid.NewGuid();
                    var actorSubjectId = context.IdentitySubjectId.ToString();

                    var unit = new ExportPackingUnit
                    {
                        Id = Guid.NewGuid(),
                        TenantId = context.TenantId,
                        FacilityId = context.FacilityId,
                        ExportCaseId = command.ExportCaseId,
                        UnitCode = unitCode,
                        UnitType = command.UnitType,
                        ParentPackingUnitId = command.ParentPackingUnitId,
                        GrossWeight = command.GrossWeight,
                        NetWeight = command.NetWeight,
                        WeightUnit = string.IsNullOrWhiteSpace(command.WeightUnit) ? "kg" : command.WeightUnit.Trim(),
                        Length = command.Length,
                        Width = command.Width,
                        Height = command.Height,
                        DimensionUnit = string.IsNullOrWhiteSpace(command.DimensionUnit) ? "cm" : command.DimensionUnit.Trim(),
                        MarksAndNumbers = command.MarksAndNumbers,
                        Status = ExportPackingStatus.Draft,
                        CreatedAtUtc = now,
                        CreatedBySubjectId = actorSubjectId,
                        UpdatedAtUtc = now,
                        LastModifiedBySubjectId = actorSubjectId,
                        Version = 1,
                    };

                    dbContext.ExportPackingUnits.Add(unit);

                    if (exportCase.PackingStatus == ExportPackingStatus.Draft)
                    {
                        exportCase.PackingStatus = ExportPackingStatus.Packing;
                    }

                    exportCase.Version += 1;
                    exportCase.UpdatedAtUtc = now;
                    exportCase.LastModifiedBySubjectId = actorSubjectId;

                    var helper = new ExportReadinessPersistence(dbContext);
                    helper.AppendActivity(
                        exportCase,
                        ExportActivityEventTypes.PackingUnitCreated,
                        "Packing unit created",
                        $"{unit.UnitType}: {unit.UnitCode}",
                        now,
                        ExportActivitySourceType.User,
                        actorSubjectId,
                        correlationId,
                        "export.packing.unit",
                        unit.Id);

                    helper.AddAudit(
                        context,
                        ExportAuditEventTypes.ExportPackingUnitCreated,
                        ExportAuditActions.ExportPackingUnitCreated,
                        "export.packing.unit",
                        unit.Id.ToString(),
                        correlationId,
                        now,
                        new
                        {
                            exportCaseId = command.ExportCaseId.ToString(),
                            packingUnitId = unit.Id.ToString(),
                            unitCode = unit.UnitCode,
                            unitType = unit.UnitType,
                        });

                    var documents = await LoadDocumentsAsync(context, command.ExportCaseId, ct);
                    var operationalContext = await helper.BuildOperationalContextAsync(
                        context, exportCase, documents, ct);
                    await helper.RecalculateAndPersistAsync(
                        context, exportCase, documents,
                        correlationId, actorSubjectId, now,
                        emitInitial: false, operationalContext, ct);

                    await dbContext.SaveChangesAsync(ct);
                    return ExportPackingUnitWriteResult.Success(unit.Id, unit.Version, exportCase.Version);
                },
                cancellationToken);
        }
        catch (DbUpdateException dbEx) when (IsUniqueViolation(dbEx))
        {
            return ExportPackingUnitWriteResult.Fail(
                Export2LFailureCodes.DuplicateUnitCode,
                $"A packing unit with code '{unitCode}' already exists for this export case.");
        }
        catch (DbUpdateConcurrencyException)
        {
            return ExportPackingUnitWriteResult.Fail(
                ExportFailureCodes.ConcurrencyConflict,
                "The export case has been modified. Please reload and try again.");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return ExportPackingUnitWriteResult.Fail(
                ExportFailureCodes.SourceUnavailable, "Export packing service is unavailable.");
        }
    }

    public async Task<ExportPackingUnitWriteResult> UpdateUnitAsync(
        ExportOperationContext context,
        UpdateExportPackingUnitCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(command);

        try
        {
            return await tenantDatabaseSession.ExecuteWriteAsync(
                async ct =>
                {
                    var exportCase = await LoadCaseAsync(context, command.ExportCaseId, ct);
                    if (exportCase is null)
                    {
                        return ExportPackingUnitWriteResult.Fail(
                            ExportFailureCodes.NotFound, "Export case was not found.");
                    }

                    var unit = await dbContext.ExportPackingUnits
                        .SingleOrDefaultAsync(
                            u => u.TenantId == context.TenantId
                                && u.FacilityId == context.FacilityId
                                && u.ExportCaseId == command.ExportCaseId
                                && u.Id == command.PackingUnitId,
                            ct);

                    if (unit is null)
                    {
                        return ExportPackingUnitWriteResult.Fail(
                            Export2LFailureCodes.PackingUnitNotFound, "Packing unit was not found.");
                    }

                    if (unit.Version != command.ExpectedVersion)
                    {
                        return ExportPackingUnitWriteResult.Fail(
                            ExportFailureCodes.ConcurrencyConflict,
                            "The packing unit has been modified. Please reload and try again.");
                    }

                    var now = DateTimeOffset.UtcNow;
                    var correlationId = context.CorrelationId ?? Guid.NewGuid();
                    var actorSubjectId = context.IdentitySubjectId.ToString();
                    var changed = false;

                    if (command.GrossWeight != unit.GrossWeight) { unit.GrossWeight = command.GrossWeight; changed = true; }
                    if (command.NetWeight != unit.NetWeight) { unit.NetWeight = command.NetWeight; changed = true; }
                    if (command.WeightUnit is not null && command.WeightUnit != unit.WeightUnit) { unit.WeightUnit = command.WeightUnit; changed = true; }
                    if (command.Length != unit.Length) { unit.Length = command.Length; changed = true; }
                    if (command.Width != unit.Width) { unit.Width = command.Width; changed = true; }
                    if (command.Height != unit.Height) { unit.Height = command.Height; changed = true; }
                    if (command.DimensionUnit is not null && command.DimensionUnit != unit.DimensionUnit) { unit.DimensionUnit = command.DimensionUnit; changed = true; }
                    if (command.MarksAndNumbers != unit.MarksAndNumbers) { unit.MarksAndNumbers = command.MarksAndNumbers; changed = true; }

                    if (!changed)
                    {
                        return ExportPackingUnitWriteResult.Success(unit.Id, unit.Version, exportCase.Version);
                    }

                    unit.Version += 1;
                    unit.UpdatedAtUtc = now;
                    unit.LastModifiedBySubjectId = actorSubjectId;

                    exportCase.Version += 1;
                    exportCase.UpdatedAtUtc = now;
                    exportCase.LastModifiedBySubjectId = actorSubjectId;

                    var helper = new ExportReadinessPersistence(dbContext);
                    helper.AppendActivity(
                        exportCase,
                        ExportActivityEventTypes.PackingUnitUpdated,
                        "Packing unit updated",
                        unit.UnitCode,
                        now,
                        ExportActivitySourceType.User,
                        actorSubjectId,
                        correlationId,
                        "export.packing.unit",
                        unit.Id);

                    helper.AddAudit(
                        context,
                        ExportAuditEventTypes.ExportPackingUnitUpdated,
                        ExportAuditActions.ExportPackingUnitUpdated,
                        "export.packing.unit",
                        unit.Id.ToString(),
                        correlationId,
                        now,
                        new { exportCaseId = command.ExportCaseId.ToString(), packingUnitId = unit.Id.ToString() });

                    await dbContext.SaveChangesAsync(ct);
                    return ExportPackingUnitWriteResult.Success(unit.Id, unit.Version, exportCase.Version);
                },
                cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return ExportPackingUnitWriteResult.Fail(
                ExportFailureCodes.ConcurrencyConflict,
                "The packing unit has been modified. Please reload and try again.");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return ExportPackingUnitWriteResult.Fail(
                ExportFailureCodes.SourceUnavailable, "Export packing service is unavailable.");
        }
    }

    public async Task<ExportPackingItemWriteResult> AddItemAsync(
        ExportOperationContext context,
        AddExportPackingItemCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(command);

        var productCode = (command.ProductCode ?? string.Empty).Trim();
        if (productCode.Length == 0)
        {
            return ExportPackingItemWriteResult.Fail(ExportFailureCodes.ValidationFailed, "Product code is required.");
        }

        if (command.Quantity <= 0)
        {
            return ExportPackingItemWriteResult.Fail(ExportFailureCodes.ValidationFailed, "Quantity must be greater than zero.");
        }

        var uom = (command.UnitOfMeasure ?? string.Empty).Trim();
        if (uom.Length == 0)
        {
            return ExportPackingItemWriteResult.Fail(ExportFailureCodes.ValidationFailed, "Unit of measure is required.");
        }

        try
        {
            return await tenantDatabaseSession.ExecuteWriteAsync(
                async ct =>
                {
                    var exportCase = await LoadCaseAsync(context, command.ExportCaseId, ct);
                    if (exportCase is null)
                    {
                        return ExportPackingItemWriteResult.Fail(ExportFailureCodes.NotFound, "Export case was not found.");
                    }

                    var unitExists = await dbContext.ExportPackingUnits
                        .AnyAsync(
                            u => u.TenantId == context.TenantId
                                && u.ExportCaseId == command.ExportCaseId
                                && u.Id == command.PackingUnitId,
                            ct);

                    if (!unitExists)
                    {
                        return ExportPackingItemWriteResult.Fail(
                            Export2LFailureCodes.PackingUnitNotFound, "Packing unit was not found.");
                    }

                    var now = DateTimeOffset.UtcNow;
                    var actorSubjectId = context.IdentitySubjectId.ToString();

                    var item = new ExportPackingItem
                    {
                        Id = Guid.NewGuid(),
                        TenantId = context.TenantId,
                        FacilityId = context.FacilityId,
                        ExportCaseId = command.ExportCaseId,
                        PackingUnitId = command.PackingUnitId,
                        SourceOrderLineId = command.SourceOrderLineId,
                        ProductCode = productCode,
                        ProductName = (command.ProductName ?? string.Empty).Trim(),
                        Quantity = command.Quantity,
                        UnitOfMeasure = uom,
                        LotBatchReference = string.IsNullOrWhiteSpace(command.LotBatchReference) ? null : command.LotBatchReference.Trim(),
                        CreatedAtUtc = now,
                        CreatedBySubjectId = actorSubjectId,
                    };

                    dbContext.ExportPackingItems.Add(item);

                    exportCase.Version += 1;
                    exportCase.UpdatedAtUtc = now;
                    exportCase.LastModifiedBySubjectId = actorSubjectId;

                    await dbContext.SaveChangesAsync(ct);
                    return ExportPackingItemWriteResult.Success(item.Id, exportCase.Version);
                },
                cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return ExportPackingItemWriteResult.Fail(ExportFailureCodes.ConcurrencyConflict,
                "The export case has been modified. Please reload and try again.");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return ExportPackingItemWriteResult.Fail(ExportFailureCodes.SourceUnavailable, "Export packing service is unavailable.");
        }
    }

    public async Task<ExportPackingStatusWriteResult> CompletePackingAsync(
        ExportOperationContext context,
        CompleteExportPackingCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(command);

        try
        {
            return await tenantDatabaseSession.ExecuteWriteAsync(
                async ct =>
                {
                    var exportCase = await LoadCaseAsync(context, command.ExportCaseId, ct);
                    if (exportCase is null)
                    {
                        return ExportPackingStatusWriteResult.Fail(ExportFailureCodes.NotFound, "Export case was not found.");
                    }

                    if (exportCase.Version != command.ExpectedCaseVersion)
                    {
                        return ExportPackingStatusWriteResult.Fail(
                            ExportFailureCodes.ConcurrencyConflict,
                            "The export case has been modified. Please reload and try again.");
                    }

                    if (exportCase.PackingStatus == ExportPackingStatus.Verified)
                    {
                        return ExportPackingStatusWriteResult.Fail(
                            Export2LFailureCodes.InvalidPackingStatus,
                            "Cannot complete packing that has already been verified.");
                    }

                    var units = await dbContext.ExportPackingUnits
                        .Where(u => u.TenantId == context.TenantId && u.ExportCaseId == command.ExportCaseId)
                        .ToListAsync(ct);

                    var items = await dbContext.ExportPackingItems
                        .Where(i => i.TenantId == context.TenantId && i.ExportCaseId == command.ExportCaseId)
                        .ToListAsync(ct);

                    var validationErrors = ExportPackingValidator.ValidateForComplete(units, items);
                    if (validationErrors.Count > 0)
                    {
                        return ExportPackingStatusWriteResult.Fail(
                            Export2LFailureCodes.PackingValidationFailed,
                            string.Join("; ", validationErrors.Select(e => e.Message)));
                    }

                    var now = DateTimeOffset.UtcNow;
                    var correlationId = context.CorrelationId ?? Guid.NewGuid();
                    var actorSubjectId = context.IdentitySubjectId.ToString();

                    exportCase.PackingStatus = ExportPackingStatus.Complete;
                    exportCase.PackingCompletedAtUtc = now;
                    exportCase.PackingCompletedBySubjectId = actorSubjectId;
                    exportCase.Version += 1;
                    exportCase.UpdatedAtUtc = now;
                    exportCase.LastModifiedBySubjectId = actorSubjectId;

                    var helper = new ExportReadinessPersistence(dbContext);
                    helper.AppendActivity(
                        exportCase,
                        ExportActivityEventTypes.PackingCompleted,
                        "Packing completed",
                        $"{units.Count} unit(s), {items.Count} item(s)",
                        now,
                        ExportActivitySourceType.User,
                        actorSubjectId,
                        correlationId,
                        "export.case",
                        exportCase.Id);

                    helper.AddAudit(
                        context,
                        ExportAuditEventTypes.ExportPackingCompleted,
                        ExportAuditActions.ExportPackingCompleted,
                        "export.case",
                        exportCase.Id.ToString(),
                        correlationId,
                        now,
                        new { exportCaseId = command.ExportCaseId.ToString(), unitCount = units.Count, itemCount = items.Count });

                    helper.AddOutbox(
                        context.TenantId,
                        ExportOutboxMessageTypes.ExportPackingCompleted,
                        "export-case",
                        exportCase.Id.ToString(),
                        correlationId,
                        $"{exportCase.Id}:{exportCase.Version}:{ExportOutboxMessageTypes.ExportPackingCompleted}",
                        now,
                        new Dictionary<string, string?>
                        {
                            ["tenantId"] = context.TenantId.ToString(),
                            ["facilityId"] = context.FacilityId.ToString(),
                            ["exportCaseId"] = exportCase.Id.ToString(),
                            ["occurredAtUtc"] = now.ToString("O"),
                        });

                    var documents = await LoadDocumentsAsync(context, command.ExportCaseId, ct);
                    var operationalContext = await helper.BuildOperationalContextAsync(
                        context, exportCase, documents, ct);
                    await helper.RecalculateAndPersistAsync(
                        context, exportCase, documents,
                        correlationId, actorSubjectId, now,
                        emitInitial: false, operationalContext, ct);

                    await dbContext.SaveChangesAsync(ct);
                    return ExportPackingStatusWriteResult.Success(
                        exportCase.Version, ExportValueConversions.ToStorage(exportCase.PackingStatus));
                },
                cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return ExportPackingStatusWriteResult.Fail(ExportFailureCodes.ConcurrencyConflict,
                "The export case has been modified. Please reload and try again.");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return ExportPackingStatusWriteResult.Fail(ExportFailureCodes.SourceUnavailable, "Export packing service is unavailable.");
        }
    }

    public async Task<ExportPackingStatusWriteResult> VerifyPackingAsync(
        ExportOperationContext context,
        VerifyExportPackingCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(command);

        try
        {
            return await tenantDatabaseSession.ExecuteWriteAsync(
                async ct =>
                {
                    var exportCase = await LoadCaseAsync(context, command.ExportCaseId, ct);
                    if (exportCase is null)
                    {
                        return ExportPackingStatusWriteResult.Fail(ExportFailureCodes.NotFound, "Export case was not found.");
                    }

                    if (exportCase.Version != command.ExpectedCaseVersion)
                    {
                        return ExportPackingStatusWriteResult.Fail(
                            ExportFailureCodes.ConcurrencyConflict,
                            "The export case has been modified. Please reload and try again.");
                    }

                    if (exportCase.PackingStatus != ExportPackingStatus.Complete)
                    {
                        return ExportPackingStatusWriteResult.Fail(
                            Export2LFailureCodes.InvalidPackingStatus,
                            "Packing must be in Complete status before it can be verified.");
                    }

                    var now = DateTimeOffset.UtcNow;
                    var correlationId = context.CorrelationId ?? Guid.NewGuid();
                    var actorSubjectId = context.IdentitySubjectId.ToString();

                    exportCase.PackingStatus = ExportPackingStatus.Verified;
                    exportCase.PackingVerifiedAtUtc = now;
                    exportCase.PackingVerifiedBySubjectId = actorSubjectId;
                    exportCase.Version += 1;
                    exportCase.UpdatedAtUtc = now;
                    exportCase.LastModifiedBySubjectId = actorSubjectId;

                    var helper = new ExportReadinessPersistence(dbContext);
                    helper.AppendActivity(
                        exportCase,
                        ExportActivityEventTypes.PackingVerified,
                        "Packing verified",
                        null,
                        now,
                        ExportActivitySourceType.User,
                        actorSubjectId,
                        correlationId,
                        "export.case",
                        exportCase.Id);

                    helper.AddAudit(
                        context,
                        ExportAuditEventTypes.ExportPackingVerified,
                        ExportAuditActions.ExportPackingVerified,
                        "export.case",
                        exportCase.Id.ToString(),
                        correlationId,
                        now,
                        new { exportCaseId = command.ExportCaseId.ToString(), verifiedBy = actorSubjectId });

                    helper.AddOutbox(
                        context.TenantId,
                        ExportOutboxMessageTypes.ExportPackingVerified,
                        "export-case",
                        exportCase.Id.ToString(),
                        correlationId,
                        $"{exportCase.Id}:{exportCase.Version}:{ExportOutboxMessageTypes.ExportPackingVerified}",
                        now,
                        new Dictionary<string, string?>
                        {
                            ["tenantId"] = context.TenantId.ToString(),
                            ["facilityId"] = context.FacilityId.ToString(),
                            ["exportCaseId"] = exportCase.Id.ToString(),
                            ["occurredAtUtc"] = now.ToString("O"),
                        });

                    var documents = await LoadDocumentsAsync(context, command.ExportCaseId, ct);
                    var operationalContext = await helper.BuildOperationalContextAsync(
                        context, exportCase, documents, ct);
                    await helper.RecalculateAndPersistAsync(
                        context, exportCase, documents,
                        correlationId, actorSubjectId, now,
                        emitInitial: false, operationalContext, ct);

                    await dbContext.SaveChangesAsync(ct);
                    return ExportPackingStatusWriteResult.Success(
                        exportCase.Version, ExportValueConversions.ToStorage(exportCase.PackingStatus));
                },
                cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return ExportPackingStatusWriteResult.Fail(ExportFailureCodes.ConcurrencyConflict,
                "The export case has been modified. Please reload and try again.");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return ExportPackingStatusWriteResult.Fail(ExportFailureCodes.SourceUnavailable, "Export packing service is unavailable.");
        }
    }

    private async Task<ExportCase?> LoadCaseAsync(
        ExportOperationContext context,
        Guid exportCaseId,
        CancellationToken cancellationToken) =>
        await dbContext.ExportCases
            .SingleOrDefaultAsync(
                c => c.TenantId == context.TenantId
                    && c.FacilityId == context.FacilityId
                    && c.Id == exportCaseId,
                cancellationToken);

    private async Task<List<ExportDocument>> LoadDocumentsAsync(
        ExportOperationContext context,
        Guid exportCaseId,
        CancellationToken cancellationToken) =>
        await dbContext.ExportDocuments
            .Where(d => d.TenantId == context.TenantId
                && d.FacilityId == context.FacilityId
                && d.ExportCaseId == exportCaseId)
            .ToListAsync(cancellationToken);

    private static bool IsUniqueViolation(DbUpdateException ex) =>
        ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation };
}

public sealed class UnavailableExportPackingWriteRepository : IExportPackingWriteRepository
{
    private static ExportPackingUnitWriteResult UnitUnavailable() =>
        ExportPackingUnitWriteResult.Fail(ExportFailureCodes.SourceUnavailable, "Export packing persistence is disabled.");

    private static ExportPackingItemWriteResult ItemUnavailable() =>
        ExportPackingItemWriteResult.Fail(ExportFailureCodes.SourceUnavailable, "Export packing persistence is disabled.");

    private static ExportPackingStatusWriteResult StatusUnavailable() =>
        ExportPackingStatusWriteResult.Fail(ExportFailureCodes.SourceUnavailable, "Export packing persistence is disabled.");

    public Task<ExportPackingUnitWriteResult> CreateUnitAsync(ExportOperationContext context, CreateExportPackingUnitCommand command, CancellationToken cancellationToken = default) => Task.FromResult(UnitUnavailable());
    public Task<ExportPackingUnitWriteResult> UpdateUnitAsync(ExportOperationContext context, UpdateExportPackingUnitCommand command, CancellationToken cancellationToken = default) => Task.FromResult(UnitUnavailable());
    public Task<ExportPackingItemWriteResult> AddItemAsync(ExportOperationContext context, AddExportPackingItemCommand command, CancellationToken cancellationToken = default) => Task.FromResult(ItemUnavailable());
    public Task<ExportPackingStatusWriteResult> CompletePackingAsync(ExportOperationContext context, CompleteExportPackingCommand command, CancellationToken cancellationToken = default) => Task.FromResult(StatusUnavailable());
    public Task<ExportPackingStatusWriteResult> VerifyPackingAsync(ExportOperationContext context, VerifyExportPackingCommand command, CancellationToken cancellationToken = default) => Task.FromResult(StatusUnavailable());
}
