using NevaxisFactOS.Persistence.AiTasks;
using NevaxisFactOS.Persistence.Authorization;
using Microsoft.EntityFrameworkCore;

namespace NevaxisFactOS.Persistence.DigitalWorkforce;

public interface IDigitalWorkforceTaskCenterBridge
{
    Task<Guid?> EnsureLinkedTaskAsync(
        DigitalWorkCase workCase,
        DigitalWorker worker,
        Guid facilityId,
        CancellationToken cancellationToken);
}

public sealed class DigitalWorkforceTaskCenterBridge : IDigitalWorkforceTaskCenterBridge
{
    private readonly NevaxisFactOSDbContext dbContext;
    private readonly IAiTaskRepository aiTaskRepository;
    private readonly IAuthorizationContext authorizationContext;

    public DigitalWorkforceTaskCenterBridge(
        NevaxisFactOSDbContext dbContext,
        IAiTaskRepository aiTaskRepository,
        IAuthorizationContext authorizationContext)
    {
        this.dbContext = dbContext;
        this.aiTaskRepository = aiTaskRepository;
        this.authorizationContext = authorizationContext;
    }

    public async Task<Guid?> EnsureLinkedTaskAsync(
        DigitalWorkCase workCase,
        DigitalWorker worker,
        Guid facilityId,
        CancellationToken cancellationToken)
    {
        if (workCase.LinkedAiTaskId.HasValue)
        {
            return workCase.LinkedAiTaskId;
        }

        if (!authorizationContext.IsAvailable || authorizationContext.Current is null)
        {
            return null;
        }

        var actor = authorizationContext.Current;
        var context = new AiTaskOperationContext(
            actor.TenantId,
            facilityId,
            actor.MembershipId,
            actor.IdentitySubjectId,
            DigitalWorkforcePermissionKeys.Manage,
            null);

        var command = new CreateSourceLinkedAiTaskCommand(
            Title: workCase.Title,
            Summary: workCase.Description,
            Category: "digital-workforce",
            Priority: MapSeverity(workCase.Severity),
            RiskLevel: "medium",
            DetectedIssue: workCase.Source,
            Reasoning: $"Dijital İş Gücü vakası {workCase.Id} için insan onayı/kararı gerekiyor.",
            Recommendation: new AiTaskRecommendationModel(
                "Dijital İş Gücü onayını değerlendir",
                workCase.BusinessImpact,
                "approval",
                true,
                true,
                "digital-workforce"),
            Confidence: new AiTaskConfidenceModel(0.85m, "Runtime policy envelope", "dw-runtime"),
            Impact: new AiTaskImpactModel(
                null, "TRY", null, null, null, null, null, workCase.BusinessImpact),
            Sources:
            [
                new AiTaskSourceReferenceModel(
                    "digital-work-case",
                    workCase.Id.ToString(),
                    worker.Name,
                    workCase.StartedAtUtc,
                    workCase.Source),
            ],
            SourceModule: "digital-workforce",
            SourceEntityId: workCase.Id.ToString(),
            IdempotencyKey: $"dw-case-{workCase.Id}",
            SiteId: facilityId.ToString(),
            ProductionLineId: null,
            MachineId: null,
            MoldId: null,
            ProductionOrderId: null,
            SalesOrderId: null,
            MaterialId: null,
            SupplierId: null,
            CustomerId: null,
            DueAtUtc: workCase.TargetAtUtc);

        var result = await aiTaskRepository.CreateSourceLinkedTaskAsync(context, command, cancellationToken);
        if (!result.Succeeded || result.EntityId is null)
        {
            return null;
        }

        workCase.LinkedAiTaskId = result.EntityId;
        workCase.UpdatedAtUtc = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
        return result.EntityId;
    }

    private static string MapSeverity(string severity) => severity switch
    {
        "critical" or "high" => "high",
        "low" => "low",
        _ => "medium",
    };
}
