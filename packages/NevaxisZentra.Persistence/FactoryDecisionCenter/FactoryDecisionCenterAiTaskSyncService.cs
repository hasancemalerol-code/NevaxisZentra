using NevaxisZentra.Persistence.AiTasks;
using NevaxisZentra.Persistence.Development;
using NevaxisZentra.Persistence.DigitalWorkforce;
using Microsoft.EntityFrameworkCore;

namespace NevaxisZentra.Persistence.FactoryDecisionCenter;

public interface IFactoryDecisionCenterAiTaskSyncService
{
    Task SyncAfterApprovalDecisionAsync(
        DigitalWorkCase rootCase,
        bool approved,
        string actorSubjectId,
        CancellationToken cancellationToken);

    Task SyncAfterExecutionProgressAsync(
        DigitalWorkCase rootCase,
        IReadOnlyList<DigitalWorkCase> childCases,
        string actorSubjectId,
        CancellationToken cancellationToken);

    Task SyncAfterOutcomeVerifiedAsync(
        DigitalWorkCase rootCase,
        string actorSubjectId,
        CancellationToken cancellationToken);

    Task ResetLinkedTasksAsync(
        Guid tenantId,
        Guid facilityId,
        string actorSubjectId,
        DateTimeOffset seedTime,
        CancellationToken cancellationToken);
}

public sealed class FactoryDecisionCenterAiTaskSyncService : IFactoryDecisionCenterAiTaskSyncService
{
    private readonly IAiTaskRepository aiTaskRepository;
    private readonly IAiTaskClosureRepository aiTaskClosureRepository;
    private readonly NevaxisZentraDbContext dbContext;

    public FactoryDecisionCenterAiTaskSyncService(
        IAiTaskRepository aiTaskRepository,
        IAiTaskClosureRepository aiTaskClosureRepository,
        NevaxisZentraDbContext dbContext)
    {
        this.aiTaskRepository = aiTaskRepository;
        this.aiTaskClosureRepository = aiTaskClosureRepository;
        this.dbContext = dbContext;
    }

    public async Task SyncAfterApprovalDecisionAsync(
        DigitalWorkCase rootCase,
        bool approved,
        string actorSubjectId,
        CancellationToken cancellationToken)
    {
        if (!ShouldSync(rootCase))
        {
            return;
        }

        var context = BuildOperationContext(rootCase, actorSubjectId);
        await EnsureTaskStatusAsync(
            context,
            DevelopmentPilotFactoryDecisionCenterIds.AiTaskApproveRecoveryPlanId,
            approved ? AiTaskStatuses.InProgress : AiTaskStatuses.Rejected,
            approved
                ? "Kritik sipariş kurtarma planı onaylandı — yürütme başlatıldı."
                : "Kritik sipariş kurtarma planı reddedildi.",
            cancellationToken);

        if (approved)
        {
            await EnsureTaskStatusAsync(
                context,
                DevelopmentPilotFactoryDecisionCenterIds.AiTaskProductionRecoveryId,
                AiTaskStatuses.InProgress,
                "INJ-001 üretim performansı geri kazanılıyor.",
                cancellationToken);
        }
    }

    public async Task SyncAfterExecutionProgressAsync(
        DigitalWorkCase rootCase,
        IReadOnlyList<DigitalWorkCase> childCases,
        string actorSubjectId,
        CancellationToken cancellationToken)
    {
        if (!ShouldSync(rootCase))
        {
            return;
        }

        var context = BuildOperationContext(rootCase, actorSubjectId);
        var productionDone = childCases.Any(c =>
            c.PrimaryWorkerId == DevelopmentPilotDigitalWorkforceIds.WorkerProductionRecovery &&
            c.Status == DigitalWorkCaseStatuses.Closed);
        var materialDone = childCases.Any(c =>
            c.PrimaryWorkerId == DevelopmentPilotDigitalWorkforceIds.WorkerMaterialShortage &&
            c.Status == DigitalWorkCaseStatuses.Closed);

        if (productionDone)
        {
            await EnsureTaskStatusAsync(
                context,
                DevelopmentPilotFactoryDecisionCenterIds.AiTaskProductionRecoveryId,
                AiTaskStatuses.Completed,
                "INJ-001 üretim performansı geri kazanıldı.",
                cancellationToken);
        }

        if (materialDone)
        {
            await EnsureTaskStatusAsync(
                context,
                DevelopmentPilotFactoryDecisionCenterIds.AiTaskMaterialReservationId,
                AiTaskStatuses.Completed,
                "ORD-2026-0915-047 malzeme rezervasyonu güvenceye alındı.",
                cancellationToken);
        }
    }

