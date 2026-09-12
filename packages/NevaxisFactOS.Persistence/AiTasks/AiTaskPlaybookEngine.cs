using System.Text.Json;

namespace NevaxisFactOS.Persistence.AiTasks;

public interface IAiTaskPlaybookEngine
{
    bool EvaluateCondition(
        AiTaskPlaybookConditionDefinition? condition,
        IReadOnlyDictionary<string, string>? contextValues);

    AiTaskPlaybookProgressReadModel CalculateProgress(IReadOnlyList<AiTaskPlaybookRunStep> steps);

    AiTaskPlaybookCriticalPathReadModel CalculateCriticalPath(
        IReadOnlyList<AiTaskPlaybookStep> templateSteps,
        IReadOnlyList<AiTaskPlaybookStepDependency> dependencies,
        IReadOnlyList<AiTaskPlaybookRunStep> runSteps);

    void ReevaluateRunSteps(
        AiTaskPlaybookRun run,
        IReadOnlyList<AiTaskPlaybookRunStep> runSteps,
        IReadOnlyList<AiTaskPlaybookStepDependency> dependencies,
        IReadOnlyDictionary<Guid, string> stepKeyByTemplateStepId);
}

public sealed class AiTaskPlaybookEngine : IAiTaskPlaybookEngine
{
    public bool EvaluateCondition(
        AiTaskPlaybookConditionDefinition? condition,
        IReadOnlyDictionary<string, string>? contextValues)
    {
        if (condition is null)
        {
            return true;
        }

        if (contextValues is null || !contextValues.TryGetValue(condition.Field, out var actual))
        {
            return false;
        }

        return condition.Operator switch
        {
            AiTaskPlaybookConditionOperators.Equal =>
                string.Equals(actual, condition.Value, StringComparison.OrdinalIgnoreCase),
            AiTaskPlaybookConditionOperators.Gte when int.TryParse(actual, out var actualValue)
                                                     && int.TryParse(condition.Value, out var threshold) =>
                actualValue >= threshold,
            _ => false,
        };
    }

    public AiTaskPlaybookProgressReadModel CalculateProgress(IReadOnlyList<AiTaskPlaybookRunStep> steps)
    {
        var required = steps.Where(step => step.Required).ToList();
        var optional = steps.Where(step => !step.Required).ToList();
        var requiredCompleted = required.Count(step => IsCompleted(step.Status));
        var optionalCompleted = optional.Count(step => IsCompleted(step.Status));
        var blocked = steps.Count(step => step.Status == AiTaskPlaybookRunStepStatuses.Blocked);
        var approvalsPending = steps.Count(step => step.Status == AiTaskPlaybookRunStepStatuses.AwaitingApproval);
        int? overallPercent = required.Count == 0
            ? null
            : (int)Math.Round(requiredCompleted * 100d / required.Count, MidpointRounding.AwayFromZero);

        return new AiTaskPlaybookProgressReadModel(
            requiredCompleted,
            required.Count,
            optionalCompleted,
            optional.Count,
            blocked,
            approvalsPending,
            overallPercent);
    }

