using System.Text.Json;
using NevaxisZentra.Persistence.AiTasks;
using NevaxisZentra.Persistence.Development;
using Microsoft.EntityFrameworkCore;

namespace NevaxisZentra.Persistence.GoldenDemoP1;

public interface IGoldenDemoPresentationAiTaskSyncService
{
    Task SyncAfterPlanApprovedAsync(
        GoldenDemoProductionEvent productionEvent,
        string actorSubjectId,
        CancellationToken cancellationToken);

    Task SyncAfterPlanExecutedAsync(
        GoldenDemoProductionEvent productionEvent,
        string actorSubjectId,
        CancellationToken cancellationToken);

    Task SyncAfterInterventionCompletedAsync(
        GoldenDemoProductionEvent productionEvent,
        string actorSubjectId,
        CancellationToken cancellationToken);

    Task SyncAfterOutcomeVerifiedAsync(
        GoldenDemoProductionEvent productionEvent,
        GoldenDemoVerificationRun verificationRun,
        string actorSubjectId,
        CancellationToken cancellationToken);

    Task ResetLinkedPresentationTasksAsync(
        GoldenDemoProductionEvent productionEvent,
        string actorSubjectId,
        CancellationToken cancellationToken);
}

public sealed class GoldenDemoPresentationAiTaskSyncService : IGoldenDemoPresentationAiTaskSyncService
{
    private const string GoldenDemoSourceModule = "golden-demo-p1";

    private static readonly Guid[] VerificationTaskIds =
    [
        DevelopmentPilotIds.PresentationDemoAiTaskCycleDeviationId,
        DevelopmentPilotIds.PresentationDemoAiTaskGainVerificationId,
    ];

    private static readonly Guid[] ExecutionTaskIds =
    [
        DevelopmentPilotIds.PresentationDemoAiTaskCycleDeviationId,
        DevelopmentPilotIds.PresentationDemoAiTaskMicroStopId,
        DevelopmentPilotIds.PresentationDemoAiTaskGainVerificationId,
    ];

    private readonly IAiTaskRepository aiTaskRepository;
    private readonly IAiTaskClosureRepository aiTaskClosureRepository;
    private readonly NevaxisZentraDbContext dbContext;

    public GoldenDemoPresentationAiTaskSyncService(
        IAiTaskRepository aiTaskRepository,
        IAiTaskClosureRepository aiTaskClosureRepository,
        NevaxisZentraDbContext dbContext)
    {
        this.aiTaskRepository = aiTaskRepository;
        this.aiTaskClosureRepository = aiTaskClosureRepository;
        this.dbContext = dbContext;
    }

    public async Task SyncAfterPlanApprovedAsync(
        GoldenDemoProductionEvent productionEvent,
        string actorSubjectId,
        CancellationToken cancellationToken)
    {
        if (!ShouldSync(productionEvent))
        {
            return;
        }

        var context = BuildOperationContext(productionEvent, actorSubjectId);
        await EnsureTaskStatusAsync(
            context,
            DevelopmentPilotIds.PresentationDemoAiTaskCoolingApprovalId,
            AiTaskStatuses.InProgress,
            "Golden Demo plan onayı — soğutma aksiyonu uygulanabilir.",
            cancellationToken);
    }

    public async Task SyncAfterPlanExecutedAsync(
        GoldenDemoProductionEvent productionEvent,
        string actorSubjectId,
        CancellationToken cancellationToken)
    {
        if (!ShouldSync(productionEvent))
        {
            return;
        }

        var context = BuildOperationContext(productionEvent, actorSubjectId);
        foreach (var taskId in ExecutionTaskIds)
        {
            await EnsureTaskStatusAsync(
                context,
                taskId,
                AiTaskStatuses.InProgress,
                "Golden Demo müdahale planı uygulanıyor.",
                cancellationToken);
        }

        await MarkTaskStartedAsync(
            context,
            DevelopmentPilotIds.PresentationDemoAiTaskGainVerificationId,
            cancellationToken);
    }