    public async Task SyncAfterOutcomeVerifiedAsync(
        DigitalWorkCase rootCase,
        string actorSubjectId,
        CancellationToken cancellationToken)
    {
        if (!ShouldSync(rootCase))
        {
            return;
        }

        var context = BuildOperationContext(rootCase, actorSubjectId);
        foreach (var taskId in DevelopmentPilotFactoryDecisionCenterIds.LinkedAiTaskIds)
        {
            await EnsureTaskStatusAsync(
                context,
                taskId,
                AiTaskStatuses.Completed,
                "Kritik sipariş kurtarma sonucu doğrulandı.",
                cancellationToken);
        }

        await EnsureTaskStatusAsync(
            context,
            DevelopmentPilotFactoryDecisionCenterIds.AiTaskDeliveryProjectionId,
            AiTaskStatuses.Completed,
            "Teslimat projeksiyonu yeniden doğrulandı.",
            cancellationToken);
    }

    public async Task ResetLinkedTasksAsync(
        Guid tenantId,
        Guid facilityId,
        string actorSubjectId,
        DateTimeOffset seedTime,
        CancellationToken cancellationToken)
    {
        var linkedTaskIds = DevelopmentPilotFactoryDecisionCenterIds.LinkedAiTaskIds.ToList();
        var evidence = await dbContext.AiTaskEvidence
            .Where(item => linkedTaskIds.Contains(item.TaskId))
            .ToListAsync(cancellationToken);
        if (evidence.Count > 0)
        {
            dbContext.AiTaskEvidence.RemoveRange(evidence);
        }

        var tasks = await dbContext.AiTasks
            .Where(task => linkedTaskIds.Contains(task.Id))
            .ToListAsync(cancellationToken);

        foreach (var task in tasks)
        {
            ApplyInitialTaskState(task, actorSubjectId, seedTime);
        }

        if (tasks.Count > 0 || evidence.Count > 0)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    private static bool ShouldSync(DigitalWorkCase rootCase) =>
        rootCase.Id == DevelopmentPilotFactoryDecisionCenterIds.RootCaseId ||
        string.Equals(rootCase.Source, FactoryDecisionCenterConstants.SourceModule, StringComparison.OrdinalIgnoreCase);

    private static AiTaskOperationContext BuildOperationContext(DigitalWorkCase rootCase, string actorSubjectId)
    {
        var membershipId = Guid.TryParse(actorSubjectId, out var parsedMembership)
            ? parsedMembership
            : DevelopmentPilotIds.TenantMembershipId;

        return new AiTaskOperationContext(
            rootCase.TenantId,
            rootCase.FacilityId ?? DevelopmentPilotIds.FacilityId,
            membershipId,
            DevelopmentPilotIds.IdentitySubjectId,
            AiTaskPermissionKeys.Review,
            null);
    }

    private async Task EnsureTaskStatusAsync(
        AiTaskOperationContext context,
        Guid taskId,
        string status,
        string summary,
        CancellationToken cancellationToken)
    {
        var task = await dbContext.AiTasks.FirstOrDefaultAsync(t => t.Id == taskId, cancellationToken);
        if (task is null)
        {
            return;
        }

        task.Status = status;
        task.ResolutionSummary = summary;
        task.UpdatedAtUtc = DateTimeOffset.UtcNow;
        task.UpdatedBy = context.MembershipId.ToString();
        task.Version += 1;

        if (status == AiTaskStatuses.Completed)
        {
            task.CompletedAtUtc = DateTimeOffset.UtcNow;
            task.ResolvedAtUtc = DateTimeOffset.UtcNow;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static void ApplyInitialTaskState(AiTask task, string actorSubjectId, DateTimeOffset seedTime)
    {
        task.Status = task.Id == DevelopmentPilotFactoryDecisionCenterIds.AiTaskApproveRecoveryPlanId
            ? AiTaskStatuses.AwaitingApproval
            : AiTaskStatuses.AwaitingReview;
        task.ApprovalState = task.Id == DevelopmentPilotFactoryDecisionCenterIds.AiTaskApproveRecoveryPlanId
            ? AiTaskApprovalStates.Pending
            : AiTaskApprovalStates.NotRequired;
        task.ClosureState = AiTaskClosureStates.None;
        task.ResolutionSummary = null;
        task.ResolutionCode = null;
        task.CompletedAtUtc = null;
        task.ResolvedAtUtc = null;
        task.ApprovalDecidedAtUtc = null;
        task.ApprovalDecidedBy = null;
        task.ApprovalDecisionNote = null;
        task.UpdatedAtUtc = seedTime;
        task.UpdatedBy = actorSubjectId;
        task.Version += 1;
    }
}
