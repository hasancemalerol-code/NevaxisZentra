#pragma warning disable CA1305, CA1859

using System.Text.Json;

namespace NevaxisZentra.Persistence.AiTasks;

public interface IAiTaskAutomationEngine
{
    AiTaskAutomationEvaluationResult Evaluate(
        AiTask task,
        string triggerType,
        IReadOnlyList<AiTaskAutomationRule> rules,
        AiTaskAutomationOverrideState? manualOverride,
        bool simulate);
}

public sealed class AiTaskAutomationEngine : IAiTaskAutomationEngine
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public AiTaskAutomationEvaluationResult Evaluate(
        AiTask task,
        string triggerType,
        IReadOnlyList<AiTaskAutomationRule> rules,
        AiTaskAutomationOverrideState? manualOverride,
        bool simulate)
    {
        var now = DateTimeOffset.UtcNow;
        var skipped = new List<AiTaskAutomationSkippedRule>();
        var matched = new List<AiTaskAutomationMatchedRule>();
        var proposedByField = new Dictionary<string, (AiTaskAutomationProposedChange Change, int SortOrder, Guid RuleId)>(
            StringComparer.Ordinal);
        var conflicts = new List<AiTaskAutomationConflict>();
        var actionsApplied = new List<AiTaskAutomationAction>();

        var eligibleRules = rules
            .Where(r => (simulate || r.Enabled)
                        && string.Equals(r.TriggerType, triggerType, StringComparison.Ordinal)
                        && (r.FacilityId is null || r.FacilityId == task.FacilityId)
                        && (r.EffectiveFromUtc is null || r.EffectiveFromUtc <= now)
                        && (r.EffectiveUntilUtc is null || r.EffectiveUntilUtc > now))
            .OrderBy(r => r.SortOrder)
            .ThenBy(r => r.RuleKey)
            .ToList();

        foreach (var rule in eligibleRules)
        {
            var conditions = DeserializeConditions(rule.ConditionsJson);
            if (!ConditionsMatch(task, conditions, out var matchedConditions, out var skipReason))
            {
                skipped.Add(new AiTaskAutomationSkippedRule(rule.Id, rule.Name, skipReason));
                continue;
            }

            matched.Add(new AiTaskAutomationMatchedRule(
                rule.Id,
                rule.Name,
                rule.Version,
                rule.SortOrder,
                matchedConditions));

            foreach (var action in DeserializeActions(rule.ActionsJson))
            {
                if (!AiTaskAutomationActionTypes.All.Contains(action.Type))
                {
                    continue;
                }

                if (IsOverridden(action.Type, manualOverride))
                {
                    skipped.Add(new AiTaskAutomationSkippedRule(rule.Id, rule.Name, $"Manual override active for {MapActionScope(action.Type)}."));
                    continue;
                }

                foreach (var change in BuildProposedChanges(task, action))
                {
                    if (proposedByField.TryGetValue(change.Field, out var existing))
                    {
                        if (!string.Equals(existing.Change.ProposedValue, change.ProposedValue, StringComparison.Ordinal))
                        {
                            conflicts.Add(new AiTaskAutomationConflict(
                                change.Field,
                                [existing.Change.ProposedValue ?? "", change.ProposedValue ?? ""],
                                rule.SortOrder < existing.SortOrder
                                    ? $"Higher-priority rule '{rule.Name}' wins over '{existing.Change.SourceRuleName}'."
                                    : $"Higher-priority rule '{existing.Change.SourceRuleName}' retained.",
                                rule.SortOrder <= existing.SortOrder ? rule.Id : existing.RuleId));
                        }

                        if (rule.SortOrder <= existing.SortOrder)
                        {
                            proposedByField[change.Field] = (change with { SourceRuleId = rule.Id, SourceRuleName = rule.Name }, rule.SortOrder, rule.Id);
                        }
                    }
                    else
                    {
                        proposedByField[change.Field] = (change with { SourceRuleId = rule.Id, SourceRuleName = rule.Name }, rule.SortOrder, rule.Id);
                    }
                }

                actionsApplied.Add(action);
            }
        }

        var proposed = proposedByField.Values
            .Select(v => v.Change)
            .OrderBy(c => c.Field, StringComparer.Ordinal)
            .ToList();

        var summary = matched.Count == 0
            ? "No automation rules matched."
            : simulate
                ? $"Simulation: {matched.Count} rule(s) matched, {proposed.Count} change(s) proposed."
                : $"Applied automation from {matched.Count} rule(s).";

        return new AiTaskAutomationEvaluationResult(
            triggerType,
            matched,
            skipped,
            proposed,
            conflicts,
            actionsApplied,
            simulate,
            summary);
    }

    internal static bool ConditionsMatch(
        AiTask task,
        IReadOnlyList<AiTaskAutomationCondition> conditions,
        out IReadOnlyList<AiTaskAutomationCondition> matchedConditions,
        out string skipReason)
    {
        var matched = new List<AiTaskAutomationCondition>();
        foreach (var condition in conditions)
        {
            if (!EvaluateCondition(task, condition))
            {
                matchedConditions = matched;
                skipReason = $"Condition failed: {condition.Field} {condition.Operator}.";
                return false;
            }

            matched.Add(condition);
        }

        matchedConditions = matched;
        skipReason = string.Empty;
        return true;
    }

    internal static bool EvaluateCondition(AiTask task, AiTaskAutomationCondition condition)
    {
        var actual = ResolveFieldValue(task, condition.Field);
        return condition.Operator switch
        {
            AiTaskAutomationConditionOperators.Equal =>
                string.Equals(actual, condition.Value, StringComparison.OrdinalIgnoreCase),
            AiTaskAutomationConditionOperators.NotEqual =>
                !string.Equals(actual, condition.Value, StringComparison.OrdinalIgnoreCase),
            AiTaskAutomationConditionOperators.In =>
                condition.Values?.Any(v => string.Equals(v, actual, StringComparison.OrdinalIgnoreCase)) == true,
            AiTaskAutomationConditionOperators.NotIn =>
                condition.Values?.All(v => !string.Equals(v, actual, StringComparison.OrdinalIgnoreCase)) != false,
            AiTaskAutomationConditionOperators.Gte =>
                int.TryParse(actual, out var actualInt) && int.TryParse(condition.Value, out var threshold)
                && actualInt >= threshold,
            AiTaskAutomationConditionOperators.Lte =>
                int.TryParse(actual, out var actualIntLte) && int.TryParse(condition.Value, out var thresholdLte)
                && actualIntLte <= thresholdLte,
            _ => false,
        };
    }

    internal static string? ResolveFieldValue(AiTask task, string field) =>
        field switch
        {
            AiTaskAutomationConditionFields.SourceModule => task.SourceModule,
            AiTaskAutomationConditionFields.TaskCategory => task.Category,
            AiTaskAutomationConditionFields.TaskType => task.Category,
            AiTaskAutomationConditionFields.Facility => task.FacilityId.ToString("D"),
            AiTaskAutomationConditionFields.Priority => task.Priority,
            AiTaskAutomationConditionFields.Severity => task.RiskLevel,
            AiTaskAutomationConditionFields.EscalationLevel => task.EscalationLevel.ToString(),
            AiTaskAutomationConditionFields.RequiresEvidence => task.ClosurePolicyId.HasValue ? "true" : "false",
            AiTaskAutomationConditionFields.ApprovalRequired => task.ClosureApprovalRequired ? "true" : "false",
            AiTaskAutomationConditionFields.SourceEntityType => task.SourceModule,
            AiTaskAutomationConditionFields.OriginType => task.SourceModule,
            AiTaskAutomationConditionFields.CurrentAssignmentState => ResolveAssignmentState(task),
            _ => null,
        };

    private static string ResolveAssignmentState(AiTask task)
    {
        if (task.AssignedMembershipId.HasValue)
        {
            return AiTaskAutomationAssignmentStates.Membership;
        }

        if (task.AssignedTeamChannelId.HasValue)
        {
            return AiTaskAutomationAssignmentStates.Team;
        }

        return AiTaskAutomationAssignmentStates.Unassigned;
    }

    private static IEnumerable<AiTaskAutomationProposedChange> BuildProposedChanges(AiTask task, AiTaskAutomationAction action) =>
        action.Type switch
        {
            AiTaskAutomationActionTypes.AssignToMembership => [
                new("assignment", DescribeAssignment(task), $"membership:{action.GuidValue:D}", null, null),
            ],
            AiTaskAutomationActionTypes.AssignToTeam or AiTaskAutomationActionTypes.RouteToQueue => [
                new("assignment", DescribeAssignment(task), $"team:{action.GuidValue:D}", null, null),
            ],
            AiTaskAutomationActionTypes.SetPriority => [
                new("priority", task.Priority, action.Value, null, null),
            ],
            AiTaskAutomationActionTypes.ApplySlaPolicy => [
                new("slaPolicy", task.SlaPolicyId?.ToString("D"), action.Value, null, null),
            ],
            AiTaskAutomationActionTypes.RequireEvidence => [
                new("evidenceRequired", task.MinimumEvidenceRequired.ToString(), Math.Max(1, action.IntValue ?? 1).ToString(), null, null),
            ],
            AiTaskAutomationActionTypes.RequireClosureApproval => [
                new("closureApprovalRequired", task.ClosureApprovalRequired.ToString(), "true", null, null),
            ],
            AiTaskAutomationActionTypes.EnableFourEyes => [
                new("fourEyesRequired", task.FourEyesRequired.ToString(), "true", null, null),
            ],
            AiTaskAutomationActionTypes.ApplyClosurePolicy => [
                new("closurePolicy", task.ClosurePolicyId?.ToString("D"), action.Value, null, null),
            ],
            AiTaskAutomationActionTypes.NotifyTeamsChannel => [
                new("teamsNotification", null, action.GuidValue?.ToString("D"), null, null),
            ],
            AiTaskAutomationActionTypes.AddTaskTag => [
                new("tag", null, action.Value, null, null),
            ],
            _ => Array.Empty<AiTaskAutomationProposedChange>(),
        };

    private static string DescribeAssignment(AiTask task)
    {
        if (task.AssignedMembershipId.HasValue)
        {
            return $"membership:{task.AssignedMembershipId:D}";
        }

        if (task.AssignedTeamChannelId.HasValue)
        {
            return $"team:{task.AssignedTeamChannelId:D}";
        }

        return "unassigned";
    }

    private static bool IsOverridden(string actionType, AiTaskAutomationOverrideState? manualOverride)
    {
        if (manualOverride is null || manualOverride.AllowAutomationResume)
        {
            return false;
        }

        var scope = MapActionScope(actionType);
        return manualOverride.Scopes.Contains(scope, StringComparer.Ordinal);
    }

    private static string MapActionScope(string actionType) =>
        actionType switch
        {
            AiTaskAutomationActionTypes.AssignToMembership or AiTaskAutomationActionTypes.AssignToTeam or AiTaskAutomationActionTypes.RouteToQueue
                => AiTaskAutomationOverrideScopes.Assignment,
            AiTaskAutomationActionTypes.SetPriority => AiTaskAutomationOverrideScopes.Priority,
            AiTaskAutomationActionTypes.ApplySlaPolicy or AiTaskAutomationActionTypes.SetDueDatePolicy
                => AiTaskAutomationOverrideScopes.SlaPolicy,
            AiTaskAutomationActionTypes.ApplyEscalationPolicy => AiTaskAutomationOverrideScopes.EscalationPolicy,
            _ => string.Empty,
        };

    internal static IReadOnlyList<AiTaskAutomationCondition> DeserializeConditions(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<List<AiTaskAutomationCondition>>(json, JsonOptions) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    internal static IReadOnlyList<AiTaskAutomationAction> DeserializeActions(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<List<AiTaskAutomationAction>>(json, JsonOptions) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    internal static string SerializeConditions(IReadOnlyList<AiTaskAutomationCondition> conditions) =>
        JsonSerializer.Serialize(conditions, JsonOptions);

    internal static string SerializeActions(IReadOnlyList<AiTaskAutomationAction> actions) =>
        JsonSerializer.Serialize(actions, JsonOptions);
}