    public async Task SyncAfterInterventionCompletedAsync(
        GoldenDemoProductionEvent productionEvent,
        string actorSubjectId,
        CancellationToken cancellationToken)
    {
        if (!ShouldSync(productionEvent))
        {
            return;
        }

        var context = BuildOperationContext(productionEvent, actorSubjectId);
        foreach (var taskId in VerificationTaskIds)
        {
            await EnsureTaskStatusAsync(
                context,
                taskId,
                AiTaskStatuses.InProgress,
                "Golden Demo müdahalesi tamamlandı — sonuç doğrulaması başlatıldı.",
                cancellationToken);

            await SubmitGoldenDemoEvidenceAsync(
                context,
                taskId,
                productionEvent.Id,
                "Müdahale sonrası ölçüm penceresi kaydedildi.",
                "post-intervention",
                cancellationToken);

            await UpdateVerificationResolutionAsync(
                context,
                taskId,
                "Production data / cycle-time trend kanıtları ile sonuç doğrulanıyor.",
                cancellationToken);

            await SetClosureStateAsync(
                context,
                taskId,
                AiTaskClosureStates.ResolutionRequested,
                cancellationToken);
        }
    }

    public async Task SyncAfterOutcomeVerifiedAsync(
        GoldenDemoProductionEvent productionEvent,
        GoldenDemoVerificationRun verificationRun,
        string actorSubjectId,
        CancellationToken cancellationToken)
    {
        if (!ShouldSync(productionEvent))
        {
            return;
        }

        if (!string.Equals(
                verificationRun.Result,
                GoldenDemoVerificationResults.Verified,
                StringComparison.Ordinal))
        {
            return;
        }

        var context = BuildOperationContext(productionEvent, actorSubjectId);
        foreach (var taskId in VerificationTaskIds)
        {
            await CompleteVerifiedTaskAsync(
                context,
                taskId,
                productionEvent.Id,
                verificationRun,
                cancellationToken);
        }

        await EnsureTaskStatusAsync(
            context,
            DevelopmentPilotIds.PresentationDemoAiTaskMicroStopId,
            AiTaskStatuses.Completed,
            "Golden Demo doğrulaması — mikro duruş kök neden analizi tamamlandı.",
            cancellationToken);

        await EnsureTaskStatusAsync(
            context,
            DevelopmentPilotIds.PresentationDemoAiTaskCoolingApprovalId,
            AiTaskStatuses.Completed,
            "Golden Demo doğrulaması — soğutma aksiyonu sonucu doğrulandı.",
            cancellationToken);
    }

    public Task ResetLinkedPresentationTasksAsync(
        GoldenDemoProductionEvent productionEvent,
        string actorSubjectId,
        CancellationToken cancellationToken)
    {
        if (!ShouldSync(productionEvent))
        {
            return Task.CompletedTask;
        }

        return DevelopmentPilotAiTaskPresentationSeed.ResetGoldenDemoLinkedPresentationTasksAsync(
            dbContext,
            productionEvent.TenantId,
            productionEvent.FacilityId,
            actorSubjectId,
            DateTimeOffset.UtcNow,
            cancellationToken);
    }

