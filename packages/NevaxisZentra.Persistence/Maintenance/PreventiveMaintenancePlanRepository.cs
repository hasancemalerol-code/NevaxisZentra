using NevaxisZentra.Persistence.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace NevaxisZentra.Persistence.Maintenance;

public interface IPreventiveMaintenancePlanRepository
{
    Task<IReadOnlyList<PreventiveMaintenancePlanSummaryReadModel>> ListAsync(
        MaintenanceOperationContext context,
        CancellationToken cancellationToken = default);

    Task<PreventiveMaintenancePlanDetailReadModel?> GetByIdAsync(
        MaintenanceOperationContext context,
        Guid planId,
        CancellationToken cancellationToken = default);

    Task<MaintenanceWriteResult> CreateAsync(
        MaintenanceOperationContext context,
        CreatePreventiveMaintenancePlanCommand command,
        CancellationToken cancellationToken = default);

    Task<MaintenanceWriteResult> UpdateAsync(
        MaintenanceOperationContext context,
        UpdatePreventiveMaintenancePlanCommand command,
        CancellationToken cancellationToken = default);

    Task<MaintenanceWriteResult> CreateWorkOrderFromPlanAsync(
        MaintenanceOperationContext context,
        Guid planId,
        uint expectedVersion,
        CancellationToken cancellationToken = default);
}

