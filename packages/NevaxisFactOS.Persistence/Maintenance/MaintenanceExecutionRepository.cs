using NevaxisFactOS.Persistence.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace NevaxisFactOS.Persistence.Maintenance;

public interface IMaintenanceWorkNoteRepository
{
    Task<IReadOnlyList<MaintenanceWorkNoteReadModel>> ListByWorkOrderAsync(
        MaintenanceOperationContext context,
        Guid workOrderId,
        CancellationToken cancellationToken = default);

    Task<MaintenanceWriteResult> CreateAsync(
        MaintenanceOperationContext context,
        CreateMaintenanceWorkNoteCommand command,
        CancellationToken cancellationToken = default);
}

public interface IMaintenanceTimelineRepository
{
    Task<IReadOnlyList<MaintenanceTimelineEntryReadModel>> GetWorkOrderTimelineAsync(
        MaintenanceOperationContext context,
        Guid workOrderId,
        CancellationToken cancellationToken = default);
}

public interface IPreventiveMaintenanceDueRepository
{
    Task<IReadOnlyList<PreventiveMaintenanceDueSummaryReadModel>> ListAsync(
        MaintenanceOperationContext context,
        string? status,
        CancellationToken cancellationToken = default);

    Task<PreventiveMaintenanceDueDetailReadModel?> GetByIdAsync(
        MaintenanceOperationContext context,
        Guid dueId,
        CancellationToken cancellationToken = default);

    Task<MaintenanceWriteResult> GenerateNextDueAsync(
        MaintenanceOperationContext context,
        Guid planId,
        CancellationToken cancellationToken = default);

    Task<MaintenanceWriteResult> CreateWorkOrderFromDueAsync(
        MaintenanceOperationContext context,
        Guid dueId,
        uint expectedVersion,
        CancellationToken cancellationToken = default);
}

public interface IMaintenancePartUsageRepository
{
    Task<IReadOnlyList<MaintenancePartUsageReadModel>> ListByWorkOrderAsync(
        MaintenanceOperationContext context,
        Guid workOrderId,
        CancellationToken cancellationToken = default);

    Task<MaintenanceWriteResult> CreateAsync(
        MaintenanceOperationContext context,
        CreateMaintenancePartUsageCommand command,
        CancellationToken cancellationToken = default);
}

public interface IMaintenanceCostSummaryRepository
{
    Task<MaintenanceCostSummaryReadModel?> GetByWorkOrderAsync(
        MaintenanceOperationContext context,
        Guid workOrderId,
        CancellationToken cancellationToken = default);
}

public interface IMaintenanceAssetHistoryRepository
{
    Task<MaintenanceAssetHistoryReadModel?> GetByAssetReferenceAsync(
        MaintenanceOperationContext context,
        string assetReference,
        CancellationToken cancellationToken = default);
}

