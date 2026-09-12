namespace NevaxisFactOS.Persistence.AiTasks;

public sealed record AiTaskSlaPolicyReadModel(
    Guid Id,
    string PolicyKey,
    string Name,
    string Priority,
    string? Category,
    string? SourceModule,
    int AcknowledgmentTargetMinutes,
    int ResponseTargetMinutes,
    int ResolutionTargetMinutes);

public sealed record AiTaskSlaStateReadModel(
    string State,
    DateTimeOffset? AcknowledgmentDueAtUtc,
    DateTimeOffset? ResponseDueAtUtc,
    DateTimeOffset? ResolutionDueAtUtc,
    bool IsOverdue,
    string? BreachType);

public sealed record AiTaskRoutingDecisionReadModel(
    Guid? RuleId,
    string? RuleKey,
    string TargetType,
    Guid? TargetMembershipId,
    Guid? TargetTeamChannelId,
    string Reason);

public sealed record AiTaskAssignmentReadModel(
    Guid? AssignedMembershipId,
    Guid? AssignedTeamChannelId,
    string? AssigneeDisplayName,
    string? AssignmentReason,
    string? AssignmentSource,
    DateTimeOffset? AcknowledgedAtUtc);

public sealed record AiTaskEscalationReadModel(
    int Level,
    string State,
    DateTimeOffset? LastEscalatedAtUtc);

public sealed record AssignAiTaskCommand(
    Guid TaskId,
    uint ExpectedVersion,
    Guid? TargetMembershipId,
    Guid? TargetTeamChannelId,
    string Reason,
    string Source);

public sealed record AcknowledgeAiTaskCommand(
    Guid TaskId,
    uint ExpectedVersion,
    string? Note);

public sealed record ChangeAiTaskPriorityCommand(
    Guid TaskId,
    uint ExpectedVersion,
    string Priority,
    string? Reason);

public sealed record ChangeAiTaskDueDateCommand(
    Guid TaskId,
    uint ExpectedVersion,
    DateTimeOffset? DueAtUtc,
    string? Reason);

public sealed record EscalateAiTaskCommand(
    Guid TaskId,
    uint ExpectedVersion,
    string BreachType,
    string? Note);

public sealed record AiTaskEscalationEvaluationResult(
    int TasksEvaluated,
    int EscalationsCreated,
    IReadOnlyList<Guid> EscalatedTaskIds);

public static class AiTaskSlaEngine
{
    public static AiTaskSlaPolicyReadModel ResolvePolicy(AiTask task, IReadOnlyList<AiTaskSlaPolicy> policies)
    {
        var match = policies
            .Where(p => p.Enabled && p.TenantId == task.TenantId)
            .Where(p => p.FacilityId is null || p.FacilityId == task.FacilityId)
            .Where(p => string.Equals(p.Priority, task.Priority, StringComparison.Ordinal))
            .Where(p => p.Category is null || string.Equals(p.Category, task.Category, StringComparison.Ordinal))
            .Where(p => p.SourceModule is null || string.Equals(p.SourceModule, task.SourceModule, StringComparison.Ordinal))
            .OrderByDescending(p => p.Category is not null)
            .ThenByDescending(p => p.SourceModule is not null)
            .ThenByDescending(p => p.FacilityId is not null)
            .FirstOrDefault();

        if (match is not null)
        {
            return new AiTaskSlaPolicyReadModel(
                match.Id,
                match.PolicyKey,
                match.Name,
                match.Priority,
                match.Category,
                match.SourceModule,
                match.AcknowledgmentTargetMinutes,
                match.ResponseTargetMinutes,
                match.ResolutionTargetMinutes);
        }

        if (!AiTaskSlaDefaults.ByPriority.TryGetValue(task.Priority, out var defaults))
        {
            defaults = AiTaskSlaDefaults.ByPriority[AiTaskPriorities.Medium];
        }

        return new AiTaskSlaPolicyReadModel(
            Guid.Empty,
            $"default-{task.Priority}",
            $"Default {task.Priority} SLA",
            task.Priority,
            null,
            null,
            defaults.AckMinutes,
            defaults.ResponseMinutes,
            defaults.ResolutionMinutes);
    }

    public static AiTaskSlaStateReadModel DeriveSlaState(AiTask task, AiTaskSlaPolicyReadModel policy, DateTimeOffset now)
    {
        if (AiTaskStatuses.Final.Contains(task.Status))
        {
            var resolutionDue = task.CreatedAtUtc.AddMinutes(policy.ResolutionTargetMinutes);
            var completedAt = task.CompletedAtUtc ?? task.ResolvedAtUtc ?? task.UpdatedAtUtc;
            return new AiTaskSlaStateReadModel(
                completedAt <= resolutionDue
                    ? AiTaskSlaStates.CompletedWithinSla
                    : AiTaskSlaStates.CompletedAfterSla,
                null,
                null,
                resolutionDue,
                false,
                null);
        }

        if (task.SlaPolicyId is null && policy.Id == Guid.Empty && task.DueAtUtc is null)
        {
            return new AiTaskSlaStateReadModel(
                AiTaskSlaStates.NotApplicable,
                null,
                null,
                null,
                false,
                null);
        }

        var ackDue = task.CreatedAtUtc.AddMinutes(policy.AcknowledgmentTargetMinutes);
        var responseDue = task.CreatedAtUtc.AddMinutes(policy.ResponseTargetMinutes);
        var resolutionDueAt = task.DueAtUtc ?? task.CreatedAtUtc.AddMinutes(policy.ResolutionTargetMinutes);

        string? breachType = null;
        if (!task.AcknowledgedAtUtc.HasValue && now > ackDue)
        {
            breachType = AiTaskEscalationBreachTypes.Acknowledgment;
        }
        else if (!task.StartedAtUtc.HasValue && now > responseDue)
        {
            breachType = AiTaskEscalationBreachTypes.Response;
        }
        else if (!AiTaskStatuses.Final.Contains(task.Status) && now > resolutionDueAt)
        {
            breachType = AiTaskEscalationBreachTypes.Resolution;
        }

        if (breachType is not null)
        {
            return new AiTaskSlaStateReadModel(
                AiTaskSlaStates.Breached,
                ackDue,
                responseDue,
                resolutionDueAt,
                true,
                breachType);
        }

        if (now > resolutionDueAt)
        {
            return new AiTaskSlaStateReadModel(
                AiTaskSlaStates.Overdue,
                ackDue,
                responseDue,
                resolutionDueAt,
                true,
                null);
        }

        var dueSoonThreshold = resolutionDueAt.AddMinutes(-AiTaskSlaDefaults.DueSoonThresholdMinutes);
        if (now >= dueSoonThreshold)
        {
            return new AiTaskSlaStateReadModel(
                AiTaskSlaStates.DueSoon,
                ackDue,
                responseDue,
                resolutionDueAt,
                false,
                null);
        }

        return new AiTaskSlaStateReadModel(
            AiTaskSlaStates.OnTrack,
            ackDue,
            responseDue,
            resolutionDueAt,
            false,
            null);
    }
}