    public AiTaskPlaybookCriticalPathReadModel CalculateCriticalPath(
        IReadOnlyList<AiTaskPlaybookStep> templateSteps,
        IReadOnlyList<AiTaskPlaybookStepDependency> dependencies,
        IReadOnlyList<AiTaskPlaybookRunStep> runSteps)
    {
        var durations = templateSteps
            .Where(step => step.TargetDurationMinutes.HasValue)
            .ToDictionary(step => step.Id, step => step.TargetDurationMinutes!.Value);

        if (durations.Count == 0)
        {
            var blocking = runSteps
                .Where(step => step.Required && !IsCompleted(step.Status) && step.Status != AiTaskPlaybookRunStepStatuses.Skipped)
                .OrderBy(step => step.SortOrder)
                .Select(step => step.StepKey)
                .Take(3)
                .ToList();

            return new AiTaskPlaybookCriticalPathReadModel(
                false,
                blocking,
                [],
                "Hedef süre verisi yetersiz; yalnızca mevcut blokaj adımları gösteriliyor.");
        }

        var stepById = templateSteps.ToDictionary(step => step.Id);
        var incoming = dependencies
            .Where(dep => dep.DependencyType == AiTaskPlaybookDependencyTypes.RequiredPredecessor)
            .GroupBy(dep => dep.StepId)
            .ToDictionary(group => group.Key, group => group.Select(dep => dep.DependsOnStepId).ToList());

        var distance = new Dictionary<Guid, int>();
        foreach (var step in templateSteps)
        {
            distance[step.Id] = durations.GetValueOrDefault(step.Id, 0);
        }

        foreach (var step in templateSteps.OrderBy(step => step.SortOrder))
        {
            if (!incoming.TryGetValue(step.Id, out var predecessors))
            {
                continue;
            }

            var maxPredecessor = predecessors.Select(id => distance.GetValueOrDefault(id, 0)).DefaultIfEmpty(0).Max();
            distance[step.Id] = maxPredecessor + durations.GetValueOrDefault(step.Id, 0);
        }

        var maxDistance = distance.Values.DefaultIfEmpty(0).Max();
        var criticalTemplateIds = distance
            .Where(pair => pair.Value == maxDistance)
            .Select(pair => pair.Key)
            .ToHashSet();

        var runByTemplate = runSteps.ToDictionary(step => step.TemplateStepId);
        var blockingKeys = runSteps
            .Where(step => step.Required && !IsCompleted(step.Status) && step.Status != AiTaskPlaybookRunStepStatuses.Skipped)
            .OrderBy(step => step.SortOrder)
            .Select(step => step.StepKey)
            .ToList();

        var impactedKeys = criticalTemplateIds
            .Select(id => runByTemplate.TryGetValue(id, out var runStep) ? runStep.StepKey : stepById[id].StepKey)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new AiTaskPlaybookCriticalPathReadModel(true, blockingKeys, impactedKeys, null);
    }