public sealed class MaintenanceWorkNoteRepository(
    NevaxisFactOSDbContext dbContext,
    TenantDatabaseSession tenantDatabaseSession) : IMaintenanceWorkNoteRepository
{
    public Task<IReadOnlyList<MaintenanceWorkNoteReadModel>> ListByWorkOrderAsync(
        MaintenanceOperationContext context,
        Guid workOrderId,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(async ct =>
        {
            var notes = await dbContext.MaintenanceWorkNotes.AsNoTracking()
                .Where(n => n.TenantId == context.TenantId
                    && n.FacilityId == context.FacilityId
                    && n.WorkOrderId == workOrderId)
                .OrderByDescending(n => n.CreatedAtUtc)
                .ToListAsync(ct);

            return (IReadOnlyList<MaintenanceWorkNoteReadModel>)notes.Select(MapNote).ToList();
        }, cancellationToken);

    public Task<MaintenanceWriteResult> CreateAsync(
        MaintenanceOperationContext context,
        CreateMaintenanceWorkNoteCommand command,
        CancellationToken cancellationToken = default) =>
        MaintenanceRepositorySupport.ExecuteWriteAsync(
            tenantDatabaseSession,
            context,
            async ct =>
            {
                if (string.IsNullOrWhiteSpace(command.Text))
                {
                    return MaintenanceWriteResult.Fail(MaintenanceFailureCodes.ValidationFailed, "Note text is required.");
                }

                var workOrderExists = await dbContext.MaintenanceWorkOrders.AsNoTracking()
                    .AnyAsync(
                        w => w.TenantId == context.TenantId
                            && w.FacilityId == context.FacilityId
                            && w.Id == command.WorkOrderId,
                        ct);

                if (!workOrderExists)
                {
                    return MaintenanceWriteResult.Fail(MaintenanceFailureCodes.NotFound, "Maintenance work order not found.");
                }

                var now = DateTimeOffset.UtcNow;
                var note = new MaintenanceWorkNote
                {
                    Id = Guid.NewGuid(),
                    TenantId = context.TenantId,
                    FacilityId = context.FacilityId,
                    WorkOrderId = command.WorkOrderId,
                    NoteType = MaintenanceValueConversions.WorkNoteTypeFromStorage(command.NoteType),
                    Text = command.Text.Trim(),
                    IsImportant = command.IsImportant,
                    CreatedAtUtc = now,
                    CreatedBySubjectId = context.IdentitySubjectId.ToString(),
                };

                dbContext.MaintenanceWorkNotes.Add(note);
                MaintenanceRepositorySupport.AddAudit(
                    dbContext, context,
                    MaintenanceAuditActions.WorkNoteCreated,
                    MaintenanceAuditEventTypes.WorkNoteCreated,
                    "maintenance.worknote",
                    note.Id,
                    now,
                    new { noteId = note.Id, workOrderId = command.WorkOrderId, noteType = command.NoteType });
                MaintenanceRepositorySupport.AddOutbox(
                    dbContext, context,
                    "maintenance-work-note",
                    note.Id,
                    1,
                    MaintenanceOutboxMessageTypes.WorkNoteCreated,
                    now,
                    new { noteId = note.Id.ToString(), workOrderId = command.WorkOrderId.ToString() });

                await dbContext.SaveChangesAsync(ct);
                return MaintenanceWriteResult.Success(note.Id, 1);
            },
            cancellationToken);

    internal static MaintenanceWorkNoteReadModel MapNote(MaintenanceWorkNote note) =>
        new(
            note.Id,
            note.WorkOrderId,
            MaintenanceValueConversions.ToStorage(note.NoteType),
            note.Text,
            note.IsImportant,
            note.CreatedAtUtc,
            note.CreatedBySubjectId);
}

public sealed class MaintenanceTimelineRepository(
    NevaxisFactOSDbContext dbContext,
    TenantDatabaseSession tenantDatabaseSession) : IMaintenanceTimelineRepository
{
    public Task<IReadOnlyList<MaintenanceTimelineEntryReadModel>> GetWorkOrderTimelineAsync(
        MaintenanceOperationContext context,
        Guid workOrderId,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(async ct =>
        {
            var workOrder = await dbContext.MaintenanceWorkOrders.AsNoTracking()
                .SingleOrDefaultAsync(
                    w => w.TenantId == context.TenantId
                        && w.FacilityId == context.FacilityId
                        && w.Id == workOrderId,
                    ct);

            if (workOrder is null)
            {
                return (IReadOnlyList<MaintenanceTimelineEntryReadModel>)[];
            }

            var entries = new List<MaintenanceTimelineEntryReadModel>
            {
                new(
                    "work-order-created",
                    "Bakım emri oluşturuldu",
                    workOrder.WorkOrderNumber,
                    workOrder.CreatedAtUtc,
                    workOrder.CreatedBySubjectId),
            };

            if (workOrder.StartedAtUtc.HasValue)
            {
                entries.Add(new("work-order-started", "Bakım başlatıldı", null, workOrder.StartedAtUtc.Value, null));
            }

            if (workOrder.CompletedAtUtc.HasValue)
            {
                entries.Add(new(
                    "work-order-completed",
                    "Bakım tamamlandı",
                    workOrder.CompletionNote,
                    workOrder.CompletedAtUtc.Value,
                    null));
            }

            var auditRecords = await dbContext.AuditRecords.AsNoTracking()
                .Where(a => a.TenantId == context.TenantId
                    && a.ResourceType == "maintenance.workorder"
                    && a.ResourceId == workOrderId.ToString())
                .OrderBy(a => a.OccurredAtUtc)
                .ToListAsync(ct);

            foreach (var audit in auditRecords)
            {
                if (audit.Action is MaintenanceAuditActions.WorkOrderCreated
                    or MaintenanceAuditActions.WorkOrderCompleted)
                {
                    continue;
                }

                entries.Add(new(
                    audit.Action,
                    MapAuditTitle(audit.Action),
                    null,
                    audit.OccurredAtUtc,
                    audit.IdentitySubjectId.ToString()));
            }

            var notes = await dbContext.MaintenanceWorkNotes.AsNoTracking()
                .Where(n => n.TenantId == context.TenantId
                    && n.FacilityId == context.FacilityId
                    && n.WorkOrderId == workOrderId)
                .OrderBy(n => n.CreatedAtUtc)
                .ToListAsync(ct);

            foreach (var note in notes)
            {
                entries.Add(new(
                    "work-note",
                    $"Not — {MaintenanceValueConversions.ToStorage(note.NoteType)}",
                    note.Text,
                    note.CreatedAtUtc,
                    note.CreatedBySubjectId));
            }

            return (IReadOnlyList<MaintenanceTimelineEntryReadModel>)entries
                .OrderBy(e => e.OccurredAtUtc)
                .ThenBy(e => e.EntryType)
                .ToList();
        }, cancellationToken);

    private static string MapAuditTitle(string action) => action switch
    {
        MaintenanceAuditActions.WorkOrderAssigned => "Teknisyen atandı",
        MaintenanceAuditActions.WorkOrderStarted => "Bakım başlatıldı",
        MaintenanceAuditActions.WorkOrderWaiting => "Beklemede",
        MaintenanceAuditActions.WorkOrderResumed => "Bakıma devam edildi",
        MaintenanceAuditActions.WorkOrderCompleted => "Bakım tamamlandı",
        MaintenanceAuditActions.WorkOrderCancelled => "Bakım emri iptal edildi",
        _ => action,
    };
}

public sealed class PreventiveMaintenanceDueRepository(
    NevaxisFactOSDbContext dbContext,
    TenantDatabaseSession tenantDatabaseSession) : IPreventiveMaintenanceDueRepository
{
    public Task<IReadOnlyList<PreventiveMaintenanceDueSummaryReadModel>> ListAsync(
        MaintenanceOperationContext context,
        string? status,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(async ct =>
        {
            var query = dbContext.PreventiveMaintenanceDues.AsNoTracking()
                .Where(d => d.TenantId == context.TenantId && d.FacilityId == context.FacilityId);

            if (!string.IsNullOrWhiteSpace(status))
            {
                var statusEnum = MaintenanceValueConversions.DueStatusFromStorage(status);
                query = query.Where(d => d.Status == statusEnum);
            }

            var dues = await query.OrderBy(d => d.DueAtUtc).ToListAsync(ct);
            return await MapSummariesAsync(dues, context, ct);
        }, cancellationToken);

    public Task<PreventiveMaintenanceDueDetailReadModel?> GetByIdAsync(
        MaintenanceOperationContext context,
        Guid dueId,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(async ct =>
        {
            var due = await dbContext.PreventiveMaintenanceDues.AsNoTracking()
                .SingleOrDefaultAsync(
                    d => d.TenantId == context.TenantId
                        && d.FacilityId == context.FacilityId
                        && d.Id == dueId,
                    ct);

            if (due is null)
            {
                return null;
            }

            var plan = await dbContext.PreventiveMaintenancePlans.AsNoTracking()
                .SingleAsync(
                    p => p.TenantId == context.TenantId
                        && p.FacilityId == context.FacilityId
                        && p.Id == due.PreventivePlanId,
                    ct);

            string? workOrderNumber = null;
            if (due.WorkOrderId.HasValue)
            {
                workOrderNumber = await dbContext.MaintenanceWorkOrders.AsNoTracking()
                    .Where(w => w.Id == due.WorkOrderId.Value)
                    .Select(w => w.WorkOrderNumber)
                    .SingleOrDefaultAsync(ct);
            }

            return new PreventiveMaintenanceDueDetailReadModel(
                due.Id,
                due.PreventivePlanId,
                plan.Code,
                plan.Name,
                due.AssetType,
                due.AssetReference,
                due.DueAtUtc,
                MaintenanceValueConversions.ToStorage(due.Status),
                due.GeneratedAtUtc,
                due.WorkOrderId,
                workOrderNumber,
                due.CompletedAtUtc,
                due.Version);
        }, cancellationToken);

    public Task<MaintenanceWriteResult> GenerateNextDueAsync(
        MaintenanceOperationContext context,
        Guid planId,
        CancellationToken cancellationToken = default) =>
        MaintenanceRepositorySupport.ExecuteWriteAsync(
            tenantDatabaseSession,
            context,
            async ct =>
            {
                var plan = await dbContext.PreventiveMaintenancePlans
                    .SingleOrDefaultAsync(
                        p => p.TenantId == context.TenantId
                            && p.FacilityId == context.FacilityId
                            && p.Id == planId,
                        ct);

                if (plan is null)
                {
                    return MaintenanceWriteResult.Fail(MaintenanceFailureCodes.NotFound, "Preventive maintenance plan not found.");
                }

                if (plan.Status != PreventivePlanStatus.Active)
                {
                    return MaintenanceWriteResult.Fail(MaintenanceFailureCodes.InvalidTransition, "Only active plans can generate dues.");
                }

                var openDueExists = await dbContext.PreventiveMaintenanceDues
                    .AnyAsync(
                        d => d.TenantId == context.TenantId
                            && d.FacilityId == context.FacilityId
                            && d.PreventivePlanId == planId
                            && d.Status == PreventiveMaintenanceDueStatus.Due,
                        ct);

                if (openDueExists)
                {
                    return MaintenanceWriteResult.Fail(MaintenanceFailureCodes.DuplicateDue, "An open preventive due already exists for this plan.");
                }

                var now = DateTimeOffset.UtcNow;
                var dueAtUtc = plan.NextDueAtUtc ?? now;
                var due = new PreventiveMaintenanceDue
                {
                    Id = Guid.NewGuid(),
                    TenantId = context.TenantId,
                    FacilityId = context.FacilityId,
                    PreventivePlanId = plan.Id,
                    AssetType = plan.AssetType,
                    AssetReference = plan.AssetReference,
                    DueAtUtc = dueAtUtc,
                    Status = PreventiveMaintenanceDueStatus.Due,
                    GeneratedAtUtc = now,
                    Version = 1,
                };

                plan.NextDueAtUtc = MaintenanceDueDateCalculator.CalculateNextDueAtUtc(
                    plan.IntervalType,
                    plan.IntervalValue,
                    dueAtUtc);
                plan.UpdatedAtUtc = now;
                plan.Version++;

                dbContext.PreventiveMaintenanceDues.Add(due);
                MaintenanceRepositorySupport.AddAudit(
                    dbContext, context,
                    MaintenanceAuditActions.PreventiveDueGenerated,
                    MaintenanceAuditEventTypes.PreventiveDueGenerated,
                    "maintenance.preventivedue",
                    due.Id,
                    now,
                    new { dueId = due.Id, planId = plan.Id, dueAtUtc });
                MaintenanceRepositorySupport.AddOutbox(
                    dbContext, context,
                    "maintenance-preventive-due",
                    due.Id,
                    due.Version,
                    MaintenanceOutboxMessageTypes.PreventiveDueGenerated,
                    now,
                    new { dueId = due.Id.ToString(), planId = plan.Id.ToString(), dueAtUtc });

                await dbContext.SaveChangesAsync(ct);
                return MaintenanceWriteResult.Success(due.Id, due.Version);
            },
            cancellationToken);

    public Task<MaintenanceWriteResult> CreateWorkOrderFromDueAsync(
        MaintenanceOperationContext context,
        Guid dueId,
        uint expectedVersion,
        CancellationToken cancellationToken = default) =>
        MaintenanceRepositorySupport.ExecuteWriteAsync(
            tenantDatabaseSession,
            context,
            async ct =>
            {
                var due = await dbContext.PreventiveMaintenanceDues
                    .SingleOrDefaultAsync(
                        d => d.TenantId == context.TenantId
                            && d.FacilityId == context.FacilityId
                            && d.Id == dueId,
                        ct);

                if (due is null)
                {
                    return MaintenanceWriteResult.Fail(MaintenanceFailureCodes.NotFound, "Preventive maintenance due not found.");
                }

                if (due.Status == PreventiveMaintenanceDueStatus.WorkOrderCreated && due.WorkOrderId.HasValue)
                {
                    var existingVersion = await dbContext.MaintenanceWorkOrders.AsNoTracking()
                        .Where(w => w.Id == due.WorkOrderId.Value)
                        .Select(w => w.Version)
                        .SingleOrDefaultAsync(ct);

                    return MaintenanceWriteResult.Success(due.WorkOrderId.Value, existingVersion);
                }

                if (due.Version != expectedVersion)
                {
                    return MaintenanceRepositorySupport.ConcurrencyFail(due.Version, expectedVersion);
                }

                if (due.Status != PreventiveMaintenanceDueStatus.Due)
                {
                    return MaintenanceWriteResult.Fail(MaintenanceFailureCodes.InvalidTransition, "Only open dues can create work orders.");
                }

                var plan = await dbContext.PreventiveMaintenancePlans
                    .SingleAsync(
                        p => p.TenantId == context.TenantId
                            && p.FacilityId == context.FacilityId
                            && p.Id == due.PreventivePlanId,
                        ct);

                var now = DateTimeOffset.UtcNow;
                var workOrder = new MaintenanceWorkOrder
                {
                    Id = Guid.NewGuid(),
                    TenantId = context.TenantId,
                    FacilityId = context.FacilityId,
                    WorkOrderNumber = await MaintenanceRepositorySupport.NextWorkOrderNumberAsync(dbContext, context, ct),
                    AssetType = due.AssetType,
                    AssetId = plan.AssetId,
                    AssetReference = due.AssetReference,
                    AssetName = plan.Name,
                    PreventivePlanId = plan.Id,
                    PreventiveDueId = due.Id,
                    Type = MaintenanceWorkOrderType.Preventive,
                    Priority = MaintenancePriority.Normal,
                    Status = MaintenanceWorkOrderStatus.Planned,
                    Title = $"Periyodik bakım — {plan.Name}",
                    Description = plan.Description ?? $"Plan {plan.Code} periyodik bakım emri.",
                    PlannedStartUtc = due.DueAtUtc,
                    CreatedAtUtc = now,
                    UpdatedAtUtc = now,
                    CreatedBySubjectId = context.IdentitySubjectId.ToString(),
                    Version = 1,
                };

                due.Status = PreventiveMaintenanceDueStatus.WorkOrderCreated;
                due.WorkOrderId = workOrder.Id;
                due.Version++;

                dbContext.MaintenanceWorkOrders.Add(workOrder);
                MaintenanceRepositorySupport.AddAudit(
                    dbContext, context,
                    MaintenanceAuditActions.PreventiveDueWorkOrderCreated,
                    MaintenanceAuditEventTypes.PreventiveDueWorkOrderCreated,
                    "maintenance.preventivedue",
                    due.Id,
                    now,
                    new { dueId = due.Id, workOrderId = workOrder.Id });
                MaintenanceRepositorySupport.AddAudit(
                    dbContext, context,
                    MaintenanceAuditActions.WorkOrderCreated,
                    MaintenanceAuditEventTypes.WorkOrderCreated,
                    "maintenance.workorder",
                    workOrder.Id,
                    now,
                    new { workOrderId = workOrder.Id, dueId = due.Id, planId = plan.Id });
                MaintenanceRepositorySupport.AddOutbox(
                    dbContext, context,
                    "maintenance-work-order",
                    workOrder.Id,
                    workOrder.Version,
                    MaintenanceOutboxMessageTypes.PreventiveWorkOrderCreated,
                    now,
                    new { workOrderId = workOrder.Id.ToString(), dueId = due.Id.ToString() });

                await dbContext.SaveChangesAsync(ct);
                return MaintenanceWriteResult.Success(workOrder.Id, workOrder.Version);
            },
            cancellationToken);

    private async Task<IReadOnlyList<PreventiveMaintenanceDueSummaryReadModel>> MapSummariesAsync(
        List<PreventiveMaintenanceDue> dues,
        MaintenanceOperationContext context,
        CancellationToken ct)
    {
        if (dues.Count == 0)
        {
            return [];
        }

        var planIds = dues.Select(d => d.PreventivePlanId).Distinct().ToList();
        var plans = await dbContext.PreventiveMaintenancePlans.AsNoTracking()
            .Where(p => p.TenantId == context.TenantId && p.FacilityId == context.FacilityId && planIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, ct);

        var workOrderIds = dues.Where(d => d.WorkOrderId.HasValue).Select(d => d.WorkOrderId!.Value).Distinct().ToList();
        var workOrders = workOrderIds.Count == 0
            ? new Dictionary<Guid, string>()
            : await dbContext.MaintenanceWorkOrders.AsNoTracking()
                .Where(w => workOrderIds.Contains(w.Id))
                .ToDictionaryAsync(w => w.Id, w => w.WorkOrderNumber, ct);

        return dues.Select(d =>
        {
            plans.TryGetValue(d.PreventivePlanId, out var plan);
            string? workOrderNumber = null;
            if (d.WorkOrderId.HasValue)
            {
                workOrders.TryGetValue(d.WorkOrderId.Value, out workOrderNumber);
            }

            return new PreventiveMaintenanceDueSummaryReadModel(
                d.Id,
                d.PreventivePlanId,
                plan?.Code,
                plan?.Name,
                d.AssetType,
                d.AssetReference,
                d.DueAtUtc,
                MaintenanceValueConversions.ToStorage(d.Status),
                d.GeneratedAtUtc,
                d.WorkOrderId,
                workOrderNumber,
                d.CompletedAtUtc,
                d.Version);
        }).ToList();
    }
}

public sealed class MaintenancePartUsageRepository(
    NevaxisFactOSDbContext dbContext,
    TenantDatabaseSession tenantDatabaseSession) : IMaintenancePartUsageRepository
{
    public Task<IReadOnlyList<MaintenancePartUsageReadModel>> ListByWorkOrderAsync(
        MaintenanceOperationContext context,
        Guid workOrderId,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(async ct =>
        {
            var items = await dbContext.MaintenancePartUsages.AsNoTracking()
                .Where(p => p.TenantId == context.TenantId
                    && p.FacilityId == context.FacilityId
                    && p.WorkOrderId == workOrderId)
                .OrderByDescending(p => p.RecordedAtUtc)
                .ToListAsync(ct);

            return (IReadOnlyList<MaintenancePartUsageReadModel>)items.Select(MapUsage).ToList();
        }, cancellationToken);

    public Task<MaintenanceWriteResult> CreateAsync(
        MaintenanceOperationContext context,
        CreateMaintenancePartUsageCommand command,
        CancellationToken cancellationToken = default) =>
        MaintenanceRepositorySupport.ExecuteWriteAsync(
            tenantDatabaseSession,
            context,
            async ct =>
            {
                if (string.IsNullOrWhiteSpace(command.PartReference))
                {
                    return MaintenanceWriteResult.Fail(MaintenanceFailureCodes.ValidationFailed, "Part reference is required.");
                }

                if (command.Quantity <= 0)
                {
                    return MaintenanceWriteResult.Fail(MaintenanceFailureCodes.ValidationFailed, "Quantity must be greater than zero.");
                }

                if (string.IsNullOrWhiteSpace(command.Unit))
                {
                    return MaintenanceWriteResult.Fail(MaintenanceFailureCodes.ValidationFailed, "Unit is required.");
                }

                var workOrder = await dbContext.MaintenanceWorkOrders
                    .SingleOrDefaultAsync(
                        w => w.TenantId == context.TenantId
                            && w.FacilityId == context.FacilityId
                            && w.Id == command.WorkOrderId,
                        ct);

                if (workOrder is null)
                {
                    return MaintenanceWriteResult.Fail(MaintenanceFailureCodes.NotFound, "Maintenance work order not found.");
                }

                if (workOrder.Status == MaintenanceWorkOrderStatus.Completed)
                {
                    return MaintenanceWriteResult.Fail(MaintenanceFailureCodes.WorkOrderClosed, "Cannot record part usage on a completed work order.");
                }

                var now = DateTimeOffset.UtcNow;
                var usage = new MaintenancePartUsage
                {
                    Id = Guid.NewGuid(),
                    TenantId = context.TenantId,
                    FacilityId = context.FacilityId,
                    WorkOrderId = command.WorkOrderId,
                    PartReference = command.PartReference.Trim(),
                    PartName = command.PartName?.Trim(),
                    Quantity = command.Quantity,
                    Unit = command.Unit.Trim(),
                    SourceSystem = command.SourceSystem?.Trim(),
                    SourceReference = command.SourceReference?.Trim(),
                    EstimatedUnitCost = command.EstimatedUnitCost,
                    ActualUnitCost = command.ActualUnitCost,
                    RecordedAtUtc = now,
                    RecordedBySubjectId = context.IdentitySubjectId.ToString(),
                };

                dbContext.MaintenancePartUsages.Add(usage);
                MaintenanceRepositorySupport.AddAudit(
                    dbContext, context,
                    MaintenanceAuditActions.PartUsageRecorded,
                    MaintenanceAuditEventTypes.PartUsageRecorded,
                    "maintenance.partusage",
                    usage.Id,
                    now,
                    new { usageId = usage.Id, workOrderId = command.WorkOrderId, partReference = usage.PartReference });
                MaintenanceRepositorySupport.AddOutbox(
                    dbContext, context,
                    "maintenance-part-usage",
                    usage.Id,
                    1,
                    MaintenanceOutboxMessageTypes.PartUsageRecorded,
                    now,
                    new { usageId = usage.Id.ToString(), workOrderId = command.WorkOrderId.ToString() });

                await dbContext.SaveChangesAsync(ct);
                return MaintenanceWriteResult.Success(usage.Id, 1);
            },
            cancellationToken);

    internal static MaintenancePartUsageReadModel MapUsage(MaintenancePartUsage usage) =>
        new(
            usage.Id,
            usage.WorkOrderId,
            usage.PartReference,
            usage.PartName,
            usage.Quantity,
            usage.Unit,
            usage.SourceSystem,
            usage.SourceReference,
            usage.EstimatedUnitCost,
            usage.ActualUnitCost,
            usage.EstimatedUnitCost.HasValue ? usage.EstimatedUnitCost * usage.Quantity : null,
            usage.ActualUnitCost.HasValue ? usage.ActualUnitCost * usage.Quantity : null,
            usage.RecordedAtUtc,
            usage.RecordedBySubjectId);
}

public sealed class MaintenanceCostSummaryRepository(
    NevaxisFactOSDbContext dbContext,
    TenantDatabaseSession tenantDatabaseSession) : IMaintenanceCostSummaryRepository
{
    public Task<MaintenanceCostSummaryReadModel?> GetByWorkOrderAsync(
        MaintenanceOperationContext context,
        Guid workOrderId,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(async ct =>
        {
            var workOrder = await dbContext.MaintenanceWorkOrders.AsNoTracking()
                .SingleOrDefaultAsync(
                    w => w.TenantId == context.TenantId
                        && w.FacilityId == context.FacilityId
                        && w.Id == workOrderId,
                    ct);

            if (workOrder is null)
            {
                return null;
            }

            var usages = await dbContext.MaintenancePartUsages.AsNoTracking()
                .Where(p => p.TenantId == context.TenantId
                    && p.FacilityId == context.FacilityId
                    && p.WorkOrderId == workOrderId)
                .ToListAsync(ct);

            decimal? estimatedParts = usages.Any(u => u.EstimatedUnitCost.HasValue)
                ? usages.Where(u => u.EstimatedUnitCost.HasValue)
                    .Sum(u => u.EstimatedUnitCost!.Value * u.Quantity)
                : null;

            decimal? actualParts = usages.Any(u => u.ActualUnitCost.HasValue)
                ? usages.Where(u => u.ActualUnitCost.HasValue)
                    .Sum(u => u.ActualUnitCost!.Value * u.Quantity)
                : null;

            var external = workOrder.ExternalServiceCost;
            var other = workOrder.OtherCost;
            decimal? total = null;
            if (actualParts.HasValue || estimatedParts.HasValue || external.HasValue || other.HasValue)
            {
                total = (actualParts ?? estimatedParts ?? 0m) + (external ?? 0m) + (other ?? 0m);
            }

            return new MaintenanceCostSummaryReadModel(
                workOrderId,
                estimatedParts,
                actualParts,
                external,
                other,
                total);
        }, cancellationToken);
}

public sealed class MaintenanceAssetHistoryRepository(
    NevaxisFactOSDbContext dbContext,
    TenantDatabaseSession tenantDatabaseSession) : IMaintenanceAssetHistoryRepository
{
    public Task<MaintenanceAssetHistoryReadModel?> GetByAssetReferenceAsync(
        MaintenanceOperationContext context,
        string assetReference,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(async ct =>
        {
            if (string.IsNullOrWhiteSpace(assetReference))
            {
                return null;
            }

            var normalized = assetReference.Trim();
            var requests = await dbContext.MaintenanceRequests.AsNoTracking()
                .Where(r => r.TenantId == context.TenantId
                    && r.FacilityId == context.FacilityId
                    && r.AssetReference == normalized)
                .OrderByDescending(r => r.ReportedAtUtc)
                .ToListAsync(ct);

            var workOrders = await dbContext.MaintenanceWorkOrders.AsNoTracking()
                .Where(w => w.TenantId == context.TenantId
                    && w.FacilityId == context.FacilityId
                    && w.AssetReference == normalized)
                .OrderByDescending(w => w.CreatedAtUtc)
                .ToListAsync(ct);

            if (requests.Count == 0 && workOrders.Count == 0)
            {
                var planOnly = await dbContext.PreventiveMaintenancePlans.AsNoTracking()
                    .AnyAsync(
                        p => p.TenantId == context.TenantId
                            && p.FacilityId == context.FacilityId
                            && p.AssetReference == normalized,
                        ct);

                if (!planOnly)
                {
                    return null;
                }
            }

            var plans = await dbContext.PreventiveMaintenancePlans.AsNoTracking()
                .Where(p => p.TenantId == context.TenantId
                    && p.FacilityId == context.FacilityId
                    && p.AssetReference == normalized)
                .OrderBy(p => p.Code)
                .ToListAsync(ct);

            var dues = await dbContext.PreventiveMaintenanceDues.AsNoTracking()
                .Where(d => d.TenantId == context.TenantId
                    && d.FacilityId == context.FacilityId
                    && d.AssetReference == normalized)
                .OrderByDescending(d => d.DueAtUtc)
                .ToListAsync(ct);

            var workOrderIds = workOrders.Select(w => w.Id).ToList();
            var notes = workOrderIds.Count == 0
                ? []
                : await dbContext.MaintenanceWorkNotes.AsNoTracking()
                    .Where(n => n.TenantId == context.TenantId
                        && n.FacilityId == context.FacilityId
                        && workOrderIds.Contains(n.WorkOrderId))
                    .OrderByDescending(n => n.CreatedAtUtc)
                    .ToListAsync(ct);

            var partUsages = workOrderIds.Count == 0
                ? []
                : await dbContext.MaintenancePartUsages.AsNoTracking()
                    .Where(p => p.TenantId == context.TenantId
                        && p.FacilityId == context.FacilityId
                        && workOrderIds.Contains(p.WorkOrderId))
                    .OrderByDescending(p => p.RecordedAtUtc)
                    .ToListAsync(ct);

            var requestSummaries = requests.Select(r => new MaintenanceRequestSummaryReadModel(
                r.Id,
                r.RequestNumber,
                r.AssetType,
                r.AssetId,
                r.AssetReference,
                r.AssetName,
                r.Title,
                MaintenanceValueConversions.ToStorage(r.Type),
                MaintenanceValueConversions.ToStorage(r.Priority),
                MaintenanceValueConversions.ToStorage(r.Status),
                r.ReportedBySubjectId,
                r.ReportedAtUtc,
                r.Version,
                r.UpdatedAtUtc)).ToList();

            var requestNumbers = requests.ToDictionary(r => r.Id, r => r.RequestNumber);
            var workOrderSummaries = workOrders.Select(w => new MaintenanceWorkOrderSummaryReadModel(
                w.Id,
                w.WorkOrderNumber,
                w.MaintenanceRequestId,
                w.MaintenanceRequestId.HasValue && requestNumbers.TryGetValue(w.MaintenanceRequestId.Value, out var num) ? num : null,
                w.AssetType,
                w.AssetId,
                w.AssetReference,
                w.AssetName,
                MaintenanceValueConversions.ToStorage(w.Type),
                MaintenanceValueConversions.ToStorage(w.Priority),
                MaintenanceValueConversions.ToStorage(w.Status),
                w.AssignedTechnicianSubjectId,
                w.PlannedStartUtc,
                w.PlannedEndUtc,
                w.StartedAtUtc,
                w.Version,
                w.UpdatedAtUtc)).ToList();

            var planSummaries = plans.Select(p => new PreventiveMaintenancePlanSummaryReadModel(
                p.Id,
                p.Code,
                p.Name,
                p.AssetType,
                p.AssetId,
                p.AssetReference,
                MaintenanceValueConversions.ToStorage(p.IntervalType),
                p.IntervalValue,
                p.NextDueAtUtc,
                MaintenanceValueConversions.ToStorage(p.Status),
                p.Version,
                p.UpdatedAtUtc)).ToList();

            var planLookup = plans.ToDictionary(p => p.Id);
            var workOrderNumberLookup = workOrders.ToDictionary(w => w.Id, w => w.WorkOrderNumber);
            var dueSummaries = dues.Select(d =>
            {
                planLookup.TryGetValue(d.PreventivePlanId, out var plan);
                string? woNum = null;
                if (d.WorkOrderId.HasValue)
                {
                    workOrderNumberLookup.TryGetValue(d.WorkOrderId.Value, out woNum);
                }

                return new PreventiveMaintenanceDueSummaryReadModel(
                    d.Id,
                    d.PreventivePlanId,
                    plan?.Code,
                    plan?.Name,
                    d.AssetType,
                    d.AssetReference,
                    d.DueAtUtc,
                    MaintenanceValueConversions.ToStorage(d.Status),
                    d.GeneratedAtUtc,
                    d.WorkOrderId,
                    woNum,
                    d.CompletedAtUtc,
                    d.Version);
            }).ToList();

            var noteModels = notes.Select(MaintenanceWorkNoteRepository.MapNote).ToList();
            var usageModels = partUsages.Select(MaintenancePartUsageRepository.MapUsage).ToList();

            var costSummaries = workOrders.Select(w =>
            {
                var woUsages = partUsages.Where(p => p.WorkOrderId == w.Id).ToList();
                decimal? estimatedParts = woUsages.Any(u => u.EstimatedUnitCost.HasValue)
                    ? woUsages.Where(u => u.EstimatedUnitCost.HasValue).Sum(u => u.EstimatedUnitCost!.Value * u.Quantity)
                    : null;
                decimal? actualParts = woUsages.Any(u => u.ActualUnitCost.HasValue)
                    ? woUsages.Where(u => u.ActualUnitCost.HasValue).Sum(u => u.ActualUnitCost!.Value * u.Quantity)
                    : null;
                decimal? total = null;
                if (actualParts.HasValue || estimatedParts.HasValue || w.ExternalServiceCost.HasValue || w.OtherCost.HasValue)
                {
                    total = (actualParts ?? estimatedParts ?? 0m) + (w.ExternalServiceCost ?? 0m) + (w.OtherCost ?? 0m);
                }

                return new MaintenanceCostSummaryReadModel(
                    w.Id,
                    estimatedParts,
                    actualParts,
                    w.ExternalServiceCost,
                    w.OtherCost,
                    total);
            }).ToList();

            return new MaintenanceAssetHistoryReadModel(
                normalized,
                requestSummaries,
                workOrderSummaries,
                planSummaries,
                dueSummaries,
                noteModels,
                usageModels,
                costSummaries);
        }, cancellationToken);
}

public sealed class UnavailableMaintenanceWorkNoteRepository : IMaintenanceWorkNoteRepository
{
    private static MaintenanceWriteResult Unavailable() =>
        MaintenanceWriteResult.Fail(MaintenanceFailureCodes.SourceUnavailable, "Maintenance work note service is unavailable.");

    public Task<IReadOnlyList<MaintenanceWorkNoteReadModel>> ListByWorkOrderAsync(
        MaintenanceOperationContext context,
        Guid workOrderId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<MaintenanceWorkNoteReadModel>>([]);

    public Task<MaintenanceWriteResult> CreateAsync(
        MaintenanceOperationContext context,
        CreateMaintenanceWorkNoteCommand command,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Unavailable());
}

public sealed class UnavailableMaintenanceTimelineRepository : IMaintenanceTimelineRepository
{
    public Task<IReadOnlyList<MaintenanceTimelineEntryReadModel>> GetWorkOrderTimelineAsync(
        MaintenanceOperationContext context,
        Guid workOrderId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<MaintenanceTimelineEntryReadModel>>([]);
}

public sealed class UnavailablePreventiveMaintenanceDueRepository : IPreventiveMaintenanceDueRepository
{
    private static MaintenanceWriteResult Unavailable() =>
        MaintenanceWriteResult.Fail(MaintenanceFailureCodes.SourceUnavailable, "Preventive maintenance due service is unavailable.");

    public Task<IReadOnlyList<PreventiveMaintenanceDueSummaryReadModel>> ListAsync(
        MaintenanceOperationContext context,
        string? status,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<PreventiveMaintenanceDueSummaryReadModel>>([]);

    public Task<PreventiveMaintenanceDueDetailReadModel?> GetByIdAsync(
        MaintenanceOperationContext context,
        Guid dueId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<PreventiveMaintenanceDueDetailReadModel?>(null);

    public Task<MaintenanceWriteResult> GenerateNextDueAsync(
        MaintenanceOperationContext context,
        Guid planId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Unavailable());

    public Task<MaintenanceWriteResult> CreateWorkOrderFromDueAsync(
        MaintenanceOperationContext context,
        Guid dueId,
        uint expectedVersion,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Unavailable());
}

public sealed class UnavailableMaintenancePartUsageRepository : IMaintenancePartUsageRepository
{
    private static MaintenanceWriteResult Unavailable() =>
        MaintenanceWriteResult.Fail(MaintenanceFailureCodes.SourceUnavailable, "Maintenance part usage service is unavailable.");

    public Task<IReadOnlyList<MaintenancePartUsageReadModel>> ListByWorkOrderAsync(
        MaintenanceOperationContext context,
        Guid workOrderId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<MaintenancePartUsageReadModel>>([]);

    public Task<MaintenanceWriteResult> CreateAsync(
        MaintenanceOperationContext context,
        CreateMaintenancePartUsageCommand command,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Unavailable());
}

public sealed class UnavailableMaintenanceCostSummaryRepository : IMaintenanceCostSummaryRepository
{
    public Task<MaintenanceCostSummaryReadModel?> GetByWorkOrderAsync(
        MaintenanceOperationContext context,
        Guid workOrderId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<MaintenanceCostSummaryReadModel?>(null);
}

public sealed class UnavailableMaintenanceAssetHistoryRepository : IMaintenanceAssetHistoryRepository
{
    public Task<MaintenanceAssetHistoryReadModel?> GetByAssetReferenceAsync(
        MaintenanceOperationContext context,
        string assetReference,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<MaintenanceAssetHistoryReadModel?>(null);
}