public sealed class PreventiveMaintenancePlanRepository(
    NevaxisZentraDbContext dbContext,
    TenantDatabaseSession tenantDatabaseSession) : IPreventiveMaintenancePlanRepository
{
    public Task<IReadOnlyList<PreventiveMaintenancePlanSummaryReadModel>> ListAsync(
        MaintenanceOperationContext context,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(async ct =>
        {
            var items = await dbContext.PreventiveMaintenancePlans.AsNoTracking()
                .Where(p => p.TenantId == context.TenantId && p.FacilityId == context.FacilityId)
                .OrderBy(p => p.NextDueAtUtc)
                .ToListAsync(ct);

            return (IReadOnlyList<PreventiveMaintenancePlanSummaryReadModel>)items.Select(MapSummary).ToList();
        }, cancellationToken);

    public Task<PreventiveMaintenancePlanDetailReadModel?> GetByIdAsync(
        MaintenanceOperationContext context,
        Guid planId,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(async ct =>
        {
            var plan = await dbContext.PreventiveMaintenancePlans.AsNoTracking()
                .SingleOrDefaultAsync(
                    p => p.TenantId == context.TenantId
                        && p.FacilityId == context.FacilityId
                        && p.Id == planId,
                    ct);

            return plan is null ? null : MapDetail(plan);
        }, cancellationToken);

    public Task<MaintenanceWriteResult> CreateAsync(
        MaintenanceOperationContext context,
        CreatePreventiveMaintenancePlanCommand command,
        CancellationToken cancellationToken = default) =>
        MaintenanceRepositorySupport.ExecuteWriteAsync(
            tenantDatabaseSession,
            context,
            async ct =>
            {
                if (string.IsNullOrWhiteSpace(command.Code) || string.IsNullOrWhiteSpace(command.Name))
                {
                    return MaintenanceWriteResult.Fail(MaintenanceFailureCodes.ValidationFailed, "Code and name are required.");
                }

                if (command.IntervalValue <= 0)
                {
                    return MaintenanceWriteResult.Fail(MaintenanceFailureCodes.ValidationFailed, "Interval value must be positive.");
                }

                var now = DateTimeOffset.UtcNow;
                var plan = new PreventiveMaintenancePlan
                {
                    Id = Guid.NewGuid(),
                    TenantId = context.TenantId,
                    FacilityId = context.FacilityId,
                    Code = command.Code.Trim(),
                    Name = command.Name.Trim(),
                    AssetType = command.AssetType,
                    AssetId = command.AssetId,
                    AssetReference = command.AssetReference,
                    IntervalType = MaintenanceValueConversions.IntervalTypeFromStorage(command.IntervalType),
                    IntervalValue = command.IntervalValue,
                    NextDueAtUtc = command.NextDueAtUtc,
                    Status = MaintenanceValueConversions.PlanStatusFromStorage(command.Status),
                    Description = command.Description,
                    CreatedAtUtc = now,
                    UpdatedAtUtc = now,
                    Version = 1,
                };

                dbContext.PreventiveMaintenancePlans.Add(plan);
                MaintenanceRepositorySupport.AddAudit(
                    dbContext, context,
                    MaintenanceAuditActions.PreventivePlanCreated,
                    MaintenanceAuditEventTypes.PreventivePlanCreated,
                    "maintenance.preventiveplan",
                    plan.Id,
                    now,
                    new { planId = plan.Id, plan.Code });
                MaintenanceRepositorySupport.AddOutbox(
                    dbContext, context,
                    "maintenance-preventive-plan",
                    plan.Id,
                    plan.Version,
                    MaintenanceOutboxMessageTypes.PreventiveDueChanged,
                    now,
                    new { planId = plan.Id.ToString(), nextDueAtUtc = plan.NextDueAtUtc });

                await dbContext.SaveChangesAsync(ct);
                return MaintenanceWriteResult.Success(plan.Id, plan.Version);
            },
            cancellationToken);

    public Task<MaintenanceWriteResult> UpdateAsync(
        MaintenanceOperationContext context,
        UpdatePreventiveMaintenancePlanCommand command,
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
                            && p.Id == command.PlanId,
                        ct);

                if (plan is null)
                {
                    return MaintenanceWriteResult.Fail(MaintenanceFailureCodes.NotFound, "Preventive maintenance plan not found.");
                }

                if (plan.Version != command.ExpectedVersion)
                {
                    return MaintenanceRepositorySupport.ConcurrencyFail(plan.Version, command.ExpectedVersion);
                }

                if (command.IntervalValue <= 0)
                {
                    return MaintenanceWriteResult.Fail(MaintenanceFailureCodes.ValidationFailed, "Interval value must be positive.");
                }

                var previousDue = plan.NextDueAtUtc;
                plan.Code = command.Code.Trim();
                plan.Name = command.Name.Trim();
                plan.AssetType = command.AssetType;
                plan.AssetId = command.AssetId;
                plan.AssetReference = command.AssetReference;
                plan.IntervalType = MaintenanceValueConversions.IntervalTypeFromStorage(command.IntervalType);
                plan.IntervalValue = command.IntervalValue;
                plan.NextDueAtUtc = command.NextDueAtUtc;
                plan.Status = MaintenanceValueConversions.PlanStatusFromStorage(command.Status);
                plan.Description = command.Description;

                var now = DateTimeOffset.UtcNow;
                plan.UpdatedAtUtc = now;
                plan.Version++;

                MaintenanceRepositorySupport.AddAudit(
                    dbContext, context,
                    MaintenanceAuditActions.PreventivePlanUpdated,
                    MaintenanceAuditEventTypes.PreventivePlanUpdated,
                    "maintenance.preventiveplan",
                    plan.Id,
                    now,
                    new { planId = plan.Id, status = MaintenanceValueConversions.ToStorage(plan.Status) });

                if (previousDue != plan.NextDueAtUtc)
                {
                    MaintenanceRepositorySupport.AddOutbox(
                        dbContext, context,
                        "maintenance-preventive-plan",
                        plan.Id,
                        plan.Version,
                        MaintenanceOutboxMessageTypes.PreventiveDueChanged,
                        now,
                        new { planId = plan.Id.ToString(), nextDueAtUtc = plan.NextDueAtUtc });
                }

                await dbContext.SaveChangesAsync(ct);
                return MaintenanceWriteResult.Success(plan.Id, plan.Version);
            },
            cancellationToken);

    public Task<MaintenanceWriteResult> CreateWorkOrderFromPlanAsync(
        MaintenanceOperationContext context,
        Guid planId,
        uint expectedVersion,
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

                if (plan.Version != expectedVersion)
                {
                    return MaintenanceRepositorySupport.ConcurrencyFail(plan.Version, expectedVersion);
                }

                if (plan.Status != PreventivePlanStatus.Active)
                {
                    return MaintenanceWriteResult.Fail(MaintenanceFailureCodes.InvalidTransition, "Only active plans can generate work orders.");
                }

                var now = DateTimeOffset.UtcNow;
                var workOrder = new MaintenanceWorkOrder
                {
                    Id = Guid.NewGuid(),
                    TenantId = context.TenantId,
                    FacilityId = context.FacilityId,
                    WorkOrderNumber = await MaintenanceRepositorySupport.NextWorkOrderNumberAsync(dbContext, context, ct),
                    AssetType = plan.AssetType,
                    AssetId = plan.AssetId,
                    AssetReference = plan.AssetReference,
                    Type = MaintenanceWorkOrderType.Preventive,
                    Priority = MaintenancePriority.Normal,
                    Status = MaintenanceWorkOrderStatus.Planned,
                    Title = $"Periyodik bakım — {plan.Name}",
                    Description = plan.Description ?? $"Plan {plan.Code} için periyodik bakım emri.",
                    PlannedStartUtc = plan.NextDueAtUtc,
                    CreatedAtUtc = now,
                    UpdatedAtUtc = now,
                    CreatedBySubjectId = context.IdentitySubjectId.ToString(),
                    Version = 1,
                };

                dbContext.MaintenanceWorkOrders.Add(workOrder);
                MaintenanceRepositorySupport.AddAudit(
                    dbContext, context,
                    MaintenanceAuditActions.WorkOrderCreated,
                    MaintenanceAuditEventTypes.WorkOrderCreated,
                    "maintenance.workorder",
                    workOrder.Id,
                    now,
                    new { workOrderId = workOrder.Id, planId = plan.Id });
                MaintenanceRepositorySupport.AddOutbox(
                    dbContext, context,
                    "maintenance-work-order",
                    workOrder.Id,
                    workOrder.Version,
                    MaintenanceOutboxMessageTypes.WorkOrderCreated,
                    now,
                    new { workOrderId = workOrder.Id.ToString(), workOrderNumber = workOrder.WorkOrderNumber });

                await dbContext.SaveChangesAsync(ct);
                return MaintenanceWriteResult.Success(workOrder.Id, workOrder.Version);
            },
            cancellationToken);

    private static PreventiveMaintenancePlanSummaryReadModel MapSummary(PreventiveMaintenancePlan plan) =>
        new(
            plan.Id,
            plan.Code,
            plan.Name,
            plan.AssetType,
            plan.AssetId,
            plan.AssetReference,
            MaintenanceValueConversions.ToStorage(plan.IntervalType),
            plan.IntervalValue,
            plan.NextDueAtUtc,
            MaintenanceValueConversions.ToStorage(plan.Status),
            plan.Version,
            plan.UpdatedAtUtc);

    private static PreventiveMaintenancePlanDetailReadModel MapDetail(PreventiveMaintenancePlan plan) =>
        new(
            plan.Id,
            plan.Code,
            plan.Name,
            plan.AssetType,
            plan.AssetId,
            plan.AssetReference,
            MaintenanceValueConversions.ToStorage(plan.IntervalType),
            plan.IntervalValue,
            plan.NextDueAtUtc,
            MaintenanceValueConversions.ToStorage(plan.Status),
            plan.Description,
            plan.Version,
            plan.CreatedAtUtc,
            plan.UpdatedAtUtc);
}