    public void ReevaluateRunSteps(
        AiTaskPlaybookRun run,
        IReadOnlyList<AiTaskPlaybookRunStep> runSteps,
        IReadOnlyList<AiTaskPlaybookStepDependency> dependencies,
        IReadOnlyDictionary<Guid, string> stepKeyByTemplateStepId)
    {
        var runStepByTemplate = runSteps.ToDictionary(step => step.TemplateStepId);
        var completed = runSteps
            .Where(step => IsCompleted(step.Status) || step.Status == AiTaskPlaybookRunStepStatuses.Skipped)
            .Select(step => step.TemplateStepId)
            .ToHashSet();

        var manuallyBlocked = runSteps
            .Where(step => step.Status == AiTaskPlaybookRunStepStatuses.Blocked && !string.IsNullOrWhiteSpace(step.BlockedBy))
            .Select(step => step.TemplateStepId)
            .ToHashSet();

        foreach (var runStep in runSteps.OrderBy(step => step.SortOrder))
        {
            if (IsTerminal(runStep.Status))
            {
                continue;
            }

            if (manuallyBlocked.Contains(runStep.TemplateStepId))
            {
                runStep.DependencyState = AiTaskPlaybookDependencyStates.Blocked;
                runStep.Status = AiTaskPlaybookRunStepStatuses.Blocked;
                continue;
            }

            var requiredDependencies = dependencies
                .Where(dep => dep.StepId == runStep.TemplateStepId
                              && dep.DependencyType == AiTaskPlaybookDependencyTypes.RequiredPredecessor)
                .ToList();

            var unsatisfied = requiredDependencies
                .Where(dep => !completed.Contains(dep.DependsOnStepId))
                .ToList();

            if (unsatisfied.Count > 0)
            {
                runStep.DependencyState = AiTaskPlaybookDependencyStates.Waiting;
                if (runStep.Status is AiTaskPlaybookRunStepStatuses.Ready or AiTaskPlaybookRunStepStatuses.InProgress)
                {
                    runStep.Status = AiTaskPlaybookRunStepStatuses.Pending;
                }

                var predecessorNames = unsatisfied
                    .Select(dep => runStepByTemplate.TryGetValue(dep.DependsOnStepId, out var predecessor)
                        ? predecessor.Name
                        : stepKeyByTemplateStepId.GetValueOrDefault(dep.DependsOnStepId, dep.DependsOnStepId.ToString()))
                    .ToList();
                runStep.BlockReason = predecessorNames.Count == 1
                    ? $"{predecessorNames[0]} tamamlanmadan {runStep.Name} başlatılamaz."
                    : $"Önkoşul adımlar tamamlanmadan {runStep.Name} başlatılamaz.";
                continue;
            }

            var upstreamBlocked = requiredDependencies.Any(dep =>
                runStepByTemplate.TryGetValue(dep.DependsOnStepId, out var predecessor)
                && predecessor.Status == AiTaskPlaybookRunStepStatuses.Blocked);

            if (upstreamBlocked)
            {
                runStep.DependencyState = AiTaskPlaybookDependencyStates.Blocked;
                runStep.Status = AiTaskPlaybookRunStepStatuses.Blocked;
                runStep.BlockReason = "Üst akış adımı bloke olduğu için bu adım bekliyor.";
                continue;
            }

            runStep.DependencyState = AiTaskPlaybookDependencyStates.Ready;
            runStep.BlockReason = null;
            if (runStep.Status == AiTaskPlaybookRunStepStatuses.Pending)
            {
                runStep.Status = AiTaskPlaybookRunStepStatuses.Ready;
                runStep.ReadyAtUtc ??= DateTimeOffset.UtcNow;
            }
        }

        var progress = CalculateProgress(runSteps);
        run.RequiredStepsCompleted = progress.RequiredCompleted;
        run.RequiredStepsTotal = progress.RequiredTotal;
        run.OptionalStepsCompleted = progress.OptionalCompleted;
        run.OptionalStepsTotal = progress.OptionalTotal;
        run.BlockedStepsCount = progress.BlockedCount;
        run.ApprovalsPending = progress.ApprovalsPending;

        if (run.RequiredStepsTotal > 0 && run.RequiredStepsCompleted == run.RequiredStepsTotal)
        {
            run.Status = AiTaskPlaybookRunStatuses.Completed;
            run.CompletedAtUtc ??= DateTimeOffset.UtcNow;
            run.BlockReason = null;
            return;
        }

        if (run.BlockedStepsCount > 0)
        {
            run.Status = AiTaskPlaybookRunStatuses.Blocked;
            run.BlockReason = runSteps.FirstOrDefault(step => step.Status == AiTaskPlaybookRunStepStatuses.Blocked)?.BlockReason;
            return;
        }

        if (run.ApprovalsPending > 0)
        {
            run.Status = AiTaskPlaybookRunStatuses.AwaitingApproval;
            return;
        }

        run.Status = AiTaskPlaybookRunStatuses.Running;
        run.BlockReason = null;
    }

    private static bool IsCompleted(string status) =>
        status is AiTaskPlaybookRunStepStatuses.Completed;

    private static bool IsTerminal(string status) =>
        status is AiTaskPlaybookRunStepStatuses.Completed
            or AiTaskPlaybookRunStepStatuses.Cancelled
            or AiTaskPlaybookRunStepStatuses.Failed
            or AiTaskPlaybookRunStepStatuses.Skipped;
}

public static class AiTaskPlaybookConditionSerializer
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public static string? Serialize(AiTaskPlaybookConditionDefinition? condition) =>
        condition is null ? null : JsonSerializer.Serialize(condition, JsonOptions);

    public static AiTaskPlaybookConditionDefinition? Deserialize(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        return JsonSerializer.Deserialize<AiTaskPlaybookConditionDefinition>(json, JsonOptions);
    }
}
