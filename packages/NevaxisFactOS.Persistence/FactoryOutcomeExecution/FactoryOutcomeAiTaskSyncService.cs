using Microsoft.EntityFrameworkCore;
using NevaxisFactOS.Persistence.AiTasks;
using NevaxisFactOS.Persistence.Authorization;
using NevaxisFactOS.Persistence.DigitalWorkforce;

namespace NevaxisFactOS.Persistence.FactoryOutcomeExecution;

public interface IFactoryOutcomeAiTaskSyncService
{
    Task EnsureActionTasksAsync(DigitalWorkCase rootCase, CancellationToken cancellationToken);
    Task SyncActionCompletedAsync(
        DigitalWorkCase rootCase,
        FactoryOutcomeExecutionAction action,
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
}

public sealed class FactoryOutcomeAiTaskSyncService : IFactoryOutcomeAiTaskSyncService
{
    private readonly IAiTaskRepository aiTaskRepository;
    private readonly NevaxisFactOSDbContext dbContext;

    public FactoryOutcomeAiTaskSyncService(
        IAiTaskRepository aiTaskRepository,
        NevaxisFactOSDbContext dbContext)
    {
        this.aiTaskRepository = aiTaskRepository;
        this.dbContext = dbContext;
    }

    public async Task EnsureActionTasksAsync(DigitalWorkCase rootCase, CancellationToken cancellationToken)
    {
        var actions = await dbContext.FactoryOutcomeExecutionActions
            .Where(a => a.RootCaseId == rootCase.Id)
            .OrderBy(a => a.ExecutionOrder)
            .ToListAsync(cancellationToken);

        foreach (var action in actions)
        {
            if (action.LinkedAiTaskId.HasValue)
            {
                continue;
            }

            var context = BuildOperationContext(rootCase);
            var existing = await aiTaskRepository.GetTaskBySourceAsync(
                context,
                "factory-outcome-action",
                action.Id.ToString(),
                cancellationToken);
            if (existing is not null)
            {
                action.LinkedAiTaskId = existing.Id;
                action.UpdatedAtUtc = DateTimeOffset.UtcNow;
                continue;
            }

            var result = await aiTaskRepository.CreateSourceLinkedTaskAsync(
                context,
                new CreateSourceLinkedAiTaskCommand(
                    action.Title,
                    action.Title,
                    "factory-outcome",
                    MapPriority(rootCase.Severity),
                    "medium",
                    rootCase.Source,
                    $"Sonuç vakası {rootCase.Id} için yürütülecek aksiyon.",
                    new AiTaskRecommendationModel(
                        action.Title,
                        rootCase.BusinessImpact,
                        "execute",
                        false,
                        false,
                        "factory-outcome"),
                    new AiTaskConfidenceModel(0.82m, "Outcome execution plan", "factory-outcome-execution"),
                    new AiTaskImpactModel(null, "TRY", null, null, null, null, null, rootCase.BusinessImpact),
                    [
                        new AiTaskSourceReferenceModel(
                            "factory-outcome-action",
                            action.Id.ToString(),
                            action.WorkerCode,
                            DateTimeOffset.UtcNow,
                            rootCase.BusinessImpact),
                    ],
                    "factory-outcome-action",
                    action.Id.ToString(),
                    $"factory-outcome-action-{action.Id:N}",
                    rootCase.FacilityId?.ToString(),
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    rootCase.TargetAtUtc),
                cancellationToken);

            if (result.Succeeded && result.EntityId.HasValue)
            {
                action.LinkedAiTaskId = result.EntityId;
                action.UpdatedAtUtc = DateTimeOffset.UtcNow;
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task SyncActionCompletedAsync(
        DigitalWorkCase rootCase,
        FactoryOutcomeExecutionAction action,
        string actorSubjectId,
        CancellationToken cancellationToken)
    {
        if (!action.LinkedAiTaskId.HasValue)
        {
            return;
        }

        var context = BuildOperationContext(rootCase);
        var task = await aiTaskRepository.GetTaskByIdAsync(context, action.LinkedAiTaskId.Value, cancellationToken);
        if (task is null)
        {
            return;
        }

        var targetStatus = action.Status == FactoryOutcomeExecutionActionStatuses.Completed
            ? AiTaskStatuses.Completed
            : AiTaskStatuses.InProgress;

        await aiTaskRepository.TransitionTaskAsync(
            context,
            new TransitionAiTaskCommand(
                action.LinkedAiTaskId.Value,
                task.Version,
                targetStatus,
                $"Aksiyon durumu: {action.Status}"),
            cancellationToken);
    }

    public async Task SyncAfterExecutionProgressAsync(
        DigitalWorkCase rootCase,
        IReadOnlyList<DigitalWorkCase> childCases,
        string actorSubjectId,
        CancellationToken cancellationToken)
    {
        var actions = await dbContext.FactoryOutcomeExecutionActions
            .Where(a => a.RootCaseId == rootCase.Id && a.LinkedAiTaskId != null)
            .ToListAsync(cancellationToken);

        foreach (var action in actions.Where(a => a.Status == FactoryOutcomeExecutionActionStatuses.Completed))
        {
            await SyncActionCompletedAsync(rootCase, action, actorSubjectId, cancellationToken);
        }
    }

    public async Task SyncAfterOutcomeVerifiedAsync(
        DigitalWorkCase rootCase,
        string actorSubjectId,
        CancellationToken cancellationToken)
    {
        if (!rootCase.LinkedAiTaskId.HasValue)
        {
            return;
        }

        var context = BuildOperationContext(rootCase);
        var task = await aiTaskRepository.GetTaskByIdAsync(context, rootCase.LinkedAiTaskId.Value, cancellationToken);
        if (task is null)
        {
            return;
        }

        await aiTaskRepository.TransitionTaskAsync(
            context,
            new TransitionAiTaskCommand(
                rootCase.LinkedAiTaskId.Value,
                task.Version,
                AiTaskStatuses.Completed,
                "Sonuç doğrulandı — kök vaka kapatıldı."),
            cancellationToken);
    }

    private static AiTaskOperationContext BuildOperationContext(DigitalWorkCase rootCase) =>
        new(
            rootCase.TenantId,
            rootCase.FacilityId ?? Guid.Empty,
            Guid.Empty,
            Guid.Empty,
            AiTaskPermissionKeys.Manage,
            null);

    private static string MapPriority(string severity) => severity switch
    {
        "critical" => "critical",
        "high" => "high",
        _ => "medium",
    };
}