    private async Task CompleteVerifiedTaskAsync(
        AiTaskOperationContext context,
        Guid taskId,
        Guid productionEventId,
        GoldenDemoVerificationRun verificationRun,
        CancellationToken cancellationToken)
    {
        await EnsureTaskStatusAsync(
            context,
            taskId,
            AiTaskStatuses.InProgress,
            "Golden Demo sonuç doğrulaması tamamlandı.",
            cancellationToken);

        await SubmitGoldenDemoEvidenceAsync(
            context,
            taskId,
            productionEventId,
            verificationRun.Summary,
            "verified-outcome",
            cancellationToken);

        var detail = await aiTaskRepository.GetTaskByIdAsync(context, taskId, cancellationToken);
        if (detail is null || detail.Status == AiTaskStatuses.Completed)
        {
            return;
        }

        var resolutionSummary =
            $"Golden Demo doğrulandı: {verificationRun.Summary} (+{verificationRun.RecoveredGoodPartsPerShift} iyi parça/vardiya).";
        var resolutionResult = await aiTaskClosureRepository.UpdateResolutionAsync(
            context,
            new UpdateAiTaskResolutionCommand(
                taskId,
                detail.Version,
                resolutionSummary,
                AiTaskResolutionCodes.Resolved,
                "Golden Demo üretim kurtarma senaryosu"),
            cancellationToken);
        if (!resolutionResult.Succeeded || resolutionResult.Version is null)
        {
            return;
        }

        var requestContext = context with { PermissionKey = AiTaskPermissionKeys.ClosureRequest };
        var closureRequest = await aiTaskClosureRepository.RequestClosureAsync(
            requestContext,
            new RequestAiTaskClosureCommand(
                taskId,
                resolutionResult.Version.Value,
                "Golden Demo doğrulanmış sonuç ile kapatılıyor."),
            cancellationToken);
        if (!closureRequest.Succeeded)
        {
            return;
        }

        if (taskId == DevelopmentPilotIds.PresentationDemoAiTaskCycleDeviationId
            && closureRequest.Version is not null)
        {
            var approveContext = BuildReviewerOperationContext(context);
            await aiTaskClosureRepository.ApproveClosureAsync(
                approveContext,
                new ApproveAiTaskClosureCommand(
                    taskId,
                    closureRequest.Version.Value,
                    "Golden Demo kritik görev kapanış onayı."),
                cancellationToken);
        }
    }