public sealed class UnavailablePreventiveMaintenancePlanRepository : IPreventiveMaintenancePlanRepository
{
    private static MaintenanceWriteResult Unavailable() =>
        MaintenanceWriteResult.Fail(MaintenanceFailureCodes.SourceUnavailable, "Preventive maintenance plan service is unavailable.");

    public Task<IReadOnlyList<PreventiveMaintenancePlanSummaryReadModel>> ListAsync(
        MaintenanceOperationContext context,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<PreventiveMaintenancePlanSummaryReadModel>>([]);

    public Task<PreventiveMaintenancePlanDetailReadModel?> GetByIdAsync(
        MaintenanceOperationContext context,
        Guid planId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<PreventiveMaintenancePlanDetailReadModel?>(null);

    public Task<MaintenanceWriteResult> CreateAsync(
        MaintenanceOperationContext context,
        CreatePreventiveMaintenancePlanCommand command,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Unavailable());

    public Task<MaintenanceWriteResult> UpdateAsync(
        MaintenanceOperationContext context,
        UpdatePreventiveMaintenancePlanCommand command,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Unavailable());

    public Task<MaintenanceWriteResult> CreateWorkOrderFromPlanAsync(
        MaintenanceOperationContext context,
        Guid planId,
        uint expectedVersion,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Unavailable());
}
