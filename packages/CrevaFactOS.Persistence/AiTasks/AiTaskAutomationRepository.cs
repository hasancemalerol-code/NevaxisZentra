#pragma warning disable CA1859

using System.Text.Json;
using CrevaFactOS.Persistence.Facilities;
using CrevaFactOS.Persistence.Identity;
using CrevaFactOS.Persistence.SaasIdentity;
using CrevaFactOS.Persistence.Teams;
using CrevaFactOS.Persistence.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace CrevaFactOS.Persistence.AiTasks;

public sealed class AiTaskAutomationRepository(
    CrevaFactOSDbContext dbContext,
    TenantDatabaseSession tenantDatabaseSession,
    IAiTaskAutomationEngine automationEngine,
    ITeamsNotificationRepository teamsNotificationRepository) : IAiTaskAutomationRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public Task<IReadOnlyList<AiTaskAutomationRuleSummaryReadModel>> ListRulesAsync(
        AiTaskOperationContext context,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(
            async ct =>
            {
                var rules = await dbContext.AiTaskAutomationRules.AsNoTracking()
                    .Where(r => r.TenantId == context.TenantId
                                && (r.FacilityId == null || r.FacilityId == context.FacilityId))
                    .OrderBy(r => r.SortOrder)
                    .ThenBy(r => r.RuleKey)
                    .ToListAsync(ct);

                return (IReadOnlyList<AiTaskAutomationRuleSummaryReadModel>)rules
                    .Select(MapSummary)
                    .ToList();
            },
            cancellationToken);

    public Task<AiTaskAutomationRuleDetailReadModel?> GetRuleAsync(
        AiTaskOperationContext context,
        Guid ruleId,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(
            async ct =>
            {
                var rule = await dbContext.AiTaskAutomationRules.AsNoTracking()
                    .SingleOrDefaultAsync(
                        r => r.TenantId == context.TenantId
                             && r.Id == ruleId
                             && (r.FacilityId == null || r.FacilityId == context.FacilityId),
                        ct);
                return rule is null ? null : MapDetail(rule);
            },
            cancellationToken);

    public Task<AiTaskAutomationWriteResult> CreateRuleAsync(
        AiTaskOperationContext context,
        CreateAiTaskAutomationRuleCommand command,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteWriteAsync(
            async ct =>
            {
                var validation = ValidateRulePayload(command.TriggerType, command.Conditions, command.Actions);
                if (validation is not null)
                {
                    return AiTaskAutomationWriteResult.Fail(AiTaskFailureCodes.ValidationFailed, validation);
                }

                if (command.FacilityId.HasValue && command.FacilityId != context.FacilityId)
                {
                    return AiTaskAutomationWriteResult.Fail(
                        AiTaskFailureCodes.ValidationFailed,
                        "Rule facility must match the active facility scope.");
                }

                var exists = await dbContext.AiTaskAutomationRules.AnyAsync(
                    r => r.TenantId == context.TenantId
                         && r.FacilityId == command.FacilityId
                         && r.RuleKey == command.RuleKey.Trim(),
                    ct);
                if (exists)
                {
                    return AiTaskAutomationWriteResult.Fail(
                        AiTaskFailureCodes.ValidationFailed,
                        "An automation rule with this key already exists.");
                }

                var now = DateTimeOffset.UtcNow;
                var actor = context.IdentitySubjectId.ToString();
                var ruleId = Guid.NewGuid();
                var revisionId = Guid.NewGuid();

                var rule = new AiTaskAutomationRule
                {
                    Id = ruleId,
                    TenantId = context.TenantId,
                    FacilityId = command.FacilityId ?? context.FacilityId,
                    RuleKey = command.RuleKey.Trim(),
                    Name = command.Name.Trim(),
                    Description = TrimOrNull(command.Description),
                    Enabled = command.Enabled,
                    SortOrder = command.SortOrder,
                    TriggerType = command.TriggerType.Trim(),
                    ConditionsJson = AiTaskAutomationEngine.SerializeConditions(command.Conditions),
                    ActionsJson = AiTaskAutomationEngine.SerializeActions(command.Actions),
                    EffectiveFromUtc = command.EffectiveFromUtc,
                    EffectiveUntilUtc = command.EffectiveUntilUtc,
                    Version = 1,
                    CurrentRevisionId = revisionId,
                    CreatedBy = actor,
                    UpdatedBy = actor,
                    CreatedAtUtc = now,
                    UpdatedAtUtc = now,
                };

                var revision = BuildRevision(rule, revisionId, actor, now, "Initial version.");
                dbContext.AiTaskAutomationRules.Add(rule);
                dbContext.AiTaskAutomationRuleRevisions.Add(revision);
                await dbContext.SaveChangesAsync(ct);
                return AiTaskAutomationWriteResult.Success(ruleId, (uint)rule.Version);
            },
            cancellationToken);

    public Task<AiTaskAutomationWriteResult> UpdateRuleAsync(
        AiTaskOperationContext context,
        UpdateAiTaskAutomationRuleCommand command,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteWriteAsync(
            async ct =>
            {
                var validation = ValidateRulePayload(command.TriggerType, command.Conditions, command.Actions);
                if (validation is not null)
                {
                    return AiTaskAutomationWriteResult.Fail(AiTaskFailureCodes.ValidationFailed, validation);
                }

                var rule = await dbContext.AiTaskAutomationRules
                    .SingleOrDefaultAsync(
                        r => r.TenantId == context.TenantId
                             && r.Id == command.RuleId
                             && (r.FacilityId == null || r.FacilityId == context.FacilityId),
                        ct);
                if (rule is null)
                {
                    return AiTaskAutomationWriteResult.Fail(AiTaskFailureCodes.NotFound, "Automation rule not found.");
                }

                if (rule.Version != command.ExpectedVersion)
                {
                    return AiTaskAutomationWriteResult.Fail(
                        AiTaskFailureCodes.ConcurrencyConflict,
                        "Automation rule was modified by another request.");
                }

                var now = DateTimeOffset.UtcNow;
                var actor = context.IdentitySubjectId.ToString();
                var revisionId = Guid.NewGuid();

                rule.Name = command.Name.Trim();
                rule.Description = TrimOrNull(command.Description);
                rule.TriggerType = command.TriggerType.Trim();
                rule.SortOrder = command.SortOrder;
                rule.Enabled = command.Enabled;
                rule.ConditionsJson = AiTaskAutomationEngine.SerializeConditions(command.Conditions);
                rule.ActionsJson = AiTaskAutomationEngine.SerializeActions(command.Actions);
                rule.EffectiveFromUtc = command.EffectiveFromUtc;
                rule.EffectiveUntilUtc = command.EffectiveUntilUtc;
                rule.Version++;
                rule.CurrentRevisionId = revisionId;
                rule.UpdatedAtUtc = now;
                rule.UpdatedBy = actor;

                dbContext.AiTaskAutomationRuleRevisions.Add(
                    BuildRevision(rule, revisionId, actor, now, command.ChangeReason.Trim()));

                await dbContext.SaveChangesAsync(ct);
                return AiTaskAutomationWriteResult.Success(rule.Id, (uint)rule.Version);
            },
            cancellationToken);

    public Task<AiTaskAutomationWriteResult> SetRuleEnabledAsync(
        AiTaskOperationContext context,
        Guid ruleId,
        uint expectedVersion,
        bool enabled,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteWriteAsync(
            async ct =>
            {
                var rule = await dbContext.AiTaskAutomationRules
                    .SingleOrDefaultAsync(
                        r => r.TenantId == context.TenantId
                             && r.Id == ruleId
                             && (r.FacilityId == null || r.FacilityId == context.FacilityId),
                        ct);
                if (rule is null)
                {
                    return AiTaskAutomationWriteResult.Fail(AiTaskFailureCodes.NotFound, "Automation rule not found.");
                }

                if (rule.Version != expectedVersion)
                {
                    return AiTaskAutomationWriteResult.Fail(
                        AiTaskFailureCodes.ConcurrencyConflict,
                        "Automation rule was modified by another request.");
                }

                var now = DateTimeOffset.UtcNow;
                var actor = context.IdentitySubjectId.ToString();
                var revisionId = Guid.NewGuid();
                rule.Enabled = enabled;
                rule.Version++;
                rule.CurrentRevisionId = revisionId;
                rule.UpdatedAtUtc = now;
                rule.UpdatedBy = actor;
                dbContext.AiTaskAutomationRuleRevisions.Add(
                    BuildRevision(rule, revisionId, actor, now, enabled ? "Rule enabled." : "Rule disabled."));
                await dbContext.SaveChangesAsync(ct);
                return AiTaskAutomationWriteResult.Success(rule.Id, (uint)rule.Version);
            },
            cancellationToken);

    public Task<IReadOnlyList<AiTaskAutomationRuleRevisionReadModel>> ListRuleHistoryAsync(
        AiTaskOperationContext context,
        Guid ruleId,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(
            async ct =>
            {
                var revisions = await dbContext.AiTaskAutomationRuleRevisions.AsNoTracking()
                    .Where(r => r.TenantId == context.TenantId && r.RuleId == ruleId)
                    .OrderByDescending(r => r.Version)
                    .ToListAsync(ct);

                return (IReadOnlyList<AiTaskAutomationRuleRevisionReadModel>)revisions
                    .Select(r => new AiTaskAutomationRuleRevisionReadModel(
                        r.Id, r.Version, r.Name, r.Enabled, r.ChangedBy, r.ChangedAtUtc, r.ChangeReason))
                    .ToList();
            },
            cancellationToken);

    public Task<AiTaskAutomationEvaluationResult> SimulateAsync(
        AiTaskOperationContext context,
        SimulateAiTaskAutomationCommand command,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(
            async ct =>
            {
                var task = await LoadTaskForEvaluationAsync(context, command.TaskId, ct);
                if (task is null)
                {
                    throw new InvalidOperationException("AI task not found.");
                }

                var rules = await LoadRulesForEvaluationAsync(context, command.RuleId, ct);
                var manualOverride = DeserializeOverride(task.AutomationOverrideJson);
                return automationEngine.Evaluate(task, command.TriggerType, rules, manualOverride, simulate: true);
            },
            cancellationToken);

    public Task<AiTaskAutomationEvaluationResult> EvaluateAndApplyToTaskAsync(
        AiTaskOperationContext context,
        AiTask task,
        string triggerType,
        DateTimeOffset now,
        string actor,
        Guid? correlationId,
        CancellationToken cancellationToken = default) =>
        ApplyEvaluationCoreAsync(context, task, triggerType, now, actor, correlationId, cancellationToken);

    public Task<AiTaskAutomationEvaluationResult> EvaluateAndApplyAsync(
        AiTaskOperationContext context,
        AiTask task,
        string triggerType,
        DateTimeOffset now,
        string actor,
        Guid? correlationId,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteWriteAsync(
            async ct =>
            {
                var result = await ApplyEvaluationCoreAsync(context, task, triggerType, now, actor, correlationId, ct);
                await dbContext.SaveChangesAsync(ct);
                return result;
            },
            cancellationToken);

    private async Task<AiTaskAutomationEvaluationResult> ApplyEvaluationCoreAsync(
        AiTaskOperationContext context,
        AiTask task,
        string triggerType,
        DateTimeOffset now,
        string actor,
        Guid? correlationId,
        CancellationToken cancellationToken)
    {
        var rules = await LoadRulesForEvaluationAsync(context, ruleId: null, cancellationToken);
        var manualOverride = DeserializeOverride(task.AutomationOverrideJson);
        var evaluation = automationEngine.Evaluate(task, triggerType, rules, manualOverride, simulate: false);
        if (evaluation.MatchedRules.Count == 0)
        {
            return evaluation;
        }

        var winningChanges = evaluation.ProposedChanges
            .GroupBy(c => c.Field, StringComparer.Ordinal)
            .Select(g => g.Last())
            .ToList();

        var appliedActions = new List<object>();
        var matchedRule = evaluation.MatchedRules[0];
        var idempotencyKey = BuildIdempotencyKey(task.Id, triggerType, matchedRule.RuleId, matchedRule.RuleVersion, task.Version);

        var alreadyApplied = await dbContext.AiTaskAutomationExecutions.AsNoTracking()
            .AnyAsync(
                e => e.TenantId == context.TenantId
                     && e.TaskId == task.Id
                     && e.IdempotencyKey == idempotencyKey,
                cancellationToken);
        if (alreadyApplied)
        {
            return evaluation with { OutcomeSummary = "Automation already applied (idempotent replay)." };
        }

        foreach (var change in winningChanges)
        {
            await ApplyProposedChangeAsync(context, task, change, now, actor, cancellationToken);
            appliedActions.Add(change);
        }

        foreach (var action in evaluation.ActionsApplied)
        {
            if (action.Type == AiTaskAutomationActionTypes.NotifyTeamsChannel && action.GuidValue.HasValue)
            {
                await TryNotifyTeamsAsync(context, task, action.GuidValue.Value, cancellationToken);
            }
        }

        task.AutomationRuleId = matchedRule.RuleId;
        task.AutomationRuleVersion = matchedRule.RuleVersion;
        task.AutomationMetadataJson = Serialize(new
        {
            triggerType,
            evaluation.MatchedRules,
            evaluation.Conflicts,
            appliedActions,
        });

        var execution = new AiTaskAutomationExecution
        {
            Id = Guid.NewGuid(),
            TenantId = context.TenantId,
            FacilityId = context.FacilityId,
            TaskId = task.Id,
            RuleId = matchedRule.RuleId,
            RuleVersion = matchedRule.RuleVersion,
            TriggerType = triggerType,
            IdempotencyKey = idempotencyKey,
            MatchedConditionsJson = Serialize(evaluation.MatchedRules),
            ActionsAppliedJson = Serialize(appliedActions),
            ConflictsJson = Serialize(evaluation.Conflicts),
            OutcomeJson = Serialize(new { evaluation.OutcomeSummary, winningChanges }),
            Simulated = false,
            CorrelationId = correlationId,
            ExecutedAtUtc = now,
        };
        dbContext.AiTaskAutomationExecutions.Add(execution);

        var ruleEntity = rules.FirstOrDefault(r => r.Id == matchedRule.RuleId);
        if (ruleEntity is not null)
        {
            ruleEntity.LastMatchedAtUtc = now;
        }

        AppendAuditEvent(
            context,
            task,
            AiTaskAuditEventTypes.AutomationApplied,
            actor,
            "Automation Engine",
            now,
            metadata: new
            {
                ruleId = matchedRule.RuleId,
                ruleVersion = matchedRule.RuleVersion,
                triggerType,
                executionId = execution.Id,
                appliedActions,
                evaluation.Conflicts,
            },
            note: evaluation.OutcomeSummary,
            correlationId: correlationId);

        return evaluation;
    }

    public Task<IReadOnlyList<AiTaskAutomationExecutionReadModel>> ListExecutionsAsync(
        AiTaskOperationContext context,
        Guid? taskId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(
            async ct =>
            {
                var query = dbContext.AiTaskAutomationExecutions.AsNoTracking()
                    .Where(e => e.TenantId == context.TenantId && e.FacilityId == context.FacilityId && !e.Simulated);

                if (taskId.HasValue)
                {
                    query = query.Where(e => e.TaskId == taskId.Value);
                }

                var executions = await query
                    .OrderByDescending(e => e.ExecutedAtUtc)
                    .Skip(Math.Max(0, page - 1) * pageSize)
                    .Take(pageSize)
                    .Join(
                        dbContext.AiTaskAutomationRules.AsNoTracking(),
                        execution => execution.RuleId,
                        rule => rule.Id,
                        (execution, rule) => new { execution, rule.Name })
                    .ToListAsync(ct);

                return (IReadOnlyList<AiTaskAutomationExecutionReadModel>)executions
                    .Select(item =>
                    {
                        var actions = DeserializeStringList(item.execution.ActionsAppliedJson);
                        return new AiTaskAutomationExecutionReadModel(
                            item.execution.Id,
                            item.execution.TaskId,
                            item.execution.RuleId,
                            item.Name,
                            item.execution.RuleVersion,
                            item.execution.TriggerType,
                            item.execution.ExecutedAtUtc,
                            actions,
                            item.execution.Simulated);
                    })
                    .ToList();
            },
            cancellationToken);

    public Task RecordManualOverrideAsync(
        AiTaskOperationContext context,
        AiTask task,
        IReadOnlyList<string> scopes,
        string reason,
        bool allowAutomationResume,
        DateTimeOffset now,
        string actor,
        CancellationToken cancellationToken = default)
    {
        var state = new AiTaskAutomationOverrideState(
            scopes,
            actor,
            reason.Trim(),
            now,
            allowAutomationResume);
        task.AutomationOverrideJson = Serialize(state);

        AppendAuditEvent(
            context,
            task,
            AiTaskAuditEventTypes.AutomationOverrideRecorded,
            actor,
            actor,
            now,
            metadata: state,
            note: reason);

        return Task.CompletedTask;
    }

    private async Task ApplyProposedChangeAsync(
        AiTaskOperationContext context,
        AiTask task,
        AiTaskAutomationProposedChange change,
        DateTimeOffset now,
        string actor,
        CancellationToken ct)
    {
        switch (change.Field)
        {
            case "assignment" when change.ProposedValue?.StartsWith("team:", StringComparison.Ordinal) == true:
            {
                var teamId = Guid.Parse(change.ProposedValue["team:".Length..]);
                await ValidateTeamChannelAsync(context, teamId, ct);
                task.AssignedTeamChannelId = teamId;
                task.AssignedMembershipId = null;
                task.AssigneeJson = null;
                task.AssignmentSource = AiTaskAssignmentSources.Automation;
                task.AssignmentReason = change.SourceRuleName is null
                    ? "Automation assignment."
                    : $"Automation rule '{change.SourceRuleName}'.";
                break;
            }
            case "assignment" when change.ProposedValue?.StartsWith("membership:", StringComparison.Ordinal) == true:
            {
                var membershipId = Guid.Parse(change.ProposedValue["membership:".Length..]);
                await ValidateMembershipAsync(context, membershipId, ct);
                task.AssignedMembershipId = membershipId;
                task.AssignedTeamChannelId = null;
                task.AssigneeJson = Serialize(await ResolveAssigneeModelAsync(context, membershipId, ct));
                task.AssignmentSource = AiTaskAssignmentSources.Automation;
                task.AssignmentReason = change.SourceRuleName is null
                    ? "Automation assignment."
                    : $"Automation rule '{change.SourceRuleName}'.";
                break;
            }
            case "priority" when !string.IsNullOrWhiteSpace(change.ProposedValue):
                task.Priority = change.ProposedValue!;
                break;
            case "slaPolicy" when !string.IsNullOrWhiteSpace(change.ProposedValue):
            {
                var policy = await dbContext.AiTaskSlaPolicies.AsNoTracking()
                    .SingleOrDefaultAsync(
                        p => p.TenantId == context.TenantId
                             && p.PolicyKey == change.ProposedValue
                             && p.Enabled,
                        ct);
                if (policy is not null)
                {
                    task.SlaPolicyId = policy.Id;
                    if (!task.DueAtUtc.HasValue)
                    {
                        task.DueAtUtc = task.CreatedAtUtc.AddMinutes(policy.ResolutionTargetMinutes);
                    }
                }

                break;
            }
            case "evidenceRequired":
                task.MinimumEvidenceRequired = int.TryParse(change.ProposedValue, out var minEvidence) ? minEvidence : 1;
                break;
            case "closureApprovalRequired":
                task.ClosureApprovalRequired = true;
                break;
            case "fourEyesRequired":
                task.FourEyesRequired = true;
                task.ClosureApprovalRequired = true;
                break;
            case "closurePolicy" when !string.IsNullOrWhiteSpace(change.ProposedValue):
            {
                var closurePolicy = await dbContext.AiTaskClosurePolicies.AsNoTracking()
                    .SingleOrDefaultAsync(
                        p => p.TenantId == context.TenantId
                             && p.PolicyKey == change.ProposedValue
                             && p.Enabled,
                        ct);
                if (closurePolicy is not null)
                {
                    task.ClosurePolicyId = closurePolicy.Id;
                    task.MinimumEvidenceRequired = Math.Max(task.MinimumEvidenceRequired, closurePolicy.MinimumEvidenceCount);
                    task.ClosureApprovalRequired = task.ClosureApprovalRequired || closurePolicy.ApprovalRequired;
                    task.FourEyesRequired = task.FourEyesRequired || closurePolicy.FourEyesRequired;
                }

                break;
            }
            case "tag" when !string.IsNullOrWhiteSpace(change.ProposedValue):
            {
                var tags = DeserializeStringList(task.AutomationTagsJson ?? "[]").ToList();
                if (!tags.Contains(change.ProposedValue, StringComparer.Ordinal))
                {
                    tags.Add(change.ProposedValue);
                    task.AutomationTagsJson = Serialize(tags);
                }

                break;
            }
        }
    }

    private async Task TryNotifyTeamsAsync(
        AiTaskOperationContext context,
        AiTask task,
        Guid teamChannelId,
        CancellationToken ct)
    {
        var idempotencyKey = $"automation-teams:{task.Id:D}:{teamChannelId:D}";
        var alreadyNotified = await dbContext.AiTaskAuditEvents.AsNoTracking()
            .AnyAsync(
                e => e.TenantId == context.TenantId
                     && e.TaskId == task.Id
                     && e.EventType == AiTaskAuditEventTypes.TeamsNotificationEmitted
                     && e.MetadataJson != null
                     && e.MetadataJson.Contains(idempotencyKey, StringComparison.Ordinal),
                ct);
        if (alreadyNotified)
        {
            return;
        }

        var teamsContext = new TeamsOperationContext(
            context.TenantId,
            context.FacilityId,
            context.MembershipId,
            context.IdentitySubjectId,
            context.PermissionKey,
            context.CorrelationId);

        await teamsNotificationRepository.TryCreateTaskAssignedAsync(
            teamsContext,
            context.MembershipId,
            task.Id,
            task.TaskReference,
            ct);

        AppendAuditEvent(
            context,
            task,
            AiTaskAuditEventTypes.TeamsNotificationEmitted,
            context.IdentitySubjectId.ToString(),
            "Automation Engine",
            DateTimeOffset.UtcNow,
            metadata: new { notificationType = "automation-channel", teamChannelId, idempotencyKey },
            note: "Automation Teams notification.");
    }

    private async Task<IReadOnlyList<AiTaskAutomationRule>> LoadRulesForEvaluationAsync(
        AiTaskOperationContext context,
        Guid? ruleId,
        CancellationToken ct)
    {
        var query = dbContext.AiTaskAutomationRules.AsNoTracking()
            .Where(r => r.TenantId == context.TenantId
                        && (r.FacilityId == null || r.FacilityId == context.FacilityId));

        if (ruleId.HasValue)
        {
            query = query.Where(r => r.Id == ruleId.Value);
        }

        return await query
            .OrderBy(r => r.SortOrder)
            .ThenBy(r => r.RuleKey)
            .ToListAsync(ct);
    }

    private async Task<AiTask?> LoadTaskForEvaluationAsync(
        AiTaskOperationContext context,
        Guid taskId,
        CancellationToken ct) =>
        await dbContext.AiTasks.AsNoTracking()
            .SingleOrDefaultAsync(
                t => t.TenantId == context.TenantId
                     && t.FacilityId == context.FacilityId
                     && t.Id == taskId,
                ct);

    private static string? ValidateRulePayload(
        string triggerType,
        IReadOnlyList<AiTaskAutomationCondition> conditions,
        IReadOnlyList<AiTaskAutomationAction> actions)
    {
        if (!AiTaskAutomationTriggers.All.Contains(triggerType.Trim()))
        {
            return "A valid trigger type is required.";
        }

        if (conditions.Count == 0)
        {
            return "At least one condition is required.";
        }

        if (actions.Count == 0)
        {
            return "At least one action is required.";
        }

        foreach (var condition in conditions)
        {
            if (!AiTaskAutomationConditionFields.All.Contains(condition.Field))
            {
                return $"Unsupported condition field: {condition.Field}";
            }

            if (!AiTaskAutomationConditionOperators.All.Contains(condition.Operator))
            {
                return $"Unsupported condition operator: {condition.Operator}";
            }
        }

        foreach (var action in actions)
        {
            if (!AiTaskAutomationActionTypes.All.Contains(action.Type))
            {
                return $"Unsupported action type: {action.Type}";
            }
        }

        return null;
    }

    private static AiTaskAutomationRuleRevision BuildRevision(
        AiTaskAutomationRule rule,
        Guid revisionId,
        string actor,
        DateTimeOffset now,
        string reason) =>
        new()
        {
            Id = revisionId,
            TenantId = rule.TenantId,
            RuleId = rule.Id,
            Version = rule.Version,
            Name = rule.Name,
            Description = rule.Description,
            Enabled = rule.Enabled,
            SortOrder = rule.SortOrder,
            TriggerType = rule.TriggerType,
            ConditionsJson = rule.ConditionsJson,
            ActionsJson = rule.ActionsJson,
            EffectiveFromUtc = rule.EffectiveFromUtc,
            EffectiveUntilUtc = rule.EffectiveUntilUtc,
            ChangedBy = actor,
            ChangedAtUtc = now,
            ChangeReason = reason,
        };

    private static AiTaskAutomationRuleSummaryReadModel MapSummary(AiTaskAutomationRule rule)
    {
        var actions = AiTaskAutomationEngine.DeserializeActions(rule.ActionsJson);
        var summaries = actions.Select(a => SummarizeAction(a)).ToList();
        return new AiTaskAutomationRuleSummaryReadModel(
            rule.Id,
            rule.RuleKey,
            rule.Name,
            rule.Description,
            rule.FacilityId,
            rule.TriggerType,
            rule.SortOrder,
            rule.Enabled,
            rule.Version,
            rule.UpdatedAtUtc,
            rule.LastMatchedAtUtc,
            summaries);
    }

    private static AiTaskAutomationRuleDetailReadModel MapDetail(AiTaskAutomationRule rule) =>
        new(
            rule.Id,
            rule.RuleKey,
            rule.Name,
            rule.Description,
            rule.FacilityId,
            rule.TriggerType,
            rule.SortOrder,
            rule.Enabled,
            rule.Version,
            AiTaskAutomationEngine.DeserializeConditions(rule.ConditionsJson),
            AiTaskAutomationEngine.DeserializeActions(rule.ActionsJson),
            rule.EffectiveFromUtc,
            rule.EffectiveUntilUtc,
            rule.CreatedAtUtc,
            rule.UpdatedAtUtc,
            rule.LastMatchedAtUtc,
            rule.CreatedBy,
            rule.UpdatedBy);

    private static string SummarizeAction(AiTaskAutomationAction action) =>
        action.Type switch
        {
            AiTaskAutomationActionTypes.AssignToTeam => $"Assign team {action.GuidValue:D}",
            AiTaskAutomationActionTypes.ApplySlaPolicy => $"SLA {action.Value}",
            AiTaskAutomationActionTypes.RequireEvidence => "Require evidence",
            AiTaskAutomationActionTypes.RequireClosureApproval => "Require closure approval",
            AiTaskAutomationActionTypes.SetPriority => $"Priority {action.Value}",
            _ => action.Type,
        };

    private async Task ValidateTeamChannelAsync(AiTaskOperationContext context, Guid teamChannelId, CancellationToken ct)
    {
        var valid = await dbContext.TeamsChannels.AsNoTracking()
            .AnyAsync(
                c => c.TenantId == context.TenantId
                     && c.FacilityId == context.FacilityId
                     && c.Id == teamChannelId,
                ct);
        if (!valid)
        {
            throw new InvalidOperationException("Team channel is not valid for this facility.");
        }
    }

    private async Task ValidateMembershipAsync(AiTaskOperationContext context, Guid membershipId, CancellationToken ct)
    {
        var valid = await dbContext.FacilityMemberships.AsNoTracking()
            .AnyAsync(
                fm => fm.TenantId == context.TenantId
                      && fm.FacilityId == context.FacilityId
                      && fm.TenantMembershipId == membershipId,
                ct);
        if (!valid)
        {
            throw new InvalidOperationException("Membership is not valid for this facility.");
        }
    }

    private async Task<AiTaskAssigneeModel> ResolveAssigneeModelAsync(
        AiTaskOperationContext context,
        Guid membershipId,
        CancellationToken ct)
    {
        var membership = await dbContext.TenantMemberships.AsNoTracking()
            .SingleAsync(m => m.TenantId == context.TenantId && m.Id == membershipId, ct);
        var account = await dbContext.SaasIdentityAccounts.AsNoTracking()
            .SingleOrDefaultAsync(a => a.IdentitySubjectId == membership.IdentitySubjectId, ct);
        var displayName = string.IsNullOrWhiteSpace(account?.DisplayName)
            ? membership.MembershipReference
            : account.DisplayName;
        return new AiTaskAssigneeModel(membershipId.ToString("D"), displayName, membership.MembershipReference);
    }

    private static string BuildIdempotencyKey(Guid taskId, string trigger, Guid ruleId, int ruleVersion, uint taskVersion) =>
        $"automation:{taskId:D}:{trigger}:{ruleId:D}:{ruleVersion}:{taskVersion}";

    private static AiTaskAutomationOverrideState? DeserializeOverride(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<AiTaskAutomationOverrideState>(json, JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static IReadOnlyList<string> DeserializeStringList(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<List<string>>(json, JsonOptions) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static string Serialize<T>(T value) => JsonSerializer.Serialize(value, JsonOptions);

    private static string? TrimOrNull(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static void AppendAuditEvent(
        AiTaskOperationContext context,
        AiTask task,
        string eventType,
        string actorId,
        string actorLabel,
        DateTimeOffset occurredAtUtc,
        object? metadata = null,
        string? note = null,
        Guid? correlationId = null)
    {
        task.AuditEvents.Add(new AiTaskAuditEvent
        {
            Id = Guid.NewGuid(),
            TenantId = context.TenantId,
            FacilityId = context.FacilityId,
            TaskId = task.Id,
            EventType = eventType,
            OccurredAtUtc = occurredAtUtc,
            ActorType = AiTaskActorTypes.System,
            ActorId = actorId,
            ActorLabel = actorLabel,
            Note = note,
            CorrelationId = correlationId ?? context.CorrelationId,
            MetadataJson = metadata is null ? null : Serialize(metadata),
        });
    }
}