    private async Task EnsureTaskStatusAsync(
        AiTaskOperationContext context,
        Guid taskId,
        string targetStatus,
        string note,
        CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < 6; attempt++)
        {
            var detail = await aiTaskRepository.GetTaskByIdAsync(context, taskId, cancellationToken);
            if (detail is null || detail.Status == targetStatus)
            {
                return;
            }

            if (AiTaskLifecycle.IsFinalStatus(detail.Status))
            {
                return;
            }

            var nextStatus = ResolveNextTransitionStatus(detail.Status, targetStatus);
            if (nextStatus is null)
            {
                return;
            }

            var result = await aiTaskRepository.TransitionTaskAsync(
                context,
                new TransitionAiTaskCommand(taskId, detail.Version, nextStatus, note),
                cancellationToken);
            if (!result.Succeeded)
            {
                return;
            }
        }
    }

    private static string? ResolveNextTransitionStatus(string currentStatus, string targetStatus)
    {
        if (AiTaskLifecycle.CanTransition(currentStatus, targetStatus))
        {
            return targetStatus;
        }

        foreach (var candidate in AiTaskLifecycle.GetAllowedTransitions(currentStatus))
        {
            if (AiTaskLifecycle.CanTransition(candidate, targetStatus)
                || string.Equals(candidate, targetStatus, StringComparison.Ordinal))
            {
                return candidate;
            }
        }

        return null;
    }

    private async Task SubmitGoldenDemoEvidenceAsync(
        AiTaskOperationContext context,
        Guid taskId,
        Guid productionEventId,
        string summary,
        string phase,
        CancellationToken cancellationToken)
    {
        var detail = await aiTaskRepository.GetTaskByIdAsync(context, taskId, cancellationToken);
        if (detail is null)
        {
            return;
        }

        var metadata = JsonSerializer.Serialize(new
        {
            phase,
            productionEventId,
            summary,
        });

        await aiTaskClosureRepository.SubmitEvidenceAsync(
            context,
            new SubmitAiTaskEvidenceCommand(
                taskId,
                detail.Version,
                AiTaskEvidenceTypes.ProductionRecord,
                phase == "verified-outcome"
                    ? "Golden Demo doğrulanmış üretim sonucu"
                    : "Golden Demo müdahale sonrası ölçüm",
                summary,
                metadata,
                GoldenDemoSourceModule,
                productionEventId.ToString()),
            cancellationToken);
    }

    private async Task UpdateVerificationResolutionAsync(
        AiTaskOperationContext context,
        Guid taskId,
        string resolutionSummary,
        CancellationToken cancellationToken)
    {
        var detail = await aiTaskRepository.GetTaskByIdAsync(context, taskId, cancellationToken);
        if (detail is null)
        {
            return;
        }

        await aiTaskClosureRepository.UpdateResolutionAsync(
            context,
            new UpdateAiTaskResolutionCommand(
                taskId,
                detail.Version,
                resolutionSummary,
                AiTaskResolutionCodes.DeferredFollowUp,
                "Golden Demo sonuç doğrulaması"),
            cancellationToken);
    }

    private async Task SetClosureStateAsync(
        AiTaskOperationContext context,
        Guid taskId,
        string closureState,
        CancellationToken cancellationToken)
    {
        var task = await dbContext.AiTasks.SingleOrDefaultAsync(
            item => item.TenantId == context.TenantId
                    && item.FacilityId == context.FacilityId
                    && item.Id == taskId,
            cancellationToken);
        if (task is null || task.ClosureState == closureState)
        {
            return;
        }

        task.ClosureState = closureState;
        task.UpdatedAtUtc = DateTimeOffset.UtcNow;
        task.UpdatedBy = context.IdentitySubjectId.ToString();
        task.Version += 1;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task MarkTaskStartedAsync(
        AiTaskOperationContext context,
        Guid taskId,
        CancellationToken cancellationToken)
    {
        var task = await dbContext.AiTasks.SingleOrDefaultAsync(
            item => item.TenantId == context.TenantId
                    && item.FacilityId == context.FacilityId
                    && item.Id == taskId,
            cancellationToken);
        if (task is null || task.StartedAtUtc.HasValue)
        {
            return;
        }

        task.StartedAtUtc = DateTimeOffset.UtcNow;
        task.UpdatedAtUtc = DateTimeOffset.UtcNow;
        task.UpdatedBy = context.IdentitySubjectId.ToString();
        task.Version += 1;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static bool ShouldSync(GoldenDemoProductionEvent productionEvent) =>
        productionEvent.Id == DevelopmentPilotGoldenDemoP1Ids.ProductionEventId
        && productionEvent.TenantId == DevelopmentPilotIds.TenantId
        && productionEvent.FacilityId == DevelopmentPilotIds.FacilityId;

    private static AiTaskOperationContext BuildOperationContext(
        GoldenDemoProductionEvent productionEvent,
        string actorSubjectId)
    {
        var membershipId = Guid.TryParse(actorSubjectId, out var parsedMembership)
            ? parsedMembership
            : DevelopmentPilotIds.OperatorTenantMembershipId;

        return new AiTaskOperationContext(
            productionEvent.TenantId,
            productionEvent.FacilityId,
            membershipId,
            ResolveIdentitySubjectId(membershipId),
            AiTaskPermissionKeys.Review,
            null);
    }

    private static AiTaskOperationContext BuildReviewerOperationContext(AiTaskOperationContext requestContext) =>
        new(
            requestContext.TenantId,
            requestContext.FacilityId,
            DevelopmentPilotIds.ReviewerTenantMembershipId,
            DevelopmentPilotIds.ReviewerIdentitySubjectId,
            AiTaskPermissionKeys.ClosureApprove,
            requestContext.CorrelationId);

    private static Guid ResolveIdentitySubjectId(Guid membershipId) =>
        membershipId switch
        {
            _ when membershipId == DevelopmentPilotIds.ReviewerTenantMembershipId
                => DevelopmentPilotIds.ReviewerIdentitySubjectId,
            _ when membershipId == DevelopmentPilotIds.OperatorTenantMembershipId
                => DevelopmentPilotIds.OperatorIdentitySubjectId,
            _ => DevelopmentPilotIds.IdentitySubjectId,
        };
}
