using System.Text.Json;
using CrevaFactOS.Persistence.Facilities;
using CrevaFactOS.Persistence.Identity;
using CrevaFactOS.Persistence.SaasIdentity;
using CrevaFactOS.Persistence.Teams;
using CrevaFactOS.Persistence.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace CrevaFactOS.Persistence.AiTasks;

public sealed class AiTaskOperationsRepository(
    CrevaFactOSDbContext dbContext,
    TenantDatabaseSession tenantDatabaseSession,
    IAiTaskRoutingService routingService,
    ITeamsNotificationRepository teamsNotificationRepository) : IAiTaskOperationsRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public Task<AiTaskWriteResult> AssignTaskAsync(
        AiTaskOperationContext context,
        AssignAiTaskCommand command,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteWriteAsync(
            async ct => await MutateTaskAsync(
                context,
                command.TaskId,
                command.ExpectedVersion,
                async (task, now, actor) =>
                {
                    if (command.TargetMembershipId is null && command.TargetTeamChannelId is null)
                    {
                        return AiTaskWriteResult.Fail(
                            AiTaskFailureCodes.ValidationFailed,
                            "An assignment target is required.");
                    }

                    var validationError = await ValidateAssignmentTargetsAsync(context, command, ct);
                    if (validationError is not null)
                    {
                        return AiTaskWriteResult.Fail(AiTaskFailureCodes.ValidationFailed, validationError);
                    }

                    var previousMembership = task.AssignedMembershipId;
                    var previousTeam = task.AssignedTeamChannelId;
                    var isReassignment = previousMembership.HasValue || previousTeam.HasValue || task.AssigneeJson is not null;
                    var source = string.IsNullOrWhiteSpace(command.Source)
                        ? (isReassignment ? AiTaskAssignmentSources.Reassignment : AiTaskAssignmentSources.Manual)
                        : command.Source.Trim();

                    task.AssignedMembershipId = command.TargetMembershipId;
                    task.AssignedTeamChannelId = command.TargetTeamChannelId;
                    task.AssignmentReason = TrimOrNull(command.Reason);
                    task.AssignmentSource = source;
                    task.AssigneeJson = command.TargetMembershipId is null
                        ? null
                        : Serialize(await ResolveAssigneeModelAsync(context, command.TargetMembershipId.Value, ct));

                    if (isReassignment)
                    {
                        AppendAuditEvent(
                            context,
                            task,
                            AiTaskAuditEventTypes.Reassigned,
                            actor,
                            actor,
                            now,
                            metadata: new
                            {
                                previousMembershipId = previousMembership,
                                previousTeamChannelId = previousTeam,
                                newMembershipId = command.TargetMembershipId,
                                newTeamChannelId = command.TargetTeamChannelId,
                                reason = command.Reason,
                                source,
                            },
                            note: command.Reason);
                    }
                    else
                    {
                        AppendAuditEvent(
                            context,
                            task,
                            AiTaskAuditEventTypes.Assigned,
                            actor,
                            actor,
                            now,
                            metadata: new
                            {
                                membershipId = command.TargetMembershipId,
                                teamChannelId = command.TargetTeamChannelId,
                                reason = command.Reason,
                                source,
                            },
                            note: command.Reason);
                    }

                    if (command.TargetMembershipId.HasValue
                        && command.TargetMembershipId.Value != context.MembershipId)
                    {
                        await TryNotifyAssignmentAsync(context, task, command.TargetMembershipId.Value, ct);
                    }

                    return AiTaskWriteResult.Success(task.Id, task.Version);
                },
                ct),
            cancellationToken);

    public Task<AiTaskWriteResult> AcknowledgeTaskAsync(
        AiTaskOperationContext context,
        AcknowledgeAiTaskCommand command,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteWriteAsync(
            async ct => await MutateTaskAsync(
                context,
                command.TaskId,
                command.ExpectedVersion,
                async (task, now, actor) =>
                {
                    if (task.AcknowledgedAtUtc.HasValue)
                    {
                        return AiTaskWriteResult.Fail(
                            AiTaskFailureCodes.ValidationFailed,
                            "Task is already acknowledged.");
                    }

                    if (task.AssignedMembershipId.HasValue
                        && task.AssignedMembershipId.Value != context.MembershipId)
                    {
                        return AiTaskWriteResult.Fail(
                            AiTaskFailureCodes.FacilityScopeDenied,
                            "Only the assigned member can acknowledge this task.");
                    }

                    task.AcknowledgedAtUtc = now;
                    if (string.Equals(task.Status, AiTaskStatuses.Draft, StringComparison.Ordinal)
                        || string.Equals(task.Status, AiTaskStatuses.AwaitingReview, StringComparison.Ordinal))
                    {
                        var previousStatus = task.Status;
                        task.Status = AiTaskStatuses.Approved;
                        AppendAuditEvent(
                            context,
                            task,
                            AiTaskAuditEventTypes.StatusChanged,
                            actor,
                            actor,
                            now,
                            previousStatus,
                            task.Status,
                            note: command.Note);
                    }

                    AppendAuditEvent(
                        context,
                        task,
                        AiTaskAuditEventTypes.Acknowledged,
                        actor,
                        actor,
                        now,
                        metadata: new { acknowledgedAt = now },
                        note: command.Note);

                    return AiTaskWriteResult.Success(task.Id, task.Version);
                },
                ct),
            cancellationToken);

    public Task<AiTaskWriteResult> ChangePriorityAsync(
        AiTaskOperationContext context,
        ChangeAiTaskPriorityCommand command,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteWriteAsync(
            async ct => await MutateTaskAsync(
                context,
                command.TaskId,
                command.ExpectedVersion,
                async (task, now, actor) =>
                {
                    var priority = command.Priority.Trim();
                    if (!AiTaskPriorities.All.Contains(priority))
                    {
                        return AiTaskWriteResult.Fail(
                            AiTaskFailureCodes.ValidationFailed,
                            "A valid priority is required.");
                    }

                    var previous = task.Priority;
                    task.Priority = priority;
                    await ApplySlaPolicyAsync(task, ct);

                    AppendAuditEvent(
                        context,
                        task,
                        AiTaskAuditEventTypes.PriorityChanged,
                        actor,
                        actor,
                        now,
                        metadata: new { previousPriority = previous, newPriority = priority, reason = command.Reason },
                        note: command.Reason);

                    return AiTaskWriteResult.Success(task.Id, task.Version);
                },
                ct),
            cancellationToken);

    public Task<AiTaskWriteResult> ChangeDueDateAsync(
        AiTaskOperationContext context,
        ChangeAiTaskDueDateCommand command,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteWriteAsync(
            async ct => await MutateTaskAsync(
                context,
                command.TaskId,
                command.ExpectedVersion,
                async (task, now, actor) =>
                {
                    var previous = task.DueAtUtc;
                    task.DueAtUtc = command.DueAtUtc;

                    AppendAuditEvent(
                        context,
                        task,
                        AiTaskAuditEventTypes.DueDateChanged,
                        actor,
                        actor,
                        now,
                        metadata: new { previousDueAtUtc = previous, newDueAtUtc = command.DueAtUtc, reason = command.Reason },
                        note: command.Reason);

                    return AiTaskWriteResult.Success(task.Id, task.Version);
                },
                ct),
            cancellationToken);

    public Task<AiTaskWriteResult> EscalateTaskAsync(
        AiTaskOperationContext context,
        EscalateAiTaskCommand command,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteWriteAsync(
            async ct =>
            {
                var task = await LoadMutableTaskAsync(context, command.TaskId, command.ExpectedVersion, ct);
                if (task.Error is not null)
                {
                    return task.Error;
                }

                var result = await CreateEscalationAsync(
                    context,
                    task.Entity!,
                    command.BreachType.Trim(),
                    command.Note,
                    manual: true,
                    ct);
                if (result.Succeeded)
                {
                    await dbContext.SaveChangesAsync(ct);
                }

                return result;
            },
            cancellationToken);

    public Task<AiTaskEscalationEvaluationResult> EvaluateEscalationsAsync(
        AiTaskOperationContext context,
        int maxTasks,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteWriteAsync(
            async ct =>
            {
                var bounded = maxTasks is < 1 or > 200 ? 50 : maxTasks;
                var now = DateTimeOffset.UtcNow;
                var policies = await dbContext.AiTaskSlaPolicies.AsNoTracking()
                    .Where(p => p.TenantId == context.TenantId && p.Enabled)
                    .ToListAsync(ct);

                var finalStatuses = AiTaskStatuses.Final.ToArray();
                var openTasks = await dbContext.AiTasks
                    .Where(t => t.TenantId == context.TenantId
                                && t.FacilityId == context.FacilityId
                                && !finalStatuses.Contains(t.Status))
                    .OrderBy(t => t.DueAtUtc ?? t.CreatedAtUtc)
                    .ToListAsync(ct);

                var breachCandidates = openTasks
                    .Select(task => new
                    {
                        Task = task,
                        Policy = AiTaskSlaEngine.ResolvePolicy(task, policies),
                    })
                    .Select(x => new
                    {
                        x.Task,
                        Sla = AiTaskSlaEngine.DeriveSlaState(x.Task, x.Policy, now),
                    })
                    .Where(x => x.Sla.BreachType is not null)
                    .OrderBy(x => x.Task.CreatedAtUtc)
                    .Take(bounded)
                    .Select(x => x.Task)
                    .ToList();

                var escalated = new List<Guid>();
                foreach (var task in breachCandidates)
                {
                    var policy = AiTaskSlaEngine.ResolvePolicy(task, policies);
                    var sla = AiTaskSlaEngine.DeriveSlaState(task, policy, now);
                    if (sla.BreachType is null)
                    {
                        continue;
                    }

                    var result = await CreateEscalationAsync(
                        context,
                        task,
                        sla.BreachType,
                        $"Automatic escalation for {sla.BreachType} breach.",
                        manual: false,
                        ct);
                    if (result.Succeeded && !result.IdempotentReplay)
                    {
                        escalated.Add(task.Id);
                    }
                }

                await dbContext.SaveChangesAsync(ct);
                return new AiTaskEscalationEvaluationResult(breachCandidates.Count, escalated.Count, escalated);
            },
            cancellationToken);

    public async Task ApplyRoutingOnCreateAsync(
        AiTaskOperationContext context,
        AiTask task,
        DateTimeOffset now,
        string actor,
        CancellationToken cancellationToken = default)
    {
        var decision = await routingService.EvaluateRoutingAsync(context, task, cancellationToken);
        if (decision is not null)
        {
            task.RoutingRuleId = decision.RuleId;
            task.RoutingMetadataJson = Serialize(decision);

            AppendAuditEvent(
                context,
                task,
                AiTaskAuditEventTypes.Routed,
                actor,
                "AI Task Routing",
                now,
                metadata: decision,
                note: decision.Reason);

            if (string.Equals(decision.TargetType, AiTaskRoutingTargetTypes.Membership, StringComparison.Ordinal)
                && decision.TargetMembershipId.HasValue)
            {
                task.AssignedMembershipId = decision.TargetMembershipId;
                task.AssignmentSource = AiTaskAssignmentSources.Routing;
                task.AssignmentReason = decision.Reason;
                task.AssigneeJson = Serialize(
                    await ResolveAssigneeModelAsync(context, decision.TargetMembershipId.Value, cancellationToken));

                AppendAuditEvent(
                    context,
                    task,
                    AiTaskAuditEventTypes.Assigned,
                    actor,
                    "AI Task Routing",
                    now,
                    metadata: decision,
                    note: decision.Reason);
            }
            else if (string.Equals(decision.TargetType, AiTaskRoutingTargetTypes.TeamChannel, StringComparison.Ordinal)
                     && decision.TargetTeamChannelId.HasValue)
            {
                task.AssignedTeamChannelId = decision.TargetTeamChannelId;
                task.AssignmentSource = AiTaskAssignmentSources.Routing;
                task.AssignmentReason = decision.Reason;

                AppendAuditEvent(
                    context,
                    task,
                    AiTaskAuditEventTypes.Assigned,
                    actor,
                    "AI Task Routing",
                    now,
                    metadata: decision,
                    note: decision.Reason);
            }
        }

        await ApplySlaPolicyAsync(task, cancellationToken);
    }

    private async Task<AiTaskWriteResult> CreateEscalationAsync(
        AiTaskOperationContext context,
        AiTask task,
        string breachType,
        string? note,
        bool manual,
        CancellationToken ct)
    {
        if (!AiTaskEscalationBreachTypes.All.Contains(breachType))
        {
            return AiTaskWriteResult.Fail(AiTaskFailureCodes.ValidationFailed, "Invalid breach type.");
        }

        var idempotencyKey = $"escalation:{task.Id:D}:{breachType}";
        var exists = await dbContext.AiTaskEscalationEvents.AsNoTracking()
            .AnyAsync(
                e => e.TenantId == context.TenantId
                     && e.TaskId == task.Id
                     && e.IdempotencyKey == idempotencyKey,
                ct);
        if (exists)
        {
            return AiTaskWriteResult.Success(task.Id, task.Version, idempotentReplay: true);
        }

        var nextLevel = manual
            ? Math.Min(task.EscalationLevel + 1, AiTaskEscalationLevels.Critical)
            : Math.Max(task.EscalationLevel + 1, 1);
        if (!manual && nextLevel == task.EscalationLevel)
        {
            return AiTaskWriteResult.Fail(AiTaskFailureCodes.ValidationFailed, "Task is already at maximum escalation.");
        }

        var now = DateTimeOffset.UtcNow;
        var actor = context.IdentitySubjectId.ToString();
        task.EscalationLevel = nextLevel;
        task.EscalationState = AiTaskEscalationStates.Active;
        task.LastEscalatedAtUtc = now;
        task.UpdatedAtUtc = now;
        task.UpdatedBy = actor;
        task.Version++;

        dbContext.AiTaskEscalationEvents.Add(new AiTaskEscalationEvent
        {
            Id = Guid.NewGuid(),
            TenantId = context.TenantId,
            FacilityId = context.FacilityId,
            TaskId = task.Id,
            EscalationLevel = nextLevel,
            BreachType = breachType,
            IdempotencyKey = idempotencyKey,
            OccurredAtUtc = now,
            ActorId = actor,
            Note = TrimOrNull(note),
        });

        AppendAuditEvent(
            context,
            task,
            AiTaskAuditEventTypes.Escalated,
            actor,
            actor,
            now,
            metadata: new { escalationLevel = nextLevel, breachType, manual },
            note: note);

        if (task.AssignedMembershipId.HasValue
            && task.AssignedMembershipId.Value != context.MembershipId)
        {
            await TryNotifyEscalationAsync(context, task, task.AssignedMembershipId.Value, nextLevel, ct);
        }

        return AiTaskWriteResult.Success(task.Id, task.Version);
    }

    private async Task<AiTaskWriteResult> MutateTaskAsync(
        AiTaskOperationContext context,
        Guid taskId,
        uint expectedVersion,
        Func<AiTask, DateTimeOffset, string, Task<AiTaskWriteResult>> mutate,
        CancellationToken ct)
    {
        var loaded = await LoadMutableTaskAsync(context, taskId, expectedVersion, ct);
        if (loaded.Error is not null)
        {
            return loaded.Error;
        }

        var now = DateTimeOffset.UtcNow;
        var actor = context.IdentitySubjectId.ToString();
        var result = await mutate(loaded.Entity!, now, actor);
        if (!result.Succeeded)
        {
            return result;
        }

        loaded.Entity!.UpdatedAtUtc = now;
        loaded.Entity.UpdatedBy = actor;
        loaded.Entity.Version++;
        await dbContext.SaveChangesAsync(ct);
        return AiTaskWriteResult.Success(loaded.Entity.Id, loaded.Entity.Version);
    }

    private async Task<(AiTask? Entity, AiTaskWriteResult? Error)> LoadMutableTaskAsync(
        AiTaskOperationContext context,
        Guid taskId,
        uint expectedVersion,
        CancellationToken ct)
    {
        var task = await dbContext.AiTasks
            .SingleOrDefaultAsync(
                t => t.TenantId == context.TenantId
                     && t.FacilityId == context.FacilityId
                     && t.Id == taskId,
                ct);

        if (task is null)
        {
            return (null, AiTaskWriteResult.Fail(AiTaskFailureCodes.NotFound, "AI task not found."));
        }

        if (task.Version != expectedVersion)
        {
            return (null, AiTaskWriteResult.Fail(
                AiTaskFailureCodes.ConcurrencyConflict,
                "AI task was modified by another request."));
        }

        if (AiTaskStatuses.Final.Contains(task.Status))
        {
            return (null, AiTaskWriteResult.Fail(AiTaskFailureCodes.FinalStatus, "AI task is in a final status."));
        }

        return (task, null);
    }

    private async Task<string?> ValidateAssignmentTargetsAsync(
        AiTaskOperationContext context,
        AssignAiTaskCommand command,
        CancellationToken ct)
    {
        if (command.TargetMembershipId is Guid membershipId)
        {
            var membershipExists = await dbContext.TenantMemberships.AsNoTracking()
                .AnyAsync(
                    m => m.TenantId == context.TenantId
                         && m.Id == membershipId
                         && m.Status == TenantMembershipStatus.Active,
                    ct);
            if (!membershipExists)
            {
                return "Assigned membership is not valid for this tenant.";
            }

            var facilityMembership = await dbContext.FacilityMemberships.AsNoTracking()
                .AnyAsync(
                    fm => fm.TenantId == context.TenantId
                          && fm.FacilityId == context.FacilityId
                          && fm.TenantMembershipId == membershipId
                          && fm.Status == FacilityMembershipStatus.Active,
                    ct);
            if (!facilityMembership)
            {
                return "Assigned membership does not have access to this facility.";
            }
        }

        if (command.TargetTeamChannelId is Guid channelId)
        {
            var channelExists = await dbContext.TeamsChannels.AsNoTracking()
                .AnyAsync(
                    c => c.TenantId == context.TenantId
                         && c.FacilityId == context.FacilityId
                         && c.Id == channelId,
                    ct);
            if (!channelExists)
            {
                return "Assigned team channel is not valid for this facility.";
            }
        }

        if (!AiTaskAssignmentSources.All.Contains(command.Source.Trim()))
        {
            return "A valid assignment source is required.";
        }

        return null;
    }

    private async Task ApplySlaPolicyAsync(AiTask task, CancellationToken ct)
    {
        var policies = await dbContext.AiTaskSlaPolicies.AsNoTracking()
            .Where(p => p.TenantId == task.TenantId && p.Enabled)
            .ToListAsync(ct);
        var policy = AiTaskSlaEngine.ResolvePolicy(task, policies);
        if (policy.Id != Guid.Empty)
        {
            task.SlaPolicyId = policy.Id;
        }

        if (!task.DueAtUtc.HasValue)
        {
            task.DueAtUtc = task.CreatedAtUtc.AddMinutes(policy.ResolutionTargetMinutes);
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

        var displayName = account?.DisplayName;
        if (string.IsNullOrWhiteSpace(displayName))
        {
            displayName = membership.MembershipReference;
        }

        return new AiTaskAssigneeModel(
            membershipId.ToString("D"),
            displayName,
            membership.MembershipReference);
    }

    private async Task TryNotifyAssignmentAsync(
        AiTaskOperationContext context,
        AiTask task,
        Guid recipientMembershipId,
        CancellationToken ct)
    {
        var teamsContext = new TeamsOperationContext(
            context.TenantId,
            context.FacilityId,
            context.MembershipId,
            context.IdentitySubjectId,
            context.PermissionKey,
            context.CorrelationId);

        await teamsNotificationRepository.TryCreateTaskAssignedAsync(
            teamsContext,
            recipientMembershipId,
            task.Id,
            task.TaskReference,
            ct);

        AppendAuditEvent(
            context,
            task,
            AiTaskAuditEventTypes.TeamsNotificationEmitted,
            context.IdentitySubjectId.ToString(),
            context.IdentitySubjectId.ToString(),
            DateTimeOffset.UtcNow,
            metadata: new { notificationType = TeamsNotificationTypes.TaskAssigned, recipientMembershipId },
            note: "Task assignment notification emitted.");
    }

    private async Task TryNotifyEscalationAsync(
        AiTaskOperationContext context,
        AiTask task,
        Guid recipientMembershipId,
        int escalationLevel,
        CancellationToken ct)
    {
        var teamsContext = new TeamsOperationContext(
            context.TenantId,
            context.FacilityId,
            context.MembershipId,
            context.IdentitySubjectId,
            context.PermissionKey,
            context.CorrelationId);

        await teamsNotificationRepository.TryCreateTaskEscalatedAsync(
            teamsContext,
            recipientMembershipId,
            task.Id,
            task.TaskReference,
            escalationLevel,
            ct);
    }

    private void AppendAuditEvent(
        AiTaskOperationContext context,
        AiTask task,
        string eventType,
        string actorId,
        string actorLabel,
        DateTimeOffset occurredAtUtc,
        string? previousStatus = null,
        string? nextStatus = null,
        object? metadata = null,
        string? note = null)
    {
        dbContext.AiTaskAuditEvents.Add(new AiTaskAuditEvent
        {
            Id = Guid.NewGuid(),
            TenantId = context.TenantId,
            FacilityId = context.FacilityId,
            TaskId = task.Id,
            EventType = eventType,
            OccurredAtUtc = occurredAtUtc,
            ActorType = AiTaskActorTypes.User,
            ActorId = actorId,
            ActorLabel = actorLabel,
            Note = TrimOrNull(note),
            PreviousStatus = previousStatus,
            NextStatus = nextStatus,
            CorrelationId = context.CorrelationId,
            MetadataJson = metadata is null ? null : Serialize(metadata),
        });
    }

    private static string Serialize<T>(T value) =>
        JsonSerializer.Serialize(value, JsonOptions);

    private static string? TrimOrNull(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
