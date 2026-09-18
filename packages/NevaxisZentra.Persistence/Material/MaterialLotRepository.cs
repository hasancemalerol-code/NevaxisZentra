using System.Text.Json;
using NevaxisZentra.Persistence.Audit;
using NevaxisZentra.Persistence.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace NevaxisZentra.Persistence.Material;

public interface IMaterialLotRepository
{
    Task<MaterialLotWorkspaceSummaryReadModel> GetWorkspaceSummaryAsync(
        MaterialOperationContext context,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MaterialLotSummaryReadModel>> ListAsync(
        MaterialOperationContext context,
        MaterialLotListFilters filters,
        CancellationToken cancellationToken = default);

    Task<MaterialLotDetailReadModel?> GetByIdAsync(
        MaterialOperationContext context,
        Guid lotId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MaterialStorageLocationReadModel>> ListStorageLocationsAsync(
        MaterialOperationContext context,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MaterialLotTimelineEntryReadModel>> GetTimelineAsync(
        MaterialOperationContext context,
        Guid lotId,
        CancellationToken cancellationToken = default);

    Task<MaterialLotBackwardTraceabilityReadModel?> GetBackwardTraceabilityAsync(
        MaterialOperationContext context,
        Guid lotId,
        CancellationToken cancellationToken = default);

    Task<MaterialLotForwardTraceabilityReadModel?> GetForwardTraceabilityAsync(
        MaterialOperationContext context,
        Guid lotId,
        CancellationToken cancellationToken = default);

    Task<MaterialLotWriteResult> CreateAsync(
        MaterialOperationContext context,
        CreateMaterialLotCommand command,
        CancellationToken cancellationToken = default);

    Task<MaterialLotWriteResult> ObserveReceiptAsync(
        MaterialOperationContext context,
        ObserveMaterialLotReceiptCommand command,
        CancellationToken cancellationToken = default);

    Task<MaterialLotWriteResult> TransferAsync(
        MaterialOperationContext context,
        TransferMaterialLotCommand command,
        CancellationToken cancellationToken = default);

    Task<MaterialLotWriteResult> TransitionStatusAsync(
        MaterialOperationContext context,
        TransitionMaterialLotStatusCommand command,
        CancellationToken cancellationToken = default);

    Task<MaterialLotWriteResult> RecordQualityStatusAsync(
        MaterialOperationContext context,
        RecordMaterialLotQualityStatusCommand command,
        CancellationToken cancellationToken = default);

    Task<MaterialLotWriteResult> LinkCertificateAsync(
        MaterialOperationContext context,
        LinkMaterialLotCertificateCommand command,
        CancellationToken cancellationToken = default);
}

public sealed class MaterialLotRepository(
    NevaxisZentraDbContext dbContext,
    TenantDatabaseSession tenantDatabaseSession) : IMaterialLotRepository
{
    public async Task<MaterialLotWorkspaceSummaryReadModel> GetWorkspaceSummaryAsync(
        MaterialOperationContext context,
        CancellationToken cancellationToken = default)
    {
        return await tenantDatabaseSession.ExecuteReadAsync(
            async ct =>
            {
                var today = DateOnly.FromDateTime(DateTime.UtcNow);
                var lots = await dbContext.MaterialLots.AsNoTracking()
                    .Where(l => l.TenantId == context.TenantId && l.FacilityId == context.FacilityId)
                    .ToListAsync(ct);

                return new MaterialLotWorkspaceSummaryReadModel(
                    lots.Count,
                    lots.Count(l => l.Status == MaterialLotStatus.Available),
                    lots.Count(l => l.Status == MaterialLotStatus.QualityHold),
                    lots.Count(l => l.Status == MaterialLotStatus.Blocked),
                    lots.Count(l => MaterialLotValidator.IsExpiryApproaching(l.ExpiryDate, l.RetestDate, today)));
            },
            cancellationToken);
    }

    public Task<IReadOnlyList<MaterialLotSummaryReadModel>> ListAsync(
        MaterialOperationContext context,
        MaterialLotListFilters filters,
        CancellationToken cancellationToken = default)
    {
        var page = Math.Max(filters.Page, 1);
        var pageSize = Math.Clamp(filters.PageSize, 1, 100);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        return tenantDatabaseSession.ExecuteReadAsync(
            async ct =>
            {
                var query = dbContext.MaterialLots.AsNoTracking()
                    .Include(l => l.Material)
                    .Where(l => l.TenantId == context.TenantId && l.FacilityId == context.FacilityId);

                if (filters.MaterialId.HasValue)
                {
                    query = query.Where(l => l.MaterialId == filters.MaterialId.Value);
                }

                if (!string.IsNullOrWhiteSpace(filters.Search))
                {
                    var search = filters.Search.Trim();
                    var normalized = search.ToUpperInvariant();
                    query = query.Where(l =>
                        l.LotNumber.Contains(search) ||
                        l.LotNumberNormalized.Contains(normalized));
                }

                if (!string.IsNullOrWhiteSpace(filters.SupplierLotSearch))
                {
                    var search = filters.SupplierLotSearch.Trim();
                    query = query.Where(l =>
                        (l.SupplierLotNumber != null && l.SupplierLotNumber.Contains(search)) ||
                        (l.ManufacturerLotNumber != null && l.ManufacturerLotNumber.Contains(search)));
                }

                if (!string.IsNullOrWhiteSpace(filters.WarehouseReference))
                {
                    var warehouse = filters.WarehouseReference.Trim();
                    query = query.Where(l =>
                        l.CurrentWarehouseReference != null &&
                        l.CurrentWarehouseReference.Contains(warehouse));
                }

                if (!string.IsNullOrWhiteSpace(filters.LocationReference))
                {
                    var location = filters.LocationReference.Trim();
                    query = query.Where(l =>
                        l.CurrentLocationReference != null &&
                        l.CurrentLocationReference.Contains(location));
                }

                if (!string.IsNullOrWhiteSpace(filters.Status))
                {
                    var status = MaterialLotValueConversions.LotStatusFromStorage(filters.Status.Trim());
                    query = query.Where(l => l.Status == status);
                }

                if (!string.IsNullOrWhiteSpace(filters.QualityStatus))
                {
                    var qualityStatus = MaterialLotValueConversions.LotQualityStatusFromStorage(filters.QualityStatus.Trim());
                    query = query.Where(l => l.QualityStatus == qualityStatus);
                }

                if (!string.IsNullOrWhiteSpace(filters.ExpiryState))
                {
                    query = filters.ExpiryState.Trim() switch
                    {
                        "approaching" => query.Where(l =>
                            (l.ExpiryDate != null && l.ExpiryDate <= today.AddDays(30)) ||
                            (l.RetestDate != null && l.RetestDate <= today.AddDays(30))),
                        "expired" => query.Where(l =>
                            (l.ExpiryDate != null && l.ExpiryDate < today) ||
                            (l.RetestDate != null && l.RetestDate < today)),
                        _ => query,
                    };
                }

                var lots = await query.ToListAsync(ct);

                return (IReadOnlyList<MaterialLotSummaryReadModel>)lots
                    .OrderBy(l => MaterialLotValueConversions.LotStatusSortPriority(l.Status))
                    .ThenByDescending(l => l.UpdatedAtUtc)
                    .ThenBy(l => l.LotNumberNormalized)
                    .ThenBy(l => l.Id)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(l => MapSummary(l, today))
                    .ToList();
            },
            cancellationToken);
    }

    public Task<MaterialLotDetailReadModel?> GetByIdAsync(
        MaterialOperationContext context,
        Guid lotId,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(
            async ct =>
            {
                var lot = await dbContext.MaterialLots.AsNoTracking()
                    .Include(l => l.Material)
                    .SingleOrDefaultAsync(
                        l => l.TenantId == context.TenantId
                            && l.FacilityId == context.FacilityId
                            && l.Id == lotId,
                        ct);

                return lot is null ? null : MapDetail(lot);
            },
            cancellationToken);

    public Task<IReadOnlyList<MaterialStorageLocationReadModel>> ListStorageLocationsAsync(
        MaterialOperationContext context,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(
            async ct => (IReadOnlyList<MaterialStorageLocationReadModel>)await dbContext.MaterialStorageLocations.AsNoTracking()
                .Where(s => s.TenantId == context.TenantId && s.FacilityId == context.FacilityId)
                .OrderBy(s => s.WarehouseCodeNormalized)
                .ThenBy(s => s.LocationCodeNormalized)
                .Select(s => new MaterialStorageLocationReadModel(
                    s.Id,
                    s.WarehouseCode,
                    s.WarehouseName,
                    s.LocationCode,
                    s.LocationName,
                    s.IsActive,
                    MaterialValueConversions.ToStorage(s.SourceType),
                    s.SourceReference,
                    s.DataRecordedAtUtc,
                    s.Version))
                .ToListAsync(ct),
            cancellationToken);

    public Task<IReadOnlyList<MaterialLotTimelineEntryReadModel>> GetTimelineAsync(
        MaterialOperationContext context,
        Guid lotId,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(
            async ct =>
            {
                var exists = await dbContext.MaterialLots.AsNoTracking()
                    .AnyAsync(
                        l => l.TenantId == context.TenantId
                            && l.FacilityId == context.FacilityId
                            && l.Id == lotId,
                        ct);

                if (!exists)
                {
                    return (IReadOnlyList<MaterialLotTimelineEntryReadModel>)[];
                }

                var events = await dbContext.MaterialLotTraceabilityEvents.AsNoTracking()
                    .Where(e => e.TenantId == context.TenantId && e.MaterialLotId == lotId)
                    .ToListAsync(ct);

                return events
                    .OrderByDescending(e => e.EventAtUtc)
                    .ThenByDescending(e => e.CreatedAtUtc)
                    .ThenByDescending(e => e.Id)
                    .Select(MapTimelineEntry)
                    .ToList();
            },
            cancellationToken);

    public Task<MaterialLotBackwardTraceabilityReadModel?> GetBackwardTraceabilityAsync(
        MaterialOperationContext context,
        Guid lotId,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(
            async ct =>
            {
                var lot = await dbContext.MaterialLots.AsNoTracking()
                    .Include(l => l.Material)
                    .SingleOrDefaultAsync(
                        l => l.TenantId == context.TenantId
                            && l.FacilityId == context.FacilityId
                            && l.Id == lotId,
                        ct);

                if (lot is null)
                {
                    return null;
                }

                var certificates = await LoadCertificatesAsync(context.TenantId, lotId, ct);
                var events = await LoadOrderedEventsAsync(context.TenantId, lotId, ct);
                var sourceEvents = events
                    .Where(e => e.EventType is MaterialLotEventType.LotCreated or MaterialLotEventType.ReceiptObserved)
                    .Select(MapTimelineEntry)
                    .ToList();

                var initialLocationEvent = events
                    .FirstOrDefault(e => e.EventType is MaterialLotEventType.ReceiptObserved or MaterialLotEventType.LocationTransfer);

                return new MaterialLotBackwardTraceabilityReadModel(
                    lot.Id,
                    lot.LotNumber,
                    lot.MaterialId,
                    lot.Material!.Code,
                    lot.Material.Name,
                    lot.SupplierLotNumber,
                    lot.ManufacturerLotNumber,
                    lot.SupplierReference,
                    lot.PurchaseOrderReference,
                    lot.GoodsReceiptReference,
                    lot.ExternalSystem,
                    lot.ExternalLotReference,
                    initialLocationEvent?.ToWarehouseReference ?? lot.CurrentWarehouseReference,
                    initialLocationEvent?.ToLocationReference ?? lot.CurrentLocationReference,
                    certificates,
                    sourceEvents);
            },
            cancellationToken);

    public Task<MaterialLotForwardTraceabilityReadModel?> GetForwardTraceabilityAsync(
        MaterialOperationContext context,
        Guid lotId,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(
            async ct =>
            {
                var lot = await dbContext.MaterialLots.AsNoTracking()
                    .SingleOrDefaultAsync(
                        l => l.TenantId == context.TenantId
                            && l.FacilityId == context.FacilityId
                            && l.Id == lotId,
                        ct);

                if (lot is null)
                {
                    return null;
                }

                var certificates = await LoadCertificatesAsync(context.TenantId, lotId, ct);
                var events = await LoadOrderedEventsAsync(context.TenantId, lotId, ct);

                return new MaterialLotForwardTraceabilityReadModel(
                    lot.Id,
                    lot.LotNumber,
                    lot.CurrentWarehouseReference,
                    lot.CurrentLocationReference,
                    MaterialLotValueConversions.ToStorage(lot.Status),
                    MaterialLotValueConversions.ToStorage(lot.QualityStatus),
                    events.Where(e => e.EventType == MaterialLotEventType.LocationTransfer).Select(MapTimelineEntry).ToList(),
                    events.Where(e => e.EventType == MaterialLotEventType.StatusChanged).Select(MapTimelineEntry).ToList(),
                    events.Where(e => e.EventType == MaterialLotEventType.QualityStatusRecorded).Select(MapTimelineEntry).ToList(),
                    certificates);
            },
            cancellationToken);

    public Task<MaterialLotWriteResult> CreateAsync(
        MaterialOperationContext context,
        CreateMaterialLotCommand command,
        CancellationToken cancellationToken = default) =>
        ExecuteWriteAsync(context, async ct =>
        {
            string? createFingerprint = null;
            if (!string.IsNullOrWhiteSpace(command.IdempotencyKey))
            {
                if (MaterialLotValidator.TryNormalizeLotNumber(
                        command.LotNumber, out _, out var normalizedForFingerprint, out _))
                {
                    createFingerprint = MaterialLotIdempotencyFingerprint.ComputeCreateFingerprint(
                        context, command, normalizedForFingerprint);
                }

                var existing = await dbContext.MaterialLots
                    .SingleOrDefaultAsync(
                        l => l.TenantId == context.TenantId
                            && l.CreateIdempotencyKey == command.IdempotencyKey.Trim(),
                        ct);

                if (existing is not null)
                {
                    return ResolveCreateIdempotency(existing, createFingerprint);
                }
            }

            if (!MaterialLotValidator.TryNormalizeLotNumber(
                    command.LotNumber, out var displayLotNumber, out var normalizedLotNumber, out var lotNumberFailure))
            {
                return MaterialLotWriteResult.Fail(MaterialLotFailureCodes.ValidationFailed, lotNumberFailure!);
            }

            var material = await dbContext.Materials.AsNoTracking()
                .SingleOrDefaultAsync(
                    m => m.TenantId == context.TenantId
                        && m.FacilityId == context.FacilityId
                        && m.Id == command.MaterialId,
                    ct);

            if (material is null)
            {
                return MaterialLotWriteResult.Fail(MaterialLotFailureCodes.NotFound, "Material not found.");
            }

            var unitFailure = MaterialLotValidator.ValidateUnitOfMeasure(command.UnitOfMeasure, material.UnitOfMeasure);
            if (unitFailure is not null)
            {
                return MaterialLotWriteResult.Fail(MaterialLotFailureCodes.ValidationFailed, unitFailure);
            }

            var quantityFailure = MaterialLotValidator.ValidateQuantity(command.InitialQuantity, "Initial quantity");
            if (quantityFailure is not null)
            {
                return MaterialLotWriteResult.Fail(MaterialLotFailureCodes.InvalidQuantity, quantityFailure);
            }

            var dateFailure = MaterialLotValidator.ValidateDates(
                command.ManufactureDate, null, command.ExpiryDate, command.RetestDate);
            if (dateFailure is not null)
            {
                return MaterialLotWriteResult.Fail(MaterialLotFailureCodes.ValidationFailed, dateFailure);
            }

            MaterialStorageLocation? storageLocation = null;
            if (command.StorageLocationId.HasValue)
            {
                storageLocation = await dbContext.MaterialStorageLocations
                    .SingleOrDefaultAsync(
                        s => s.TenantId == context.TenantId && s.Id == command.StorageLocationId.Value,
                        ct);

                var locationFailure = MaterialLotValidator.ValidateTransferTarget(storageLocation, context.FacilityId);
                if (locationFailure is not null)
                {
                    return MaterialLotWriteResult.Fail(MaterialLotFailureCodes.InvalidLocation, locationFailure);
                }
            }

            var now = DateTimeOffset.UtcNow;
            var actor = context.IdentitySubjectId.ToString();
            var lotId = Guid.NewGuid();
            createFingerprint ??= MaterialLotIdempotencyFingerprint.ComputeCreateFingerprint(
                context, command, normalizedLotNumber);

            var lot = new MaterialLot
            {
                Id = lotId,
                TenantId = context.TenantId,
                FacilityId = context.FacilityId,
                MaterialId = command.MaterialId,
                LotNumber = displayLotNumber,
                LotNumberNormalized = normalizedLotNumber,
                SupplierLotNumber = command.SupplierLotNumber?.Trim(),
                ManufacturerLotNumber = command.ManufacturerLotNumber?.Trim(),
                SupplierReference = command.SupplierReference?.Trim(),
                PurchaseOrderReference = command.PurchaseOrderReference?.Trim(),
                GoodsReceiptReference = command.GoodsReceiptReference?.Trim(),
                ExternalSystem = command.ExternalSystem?.Trim(),
                ExternalLotReference = command.ExternalLotReference?.Trim(),
                ManufactureDate = command.ManufactureDate,
                ExpiryDate = command.ExpiryDate,
                RetestDate = command.RetestDate,
                InitialQuantity = command.InitialQuantity,
                CurrentObservedQuantity = command.InitialQuantity,
                UnitOfMeasure = command.UnitOfMeasure.Trim(),
                Status = MaterialLotStatus.Draft,
                QualityStatus = MaterialLotValueConversions.LotQualityStatusFromStorage(command.QualityStatus),
                CurrentStorageLocationId = storageLocation?.Id,
                CurrentWarehouseReference = storageLocation?.WarehouseCode,
                CurrentLocationReference = storageLocation?.LocationCode,
                SourceType = MaterialValueConversions.SourceFromStorage(command.SourceType),
                SourceReference = command.SourceReference?.Trim(),
                DataRecordedAtUtc = command.DataRecordedAtUtc,
                Notes = command.Notes?.Trim(),
                CreateIdempotencyKey = command.IdempotencyKey?.Trim(),
                CreateIdempotencyPayloadFingerprint = createFingerprint,
                CreatedAtUtc = now,
                UpdatedAtUtc = now,
                CreatedBySubjectId = actor,
                UpdatedBySubjectId = actor,
                Version = 1,
            };

            dbContext.MaterialLots.Add(lot);
            dbContext.MaterialLotTraceabilityEvents.Add(BuildEvent(
                context, lot, MaterialLotEventType.LotCreated, now, actor,
                idempotencyKey: $"lot-created:{lotId}",
                fingerprint: createFingerprint,
                sourceType: lot.SourceType,
                sourceReference: lot.SourceReference));

            AddAudit(context, MaterialAuditActions.LotCreated, MaterialAuditEventTypes.LotCreated,
                "material.lot", lotId, now, new { lotId, lot.LotNumber, lot.MaterialId });

            try
            {
                await dbContext.SaveChangesAsync(ct);
            }
            catch (DbUpdateException ex) when (IsUniqueViolation(ex))
            {
                if (!string.IsNullOrWhiteSpace(command.IdempotencyKey))
                {
                    var raced = await dbContext.MaterialLots
                        .SingleOrDefaultAsync(
                            l => l.TenantId == context.TenantId
                                && l.CreateIdempotencyKey == command.IdempotencyKey!.Trim(),
                            ct);

                    if (raced is not null)
                    {
                        return ResolveCreateIdempotency(raced, createFingerprint);
                    }
                }

                var duplicateLot = await dbContext.MaterialLots.AsNoTracking()
                    .AnyAsync(
                        l => l.TenantId == context.TenantId
                            && l.MaterialId == command.MaterialId
                            && l.LotNumberNormalized == normalizedLotNumber,
                        ct);

                if (duplicateLot)
                {
                    return MaterialLotWriteResult.Fail(
                        MaterialLotFailureCodes.LotNumberConflict,
                        "A lot with this number already exists for the material.");
                }

                throw;
            }

            return MaterialLotWriteResult.Success(lotId, lot.Version);
        }, cancellationToken);

    public Task<MaterialLotWriteResult> ObserveReceiptAsync(
        MaterialOperationContext context,
        ObserveMaterialLotReceiptCommand command,
        CancellationToken cancellationToken = default) =>
        ExecuteWriteAsync(context, async ct =>
        {
            if (string.IsNullOrWhiteSpace(command.IdempotencyKey))
            {
                return MaterialLotWriteResult.Fail(MaterialLotFailureCodes.ValidationFailed, "Idempotency key is required.");
            }

            var idempotencyKey = command.IdempotencyKey.Trim();
            var fingerprint = MaterialLotIdempotencyFingerprint.ComputeReceiptFingerprint(command);
            var existingEvent = await dbContext.MaterialLotTraceabilityEvents
                .SingleOrDefaultAsync(
                    e => e.TenantId == context.TenantId
                        && e.MaterialLotId == command.LotId
                        && e.IdempotencyKey == idempotencyKey,
                    ct);

            if (existingEvent is not null)
            {
                if (!MaterialLotIdempotencyFingerprint.ReceiptPayloadMatches(existingEvent, command, fingerprint))
                {
                    return IdempotencyConflictResult();
                }

                var replayLot = await dbContext.MaterialLots
                    .SingleAsync(l => l.Id == command.LotId && l.TenantId == context.TenantId, ct);
                return MaterialLotWriteResult.Success(replayLot.Id, replayLot.Version, idempotentReplay: true);
            }

            var lot = await dbContext.MaterialLots
                .Include(l => l.Material)
                .SingleOrDefaultAsync(
                    l => l.TenantId == context.TenantId
                        && l.FacilityId == context.FacilityId
                        && l.Id == command.LotId,
                    ct);

            if (lot is null)
            {
                return MaterialLotWriteResult.Fail(MaterialLotFailureCodes.NotFound, "Material lot not found.");
            }

            if (lot.Version != command.ExpectedVersion)
            {
                return MaterialLotWriteResult.Fail(
                    MaterialLotFailureCodes.ConcurrencyConflict,
                    "The lot record was modified. Please reload and try again.");
            }

            if (MaterialLotLifecycleStateMachine.IsTerminal(lot.Status))
            {
                return MaterialLotWriteResult.Fail(
                    MaterialLotFailureCodes.InvalidTransition,
                    "Closed lots cannot accept receipt observations.");
            }

            var quantityFailure = MaterialLotValidator.ValidateQuantity(command.ObservedQuantity, "Observed quantity");
            if (quantityFailure is not null || command.ObservedQuantity <= 0)
            {
                return MaterialLotWriteResult.Fail(
                    MaterialLotFailureCodes.InvalidQuantity,
                    quantityFailure ?? "Observed quantity must be greater than zero.");
            }

            var dateFailure = MaterialLotValidator.ValidateDates(
                lot.ManufactureDate, command.ReceivedAtUtc, lot.ExpiryDate, lot.RetestDate);
            if (dateFailure is not null)
            {
                return MaterialLotWriteResult.Fail(MaterialLotFailureCodes.ValidationFailed, dateFailure);
            }

            var now = DateTimeOffset.UtcNow;
            var actor = context.IdentitySubjectId.ToString();
            var previousStatus = lot.Status;

            if (lot.Status == MaterialLotStatus.Draft
                && MaterialLotLifecycleStateMachine.CanTransition(lot.Status, MaterialLotStatus.Received))
            {
                lot.Status = MaterialLotStatus.Received;
            }

            lot.ReceivedAtUtc = command.ReceivedAtUtc;
            lot.InitialQuantity ??= command.ObservedQuantity;
            lot.CurrentObservedQuantity = command.ObservedQuantity;
            lot.GoodsReceiptReference = command.GoodsReceiptReference?.Trim() ?? lot.GoodsReceiptReference;
            lot.UpdatedAtUtc = now;
            lot.UpdatedBySubjectId = actor;
            lot.Version += 1;

            dbContext.MaterialLotTraceabilityEvents.Add(BuildEvent(
                context, lot, MaterialLotEventType.ReceiptObserved, command.ReceivedAtUtc, actor,
                idempotencyKey,
                fingerprint,
                quantity: command.ObservedQuantity,
                unitOfMeasure: lot.UnitOfMeasure,
                statusBefore: previousStatus,
                statusAfter: lot.Status,
                sourceReference: command.SourceReference));

            AddAudit(context, MaterialAuditActions.LotReceiptObserved, MaterialAuditEventTypes.LotReceiptObserved,
                "material.lot", lot.Id, now, new { lotId = lot.Id, command.ObservedQuantity });

            try
            {
                await dbContext.SaveChangesAsync(ct);
            }
            catch (DbUpdateException ex) when (IsUniqueViolation(ex))
            {
                return await ResolveRacedEventAsync(
                    context.TenantId, command.LotId, idempotencyKey,
                    existing => MaterialLotIdempotencyFingerprint.ReceiptPayloadMatches(existing, command, fingerprint),
                    ct);
            }

            return MaterialLotWriteResult.Success(lot.Id, lot.Version);
        }, cancellationToken);

    public Task<MaterialLotWriteResult> TransferAsync(
        MaterialOperationContext context,
        TransferMaterialLotCommand command,
        CancellationToken cancellationToken = default) =>
        ExecuteWriteAsync(context, async ct =>
        {
            if (string.IsNullOrWhiteSpace(command.IdempotencyKey))
            {
                return MaterialLotWriteResult.Fail(MaterialLotFailureCodes.ValidationFailed, "Idempotency key is required.");
            }

            var idempotencyKey = command.IdempotencyKey.Trim();
            var targetLocation = await dbContext.MaterialStorageLocations
                .SingleOrDefaultAsync(
                    s => s.TenantId == context.TenantId && s.Id == command.TargetStorageLocationId,
                    ct);

            var fingerprint = MaterialLotIdempotencyFingerprint.ComputeTransferFingerprint(command);
            var existingEvent = await dbContext.MaterialLotTraceabilityEvents
                .SingleOrDefaultAsync(
                    e => e.TenantId == context.TenantId
                        && e.MaterialLotId == command.LotId
                        && e.IdempotencyKey == idempotencyKey,
                    ct);

            if (existingEvent is not null)
            {
                if (targetLocation is null
                    || !MaterialLotIdempotencyFingerprint.TransferPayloadMatches(
                        existingEvent, command, fingerprint, targetLocation))
                {
                    return IdempotencyConflictResult();
                }

                var replayLot = await dbContext.MaterialLots
                    .SingleAsync(l => l.Id == command.LotId && l.TenantId == context.TenantId, ct);
                return MaterialLotWriteResult.Success(replayLot.Id, replayLot.Version, idempotentReplay: true);
            }

            var lot = await dbContext.MaterialLots
                .SingleOrDefaultAsync(
                    l => l.TenantId == context.TenantId
                        && l.FacilityId == context.FacilityId
                        && l.Id == command.LotId,
                    ct);

            if (lot is null)
            {
                return MaterialLotWriteResult.Fail(MaterialLotFailureCodes.NotFound, "Material lot not found.");
            }

            if (lot.Version != command.ExpectedVersion)
            {
                return MaterialLotWriteResult.Fail(
                    MaterialLotFailureCodes.ConcurrencyConflict,
                    "The lot record was modified. Please reload and try again.");
            }

            if (MaterialLotLifecycleStateMachine.IsTerminal(lot.Status))
            {
                return MaterialLotWriteResult.Fail(
                    MaterialLotFailureCodes.InvalidTransition,
                    "Closed lots cannot be transferred.");
            }

            MaterialStorageLocation? sourceLocation = null;
            if (lot.CurrentStorageLocationId.HasValue)
            {
                sourceLocation = await dbContext.MaterialStorageLocations
                    .SingleOrDefaultAsync(
                        s => s.TenantId == context.TenantId && s.Id == lot.CurrentStorageLocationId.Value,
                        ct);
            }

            string? transferFailure;
            if (sourceLocation is null)
            {
                transferFailure = MaterialLotValidator.ValidateTransferTarget(targetLocation, context.FacilityId);
            }
            else
            {
                transferFailure = MaterialLotValidator.ValidateTransferEndpoints(
                    sourceLocation, targetLocation, context.FacilityId);
            }

            if (transferFailure is not null)
            {
                return MaterialLotWriteResult.Fail(MaterialLotFailureCodes.InvalidLocation, transferFailure);
            }

            var now = DateTimeOffset.UtcNow;
            var actor = context.IdentitySubjectId.ToString();
            var observedQuantity = lot.CurrentObservedQuantity;

            lot.CurrentStorageLocationId = targetLocation!.Id;
            lot.CurrentWarehouseReference = targetLocation.WarehouseCode;
            lot.CurrentLocationReference = targetLocation.LocationCode;
            lot.UpdatedAtUtc = now;
            lot.UpdatedBySubjectId = actor;
            lot.Version += 1;

            dbContext.MaterialLotTraceabilityEvents.Add(BuildEvent(
                context, lot, MaterialLotEventType.LocationTransfer, now, actor,
                idempotencyKey,
                fingerprint,
                quantity: observedQuantity,
                unitOfMeasure: lot.UnitOfMeasure,
                fromWarehouseReference: sourceLocation?.WarehouseCode,
                fromLocationReference: sourceLocation?.LocationCode,
                toWarehouseReference: targetLocation.WarehouseCode,
                toLocationReference: targetLocation.LocationCode,
                reason: command.Reason,
                sourceReference: command.SourceReference));

            AddAudit(context, MaterialAuditActions.LotTransferred, MaterialAuditEventTypes.LotTransferred,
                "material.lot", lot.Id, now, new { lotId = lot.Id, targetLocationId = targetLocation.Id });

            try
            {
                await dbContext.SaveChangesAsync(ct);
            }
            catch (DbUpdateException ex) when (IsUniqueViolation(ex))
            {
                return await ResolveRacedEventAsync(
                    context.TenantId, command.LotId, idempotencyKey,
                    existing => targetLocation is not null
                        && MaterialLotIdempotencyFingerprint.TransferPayloadMatches(
                            existing, command, fingerprint, targetLocation),
                    ct);
            }

            return MaterialLotWriteResult.Success(lot.Id, lot.Version);
        }, cancellationToken);

    public Task<MaterialLotWriteResult> TransitionStatusAsync(
        MaterialOperationContext context,
        TransitionMaterialLotStatusCommand command,
        CancellationToken cancellationToken = default) =>
        ExecuteWriteAsync(context, async ct =>
        {
            if (string.IsNullOrWhiteSpace(command.Reason))
            {
                return MaterialLotWriteResult.Fail(MaterialLotFailureCodes.ValidationFailed, "Transition reason is required.");
            }

            if (string.IsNullOrWhiteSpace(command.IdempotencyKey))
            {
                return MaterialLotWriteResult.Fail(MaterialLotFailureCodes.ValidationFailed, "Idempotency key is required.");
            }

            var idempotencyKey = command.IdempotencyKey.Trim();
            var existingEvent = await dbContext.MaterialLotTraceabilityEvents
                .SingleOrDefaultAsync(
                    e => e.TenantId == context.TenantId
                        && e.MaterialLotId == command.LotId
                        && e.IdempotencyKey == idempotencyKey,
                    ct);

            if (existingEvent is not null)
            {
                if (!MaterialLotIdempotencyFingerprint.StatusPayloadMatches(existingEvent, command))
                {
                    return IdempotencyConflictResult();
                }

                var replayLot = await dbContext.MaterialLots
                    .SingleAsync(l => l.Id == command.LotId && l.TenantId == context.TenantId, ct);
                return MaterialLotWriteResult.Success(replayLot.Id, replayLot.Version, idempotentReplay: true);
            }

            var lot = await dbContext.MaterialLots
                .SingleOrDefaultAsync(
                    l => l.TenantId == context.TenantId
                        && l.FacilityId == context.FacilityId
                        && l.Id == command.LotId,
                    ct);

            if (lot is null)
            {
                return MaterialLotWriteResult.Fail(MaterialLotFailureCodes.NotFound, "Material lot not found.");
            }

            if (lot.Version != command.ExpectedVersion)
            {
                return MaterialLotWriteResult.Fail(
                    MaterialLotFailureCodes.ConcurrencyConflict,
                    "The lot record was modified. Please reload and try again.");
            }

            var targetStatus = MaterialLotValueConversions.LotStatusFromStorage(command.TargetStatus);
            if (!MaterialLotLifecycleStateMachine.CanTransition(lot.Status, targetStatus))
            {
                return MaterialLotWriteResult.Fail(
                    MaterialLotFailureCodes.InvalidTransition,
                    "This lot status transition is not allowed.");
            }

            var now = DateTimeOffset.UtcNow;
            var actor = context.IdentitySubjectId.ToString();
            var previousStatus = lot.Status;
            lot.Status = targetStatus;
            lot.UpdatedAtUtc = now;
            lot.UpdatedBySubjectId = actor;
            lot.Version += 1;

            dbContext.MaterialLotTraceabilityEvents.Add(BuildEvent(
                context, lot, MaterialLotEventType.StatusChanged, now, actor,
                idempotencyKey,
                MaterialLotIdempotencyFingerprint.ComputeStatusFingerprint(command),
                statusBefore: previousStatus,
                statusAfter: targetStatus,
                reason: command.Reason,
                sourceReference: command.SourceReference));

            AddAudit(context, MaterialAuditActions.LotStatusChanged, MaterialAuditEventTypes.LotStatusChanged,
                "material.lot", lot.Id, now, new { lotId = lot.Id, previousStatus, targetStatus });

            try
            {
                await dbContext.SaveChangesAsync(ct);
            }
            catch (DbUpdateException ex) when (IsUniqueViolation(ex))
            {
                return await ResolveRacedEventAsync(
                    context.TenantId, command.LotId, idempotencyKey,
                    existing => MaterialLotIdempotencyFingerprint.StatusPayloadMatches(existing, command),
                    ct);
            }

            return MaterialLotWriteResult.Success(lot.Id, lot.Version);
        }, cancellationToken);

    public Task<MaterialLotWriteResult> RecordQualityStatusAsync(
        MaterialOperationContext context,
        RecordMaterialLotQualityStatusCommand command,
        CancellationToken cancellationToken = default) =>
        ExecuteWriteAsync(context, async ct =>
        {
            if (string.IsNullOrWhiteSpace(command.IdempotencyKey))
            {
                return MaterialLotWriteResult.Fail(MaterialLotFailureCodes.ValidationFailed, "Idempotency key is required.");
            }

            var idempotencyKey = command.IdempotencyKey.Trim();
            var existingEvent = await dbContext.MaterialLotTraceabilityEvents
                .SingleOrDefaultAsync(
                    e => e.TenantId == context.TenantId
                        && e.MaterialLotId == command.LotId
                        && e.IdempotencyKey == idempotencyKey,
                    ct);

            if (existingEvent is not null)
            {
                if (!MaterialLotIdempotencyFingerprint.QualityPayloadMatches(existingEvent, command))
                {
                    return IdempotencyConflictResult();
                }

                var replayLot = await dbContext.MaterialLots
                    .SingleAsync(l => l.Id == command.LotId && l.TenantId == context.TenantId, ct);
                return MaterialLotWriteResult.Success(replayLot.Id, replayLot.Version, idempotentReplay: true);
            }

            var lot = await dbContext.MaterialLots
                .SingleOrDefaultAsync(
                    l => l.TenantId == context.TenantId
                        && l.FacilityId == context.FacilityId
                        && l.Id == command.LotId,
                    ct);

            if (lot is null)
            {
                return MaterialLotWriteResult.Fail(MaterialLotFailureCodes.NotFound, "Material lot not found.");
            }

            if (lot.Version != command.ExpectedVersion)
            {
                return MaterialLotWriteResult.Fail(
                    MaterialLotFailureCodes.ConcurrencyConflict,
                    "The lot record was modified. Please reload and try again.");
            }

            if (MaterialLotLifecycleStateMachine.IsTerminal(lot.Status))
            {
                return MaterialLotWriteResult.Fail(
                    MaterialLotFailureCodes.InvalidTransition,
                    "Closed lots cannot record quality status changes.");
            }

            var now = DateTimeOffset.UtcNow;
            var actor = context.IdentitySubjectId.ToString();
            var previousQualityStatus = lot.QualityStatus;
            lot.QualityStatus = MaterialLotValueConversions.LotQualityStatusFromStorage(command.QualityStatus);
            lot.UpdatedAtUtc = now;
            lot.UpdatedBySubjectId = actor;
            lot.Version += 1;

            dbContext.MaterialLotTraceabilityEvents.Add(BuildEvent(
                context, lot, MaterialLotEventType.QualityStatusRecorded, now, actor,
                idempotencyKey,
                MaterialLotIdempotencyFingerprint.ComputeQualityFingerprint(command),
                qualityStatusBefore: previousQualityStatus,
                qualityStatusAfter: lot.QualityStatus,
                reason: command.Reason,
                sourceReference: command.SourceReference));

            AddAudit(context, MaterialAuditActions.LotQualityRecorded, MaterialAuditEventTypes.LotQualityRecorded,
                "material.lot", lot.Id, now, new { lotId = lot.Id, previousQualityStatus, lot.QualityStatus });

            try
            {
                await dbContext.SaveChangesAsync(ct);
            }
            catch (DbUpdateException ex) when (IsUniqueViolation(ex))
            {
                return await ResolveRacedEventAsync(
                    context.TenantId, command.LotId, idempotencyKey,
                    existing => MaterialLotIdempotencyFingerprint.QualityPayloadMatches(existing, command),
                    ct);
            }

            return MaterialLotWriteResult.Success(lot.Id, lot.Version);
        }, cancellationToken);

    public Task<MaterialLotWriteResult> LinkCertificateAsync(
        MaterialOperationContext context,
        LinkMaterialLotCertificateCommand command,
        CancellationToken cancellationToken = default) =>
        ExecuteWriteAsync(context, async ct =>
        {
            if (string.IsNullOrWhiteSpace(command.DocumentReference))
            {
                return MaterialLotWriteResult.Fail(
                    MaterialLotFailureCodes.ValidationFailed,
                    "Document reference is required.");
            }

            if (string.IsNullOrWhiteSpace(command.IdempotencyKey))
            {
                return MaterialLotWriteResult.Fail(MaterialLotFailureCodes.ValidationFailed, "Idempotency key is required.");
            }

            var idempotencyKey = command.IdempotencyKey.Trim();
            var fingerprint = MaterialLotIdempotencyFingerprint.ComputeCertificateFingerprint(command);
            var existingCertificate = await dbContext.MaterialLotCertificateReferences
                .SingleOrDefaultAsync(
                    c => c.TenantId == context.TenantId
                        && c.MaterialLotId == command.LotId
                        && c.IdempotencyKey == idempotencyKey,
                    ct);

            if (existingCertificate is not null)
            {
                if (!MaterialLotIdempotencyFingerprint.CertificatePayloadMatches(
                        existingCertificate, command, fingerprint))
                {
                    return IdempotencyConflictResult();
                }

                var replayLot = await dbContext.MaterialLots
                    .SingleAsync(l => l.Id == command.LotId && l.TenantId == context.TenantId, ct);
                return MaterialLotWriteResult.Success(replayLot.Id, replayLot.Version, idempotentReplay: true);
            }

            var lot = await dbContext.MaterialLots
                .SingleOrDefaultAsync(
                    l => l.TenantId == context.TenantId
                        && l.FacilityId == context.FacilityId
                        && l.Id == command.LotId,
                    ct);

            if (lot is null)
            {
                return MaterialLotWriteResult.Fail(MaterialLotFailureCodes.NotFound, "Material lot not found.");
            }

            if (MaterialLotLifecycleStateMachine.IsTerminal(lot.Status))
            {
                return MaterialLotWriteResult.Fail(
                    MaterialLotFailureCodes.InvalidTransition,
                    "Closed lots cannot accept certificate links.");
            }

            var now = DateTimeOffset.UtcNow;
            var actor = context.IdentitySubjectId.ToString();
            var certificateId = Guid.NewGuid();

            dbContext.MaterialLotCertificateReferences.Add(new MaterialLotCertificateReference
            {
                Id = certificateId,
                TenantId = context.TenantId,
                FacilityId = context.FacilityId,
                MaterialLotId = lot.Id,
                CertificateType = MaterialLotValueConversions.LotCertificateTypeFromStorage(command.CertificateType),
                DocumentReference = command.DocumentReference.Trim(),
                DocumentNumber = command.DocumentNumber?.Trim(),
                Issuer = command.Issuer?.Trim(),
                IssuedDate = command.IssuedDate,
                ExpiryDate = command.ExpiryDate,
                SourceType = MaterialValueConversions.SourceFromStorage(command.SourceType),
                SourceReference = command.SourceReference?.Trim(),
                IdempotencyKey = idempotencyKey,
                PayloadFingerprint = fingerprint,
                CreatedAtUtc = now,
                CreatedBySubjectId = actor,
                Version = 1,
            });

            dbContext.MaterialLotTraceabilityEvents.Add(BuildEvent(
                context, lot, MaterialLotEventType.CertificateLinked, now, actor,
                idempotencyKey,
                fingerprint,
                reason: command.DocumentReference.Trim(),
                sourceType: MaterialValueConversions.SourceFromStorage(command.SourceType),
                sourceReference: command.SourceReference));

            lot.UpdatedAtUtc = now;
            lot.UpdatedBySubjectId = actor;
            lot.Version += 1;

            AddAudit(context, MaterialAuditActions.LotCertificateLinked, MaterialAuditEventTypes.LotCertificateLinked,
                "material.lot", lot.Id, now, new { lotId = lot.Id, certificateId, command.DocumentReference });

            try
            {
                await dbContext.SaveChangesAsync(ct);
            }
            catch (DbUpdateException ex) when (IsUniqueViolation(ex))
            {
                var racedCertificate = await dbContext.MaterialLotCertificateReferences
                    .SingleOrDefaultAsync(
                        c => c.TenantId == context.TenantId
                            && c.MaterialLotId == command.LotId
                            && c.IdempotencyKey == idempotencyKey,
                        ct);

                if (racedCertificate is null)
                {
                    throw;
                }

                if (!MaterialLotIdempotencyFingerprint.CertificatePayloadMatches(
                        racedCertificate, command, fingerprint))
                {
                    return IdempotencyConflictResult();
                }

                await dbContext.Entry(lot).ReloadAsync(ct);
                return MaterialLotWriteResult.Success(lot.Id, lot.Version, idempotentReplay: true);
            }

            return MaterialLotWriteResult.Success(lot.Id, lot.Version);
        }, cancellationToken);

    private async Task<MaterialLotWriteResult> ExecuteWriteAsync(
        MaterialOperationContext context,
        Func<CancellationToken, Task<MaterialLotWriteResult>> action,
        CancellationToken cancellationToken)
    {
        if (context.TenantId == Guid.Empty || context.FacilityId == Guid.Empty)
        {
            return MaterialLotWriteResult.Fail(MaterialLotFailureCodes.InvalidScope, "Tenant and facility are required.");
        }

        try
        {
            return await tenantDatabaseSession.ExecuteWriteAsync(action, cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return MaterialLotWriteResult.Fail(
                MaterialLotFailureCodes.ConcurrencyConflict,
                "The record was modified by another user. Please reload and try again.");
        }
        catch (Exception) when (cancellationToken.IsCancellationRequested == false)
        {
            return MaterialLotWriteResult.Fail(MaterialLotFailureCodes.SourceUnavailable, "Material lot service is unavailable.");
        }
    }

    private async Task<MaterialLotWriteResult> ResolveRacedEventAsync(
        Guid tenantId,
        Guid lotId,
        string idempotencyKey,
        Func<MaterialLotTraceabilityEvent, bool> payloadMatches,
        CancellationToken cancellationToken)
    {
        var racedEvent = await dbContext.MaterialLotTraceabilityEvents
            .SingleOrDefaultAsync(
                e => e.TenantId == tenantId && e.MaterialLotId == lotId && e.IdempotencyKey == idempotencyKey,
                cancellationToken);

        if (racedEvent is null || !payloadMatches(racedEvent))
        {
            return IdempotencyConflictResult();
        }

        var lot = await dbContext.MaterialLots
            .SingleAsync(l => l.Id == lotId && l.TenantId == tenantId, cancellationToken);
        return MaterialLotWriteResult.Success(lot.Id, lot.Version, idempotentReplay: true);
    }

    private static MaterialLotWriteResult IdempotencyConflictResult() =>
        MaterialLotWriteResult.Fail(
            MaterialLotFailureCodes.IdempotencyConflict,
            "The idempotency key was already used with a different command payload.");

    private static MaterialLotWriteResult IdempotencyLegacyUnverifiableResult() =>
        MaterialLotWriteResult.Fail(
            MaterialLotFailureCodes.IdempotencyLegacyUnverifiable,
            "The idempotency key belongs to a legacy create that cannot be verified safely.");

    private static MaterialLotWriteResult ResolveCreateIdempotency(
        MaterialLot existing,
        string? createFingerprint)
    {
        if (string.IsNullOrEmpty(existing.CreateIdempotencyPayloadFingerprint))
        {
            return IdempotencyLegacyUnverifiableResult();
        }

        if (!string.IsNullOrEmpty(createFingerprint)
            && existing.CreateIdempotencyPayloadFingerprint == createFingerprint)
        {
            return MaterialLotWriteResult.Success(existing.Id, existing.Version, idempotentReplay: true);
        }

        return IdempotencyConflictResult();
    }

    private async Task<IReadOnlyList<MaterialLotCertificateReadModel>> LoadCertificatesAsync(
        Guid tenantId,
        Guid lotId,
        CancellationToken cancellationToken) =>
        await dbContext.MaterialLotCertificateReferences.AsNoTracking()
            .Where(c => c.TenantId == tenantId && c.MaterialLotId == lotId)
            .OrderByDescending(c => c.CreatedAtUtc)
            .ThenByDescending(c => c.Id)
            .Select(c => new MaterialLotCertificateReadModel(
                c.Id,
                MaterialLotValueConversions.ToStorage(c.CertificateType),
                c.DocumentReference,
                c.DocumentNumber,
                c.Issuer,
                c.IssuedDate,
                c.ExpiryDate,
                MaterialValueConversions.ToStorage(c.SourceType),
                c.SourceReference,
                c.CreatedAtUtc,
                c.Version))
            .ToListAsync(cancellationToken);

    private async Task<List<MaterialLotTraceabilityEvent>> LoadOrderedEventsAsync(
        Guid tenantId,
        Guid lotId,
        CancellationToken cancellationToken) =>
        await dbContext.MaterialLotTraceabilityEvents.AsNoTracking()
            .Where(e => e.TenantId == tenantId && e.MaterialLotId == lotId)
            .OrderByDescending(e => e.EventAtUtc)
            .ThenByDescending(e => e.CreatedAtUtc)
            .ThenByDescending(e => e.Id)
            .ToListAsync(cancellationToken);

    private static MaterialLotSummaryReadModel MapSummary(MaterialLot lot, DateOnly today) =>
        new(
            lot.Id,
            lot.MaterialId,
            lot.Material?.Code ?? string.Empty,
            lot.Material?.Name ?? string.Empty,
            lot.LotNumber,
            lot.SupplierLotNumber,
            lot.ManufacturerLotNumber,
            MaterialLotValueConversions.ToStorage(lot.Status),
            MaterialLotValueConversions.ToStorage(lot.QualityStatus),
            lot.CurrentWarehouseReference,
            lot.CurrentLocationReference,
            lot.CurrentObservedQuantity,
            lot.UnitOfMeasure,
            lot.ExpiryDate,
            lot.RetestDate,
            MaterialLotValidator.IsExpiryApproaching(lot.ExpiryDate, lot.RetestDate, today),
            MaterialValueConversions.ToStorage(lot.SourceType),
            lot.SourceReference,
            lot.DataRecordedAtUtc,
            lot.UpdatedAtUtc,
            lot.Version);

    private static MaterialLotDetailReadModel MapDetail(MaterialLot lot) =>
        new(
            lot.Id,
            lot.FacilityId,
            lot.MaterialId,
            lot.Material!.Code,
            lot.Material.Name,
            lot.LotNumber,
            lot.SupplierLotNumber,
            lot.ManufacturerLotNumber,
            lot.SupplierReference,
            lot.PurchaseOrderReference,
            lot.GoodsReceiptReference,
            lot.ExternalSystem,
            lot.ExternalLotReference,
            lot.ManufactureDate,
            lot.ReceivedAtUtc,
            lot.ExpiryDate,
            lot.RetestDate,
            lot.InitialQuantity,
            lot.CurrentObservedQuantity,
            lot.UnitOfMeasure,
            MaterialLotValueConversions.ToStorage(lot.Status),
            MaterialLotValueConversions.ToStorage(lot.QualityStatus),
            lot.CurrentStorageLocationId,
            lot.CurrentWarehouseReference,
            lot.CurrentLocationReference,
            MaterialValueConversions.ToStorage(lot.SourceType),
            lot.SourceReference,
            lot.DataRecordedAtUtc,
            lot.Notes,
            lot.CreatedAtUtc,
            lot.UpdatedAtUtc,
            lot.Version);

    private static MaterialLotTimelineEntryReadModel MapTimelineEntry(MaterialLotTraceabilityEvent traceEvent) =>
        new(
            traceEvent.Id,
            MaterialLotValueConversions.ToStorage(traceEvent.EventType),
            traceEvent.EventAtUtc,
            traceEvent.Quantity,
            traceEvent.UnitOfMeasure,
            traceEvent.FromWarehouseReference,
            traceEvent.FromLocationReference,
            traceEvent.ToWarehouseReference,
            traceEvent.ToLocationReference,
            traceEvent.StatusBefore.HasValue
                ? MaterialLotValueConversions.ToStorage(traceEvent.StatusBefore.Value)
                : null,
            traceEvent.StatusAfter.HasValue
                ? MaterialLotValueConversions.ToStorage(traceEvent.StatusAfter.Value)
                : null,
            traceEvent.QualityStatusBefore.HasValue
                ? MaterialLotValueConversions.ToStorage(traceEvent.QualityStatusBefore.Value)
                : null,
            traceEvent.QualityStatusAfter.HasValue
                ? MaterialLotValueConversions.ToStorage(traceEvent.QualityStatusAfter.Value)
                : null,
            traceEvent.Reason,
            MaterialValueConversions.ToStorage(traceEvent.SourceType),
            traceEvent.SourceReference,
            MaterialActorDisplayNames.FromSubjectId(traceEvent.CreatedBySubjectId),
            traceEvent.CreatedAtUtc,
            traceEvent.Version);

    private static MaterialLotTraceabilityEvent BuildEvent(
        MaterialOperationContext context,
        MaterialLot lot,
        MaterialLotEventType eventType,
        DateTimeOffset eventAtUtc,
        string actor,
        string idempotencyKey,
        string? fingerprint = null,
        decimal? quantity = null,
        string? unitOfMeasure = null,
        string? fromWarehouseReference = null,
        string? fromLocationReference = null,
        string? toWarehouseReference = null,
        string? toLocationReference = null,
        MaterialLotStatus? statusBefore = null,
        MaterialLotStatus? statusAfter = null,
        MaterialLotQualityStatus? qualityStatusBefore = null,
        MaterialLotQualityStatus? qualityStatusAfter = null,
        string? reason = null,
        MaterialSourceType? sourceType = null,
        string? sourceReference = null) =>
        new()
        {
            Id = Guid.NewGuid(),
            TenantId = context.TenantId,
            FacilityId = context.FacilityId,
            MaterialLotId = lot.Id,
            EventType = eventType,
            EventAtUtc = eventAtUtc,
            Quantity = quantity,
            UnitOfMeasure = unitOfMeasure,
            FromWarehouseReference = fromWarehouseReference,
            FromLocationReference = fromLocationReference,
            ToWarehouseReference = toWarehouseReference ?? lot.CurrentWarehouseReference,
            ToLocationReference = toLocationReference ?? lot.CurrentLocationReference,
            StatusBefore = statusBefore,
            StatusAfter = statusAfter,
            QualityStatusBefore = qualityStatusBefore,
            QualityStatusAfter = qualityStatusAfter,
            Reason = reason?.Trim(),
            SourceType = sourceType ?? lot.SourceType,
            SourceReference = sourceReference?.Trim(),
            IdempotencyKey = idempotencyKey,
            PayloadFingerprint = fingerprint,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            CreatedBySubjectId = actor,
            Version = 1,
        };

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

public sealed class UnavailableMaterialLotRepository : IMaterialLotRepository
{
    private static MaterialLotWriteResult Unavailable() =>
        MaterialLotWriteResult.Fail(MaterialLotFailureCodes.SourceUnavailable, "Material lot service is unavailable.");

    public Task<MaterialLotWorkspaceSummaryReadModel> GetWorkspaceSummaryAsync(
        MaterialOperationContext context,
        CancellationToken cancellationToken = default) =>
        throw new InvalidOperationException("Material lot repository unavailable.");

    public Task<IReadOnlyList<MaterialLotSummaryReadModel>> ListAsync(
        MaterialOperationContext context,
        MaterialLotListFilters filters,
        CancellationToken cancellationToken = default) =>
        throw new InvalidOperationException("Material lot repository unavailable.");

    public Task<MaterialLotDetailReadModel?> GetByIdAsync(
        MaterialOperationContext context,
        Guid lotId,
        CancellationToken cancellationToken = default) =>
        throw new InvalidOperationException("Material lot repository unavailable.");

    public Task<IReadOnlyList<MaterialStorageLocationReadModel>> ListStorageLocationsAsync(
        MaterialOperationContext context,
        CancellationToken cancellationToken = default) =>
        throw new InvalidOperationException("Material lot repository unavailable.");

    public Task<IReadOnlyList<MaterialLotTimelineEntryReadModel>> GetTimelineAsync(
        MaterialOperationContext context,
        Guid lotId,
        CancellationToken cancellationToken = default) =>
        throw new InvalidOperationException("Material lot repository unavailable.");

    public Task<MaterialLotBackwardTraceabilityReadModel?> GetBackwardTraceabilityAsync(
        MaterialOperationContext context,
        Guid lotId,
        CancellationToken cancellationToken = default) =>
        throw new InvalidOperationException("Material lot repository unavailable.");

    public Task<MaterialLotForwardTraceabilityReadModel?> GetForwardTraceabilityAsync(
        MaterialOperationContext context,
        Guid lotId,
        CancellationToken cancellationToken = default) =>
        throw new InvalidOperationException("Material lot repository unavailable.");

    public Task<MaterialLotWriteResult> CreateAsync(
        MaterialOperationContext context,
        CreateMaterialLotCommand command,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Unavailable());

    public Task<MaterialLotWriteResult> ObserveReceiptAsync(
        MaterialOperationContext context,
        ObserveMaterialLotReceiptCommand command,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Unavailable());

    public Task<MaterialLotWriteResult> TransferAsync(
        MaterialOperationContext context,
        TransferMaterialLotCommand command,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Unavailable());

    public Task<MaterialLotWriteResult> TransitionStatusAsync(
        MaterialOperationContext context,
        TransitionMaterialLotStatusCommand command,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Unavailable());

    public Task<MaterialLotWriteResult> RecordQualityStatusAsync(
        MaterialOperationContext context,
        RecordMaterialLotQualityStatusCommand command,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Unavailable());

    public Task<MaterialLotWriteResult> LinkCertificateAsync(
        MaterialOperationContext context,
        LinkMaterialLotCertificateCommand command,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Unavailable());
}
