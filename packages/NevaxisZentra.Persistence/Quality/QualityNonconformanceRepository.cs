using System.Text.Json;
using NevaxisZentra.Persistence.Audit;
using NevaxisZentra.Persistence.Outbox;
using NevaxisZentra.Persistence.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace NevaxisZentra.Persistence.Quality;

public interface IQualityNonconformanceRepository
{
    Task<QualityNcrWorkspaceExtensionReadModel> GetWorkspaceExtensionAsync(
        QualityOperationContext context,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<QualityNonconformanceSummaryReadModel>> ListAsync(
        QualityOperationContext context,
        string? status = null,
        string? severity = null,
        bool? holdActive = null,
        Guid? productionOrderId = null,
        CancellationToken cancellationToken = default);

    Task<QualityNonconformanceDetailReadModel?> GetByIdAsync(
        QualityOperationContext context,
        Guid ncrId,
        CancellationToken cancellationToken = default);

    Task<LinkedNonconformanceReadModel?> FindByInspectionResultAsync(
        QualityOperationContext context,
        Guid inspectionResultId,
        CancellationToken cancellationToken = default);

    Task<QualityWriteResult> CreateAsync(
        QualityOperationContext context,
        CreateQualityNonconformanceCommand command,
        CancellationToken cancellationToken = default);

    Task<QualityWriteResult> UpdateAsync(
        QualityOperationContext context,
        UpdateQualityNonconformanceCommand command,
        CancellationToken cancellationToken = default);

    Task<QualityWriteResult> PlaceHoldAsync(
        QualityOperationContext context,
        PlaceQualityHoldCommand command,
        CancellationToken cancellationToken = default);

    Task<QualityWriteResult> ReleaseHoldAsync(
        QualityOperationContext context,
        ReleaseQualityHoldCommand command,
        CancellationToken cancellationToken = default);

    Task<QualityHoldReadModel?> GetHoldAsync(
        QualityOperationContext context,
        Guid ncrId,
        CancellationToken cancellationToken = default);

    Task<QualityWriteResult> DecideDispositionAsync(
        QualityOperationContext context,
        DecideQualityDispositionCommand command,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<QualityCorrectiveActionReadModel>> ListCorrectiveActionsAsync(
        QualityOperationContext context,
        Guid ncrId,
        CancellationToken cancellationToken = default);

    Task<QualityWriteResult> CreateCorrectiveActionAsync(
        QualityOperationContext context,
        CreateQualityCorrectiveActionCommand command,
        CancellationToken cancellationToken = default);

    Task<QualityWriteResult> StartCorrectiveActionAsync(
        QualityOperationContext context,
        QualityCorrectiveActionLifecycleCommand command,
        CancellationToken cancellationToken = default);

    Task<QualityWriteResult> CompleteCorrectiveActionAsync(
        QualityOperationContext context,
        QualityCorrectiveActionLifecycleCommand command,
        CancellationToken cancellationToken = default);

    Task<QualityWriteResult> ResolveAsync(
        QualityOperationContext context,
        QualityNonconformanceLifecycleCommand command,
        CancellationToken cancellationToken = default);

    Task<QualityWriteResult> CloseAsync(
        QualityOperationContext context,
        QualityNonconformanceLifecycleCommand command,
        CancellationToken cancellationToken = default);

    Task<QualityNcrFollowUpWorkspaceReadModel> GetFollowUpWorkspaceAsync(
        QualityOperationContext context,
        bool canManage,
        bool canCreateTask,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<QualityNcrFollowUpRowReadModel>> ListFollowUpRowsAsync(
        QualityOperationContext context,
        string? followUpStatus = null,
        string? severity = null,
        CancellationToken cancellationToken = default);

    Task<QualityNcrFollowUpDetailReadModel?> GetFollowUpDetailAsync(
        QualityOperationContext context,
        Guid ncrId,
        CancellationToken cancellationToken = default);

    Task<QualityNcrFollowUpWriteResult> LinkTaskAsync(
        QualityOperationContext context,
        LinkQualityNcrFollowUpTaskCommand command,
        CancellationToken cancellationToken = default);

    Task<QualityNcrFollowUpWriteResult> TransitionFollowUpAsync(
        QualityOperationContext context,
        TransitionQualityNcrFollowUpCommand command,
        CancellationToken cancellationToken = default);
}

public sealed class QualityNonconformanceRepository(
    NevaxisZentraDbContext dbContext,
    TenantDatabaseSession tenantDatabaseSession,
    IProductionOrderContextReader productionOrderContextReader) : IQualityNonconformanceRepository
{
    public async Task<QualityNcrWorkspaceExtensionReadModel> GetWorkspaceExtensionAsync(
        QualityOperationContext context,
        CancellationToken cancellationToken = default)
    {
        var ncrs = await tenantDatabaseSession.ExecuteReadAsync(
            ct => dbContext.QualityNonconformances.AsNoTracking()
                .Where(n => n.TenantId == context.TenantId && n.FacilityId == context.FacilityId)
                .Select(n => new { n.Status, n.HoldStatus })
                .ToListAsync(ct),
            cancellationToken);

        var activeHolds = await tenantDatabaseSession.ExecuteReadAsync(
            ct => dbContext.QualityHolds.AsNoTracking()
                .CountAsync(
                    h => h.TenantId == context.TenantId
                        && h.FacilityId == context.FacilityId
                        && h.Status == QualityHoldRecordStatus.Active,
                    ct),
            cancellationToken);

        return new QualityNcrWorkspaceExtensionReadModel(
            ncrs.Count(n => n.Status != QualityNcrStatus.Closed && n.Status != QualityNcrStatus.Cancelled),
            activeHolds,
            ncrs.Count(n => n.Status == QualityNcrStatus.ActionRequired || n.Status == QualityNcrStatus.Open));
    }

    public Task<IReadOnlyList<QualityNonconformanceSummaryReadModel>> ListAsync(
        QualityOperationContext context,
        string? status = null,
        string? severity = null,
        bool? holdActive = null,
        Guid? productionOrderId = null,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(async ct =>
        {
            var query = dbContext.QualityNonconformances.AsNoTracking()
                .Where(n => n.TenantId == context.TenantId && n.FacilityId == context.FacilityId);

            if (!string.IsNullOrWhiteSpace(status))
            {
                var parsed = QualityValueConversions.NcrStatusFromStorage(status);
                query = query.Where(n => n.Status == parsed);
            }

            if (!string.IsNullOrWhiteSpace(severity))
            {
                var parsed = QualityValueConversions.NcrSeverityFromStorage(severity);
                query = query.Where(n => n.Severity == parsed);
            }

            if (holdActive == true)
            {
                query = query.Where(n => n.HoldStatus == QualityNcrHoldStatus.Active);
            }

            if (productionOrderId.HasValue)
            {
                query = query.Where(n => n.ProductionOrderId == productionOrderId.Value);
            }

            var items = await query.OrderByDescending(n => n.UpdatedAtUtc).ToListAsync(ct);
            var orderIds = items
                .Where(n => n.ProductionOrderId.HasValue)
                .Select(n => n.ProductionOrderId!.Value)
                .Distinct()
                .ToList();

            var orders = orderIds.Count == 0
                ? new Dictionary<Guid, Production.ProductionOrder>()
                : await dbContext.ProductionOrders.AsNoTracking()
                    .Where(o => orderIds.Contains(o.Id))
                    .ToDictionaryAsync(o => o.Id, ct);

            IReadOnlyList<QualityNonconformanceSummaryReadModel> mapped = items.Select(n => new QualityNonconformanceSummaryReadModel(
                n.Id,
                n.NcrNumber,
                n.Title,
                QualityValueConversions.ToStorage(n.Severity),
                QualityValueConversions.ToStorage(n.Status),
                QualityValueConversions.ToStorage(n.HoldStatus),
                QualityValueConversions.ToStorage(n.DispositionStatus),
                n.DispositionType.HasValue
                    ? QualityValueConversions.ToStorage(n.DispositionType.Value)
                    : null,
                n.ProductionOrderId,
                n.ProductionOrderId.HasValue && orders.TryGetValue(n.ProductionOrderId.Value, out var order)
                    ? order.ReferenceCode
                    : null,
                n.ProductReference,
                n.OwnerSubjectId,
                n.DetectedAtUtc,
                n.CreatedAtUtc,
                n.Version,
                n.UpdatedAtUtc)).ToList();
            return mapped;
        }, cancellationToken);

    public Task<QualityNonconformanceDetailReadModel?> GetByIdAsync(
        QualityOperationContext context,
        Guid ncrId,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(
            ct => MapDetailAsync(context, ncrId, ct),
            cancellationToken);

    public Task<LinkedNonconformanceReadModel?> FindByInspectionResultAsync(
        QualityOperationContext context,
        Guid inspectionResultId,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(async ct =>
        {
            var ncr = await dbContext.QualityNonconformances.AsNoTracking()
                .SingleOrDefaultAsync(
                    n => n.TenantId == context.TenantId
                        && n.FacilityId == context.FacilityId
                        && n.QualityInspectionResultId == inspectionResultId
                        && n.Status != QualityNcrStatus.Cancelled,
                    ct);

            return ncr is null
                ? null
                : new LinkedNonconformanceReadModel(
                    ncr.Id,
                    ncr.NcrNumber,
                    QualityValueConversions.ToStorage(ncr.Status));
        }, cancellationToken);

    public Task<QualityWriteResult> CreateAsync(
        QualityOperationContext context,
        CreateQualityNonconformanceCommand command,
        CancellationToken cancellationToken = default) =>
        ExecuteWriteAsync(context, async ct =>
        {
            if (command.QualityInspectionResultId.HasValue)
            {
                var duplicate = await dbContext.QualityNonconformances.AsNoTracking()
                    .AnyAsync(
                        n => n.TenantId == context.TenantId
                            && n.FacilityId == context.FacilityId
                            && n.QualityInspectionResultId == command.QualityInspectionResultId.Value
                            && n.Status != QualityNcrStatus.Cancelled,
                        ct);

                if (duplicate)
                {
                    return QualityWriteResult.Fail(
                        QualityNcrFailureCodes.DuplicateNonconformance,
                        "A nonconformance already exists for this inspection result.");
                }
            }

            ProductionOrderQualityContext? production = null;
            if (command.ProductionOrderId.HasValue)
            {
                production = await productionOrderContextReader.ReadAsync(
                    context, command.ProductionOrderId.Value, ct);
                if (production is null || !production.Available)
                {
                    return QualityWriteResult.Fail(
                        QualityFailureCodes.ProductionOrderNotFound,
                        "Production order not found in facility scope.");
                }
            }

            if (command.QualityInspectionId.HasValue)
            {
                var inspection = await dbContext.QualityInspections.AsNoTracking()
                    .SingleOrDefaultAsync(
                        i => i.TenantId == context.TenantId
                            && i.FacilityId == context.FacilityId
                            && i.Id == command.QualityInspectionId.Value,
                        ct);

                if (inspection is null)
                {
                    return QualityWriteResult.Fail(QualityFailureCodes.NotFound, "Quality inspection not found.");
                }
            }

            var now = DateTimeOffset.UtcNow;
            var id = Guid.NewGuid();
            var severity = QualityValueConversions.NcrSeverityFromStorage(command.Severity);
            var ncr = new QualityNonconformance
            {
                Id = id,
                TenantId = context.TenantId,
                FacilityId = context.FacilityId,
                NcrNumber = await NextNcrNumberAsync(context, ct),
                ProductionOrderId = command.ProductionOrderId ?? production?.ProductionOrderId,
                QualityInspectionId = command.QualityInspectionId,
                QualityInspectionResultId = command.QualityInspectionResultId,
                ProductReference = command.ProductReference ?? production?.ProductReference,
                OperationReference = command.OperationReference,
                Title = command.Title.Trim(),
                Description = command.Description.Trim(),
                Category = command.Category?.Trim(),
                Severity = severity,
                Status = QualityNcrStatus.Open,
                DetectedAtUtc = now,
                DetectedBySubjectId = context.IdentitySubjectId.ToString(),
                OwnerSubjectId = command.OwnerSubjectId?.Trim(),
                DispositionStatus = QualityNcrDispositionStatus.Pending,
                HoldStatus = QualityNcrHoldStatus.None,
                FollowUpStatus = QualityNcrFollowUpStatuses.RequiresReview,
                CreatedAtUtc = now,
                UpdatedAtUtc = now,
                Version = 1,
            };

            dbContext.QualityNonconformances.Add(ncr);
            AddAudit(context, QualityNcrAuditActions.Created, QualityNcrAuditEventTypes.Created,
                ncr.Id, now, new { ncrId = ncr.Id, ncr.NcrNumber });
            AddOutbox(context, ncr, QualityNcrOutboxMessageTypes.Created, now,
                new { ncrId = ncr.Id.ToString(), ncrNumber = ncr.NcrNumber });

            await dbContext.SaveChangesAsync(ct);
            return QualityWriteResult.Success(id, 1);
        }, cancellationToken);

    public Task<QualityWriteResult> UpdateAsync(
        QualityOperationContext context,
        UpdateQualityNonconformanceCommand command,
        CancellationToken cancellationToken = default) =>
        ExecuteWriteAsync(context, async ct =>
        {
            var ncr = await LoadNcrForWriteAsync(context, command.NcrId, ct);
            if (ncr is null)
            {
                return QualityWriteResult.Fail(QualityFailureCodes.NotFound, "Nonconformance not found.");
            }

            if (ncr.Version != command.ExpectedVersion)
            {
                return ConcurrencyFail(ncr.Version, command.ExpectedVersion);
            }

            if (ncr.Status is QualityNcrStatus.Closed or QualityNcrStatus.Cancelled)
            {
                return QualityWriteResult.Fail(QualityFailureCodes.InvalidTransition, "Closed NCR cannot be updated.");
            }

            var now = DateTimeOffset.UtcNow;
            ncr.Title = command.Title.Trim();
            ncr.Description = command.Description.Trim();
            ncr.Category = command.Category?.Trim();
            ncr.Severity = QualityValueConversions.NcrSeverityFromStorage(command.Severity);
            ncr.Status = QualityValueConversions.NcrStatusFromStorage(command.Status);
            ncr.OwnerSubjectId = command.OwnerSubjectId?.Trim();
            ncr.UpdatedAtUtc = now;
            ncr.Version += 1;

            AddAudit(context, QualityNcrAuditActions.Updated, QualityNcrAuditEventTypes.Updated,
                ncr.Id, now, new { ncrId = ncr.Id });

            await dbContext.SaveChangesAsync(ct);
            return QualityWriteResult.Success(ncr.Id, ncr.Version);
        }, cancellationToken);

    public Task<QualityWriteResult> PlaceHoldAsync(
        QualityOperationContext context,
        PlaceQualityHoldCommand command,
        CancellationToken cancellationToken = default) =>
        ExecuteWriteAsync(context, async ct =>
        {
            var ncr = await LoadNcrForWriteAsync(context, command.NcrId, ct);
            if (ncr is null)
            {
                return QualityWriteResult.Fail(QualityFailureCodes.NotFound, "Nonconformance not found.");
            }

            if (ncr.Version != command.ExpectedVersion)
            {
                return ConcurrencyFail(ncr.Version, command.ExpectedVersion);
            }

            if (ncr.Status is QualityNcrStatus.Closed or QualityNcrStatus.Cancelled)
            {
                return QualityWriteResult.Fail(QualityFailureCodes.InvalidTransition, "Cannot hold a closed NCR.");
            }

            var activeHold = await dbContext.QualityHolds
                .AnyAsync(
                    h => h.NcrId == ncr.Id && h.Status == QualityHoldRecordStatus.Active,
                    ct);

            if (activeHold)
            {
                return QualityWriteResult.Fail(QualityFailureCodes.InvalidTransition, "An active hold already exists.");
            }

            var now = DateTimeOffset.UtcNow;
            var hold = new QualityHold
            {
                Id = Guid.NewGuid(),
                TenantId = context.TenantId,
                FacilityId = context.FacilityId,
                NcrId = ncr.Id,
                ProductionOrderId = ncr.ProductionOrderId,
                Scope = command.Scope.Trim(),
                Status = QualityHoldRecordStatus.Active,
                Reason = command.Reason.Trim(),
                PlacedAtUtc = now,
                PlacedBySubjectId = context.IdentitySubjectId.ToString(),
                Version = 1,
            };

            dbContext.QualityHolds.Add(hold);
            ncr.HoldStatus = QualityNcrHoldStatus.Active;
            if (ncr.Status == QualityNcrStatus.Open || ncr.Status == QualityNcrStatus.UnderReview)
            {
                ncr.Status = QualityNcrStatus.ActionRequired;
            }

            ncr.UpdatedAtUtc = now;
            ncr.Version += 1;

            AddAudit(context, QualityNcrAuditActions.HoldPlaced, QualityNcrAuditEventTypes.HoldPlaced,
                ncr.Id, now, new { ncrId = ncr.Id, holdId = hold.Id });
            AddOutbox(context, ncr, QualityNcrOutboxMessageTypes.HoldPlaced, now,
                new { ncrId = ncr.Id.ToString(), holdId = hold.Id.ToString() });

            await dbContext.SaveChangesAsync(ct);
            return QualityWriteResult.Success(ncr.Id, ncr.Version);
        }, cancellationToken);

    public Task<QualityWriteResult> ReleaseHoldAsync(
        QualityOperationContext context,
        ReleaseQualityHoldCommand command,
        CancellationToken cancellationToken = default) =>
        ExecuteWriteAsync(context, async ct =>
        {
            var ncr = await LoadNcrForWriteAsync(context, command.NcrId, ct);
            if (ncr is null)
            {
                return QualityWriteResult.Fail(QualityFailureCodes.NotFound, "Nonconformance not found.");
            }

            if (ncr.Version != command.ExpectedVersion)
            {
                return ConcurrencyFail(ncr.Version, command.ExpectedVersion);
            }

            var hold = await dbContext.QualityHolds
                .SingleOrDefaultAsync(
                    h => h.NcrId == ncr.Id && h.Status == QualityHoldRecordStatus.Active,
                    ct);

            if (hold is null)
            {
                return QualityWriteResult.Fail(QualityFailureCodes.InvalidTransition, "No active hold to release.");
            }

            var now = DateTimeOffset.UtcNow;
            hold.Status = QualityHoldRecordStatus.Released;
            hold.ReleasedAtUtc = now;
            hold.ReleasedBySubjectId = context.IdentitySubjectId.ToString();
            hold.ReleaseReason = command.ReleaseReason.Trim();
            hold.Version += 1;

            ncr.HoldStatus = QualityNcrHoldStatus.Released;
            ncr.UpdatedAtUtc = now;
            ncr.Version += 1;

            AddAudit(context, QualityNcrAuditActions.HoldReleased, QualityNcrAuditEventTypes.HoldReleased,
                ncr.Id, now, new { ncrId = ncr.Id, holdId = hold.Id });
            AddOutbox(context, ncr, QualityNcrOutboxMessageTypes.HoldReleased, now,
                new { ncrId = ncr.Id.ToString(), holdId = hold.Id.ToString() });

            await dbContext.SaveChangesAsync(ct);
            return QualityWriteResult.Success(ncr.Id, ncr.Version);
        }, cancellationToken);

    public Task<QualityHoldReadModel?> GetHoldAsync(
        QualityOperationContext context,
        Guid ncrId,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(async ct =>
        {
            var hold = await dbContext.QualityHolds.AsNoTracking()
                .Where(
                    h => h.TenantId == context.TenantId
                        && h.FacilityId == context.FacilityId
                        && h.NcrId == ncrId)
                .OrderByDescending(h => h.PlacedAtUtc)
                .FirstOrDefaultAsync(ct);

            return hold is null ? null : MapHold(hold);
        }, cancellationToken);

    public Task<QualityWriteResult> DecideDispositionAsync(
        QualityOperationContext context,
        DecideQualityDispositionCommand command,
        CancellationToken cancellationToken = default) =>
        ExecuteWriteAsync(context, async ct =>
        {
            var ncr = await LoadNcrForWriteAsync(context, command.NcrId, ct);
            if (ncr is null)
            {
                return QualityWriteResult.Fail(QualityFailureCodes.NotFound, "Nonconformance not found.");
            }

            if (ncr.Version != command.ExpectedVersion)
            {
                return ConcurrencyFail(ncr.Version, command.ExpectedVersion);
            }

            if (ncr.Status is QualityNcrStatus.Closed or QualityNcrStatus.Cancelled)
            {
                return QualityWriteResult.Fail(QualityFailureCodes.InvalidTransition, "Cannot decide disposition on closed NCR.");
            }

            var now = DateTimeOffset.UtcNow;
            ncr.DispositionType = QualityValueConversions.DispositionTypeFromStorage(command.DispositionType);
            ncr.DispositionNote = command.Note?.Trim();
            ncr.DispositionDecidedBySubjectId = context.IdentitySubjectId.ToString();
            ncr.DispositionDecidedAtUtc = now;
            ncr.DispositionStatus = QualityNcrDispositionStatus.Decided;
            ncr.UpdatedAtUtc = now;
            ncr.Version += 1;

            AddAudit(context, QualityNcrAuditActions.DispositionDecided, QualityNcrAuditEventTypes.DispositionDecided,
                ncr.Id, now, new { ncrId = ncr.Id, disposition = command.DispositionType });

            await dbContext.SaveChangesAsync(ct);
            return QualityWriteResult.Success(ncr.Id, ncr.Version);
        }, cancellationToken);

    public Task<IReadOnlyList<QualityCorrectiveActionReadModel>> ListCorrectiveActionsAsync(
        QualityOperationContext context,
        Guid ncrId,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(async ct =>
        {
            var actions = await dbContext.QualityCorrectiveActions.AsNoTracking()
                .Where(
                    a => a.TenantId == context.TenantId
                        && a.FacilityId == context.FacilityId
                        && a.NcrId == ncrId)
                .OrderBy(a => a.CreatedAtUtc)
                .ToListAsync(ct);
            IReadOnlyList<QualityCorrectiveActionReadModel> mapped = actions.Select(MapAction).ToList();
            return mapped;
        }, cancellationToken);

    public Task<QualityWriteResult> CreateCorrectiveActionAsync(
        QualityOperationContext context,
        CreateQualityCorrectiveActionCommand command,
        CancellationToken cancellationToken = default) =>
        ExecuteWriteAsync(context, async ct =>
        {
            var ncr = await dbContext.QualityNonconformances
                .SingleOrDefaultAsync(
                    n => n.TenantId == context.TenantId
                        && n.FacilityId == context.FacilityId
                        && n.Id == command.NcrId,
                    ct);

            if (ncr is null)
            {
                return QualityWriteResult.Fail(QualityFailureCodes.NotFound, "Nonconformance not found.");
            }

            if (ncr.Status is QualityNcrStatus.Closed or QualityNcrStatus.Cancelled)
            {
                return QualityWriteResult.Fail(QualityFailureCodes.InvalidTransition, "Cannot add action to closed NCR.");
            }

            var now = DateTimeOffset.UtcNow;
            var id = Guid.NewGuid();
            var action = new QualityCorrectiveAction
            {
                Id = id,
                TenantId = context.TenantId,
                FacilityId = context.FacilityId,
                NcrId = ncr.Id,
                Title = command.Title.Trim(),
                Description = command.Description.Trim(),
                OwnerSubjectId = command.OwnerSubjectId?.Trim(),
                Status = QualityCorrectiveActionStatus.Open,
                DueAtUtc = command.DueAtUtc,
                CreatedAtUtc = now,
                UpdatedAtUtc = now,
                Version = 1,
            };

            dbContext.QualityCorrectiveActions.Add(action);
            if (ncr.Status == QualityNcrStatus.Open || ncr.Status == QualityNcrStatus.UnderReview)
            {
                ncr.Status = QualityNcrStatus.ActionRequired;
            }

            ncr.UpdatedAtUtc = now;
            ncr.Version += 1;

            AddAudit(context, QualityNcrAuditActions.CorrectiveActionCreated,
                QualityNcrAuditEventTypes.CorrectiveActionCreated,
                ncr.Id, now, new { ncrId = ncr.Id, actionId = id });

            await dbContext.SaveChangesAsync(ct);
            return QualityWriteResult.Success(id, 1);
        }, cancellationToken);

    public Task<QualityWriteResult> StartCorrectiveActionAsync(
        QualityOperationContext context,
        QualityCorrectiveActionLifecycleCommand command,
        CancellationToken cancellationToken = default) =>
        MutateCorrectiveActionAsync(context, command, QualityCorrectiveActionStatus.Open, (action, now) =>
        {
            action.Status = QualityCorrectiveActionStatus.InProgress;
            action.UpdatedAtUtc = now;
            action.Version += 1;
        }, cancellationToken);

    public Task<QualityWriteResult> CompleteCorrectiveActionAsync(
        QualityOperationContext context,
        QualityCorrectiveActionLifecycleCommand command,
        CancellationToken cancellationToken = default) =>
        ExecuteWriteAsync(context, async ct =>
        {
            var action = await LoadActionForWriteAsync(context, command.CorrectiveActionId, ct);
            if (action is null)
            {
                return QualityWriteResult.Fail(QualityFailureCodes.NotFound, "Corrective action not found.");
            }

            if (action.Version != command.ExpectedVersion)
            {
                return ConcurrencyFail(action.Version, command.ExpectedVersion);
            }

            if (action.Status is not (QualityCorrectiveActionStatus.Open or QualityCorrectiveActionStatus.InProgress))
            {
                return QualityWriteResult.Fail(QualityFailureCodes.InvalidTransition, "Action cannot be completed.");
            }

            var now = DateTimeOffset.UtcNow;
            action.Status = QualityCorrectiveActionStatus.Completed;
            action.CompletedAtUtc = now;
            action.CompletedBySubjectId = context.IdentitySubjectId.ToString();
            action.CompletionNote = command.Note?.Trim();
            action.UpdatedAtUtc = now;
            action.Version += 1;

            AddAudit(context, QualityNcrAuditActions.CorrectiveActionCompleted,
                QualityNcrAuditEventTypes.CorrectiveActionCompleted,
                action.NcrId, now, new { actionId = action.Id, ncrId = action.NcrId });
            AddOutboxForAction(context, action, QualityNcrOutboxMessageTypes.CorrectiveActionCompleted, now,
                new { actionId = action.Id.ToString(), ncrId = action.NcrId.ToString() });

            await dbContext.SaveChangesAsync(ct);
            return QualityWriteResult.Success(action.Id, action.Version);
        }, cancellationToken);

    public Task<QualityWriteResult> ResolveAsync(
        QualityOperationContext context,
        QualityNonconformanceLifecycleCommand command,
        CancellationToken cancellationToken = default) =>
        ExecuteWriteAsync(context, async ct =>
        {
            var ncr = await LoadNcrForWriteAsync(context, command.NcrId, ct);
            if (ncr is null)
            {
                return QualityWriteResult.Fail(QualityFailureCodes.NotFound, "Nonconformance not found.");
            }

            if (ncr.Version != command.ExpectedVersion)
            {
                return ConcurrencyFail(ncr.Version, command.ExpectedVersion);
            }

            var actions = await dbContext.QualityCorrectiveActions
                .Where(a => a.NcrId == ncr.Id)
                .ToListAsync(ct);
            var holds = await dbContext.QualityHolds
                .Where(h => h.NcrId == ncr.Id)
                .ToListAsync(ct);

            if (!QualityNcrLifecycleEngine.CanResolve(ncr, actions, holds))
            {
                var reason = QualityNcrLifecycleEngine.ResolveBlockReason(ncr, actions, holds, closing: false)
                    ?? "NCR cannot be resolved in current state.";
                return QualityWriteResult.Fail(QualityNcrFailureCodes.ResolveNotAllowed, reason);
            }

            var now = DateTimeOffset.UtcNow;
            ncr.Status = QualityNcrStatus.Resolved;
            ncr.UpdatedAtUtc = now;
            ncr.Version += 1;

            AddAudit(context, QualityNcrAuditActions.Resolved, QualityNcrAuditEventTypes.Resolved,
                ncr.Id, now, new { ncrId = ncr.Id });
            AddOutbox(context, ncr, QualityNcrOutboxMessageTypes.Resolved, now,
                new { ncrId = ncr.Id.ToString() });

            await dbContext.SaveChangesAsync(ct);
            return QualityWriteResult.Success(ncr.Id, ncr.Version);
        }, cancellationToken);

    public Task<QualityWriteResult> CloseAsync(
        QualityOperationContext context,
        QualityNonconformanceLifecycleCommand command,
        CancellationToken cancellationToken = default) =>
        ExecuteWriteAsync(context, async ct =>
        {
            var ncr = await LoadNcrForWriteAsync(context, command.NcrId, ct);
            if (ncr is null)
            {
                return QualityWriteResult.Fail(QualityFailureCodes.NotFound, "Nonconformance not found.");
            }

            if (ncr.Version != command.ExpectedVersion)
            {
                return ConcurrencyFail(ncr.Version, command.ExpectedVersion);
            }

            var actions = await dbContext.QualityCorrectiveActions
                .Where(a => a.NcrId == ncr.Id)
                .ToListAsync(ct);
            var holds = await dbContext.QualityHolds
                .Where(h => h.NcrId == ncr.Id)
                .ToListAsync(ct);

            if (!QualityNcrLifecycleEngine.CanClose(ncr, actions, holds))
            {
                var reason = QualityNcrLifecycleEngine.ResolveBlockReason(ncr, actions, holds, closing: true)
                    ?? "NCR cannot be closed in current state.";
                return QualityWriteResult.Fail(QualityNcrFailureCodes.CloseNotAllowed, reason);
            }

            var now = DateTimeOffset.UtcNow;
            ncr.Status = QualityNcrStatus.Closed;
            ncr.ClosedAtUtc = now;
            ncr.ClosedBySubjectId = context.IdentitySubjectId.ToString();
            ncr.UpdatedAtUtc = now;
            ncr.Version += 1;

            AddAudit(context, QualityNcrAuditActions.Closed, QualityNcrAuditEventTypes.Closed,
                ncr.Id, now, new { ncrId = ncr.Id });
            AddOutbox(context, ncr, QualityNcrOutboxMessageTypes.Closed, now,
                new { ncrId = ncr.Id.ToString() });

            await dbContext.SaveChangesAsync(ct);
            return QualityWriteResult.Success(ncr.Id, ncr.Version);
        }, cancellationToken);

    private Task<QualityWriteResult> MutateCorrectiveActionAsync(
        QualityOperationContext context,
        QualityCorrectiveActionLifecycleCommand command,
        QualityCorrectiveActionStatus requiredStatus,
        Action<QualityCorrectiveAction, DateTimeOffset> mutate,
        CancellationToken cancellationToken) =>
        ExecuteWriteAsync(context, async ct =>
        {
            var action = await LoadActionForWriteAsync(context, command.CorrectiveActionId, ct);
            if (action is null)
            {
                return QualityWriteResult.Fail(QualityFailureCodes.NotFound, "Corrective action not found.");
            }

            if (action.Version != command.ExpectedVersion)
            {
                return ConcurrencyFail(action.Version, command.ExpectedVersion);
            }

            if (action.Status != requiredStatus)
            {
                return QualityWriteResult.Fail(QualityFailureCodes.InvalidTransition, "Invalid corrective action status.");
            }

            var now = DateTimeOffset.UtcNow;
            mutate(action, now);
            await dbContext.SaveChangesAsync(ct);
            return QualityWriteResult.Success(action.Id, action.Version);
        }, cancellationToken);

    private async Task<QualityNonconformanceDetailReadModel?> MapDetailAsync(
        QualityOperationContext context,
        Guid ncrId,
        CancellationToken ct)
    {
        var ncr = await dbContext.QualityNonconformances.AsNoTracking()
            .SingleOrDefaultAsync(
                n => n.TenantId == context.TenantId
                    && n.FacilityId == context.FacilityId
                    && n.Id == ncrId,
                ct);

        if (ncr is null)
        {
            return null;
        }

        ProductionOrderQualityContext? production = null;
        if (ncr.ProductionOrderId.HasValue)
        {
            production = await productionOrderContextReader.ReadAsync(context, ncr.ProductionOrderId.Value, ct);
        }

        string? inspectionNumber = null;
        if (ncr.QualityInspectionId.HasValue)
        {
            inspectionNumber = await dbContext.QualityInspections.AsNoTracking()
                .Where(i => i.Id == ncr.QualityInspectionId.Value)
                .Select(i => i.InspectionNumber)
                .FirstOrDefaultAsync(ct);
        }

        var activeHold = await dbContext.QualityHolds.AsNoTracking()
            .Where(h => h.NcrId == ncr.Id && h.Status == QualityHoldRecordStatus.Active)
            .OrderByDescending(h => h.PlacedAtUtc)
            .FirstOrDefaultAsync(ct);

        var actions = await dbContext.QualityCorrectiveActions.AsNoTracking()
            .Where(a => a.NcrId == ncr.Id)
            .OrderBy(a => a.CreatedAtUtc)
            .ToListAsync(ct);

        return new QualityNonconformanceDetailReadModel(
            ncr.Id,
            ncr.NcrNumber,
            ncr.Title,
            ncr.Description,
            ncr.Category,
            QualityValueConversions.ToStorage(ncr.Severity),
            QualityValueConversions.ToStorage(ncr.Status),
            QualityValueConversions.ToStorage(ncr.HoldStatus),
            QualityValueConversions.ToStorage(ncr.DispositionStatus),
            ncr.DispositionType.HasValue ? QualityValueConversions.ToStorage(ncr.DispositionType.Value) : null,
            ncr.DispositionNote,
            ncr.DispositionDecidedBySubjectId,
            ncr.DispositionDecidedAtUtc,
            ncr.ProductionOrderId,
            production,
            ncr.QualityInspectionId,
            inspectionNumber,
            ncr.QualityInspectionResultId,
            ncr.ProductReference,
            ncr.OperationReference,
            ncr.DetectedBySubjectId,
            ncr.OwnerSubjectId,
            ncr.DetectedAtUtc,
            ncr.ClosedAtUtc,
            ncr.ClosedBySubjectId,
            ncr.Version,
            ncr.CreatedAtUtc,
            ncr.UpdatedAtUtc,
            activeHold is null ? null : MapHold(activeHold),
            actions.Select(MapAction).ToList());
    }

    private static QualityHoldReadModel MapHold(QualityHold hold) =>
        new(
            hold.Id,
            hold.Scope,
            QualityValueConversions.ToStorage(hold.Status),
            hold.Reason,
            hold.PlacedAtUtc,
            hold.PlacedBySubjectId,
            hold.ReleasedAtUtc,
            hold.ReleasedBySubjectId,
            hold.ReleaseReason,
            hold.Version);

    private static QualityCorrectiveActionReadModel MapAction(QualityCorrectiveAction action) =>
        new(
            action.Id,
            action.Title,
            action.Description,
            QualityValueConversions.ToStorage(action.Status),
            action.OwnerSubjectId,
            action.DueAtUtc,
            action.CompletedAtUtc,
            action.CompletionNote,
            action.Version,
            action.UpdatedAtUtc);

    private async Task<QualityNonconformance?> LoadNcrForWriteAsync(
        QualityOperationContext context,
        Guid ncrId,
        CancellationToken ct) =>
        await dbContext.QualityNonconformances
            .SingleOrDefaultAsync(
                n => n.TenantId == context.TenantId
                    && n.FacilityId == context.FacilityId
                    && n.Id == ncrId,
                ct);

    private async Task<QualityCorrectiveAction?> LoadActionForWriteAsync(
        QualityOperationContext context,
        Guid actionId,
        CancellationToken ct) =>
        await dbContext.QualityCorrectiveActions
            .SingleOrDefaultAsync(
                a => a.TenantId == context.TenantId
                    && a.FacilityId == context.FacilityId
                    && a.Id == actionId,
                ct);

    private async Task<string> NextNcrNumberAsync(QualityOperationContext context, CancellationToken ct)
    {
        var year = DateTimeOffset.UtcNow.Year;
        var count = await dbContext.QualityNonconformances
            .CountAsync(
                n => n.TenantId == context.TenantId
                    && n.FacilityId == context.FacilityId
                    && n.NcrNumber.StartsWith($"NCR-{year}-"),
                ct);
        return $"NCR-{year}-{(count + 1):D6}";
    }

    private static QualityWriteResult ConcurrencyFail(uint actual, uint expected) =>
        QualityWriteResult.Fail(
            QualityFailureCodes.ConcurrencyConflict,
            $"The record was modified (have {actual}, expected {expected}).");

    private async Task<QualityWriteResult> ExecuteWriteAsync(
        QualityOperationContext context,
        Func<CancellationToken, Task<QualityWriteResult>> action,
        CancellationToken cancellationToken)
    {
        if (context.TenantId == Guid.Empty || context.FacilityId == Guid.Empty)
        {
            return QualityWriteResult.Fail(QualityFailureCodes.InvalidScope, "Tenant and facility are required.");
        }

        try
        {
            return await tenantDatabaseSession.ExecuteWriteAsync(action, cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return QualityWriteResult.Fail(
                QualityFailureCodes.ConcurrencyConflict,
                "The record was modified by another user. Please reload and try again.");
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            return QualityWriteResult.Fail(
                QualityNcrFailureCodes.DuplicateNonconformance,
                "A nonconformance already exists for this inspection result.");
        }
        catch (Exception) when (!cancellationToken.IsCancellationRequested)
        {
            return QualityWriteResult.Fail(QualityFailureCodes.SourceUnavailable, "Quality NCR service is unavailable.");
        }
    }

    private void AddAudit(
        QualityOperationContext context,
        string action,
        string eventType,
        Guid ncrId,
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
            ResourceType = "quality.nonconformance",
            ResourceId = ncrId.ToString(),
            Outcome = AuditOutcome.Success,
            PermissionKey = context.PermissionKey,
            CorrelationId = context.CorrelationId ?? Guid.NewGuid(),
            Source = AuditSource.Api,
            OccurredAtUtc = now,
            RecordedAtUtc = now,
            MetadataJson = JsonSerializer.Serialize(metadata),
        });
    }

    private void AddOutbox(
        QualityOperationContext context,
        QualityNonconformance ncr,
        string messageType,
        DateTimeOffset now,
        object payload)
    {
        dbContext.OutboxMessages.Add(new OutboxMessage
        {
            Id = Guid.NewGuid(),
            TenantId = context.TenantId,
            MessageType = messageType,
            SchemaVersion = OutboxMessageTypes.SchemaVersion,
            AggregateType = "quality-nonconformance",
            AggregateId = ncr.Id.ToString(),
            CorrelationId = context.CorrelationId ?? Guid.NewGuid(),
            CausationId = null,
            IdempotencyKey = $"{ncr.Id}:{ncr.Version}:{messageType}",
            PayloadJson = JsonSerializer.Serialize(payload),
            HeadersJson = "{}",
            Status = OutboxMessageStatus.Pending,
            OccurredAtUtc = now,
            RecordedAtUtc = now,
            AvailableAtUtc = now,
            AttemptCount = 0,
        });
    }

    private void AddOutboxForAction(
        QualityOperationContext context,
        QualityCorrectiveAction action,
        string messageType,
        DateTimeOffset now,
        object payload)
    {
        dbContext.OutboxMessages.Add(new OutboxMessage
        {
            Id = Guid.NewGuid(),
            TenantId = context.TenantId,
            MessageType = messageType,
            SchemaVersion = OutboxMessageTypes.SchemaVersion,
            AggregateType = "quality-corrective-action",
            AggregateId = action.Id.ToString(),
            CorrelationId = context.CorrelationId ?? Guid.NewGuid(),
            CausationId = null,
            IdempotencyKey = $"{action.Id}:{action.Version}:{messageType}",
            PayloadJson = JsonSerializer.Serialize(payload),
            HeadersJson = "{}",
            Status = OutboxMessageStatus.Pending,
            OccurredAtUtc = now,
            RecordedAtUtc = now,
            AvailableAtUtc = now,
            AttemptCount = 0,
        });
    }

    private static bool IsUniqueViolation(DbUpdateException ex) =>
        ex.InnerException?.Message.Contains("23505", StringComparison.OrdinalIgnoreCase) == true
        || ex.InnerException?.Message.Contains("unique constraint", StringComparison.OrdinalIgnoreCase) == true;

    public Task<QualityNcrFollowUpWorkspaceReadModel> GetFollowUpWorkspaceAsync(
        QualityOperationContext context,
        bool canManage,
        bool canCreateTask,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(async ct =>
        {
            var ncrs = await dbContext.QualityNonconformances.AsNoTracking()
                .Where(n => n.TenantId == context.TenantId && n.FacilityId == context.FacilityId)
                .Where(n => n.Status != QualityNcrStatus.Closed
                            && n.Status != QualityNcrStatus.Cancelled
                            && n.Status != QualityNcrStatus.Resolved)
                .ToListAsync(ct);

            return new QualityNcrFollowUpWorkspaceReadModel(
                QualityFollowUpConstants.AutomationDisclaimer,
                ncrs.Count(n => n.FollowUpStatus == QualityNcrFollowUpStatuses.RequiresReview),
                ncrs.Count(n => n.FollowUpStatus == QualityNcrFollowUpStatuses.Acknowledged),
                ncrs.Count(n => n.LinkedAiTaskId.HasValue),
                ncrs.Count(n => n.Severity is QualityNcrSeverity.Major or QualityNcrSeverity.Critical),
                ncrs.Count(QualityFollowUpPresentationBuilder.HasInsufficientEvidence),
                CanRead: true,
                canManage,
                canCreateTask);
        }, cancellationToken);

    public Task<IReadOnlyList<QualityNcrFollowUpRowReadModel>> ListFollowUpRowsAsync(
        QualityOperationContext context,
        string? followUpStatus = null,
        string? severity = null,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(async ct =>
        {
            var query = dbContext.QualityNonconformances.AsNoTracking()
                .Where(n => n.TenantId == context.TenantId && n.FacilityId == context.FacilityId)
                .Where(n => n.Status != QualityNcrStatus.Closed
                            && n.Status != QualityNcrStatus.Cancelled
                            && n.Status != QualityNcrStatus.Resolved);

            if (!string.IsNullOrWhiteSpace(followUpStatus))
            {
                query = query.Where(n => n.FollowUpStatus == followUpStatus);
            }

            if (!string.IsNullOrWhiteSpace(severity))
            {
                var parsed = QualityValueConversions.NcrSeverityFromStorage(severity);
                query = query.Where(n => n.Severity == parsed);
            }

            var items = await query.OrderByDescending(n => n.UpdatedAtUtc).ToListAsync(ct);
            var orderIds = items.Where(n => n.ProductionOrderId.HasValue)
                .Select(n => n.ProductionOrderId!.Value).Distinct().ToList();
            var orders = orderIds.Count == 0
                ? new Dictionary<Guid, Production.ProductionOrder>()
                : await dbContext.ProductionOrders.AsNoTracking()
                    .Where(o => orderIds.Contains(o.Id))
                    .ToDictionaryAsync(o => o.Id, ct);

            return (IReadOnlyList<QualityNcrFollowUpRowReadModel>)items.Select(n =>
            {
                var insufficient = QualityFollowUpPresentationBuilder.HasInsufficientEvidence(n);
                var purpose = QualityFollowUpPresentationBuilder.ResolveTaskPurpose(n);
                return new QualityNcrFollowUpRowReadModel(
                    n.Id,
                    n.NcrNumber,
                    n.Title,
                    QualityValueConversions.ToStorage(n.Severity),
                    QualityValueConversions.ToStorage(n.Status),
                    n.FollowUpStatus,
                    QualityValueConversions.ToStorage(n.HoldStatus),
                    QualityValueConversions.ToStorage(n.DispositionStatus),
                    n.ProductReference,
                    n.ProductionOrderId,
                    n.ProductionOrderId.HasValue && orders.TryGetValue(n.ProductionOrderId.Value, out var order)
                        ? order.ReferenceCode
                        : null,
                    n.DetectedAtUtc,
                    n.LinkedAiTaskId,
                    QualityFollowUpPresentationBuilder.CanCreateTaskDraft(n),
                    purpose,
                    insufficient);
            }).ToList();
        }, cancellationToken);

    public Task<QualityNcrFollowUpDetailReadModel?> GetFollowUpDetailAsync(
        QualityOperationContext context,
        Guid ncrId,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(async ct =>
        {
            var detail = await MapDetailAsync(context, ncrId, ct);
            if (detail is null)
            {
                return null;
            }

            var ncr = await dbContext.QualityNonconformances.AsNoTracking()
                .SingleAsync(
                    n => n.TenantId == context.TenantId
                         && n.FacilityId == context.FacilityId
                         && n.Id == ncrId,
                    ct);

            var auditEvents = await dbContext.QualityNcrFollowUpAuditEvents.AsNoTracking()
                .Where(e => e.TenantId == context.TenantId
                            && e.FacilityId == context.FacilityId
                            && e.NcrId == ncrId)
                .OrderByDescending(e => e.OccurredAtUtc)
                .Take(20)
                .Select(e => new QualityNcrFollowUpAuditEventReadModel(
                    e.EventType,
                    e.PayloadJson,
                    e.Actor,
                    e.OccurredAtUtc))
                .ToListAsync(ct);

            var insufficient = QualityFollowUpPresentationBuilder.HasInsufficientEvidence(ncr);
            return new QualityNcrFollowUpDetailReadModel(
                detail,
                ncr.FollowUpStatus,
                ncr.LinkedAiTaskId,
                QualityFollowUpPresentationBuilder.CanCreateTaskDraft(ncr),
                QualityFollowUpPresentationBuilder.ResolveTaskPurpose(ncr),
                insufficient,
                QualityFollowUpPresentationBuilder.BuildEvidenceSummary(ncr),
                auditEvents);
        }, cancellationToken);

    public Task<QualityNcrFollowUpWriteResult> LinkTaskAsync(
        QualityOperationContext context,
        LinkQualityNcrFollowUpTaskCommand command,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteWriteAsync(
            async ct =>
            {
                var ncr = await LoadNcrForWriteAsync(context, command.NcrId, ct);
                if (ncr is null)
                {
                    return QualityNcrFollowUpWriteResult.Fail(
                        QualityFailureCodes.NotFound,
                        "Uygunsuzluk kaydı bulunamadı.");
                }

                if (ncr.LinkedAiTaskId.HasValue)
                {
                    if (ncr.LinkedAiTaskId.Value == command.AiTaskId)
                    {
                        var existing = await GetFollowUpDetailAsync(context, ncr.Id, ct);
                        return QualityNcrFollowUpWriteResult.Success(
                            ncr.Id,
                            idempotentReplay: true,
                            detail: existing);
                    }

                    return QualityNcrFollowUpWriteResult.Fail(
                        QualityFailureCodes.ValidationFailed,
                        "Uygunsuzluk kaydı zaten farklı bir göreve bağlı.");
                }

                if (!QualityFollowUpPresentationBuilder.CanCreateTaskDraft(ncr))
                {
                    return QualityNcrFollowUpWriteResult.Fail(
                        QualityFailureCodes.ValidationFailed,
                        "Bu uygunsuzluk kaydı için görev taslağı oluşturulamaz.");
                }

                var taskExists = await dbContext.AiTasks.AsNoTracking()
                    .AnyAsync(
                        task => task.TenantId == context.TenantId
                                && task.FacilityId == context.FacilityId
                                && task.Id == command.AiTaskId,
                        ct);

                if (!taskExists)
                {
                    return QualityNcrFollowUpWriteResult.Fail(
                        QualityFailureCodes.NotFound,
                        "Bağlanacak AI görevi bulunamadı.");
                }

                var now = DateTimeOffset.UtcNow;
                var actor = context.IdentitySubjectId.ToString();
                ncr.LinkedAiTaskId = command.AiTaskId;
                ncr.FollowUpStatus = QualityNcrFollowUpStatuses.TaskCreated;
                ncr.FollowUpUpdatedBy = actor;
                ncr.UpdatedAtUtc = now;
                ncr.Version += 1;

                await AppendFollowUpAuditEventAsync(ncr, QualityNcrFollowUpAuditEventTypes.TaskLinked,
                    new { aiTaskId = command.AiTaskId }, actor, now, ct);

                await dbContext.SaveChangesAsync(ct);

                var detail = await GetFollowUpDetailAsync(context, ncr.Id, ct);
                return QualityNcrFollowUpWriteResult.Success(ncr.Id, detail: detail);
            },
            cancellationToken);

    public Task<QualityNcrFollowUpWriteResult> TransitionFollowUpAsync(
        QualityOperationContext context,
        TransitionQualityNcrFollowUpCommand command,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteWriteAsync(
            async ct =>
            {
                var ncr = await LoadNcrForWriteAsync(context, command.NcrId, ct);
                if (ncr is null)
                {
                    return QualityNcrFollowUpWriteResult.Fail(
                        QualityFailureCodes.NotFound,
                        "Uygunsuzluk kaydı bulunamadı.");
                }

                if (ncr.Version != command.ExpectedVersion)
                {
                    return QualityNcrFollowUpWriteResult.Fail(
                        QualityFailureCodes.ConcurrencyConflict,
                        "Kayıt başka bir kullanıcı tarafından güncellendi.");
                }

                if (command.TargetFollowUpStatus == QualityNcrFollowUpStatuses.Dismissed
                    && string.IsNullOrWhiteSpace(command.Note))
                {
                    return QualityNcrFollowUpWriteResult.Fail(
                        QualityFailureCodes.ValidationFailed,
                        "Red gerekçesi zorunludur.");
                }

                if (!QualityFollowUpPresentationBuilder.IsValidFollowUpTransition(
                        ncr.FollowUpStatus,
                        command.TargetFollowUpStatus))
                {
                    return QualityNcrFollowUpWriteResult.Fail(
                        QualityFailureCodes.InvalidTransition,
                        "Geçersiz takip durumu geçişi.");
                }

                var now = DateTimeOffset.UtcNow;
                var actor = context.IdentitySubjectId.ToString();
                var previous = ncr.FollowUpStatus;
                ncr.FollowUpStatus = command.TargetFollowUpStatus;
                ncr.FollowUpUpdatedBy = actor;
                ncr.UpdatedAtUtc = now;
                ncr.Version += 1;

                await AppendFollowUpAuditEventAsync(
                    ncr,
                    QualityNcrFollowUpAuditEventTypes.FollowUpTransitioned,
                    new
                    {
                        previousStatus = previous,
                        targetStatus = command.TargetFollowUpStatus,
                        note = command.Note,
                    },
                    actor,
                    now,
                    ct);

                await dbContext.SaveChangesAsync(ct);

                var detail = await GetFollowUpDetailAsync(context, ncr.Id, ct);
                return QualityNcrFollowUpWriteResult.Success(ncr.Id, detail: detail);
            },
            cancellationToken);

    private async Task AppendFollowUpAuditEventAsync(
        QualityNonconformance ncr,
        string eventType,
        object payload,
        string actor,
        DateTimeOffset occurredAtUtc,
        CancellationToken cancellationToken)
    {
        dbContext.QualityNcrFollowUpAuditEvents.Add(new QualityNcrFollowUpAuditEvent
        {
            Id = Guid.NewGuid(),
            TenantId = ncr.TenantId,
            FacilityId = ncr.FacilityId,
            NcrId = ncr.Id,
            EventType = eventType,
            PayloadJson = JsonSerializer.Serialize(payload),
            Actor = actor,
            OccurredAtUtc = occurredAtUtc,
        });

        await Task.CompletedTask;
    }
}

public sealed class UnavailableQualityNonconformanceRepository : IQualityNonconformanceRepository
{
    private static QualityWriteResult Unavailable() =>
        QualityWriteResult.Fail(QualityFailureCodes.SourceUnavailable, "Quality NCR service is unavailable.");

    public Task<QualityNcrWorkspaceExtensionReadModel> GetWorkspaceExtensionAsync(
        QualityOperationContext context,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(new QualityNcrWorkspaceExtensionReadModel(0, 0, 0));

    public Task<IReadOnlyList<QualityNonconformanceSummaryReadModel>> ListAsync(
        QualityOperationContext context,
        string? status = null,
        string? severity = null,
        bool? holdActive = null,
        Guid? productionOrderId = null,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<QualityNonconformanceSummaryReadModel>>([]);

    public Task<QualityNonconformanceDetailReadModel?> GetByIdAsync(
        QualityOperationContext context,
        Guid ncrId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<QualityNonconformanceDetailReadModel?>(null);

    public Task<LinkedNonconformanceReadModel?> FindByInspectionResultAsync(
        QualityOperationContext context,
        Guid inspectionResultId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<LinkedNonconformanceReadModel?>(null);

    public Task<QualityWriteResult> CreateAsync(
        QualityOperationContext context,
        CreateQualityNonconformanceCommand command,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Unavailable());

    public Task<QualityWriteResult> UpdateAsync(
        QualityOperationContext context,
        UpdateQualityNonconformanceCommand command,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Unavailable());

    public Task<QualityWriteResult> PlaceHoldAsync(
        QualityOperationContext context,
        PlaceQualityHoldCommand command,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Unavailable());

    public Task<QualityWriteResult> ReleaseHoldAsync(
        QualityOperationContext context,
        ReleaseQualityHoldCommand command,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Unavailable());

    public Task<QualityHoldReadModel?> GetHoldAsync(
        QualityOperationContext context,
        Guid ncrId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<QualityHoldReadModel?>(null);

    public Task<QualityWriteResult> DecideDispositionAsync(
        QualityOperationContext context,
        DecideQualityDispositionCommand command,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Unavailable());

    public Task<IReadOnlyList<QualityCorrectiveActionReadModel>> ListCorrectiveActionsAsync(
        QualityOperationContext context,
        Guid ncrId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<QualityCorrectiveActionReadModel>>([]);

    public Task<QualityWriteResult> CreateCorrectiveActionAsync(
        QualityOperationContext context,
        CreateQualityCorrectiveActionCommand command,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Unavailable());

    public Task<QualityWriteResult> StartCorrectiveActionAsync(
        QualityOperationContext context,
        QualityCorrectiveActionLifecycleCommand command,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Unavailable());

    public Task<QualityWriteResult> CompleteCorrectiveActionAsync(
        QualityOperationContext context,
        QualityCorrectiveActionLifecycleCommand command,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Unavailable());

    public Task<QualityWriteResult> ResolveAsync(
        QualityOperationContext context,
        QualityNonconformanceLifecycleCommand command,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Unavailable());

    public Task<QualityWriteResult> CloseAsync(
        QualityOperationContext context,
        QualityNonconformanceLifecycleCommand command,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Unavailable());

    private static QualityNcrFollowUpWriteResult FollowUpUnavailable() =>
        QualityNcrFollowUpWriteResult.Fail(QualityFailureCodes.SourceUnavailable, "Quality NCR follow-up service is unavailable.");

    public Task<QualityNcrFollowUpWorkspaceReadModel> GetFollowUpWorkspaceAsync(
        QualityOperationContext context,
        bool canManage,
        bool canCreateTask,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(new QualityNcrFollowUpWorkspaceReadModel(
            QualityFollowUpConstants.AutomationDisclaimer, 0, 0, 0, 0, 0, false, false, false));

    public Task<IReadOnlyList<QualityNcrFollowUpRowReadModel>> ListFollowUpRowsAsync(
        QualityOperationContext context,
        string? followUpStatus = null,
        string? severity = null,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<QualityNcrFollowUpRowReadModel>>([]);

    public Task<QualityNcrFollowUpDetailReadModel?> GetFollowUpDetailAsync(
        QualityOperationContext context,
        Guid ncrId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<QualityNcrFollowUpDetailReadModel?>(null);

    public Task<QualityNcrFollowUpWriteResult> LinkTaskAsync(
        QualityOperationContext context,
        LinkQualityNcrFollowUpTaskCommand command,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(FollowUpUnavailable());

    public Task<QualityNcrFollowUpWriteResult> TransitionFollowUpAsync(
        QualityOperationContext context,
        TransitionQualityNcrFollowUpCommand command,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(FollowUpUnavailable());
}
