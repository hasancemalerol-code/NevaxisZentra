#pragma warning disable CA1859

using System.Text.Json;
using CrevaFactOS.Persistence.Teams;
using CrevaFactOS.Persistence.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace CrevaFactOS.Persistence.AiTasks;

public sealed class AiTaskPlaybookRepository(
    CrevaFactOSDbContext dbContext,
    TenantDatabaseSession tenantDatabaseSession,
    IAiTaskPlaybookEngine playbookEngine,
    IAiTaskOperationsRepository operationsRepository,
    ITeamsNotificationRepository teamsNotificationRepository) : IAiTaskPlaybookRepository, IAiTaskPlaybookOrchestrator
{
    private const string PlaybookRunSourceModule = "playbook-run";

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public Task<IReadOnlyList<AiTaskPlaybookSummaryReadModel>> ListPlaybooksAsync(
        AiTaskOperationContext context,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(
            async ct =>
            {
                var playbooks = await dbContext.AiTaskPlaybooks.AsNoTracking()
                    .Where(p => p.TenantId == context.TenantId
                                && (p.FacilityId == null || p.FacilityId == context.FacilityId))
                    .OrderBy(p => p.Name)
                    .ThenBy(p => p.PlaybookKey)
                    .ToListAsync(ct);

                if (playbooks.Count == 0)
                {
                    return (IReadOnlyList<AiTaskPlaybookSummaryReadModel>)[];
                }

                var revisionIds = playbooks
                    .Where(p => p.CurrentRevisionId.HasValue)
                    .Select(p => p.CurrentRevisionId!.Value)
                    .ToList();

                var stepCounts = revisionIds.Count == 0
                    ? new Dictionary<Guid, int>()
                    : await dbContext.AiTaskPlaybookSteps.AsNoTracking()
                        .Where(s => s.TenantId == context.TenantId && revisionIds.Contains(s.PlaybookRevisionId))
                        .GroupBy(s => s.PlaybookRevisionId)
                        .Select(g => new { g.Key, Count = g.Count() })
                        .ToDictionaryAsync(x => x.Key, x => x.Count, ct);

                var playbookIds = playbooks.Select(p => p.Id).ToList();
                var lastRuns = await dbContext.AiTaskPlaybookRuns.AsNoTracking()
                    .Where(r => r.TenantId == context.TenantId
                                && r.FacilityId == context.FacilityId
                                && playbookIds.Contains(r.PlaybookId))
                    .GroupBy(r => r.PlaybookId)
                    .Select(g => new { PlaybookId = g.Key, LastRunAtUtc = g.Max(x => x.StartedAtUtc) })
                    .ToDictionaryAsync(x => x.PlaybookId, x => (DateTimeOffset?)x.LastRunAtUtc, ct);

                return (IReadOnlyList<AiTaskPlaybookSummaryReadModel>)playbooks
                    .Select(p => MapSummary(
                        p,
                        p.CurrentRevisionId.HasValue ? stepCounts.GetValueOrDefault(p.CurrentRevisionId.Value) : 0,
                        lastRuns.GetValueOrDefault(p.Id)))
                    .ToList();
            },
            cancellationToken);

    public Task<AiTaskPlaybookDetailReadModel?> GetPlaybookAsync(
        AiTaskOperationContext context,
        Guid playbookId,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(
            async ct =>
            {
                var playbook = await dbContext.AiTaskPlaybooks.AsNoTracking()
                    .SingleOrDefaultAsync(
                        p => p.TenantId == context.TenantId
                             && p.Id == playbookId
                             && (p.FacilityId == null || p.FacilityId == context.FacilityId),
                        ct);
                if (playbook is null || !playbook.CurrentRevisionId.HasValue)
                {
                    return null;
                }

                var revision = await LoadRevisionGraphAsync(context, playbook.CurrentRevisionId.Value, ct);
                return revision is null ? null : MapDetail(playbook, revision.Steps, revision.Dependencies);
            },
            cancellationToken);

    public Task<AiTaskPlaybookWriteResult> CreatePlaybookAsync(
        AiTaskOperationContext context,
        CreateAiTaskPlaybookCommand command,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteWriteAsync(
            async ct =>
            {
                var validation = ValidatePlaybookPayload(command.Category, command.Steps, command.Dependencies);
                if (validation is not null)
                {
                    return AiTaskPlaybookWriteResult.Fail(AiTaskFailureCodes.ValidationFailed, validation);
                }

                var graphValidation = AiTaskPlaybookGraphValidator.Validate(command.Steps, command.Dependencies);
                if (!graphValidation.IsValid)
                {
                    return AiTaskPlaybookWriteResult.Fail(
                        AiTaskFailureCodes.ValidationFailed,
                        string.Join(' ', graphValidation.Errors));
                }

                if (command.FacilityId.HasValue && command.FacilityId != context.FacilityId)
                {
                    return AiTaskPlaybookWriteResult.Fail(
                        AiTaskFailureCodes.ValidationFailed,
                        "Playbook facility must match the active facility scope.");
                }

                var playbookKey = command.PlaybookKey.Trim();
                var exists = await dbContext.AiTaskPlaybooks.AnyAsync(
                    p => p.TenantId == context.TenantId
                         && p.FacilityId == (command.FacilityId ?? context.FacilityId)
                         && p.PlaybookKey == playbookKey,
                    ct);
                if (exists)
                {
                    return AiTaskPlaybookWriteResult.Fail(
                        AiTaskFailureCodes.ValidationFailed,
                        "A playbook with this key already exists.");
                }

                var now = DateTimeOffset.UtcNow;
                var actor = context.IdentitySubjectId.ToString();
                var playbookId = Guid.NewGuid();
                var revisionId = Guid.NewGuid();

                var playbook = new AiTaskPlaybook
                {
                    Id = playbookId,
                    TenantId = context.TenantId,
                    FacilityId = command.FacilityId ?? context.FacilityId,
                    PlaybookKey = playbookKey,
                    Name = command.Name.Trim(),
                    Description = TrimOrNull(command.Description),
                    Category = command.Category.Trim(),
                    TriggerContext = TrimOrNull(command.TriggerContext),
                    Enabled = command.Enabled,
                    Version = 1,
                    CurrentRevisionId = revisionId,
                    EffectiveFromUtc = command.EffectiveFromUtc,
                    EffectiveUntilUtc = command.EffectiveUntilUtc,
                    CreatedByMembershipId = context.MembershipId,
                    UpdatedByMembershipId = context.MembershipId,
                    CreatedBy = actor,
                    UpdatedBy = actor,
                    CreatedAtUtc = now,
                    UpdatedAtUtc = now,
                };

                var revision = BuildRevision(
                    playbook,
                    revisionId,
                    actor,
                    now,
                    "Initial version.",
                    command.Steps,
                    command.Dependencies);

                dbContext.AiTaskPlaybooks.Add(playbook);
                dbContext.AiTaskPlaybookRevisions.Add(revision);
                PersistRevisionGraph(dbContext, context, revision, command.Steps, command.Dependencies);

                await dbContext.SaveChangesAsync(ct);
                return AiTaskPlaybookWriteResult.SuccessResult(playbookId, (uint)playbook.Version);
            },
            cancellationToken);

    public Task<AiTaskPlaybookWriteResult> UpdatePlaybookAsync(
        AiTaskOperationContext context,
        UpdateAiTaskPlaybookCommand command,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteWriteAsync(
            async ct =>
            {
                var validation = ValidatePlaybookPayload(command.Category, command.Steps, command.Dependencies);
                if (validation is not null)
                {
                    return AiTaskPlaybookWriteResult.Fail(AiTaskFailureCodes.ValidationFailed, validation);
                }

                var graphValidation = AiTaskPlaybookGraphValidator.Validate(command.Steps, command.Dependencies);
                if (!graphValidation.IsValid)
                {
                    return AiTaskPlaybookWriteResult.Fail(
                        AiTaskFailureCodes.ValidationFailed,
                        string.Join(' ', graphValidation.Errors));
                }

                var playbook = await dbContext.AiTaskPlaybooks
                    .SingleOrDefaultAsync(
                        p => p.TenantId == context.TenantId
                             && p.Id == command.PlaybookId
                             && (p.FacilityId == null || p.FacilityId == context.FacilityId),
                        ct);
                if (playbook is null)
                {
                    return AiTaskPlaybookWriteResult.Fail(AiTaskFailureCodes.NotFound, "Playbook not found.");
                }

                if (playbook.Version != command.ExpectedVersion)
                {
                    return AiTaskPlaybookWriteResult.Fail(
                        AiTaskFailureCodes.ConcurrencyConflict,
                        "Playbook was modified by another request.");
                }

                var now = DateTimeOffset.UtcNow;
                var actor = context.IdentitySubjectId.ToString();
                var revisionId = Guid.NewGuid();

                playbook.Name = command.Name.Trim();
                playbook.Description = TrimOrNull(command.Description);
                playbook.Category = command.Category.Trim();
                playbook.TriggerContext = TrimOrNull(command.TriggerContext);
                playbook.Enabled = command.Enabled;
                playbook.EffectiveFromUtc = command.EffectiveFromUtc;
                playbook.EffectiveUntilUtc = command.EffectiveUntilUtc;
                playbook.Version++;
                playbook.CurrentRevisionId = revisionId;
                playbook.UpdatedAtUtc = now;
                playbook.UpdatedBy = actor;
                playbook.UpdatedByMembershipId = context.MembershipId;

                var revision = BuildRevision(
                    playbook,
                    revisionId,
                    actor,
                    now,
                    command.ChangeReason.Trim(),
                    command.Steps,
                    command.Dependencies);

                dbContext.AiTaskPlaybookRevisions.Add(revision);
                PersistRevisionGraph(dbContext, context, revision, command.Steps, command.Dependencies);

                await dbContext.SaveChangesAsync(ct);
                return AiTaskPlaybookWriteResult.SuccessResult(playbook.Id, (uint)playbook.Version);
            },
            cancellationToken);

    public Task<AiTaskPlaybookWriteResult> SetPlaybookEnabledAsync(
        AiTaskOperationContext context,
        Guid playbookId,
        uint expectedVersion,
        bool enabled,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteWriteAsync(
            async ct =>
            {
                var playbook = await dbContext.AiTaskPlaybooks
                    .SingleOrDefaultAsync(
                        p => p.TenantId == context.TenantId
                             && p.Id == playbookId
                             && (p.FacilityId == null || p.FacilityId == context.FacilityId),
                        ct);
                if (playbook is null)
                {
                    return AiTaskPlaybookWriteResult.Fail(AiTaskFailureCodes.NotFound, "Playbook not found.");
                }

                if (playbook.Version != expectedVersion)
                {
                    return AiTaskPlaybookWriteResult.Fail(
                        AiTaskFailureCodes.ConcurrencyConflict,
                        "Playbook was modified by another request.");
                }

                var now = DateTimeOffset.UtcNow;
                var actor = context.IdentitySubjectId.ToString();
                playbook.Enabled = enabled;
                playbook.Version++;
                playbook.UpdatedAtUtc = now;
                playbook.UpdatedBy = actor;
                playbook.UpdatedByMembershipId = context.MembershipId;

                await dbContext.SaveChangesAsync(ct);
                return AiTaskPlaybookWriteResult.SuccessResult(playbook.Id, (uint)playbook.Version);
            },
            cancellationToken);

    public Task<IReadOnlyList<AiTaskPlaybookRunSummaryReadModel>> ListRunsAsync(
        AiTaskOperationContext context,
        Guid? playbookId,
        string? statusFilter,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(
            async ct =>
            {
                var query = dbContext.AiTaskPlaybookRuns.AsNoTracking()
                    .Where(r => r.TenantId == context.TenantId && r.FacilityId == context.FacilityId);

                if (playbookId.HasValue)
                {
                    query = query.Where(r => r.PlaybookId == playbookId.Value);
                }

                if (!string.IsNullOrWhiteSpace(statusFilter))
                {
                    var status = statusFilter.Trim();
                    query = query.Where(r => r.Status == status);
                }

                var runs = await query
                    .OrderByDescending(r => r.StartedAtUtc)
                    .Join(
                        dbContext.AiTaskPlaybooks.AsNoTracking(),
                        run => run.PlaybookId,
                        playbook => playbook.Id,
                        (run, playbook) => new { run, playbook.Name })
                    .ToListAsync(ct);

                return (IReadOnlyList<AiTaskPlaybookRunSummaryReadModel>)runs
                    .Select(item => new AiTaskPlaybookRunSummaryReadModel(
                        item.run.Id,
                        item.run.PlaybookId,
                        item.Name,
                        item.run.Status,
                        item.run.FacilityId,
                        item.run.TriggerSource,
                        item.run.SourceTaskId,
                        item.run.RequiredStepsCompleted,
                        item.run.RequiredStepsTotal,
                        item.run.OptionalStepsCompleted,
                        item.run.OptionalStepsTotal,
                        item.run.BlockedStepsCount,
                        item.run.ApprovalsPending,
                        item.run.AtRisk,
                        item.run.StartedAtUtc,
                        item.run.CompletedAtUtc))
                    .ToList();
            },
            cancellationToken);

    public Task<AiTaskPlaybookRunDetailReadModel?> GetRunAsync(
        AiTaskOperationContext context,
        Guid runId,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(
            async ct =>
            {
                var run = await dbContext.AiTaskPlaybookRuns.AsNoTracking()
                    .Include(r => r.Steps)
                    .SingleOrDefaultAsync(
                        r => r.TenantId == context.TenantId
                             && r.FacilityId == context.FacilityId
                             && r.Id == runId,
                        ct);
                if (run is null)
                {
                    return null;
                }

                var playbookName = await dbContext.AiTaskPlaybooks.AsNoTracking()
                    .Where(p => p.Id == run.PlaybookId)
                    .Select(p => p.Name)
                    .SingleOrDefaultAsync(ct);

                return MapRunDetail(run, playbookName ?? "Playbook");
            },
            cancellationToken);

    public Task<AiTaskPlaybookWriteResult> StartRunAsync(
        AiTaskOperationContext context,
        StartAiTaskPlaybookRunCommand command,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteWriteAsync(
            async ct => await StartRunCoreAsync(context, command, ct),
            cancellationToken);

    public Task<AiTaskPlaybookWriteResult> CancelRunAsync(
        AiTaskOperationContext context,
        CancelAiTaskPlaybookRunCommand command,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteWriteAsync(
            async ct =>
            {
                if (string.IsNullOrWhiteSpace(command.Reason))
                {
                    return AiTaskPlaybookWriteResult.Fail(
                        AiTaskFailureCodes.ValidationFailed,
                        "Cancellation reason is required.");
                }

                var run = await dbContext.AiTaskPlaybookRuns
                    .Include(r => r.Steps)
                    .SingleOrDefaultAsync(
                        r => r.TenantId == context.TenantId
                             && r.FacilityId == context.FacilityId
                             && r.Id == command.RunId,
                        ct);
                if (run is null)
                {
                    return AiTaskPlaybookWriteResult.Fail(AiTaskFailureCodes.NotFound, "Playbook run not found.");
                }

                if (run.Status == AiTaskPlaybookRunStatuses.Cancelled)
                {
                    return AiTaskPlaybookWriteResult.SuccessResult(run.Id, runId: run.Id);
                }

                var now = DateTimeOffset.UtcNow;
                var actor = context.IdentitySubjectId.ToString();
                run.Status = AiTaskPlaybookRunStatuses.Cancelled;
                run.CancelledAtUtc = now;
                run.CancelledBy = actor;
                run.CancellationReason = command.Reason.Trim();
                run.BlockReason = null;

                foreach (var step in run.Steps.Where(s => !IsTerminalRunStepStatus(s.Status)))
                {
                    step.Status = AiTaskPlaybookRunStepStatuses.Cancelled;
                }

                var parentTask = await LoadAuditParentTaskAsync(context, run, ct);
                if (parentTask is not null)
                {
                    AppendPlaybookAuditAsync(
                        context,
                        parentTask,
                        AiTaskPlaybookAuditEventTypes.PlaybookRunCancelled,
                        actor,
                        "Playbook Engine",
                        now,
                        new { runId = run.Id, reason = command.Reason.Trim() },
                        command.Reason.Trim());
                }

                await dbContext.SaveChangesAsync(ct);
                return AiTaskPlaybookWriteResult.SuccessResult(run.Id, runId: run.Id);
            },
            cancellationToken);

    public Task<AiTaskPlaybookWriteResult> BlockRunStepAsync(
        AiTaskOperationContext context,
        BlockAiTaskPlaybookRunStepCommand command,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteWriteAsync(
            async ct => await MutateRunStepAsync(
                context,
                command.RunId,
                command.StepId,
                command.Reason,
                block: true,
                skip: false,
                ct),
            cancellationToken);

    public Task<AiTaskPlaybookWriteResult> UnblockRunStepAsync(
        AiTaskOperationContext context,
        UnblockAiTaskPlaybookRunStepCommand command,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteWriteAsync(
            async ct => await MutateRunStepAsync(
                context,
                command.RunId,
                command.StepId,
                command.Reason,
                block: false,
                skip: false,
                ct),
            cancellationToken);

    public Task<AiTaskPlaybookWriteResult> SkipRunStepAsync(
        AiTaskOperationContext context,
        SkipAiTaskPlaybookRunStepCommand command,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteWriteAsync(
            async ct => await MutateRunStepAsync(
                context,
                command.RunId,
                command.StepId,
                command.Reason,
                block: false,
                skip: true,
                ct),
            cancellationToken);

    public Task<AiTaskPlaybookGraphValidationResult> ValidateGraphAsync(
        IReadOnlyList<AiTaskPlaybookStepDefinition> steps,
        IReadOnlyList<AiTaskPlaybookDependencyDefinition> dependencies) =>
        Task.FromResult(AiTaskPlaybookGraphValidator.Validate(steps, dependencies));

    public async Task NotifyTaskTransitionAsync(
        AiTaskOperationContext context,
        AiTask task,
        string previousStatus,
        string targetStatus,
        CancellationToken cancellationToken = default)
    {
        await tenantDatabaseSession.ExecuteWriteAsync(
            async ct =>
            {
                if (!task.PlaybookRunStepId.HasValue)
                {
                    return false;
                }

                var runStep = await dbContext.AiTaskPlaybookRunSteps
                    .SingleOrDefaultAsync(
                        s => s.TenantId == context.TenantId
                             && s.FacilityId == context.FacilityId
                             && s.Id == task.PlaybookRunStepId.Value,
                        ct);
                if (runStep is null)
                {
                    return false;
                }

                var run = await dbContext.AiTaskPlaybookRuns
                    .Include(r => r.Steps)
                    .SingleAsync(
                        r => r.TenantId == context.TenantId
                             && r.FacilityId == context.FacilityId
                             && r.Id == runStep.RunId,
                        ct);

                if (run.Status == AiTaskPlaybookRunStatuses.Cancelled)
                {
                    return false;
                }

                var now = DateTimeOffset.UtcNow;
                var previousRunStatus = run.Status;
                UpdateRunStepForTaskTransition(runStep, targetStatus, now);

                if (string.Equals(targetStatus, AiTaskStatuses.Completed, StringComparison.Ordinal))
                {
                    runStep.Status = AiTaskPlaybookRunStepStatuses.Completed;
                    runStep.CompletedAtUtc = now;
                }

                var graph = await LoadRevisionGraphAsync(context, run.PlaybookRevisionId, ct);
                if (graph is null)
                {
                    return false;
                }

                var stepKeyByTemplateStepId = graph.Steps.ToDictionary(s => s.Id, s => s.StepKey);
                playbookEngine.ReevaluateRunSteps(run, run.Steps.ToList(), graph.Dependencies, stepKeyByTemplateStepId);
                UpdateCriticalPath(run, graph.Steps, graph.Dependencies);

                var playbookCategory = graph.Revision.Category;
                await MaterializeReadyStepsAsync(context, run, graph.Steps, playbookCategory, now, ct);
                await AppendRunCompletionAuditIfNeededAsync(context, run, previousRunStatus, now, ct);

                await dbContext.SaveChangesAsync(ct);
                return true;
            },
            cancellationToken);
    }

    public async Task NotifyTaskReopenedAsync(
        AiTaskOperationContext context,
        AiTask task,
        CancellationToken cancellationToken = default)
    {
        await tenantDatabaseSession.ExecuteWriteAsync(
            async ct =>
            {
                if (!task.PlaybookRunStepId.HasValue)
                {
                    return false;
                }

                var runStep = await dbContext.AiTaskPlaybookRunSteps
                    .SingleOrDefaultAsync(
                        s => s.TenantId == context.TenantId
                             && s.FacilityId == context.FacilityId
                             && s.Id == task.PlaybookRunStepId.Value,
                        ct);
                if (runStep is null)
                {
                    return false;
                }

                var run = await dbContext.AiTaskPlaybookRuns
                    .Include(r => r.Steps)
                    .SingleAsync(
                        r => r.TenantId == context.TenantId
                             && r.FacilityId == context.FacilityId
                             && r.Id == runStep.RunId,
                        ct);

                if (run.Status == AiTaskPlaybookRunStatuses.Cancelled)
                {
                    return false;
                }

                runStep.Status = runStep.StepType == AiTaskPlaybookStepTypes.Approval
                    ? AiTaskPlaybookRunStepStatuses.AwaitingApproval
                    : AiTaskPlaybookRunStepStatuses.InProgress;
                runStep.CompletedAtUtc = null;

                if (run.Status == AiTaskPlaybookRunStatuses.Completed)
                {
                    run.Status = AiTaskPlaybookRunStatuses.Running;
                    run.CompletedAtUtc = null;
                }

                var graph = await LoadRevisionGraphAsync(context, run.PlaybookRevisionId, ct);
                if (graph is null)
                {
                    await dbContext.SaveChangesAsync(ct);
                    return true;
                }

                var stepKeyByTemplateStepId = graph.Steps.ToDictionary(s => s.Id, s => s.StepKey);
                playbookEngine.ReevaluateRunSteps(run, run.Steps.ToList(), graph.Dependencies, stepKeyByTemplateStepId);
                UpdateCriticalPath(run, graph.Steps, graph.Dependencies);

                await dbContext.SaveChangesAsync(ct);
                return true;
            },
            cancellationToken);
    }

    private async Task<AiTaskPlaybookWriteResult> StartRunCoreAsync(
        AiTaskOperationContext context,
        StartAiTaskPlaybookRunCommand command,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(command.IdempotencyKey))
        {
            return AiTaskPlaybookWriteResult.Fail(
                AiTaskFailureCodes.ValidationFailed,
                "Idempotency key is required.");
        }

        if (!AiTaskPlaybookTriggerSources.All.Contains(command.TriggerSource.Trim()))
        {
            return AiTaskPlaybookWriteResult.Fail(
                AiTaskFailureCodes.ValidationFailed,
                "A valid trigger source is required.");
        }

        var normalizedKey = command.IdempotencyKey.Trim();
        var existingRun = await dbContext.AiTaskPlaybookRuns.AsNoTracking()
            .SingleOrDefaultAsync(
                r => r.TenantId == context.TenantId && r.IdempotencyKey == normalizedKey,
                ct);
        if (existingRun is not null)
        {
            return AiTaskPlaybookWriteResult.SuccessResult(existingRun.Id, runId: existingRun.Id);
        }

        var playbook = await dbContext.AiTaskPlaybooks.AsNoTracking()
            .SingleOrDefaultAsync(
                p => p.TenantId == context.TenantId
                     && p.Id == command.PlaybookId
                     && (p.FacilityId == null || p.FacilityId == context.FacilityId),
                ct);
        if (playbook is null || !playbook.CurrentRevisionId.HasValue)
        {
            return AiTaskPlaybookWriteResult.Fail(AiTaskFailureCodes.NotFound, "Playbook not found.");
        }

        if (!playbook.Enabled)
        {
            return AiTaskPlaybookWriteResult.Fail(
                AiTaskFailureCodes.ValidationFailed,
                "Playbook is disabled.");
        }

        var now = DateTimeOffset.UtcNow;
        if (playbook.EffectiveFromUtc.HasValue && now < playbook.EffectiveFromUtc.Value)
        {
            return AiTaskPlaybookWriteResult.Fail(
                AiTaskFailureCodes.ValidationFailed,
                "Playbook is not yet effective.");
        }

        if (playbook.EffectiveUntilUtc.HasValue && now > playbook.EffectiveUntilUtc.Value)
        {
            return AiTaskPlaybookWriteResult.Fail(
                AiTaskFailureCodes.ValidationFailed,
                "Playbook is no longer effective.");
        }

        var graph = await LoadRevisionGraphAsync(context, playbook.CurrentRevisionId.Value, ct);
        if (graph is null)
        {
            return AiTaskPlaybookWriteResult.Fail(AiTaskFailureCodes.NotFound, "Playbook revision not found.");
        }

        var includedTemplateSteps = graph.Steps
            .Where(step => playbookEngine.EvaluateCondition(
                AiTaskPlaybookConditionSerializer.Deserialize(step.ConditionJson),
                command.ContextValues))
            .OrderBy(step => step.SortOrder)
            .ToList();

        if (includedTemplateSteps.Count == 0)
        {
            return AiTaskPlaybookWriteResult.Fail(
                AiTaskFailureCodes.ValidationFailed,
                "No playbook steps matched the supplied context.");
        }

        var actor = context.IdentitySubjectId.ToString();
        var runId = Guid.NewGuid();
        var parentTaskId = command.SourceTaskId;
        AiTask? sourceTask = null;
        if (parentTaskId.HasValue)
        {
            sourceTask = await dbContext.AiTasks.AsNoTracking()
                .SingleOrDefaultAsync(
                    t => t.TenantId == context.TenantId
                             && t.FacilityId == context.FacilityId
                             && t.Id == parentTaskId.Value,
                    ct);
        }

        var run = new AiTaskPlaybookRun
        {
            Id = runId,
            TenantId = context.TenantId,
            FacilityId = context.FacilityId,
            PlaybookId = playbook.Id,
            PlaybookVersion = playbook.Version,
            PlaybookRevisionId = playbook.CurrentRevisionId.Value,
            TriggerSource = command.TriggerSource.Trim(),
            SourceTaskId = command.SourceTaskId,
            SourceEntityJson = TrimOrNull(command.SourceEntityJson),
            Status = AiTaskPlaybookRunStatuses.Running,
            IdempotencyKey = normalizedKey,
            CorrelationId = command.CorrelationId ?? context.CorrelationId,
            ParentTaskId = parentTaskId,
            StartedBy = actor,
            StartedAtUtc = now,
        };

        var runSteps = includedTemplateSteps
            .Select(templateStep => new AiTaskPlaybookRunStep
            {
                Id = Guid.NewGuid(),
                TenantId = context.TenantId,
                FacilityId = context.FacilityId,
                RunId = runId,
                TemplateStepId = templateStep.Id,
                StepKey = templateStep.StepKey,
                Name = templateStep.Name,
                Required = templateStep.Required,
                StepType = templateStep.StepType,
                Status = AiTaskPlaybookRunStepStatuses.Pending,
                DependencyState = AiTaskPlaybookDependencyStates.Waiting,
                SortOrder = templateStep.SortOrder,
            })
            .ToList();

        run.Steps = runSteps;
        var stepKeyByTemplateStepId = includedTemplateSteps.ToDictionary(s => s.Id, s => s.StepKey);
        playbookEngine.ReevaluateRunSteps(run, runSteps, graph.Dependencies, stepKeyByTemplateStepId);
        UpdateCriticalPath(run, includedTemplateSteps, graph.Dependencies);

        dbContext.AiTaskPlaybookRuns.Add(run);
        dbContext.AiTaskPlaybookRunSteps.AddRange(runSteps);

        await MaterializeReadyStepsAsync(context, run, includedTemplateSteps, playbook.Category, now, ct, sourceTask);

        if (sourceTask is not null)
        {
            var trackedParent = await dbContext.AiTasks
                .SingleAsync(
                    t => t.TenantId == context.TenantId
                         && t.FacilityId == context.FacilityId
                         && t.Id == sourceTask.Id,
                    ct);

            AppendPlaybookAuditAsync(
                context,
                trackedParent,
                AiTaskPlaybookAuditEventTypes.PlaybookRunStarted,
                actor,
                "Playbook Engine",
                now,
                new
                {
                    runId = run.Id,
                    playbookId = playbook.Id,
                    playbookKey = playbook.PlaybookKey,
                    triggerSource = run.TriggerSource,
                },
                $"Playbook '{playbook.Name}' run started.");

            await TryNotifyPlaybookStartedAsync(context, trackedParent, run, ct);
        }

        await dbContext.SaveChangesAsync(ct);
        return AiTaskPlaybookWriteResult.SuccessResult(run.Id, runId: run.Id);
    }

    private async Task<AiTaskPlaybookWriteResult> MutateRunStepAsync(
        AiTaskOperationContext context,
        Guid runId,
        Guid stepId,
        string reason,
        bool block,
        bool skip,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            return AiTaskPlaybookWriteResult.Fail(
                AiTaskFailureCodes.ValidationFailed,
                "Reason is required.");
        }

        var run = await dbContext.AiTaskPlaybookRuns
            .Include(r => r.Steps)
            .SingleOrDefaultAsync(
                r => r.TenantId == context.TenantId
                     && r.FacilityId == context.FacilityId
                     && r.Id == runId,
                ct);
        if (run is null)
        {
            return AiTaskPlaybookWriteResult.Fail(AiTaskFailureCodes.NotFound, "Playbook run not found.");
        }

        if (run.Status == AiTaskPlaybookRunStatuses.Cancelled)
        {
            return AiTaskPlaybookWriteResult.Fail(
                AiTaskFailureCodes.ValidationFailed,
                "Cancelled playbook runs cannot be modified.");
        }

        var runStep = run.Steps.SingleOrDefault(s => s.Id == stepId);
        if (runStep is null)
        {
            return AiTaskPlaybookWriteResult.Fail(AiTaskFailureCodes.NotFound, "Playbook run step not found.");
        }

        var now = DateTimeOffset.UtcNow;
        var actor = context.IdentitySubjectId.ToString();
        var previousRunStatus = run.Status;
        string auditEventType;

        if (skip)
        {
            runStep.Status = AiTaskPlaybookRunStepStatuses.Skipped;
            runStep.CompletedAtUtc = now;
            runStep.BlockedBy = null;
            runStep.BlockedAtUtc = null;
            runStep.BlockReason = null;
            auditEventType = AiTaskPlaybookAuditEventTypes.PlaybookStepSkipped;
        }
        else if (block)
        {
            runStep.Status = AiTaskPlaybookRunStepStatuses.Blocked;
            runStep.BlockedBy = actor;
            runStep.BlockedAtUtc = now;
            runStep.BlockReason = reason.Trim();
            auditEventType = AiTaskPlaybookAuditEventTypes.PlaybookStepBlocked;
        }
        else
        {
            runStep.BlockedBy = null;
            runStep.BlockedAtUtc = null;
            runStep.BlockReason = null;
            runStep.Status = AiTaskPlaybookRunStepStatuses.Pending;
            auditEventType = AiTaskPlaybookAuditEventTypes.PlaybookStepUnblocked;
        }

        var graph = await LoadRevisionGraphAsync(context, run.PlaybookRevisionId, ct);
        if (graph is not null)
        {
            var stepKeyByTemplateStepId = graph.Steps.ToDictionary(s => s.Id, s => s.StepKey);
            playbookEngine.ReevaluateRunSteps(run, run.Steps.ToList(), graph.Dependencies, stepKeyByTemplateStepId);
            UpdateCriticalPath(run, graph.Steps, graph.Dependencies);

            if (!skip)
            {
                await MaterializeReadyStepsAsync(context, run, graph.Steps, graph.Revision.Category, now, ct);
            }
        }

        var parentTask = await LoadAuditParentTaskAsync(context, run, ct);
        if (parentTask is not null)
        {
            AppendPlaybookAuditAsync(
                context,
                parentTask,
                auditEventType,
                actor,
                actor,
                now,
                new { runId = run.Id, stepId = runStep.Id, stepKey = runStep.StepKey },
                reason.Trim());
        }

        await AppendRunCompletionAuditIfNeededAsync(context, run, previousRunStatus, now, ct);
        await dbContext.SaveChangesAsync(ct);
        return AiTaskPlaybookWriteResult.SuccessResult(run.Id, runId: run.Id);
    }

    private async Task MaterializeReadyStepsAsync(
        AiTaskOperationContext context,
        AiTaskPlaybookRun run,
        IReadOnlyList<AiTaskPlaybookStep> templateSteps,
        string playbookCategory,
        DateTimeOffset now,
        CancellationToken ct,
        AiTask? sourceTask = null)
    {
        if (run.Status == AiTaskPlaybookRunStatuses.Cancelled)
        {
            return;
        }

        var templateById = templateSteps.ToDictionary(s => s.Id);
        var actor = context.IdentitySubjectId.ToString();
        sourceTask ??= run.ParentTaskId.HasValue
            ? await dbContext.AiTasks.AsNoTracking()
                .SingleOrDefaultAsync(
                    t => t.TenantId == context.TenantId
                         && t.FacilityId == context.FacilityId
                         && t.Id == run.ParentTaskId.Value,
                    ct)
            : null;

        foreach (var runStep in run.Steps
                     .Where(s => s.Status == AiTaskPlaybookRunStepStatuses.Ready && !s.LinkedTaskId.HasValue)
                     .OrderBy(s => s.SortOrder))
        {
            if (!templateById.TryGetValue(runStep.TemplateStepId, out var templateStep))
            {
                continue;
            }

            var idempotencyKey = $"playbook-run-step:{run.Id}:{runStep.StepKey}";
            var existingTask = await dbContext.AiTasks
                .SingleOrDefaultAsync(
                    t => t.TenantId == context.TenantId && t.IdempotencyKey == idempotencyKey,
                    ct);
            if (existingTask is not null)
            {
                runStep.LinkedTaskId = existingTask.Id;
                continue;
            }

            var childTask = BuildPlaybookChildTask(
                context,
                run,
                runStep,
                templateStep,
                playbookCategory,
                sourceTask,
                idempotencyKey,
                now,
                actor);

            childTask.TaskReference = await GenerateTaskReferenceAsync(context, ct);
            dbContext.AiTasks.Add(childTask);

            AppendAuditEvent(
                context,
                childTask,
                AiTaskAuditEventTypes.Created,
                actor,
                "Playbook Engine",
                now,
                null,
                childTask.Status,
                $"Created from playbook step '{runStep.StepKey}'.");

            await operationsRepository.ApplyRoutingOnCreateAsync(context, childTask, now, actor, ct);

            runStep.LinkedTaskId = childTask.Id;
            runStep.StartedAtUtc = now;
            runStep.Status = templateStep.StepType == AiTaskPlaybookStepTypes.Approval
                ? AiTaskPlaybookRunStepStatuses.AwaitingApproval
                : AiTaskPlaybookRunStepStatuses.InProgress;
        }
    }

    private async Task TryNotifyPlaybookStartedAsync(
        AiTaskOperationContext context,
        AiTask parentTask,
        AiTaskPlaybookRun run,
        CancellationToken ct)
    {
        var idempotencyKey = $"playbook-started:{run.Id:D}";
        var alreadyNotified = await dbContext.AiTaskAuditEvents.AsNoTracking()
            .AnyAsync(
                e => e.TenantId == context.TenantId
                     && e.TaskId == parentTask.Id
                     && e.EventType == AiTaskAuditEventTypes.TeamsNotificationEmitted
                     && e.MetadataJson != null
                     && e.MetadataJson.Contains(idempotencyKey),
                ct);
        if (alreadyNotified)
        {
            return;
        }

        var recipientMembershipId = parentTask.AssignedMembershipId ?? context.MembershipId;
        var teamsContext = new TeamsOperationContext(
            context.TenantId,
            context.FacilityId,
            context.MembershipId,
            context.IdentitySubjectId,
            context.PermissionKey,
            context.CorrelationId);

        await teamsNotificationRepository.TryCreateTaskLinkAsync(
            teamsContext,
            recipientMembershipId,
            run.Id,
            parentTask.Id,
            parentTask.TaskReference,
            ct);

        AppendPlaybookAuditAsync(
            context,
            parentTask,
            AiTaskAuditEventTypes.TeamsNotificationEmitted,
            context.IdentitySubjectId.ToString(),
            "Playbook Engine",
            DateTimeOffset.UtcNow,
            new { notificationType = "playbook-started", runId = run.Id, idempotencyKey },
            "Playbook started Teams notification.");
    }

    private async Task AppendRunCompletionAuditIfNeededAsync(
        AiTaskOperationContext context,
        AiTaskPlaybookRun run,
        string previousRunStatus,
        DateTimeOffset now,
        CancellationToken ct)
    {
        if (run.Status != AiTaskPlaybookRunStatuses.Completed
            || previousRunStatus == AiTaskPlaybookRunStatuses.Completed)
        {
            return;
        }

        var parentTask = await LoadAuditParentTaskAsync(context, run, ct);
        if (parentTask is null)
        {
            return;
        }

        AppendPlaybookAuditAsync(
            context,
            parentTask,
            AiTaskPlaybookAuditEventTypes.PlaybookRunCompleted,
            context.IdentitySubjectId.ToString(),
            "Playbook Engine",
            now,
            new { runId = run.Id },
            "Playbook run completed.");
    }

    private async Task<AiTask?> LoadAuditParentTaskAsync(
        AiTaskOperationContext context,
        AiTaskPlaybookRun run,
        CancellationToken ct)
    {
        var parentTaskId = run.ParentTaskId ?? run.SourceTaskId;
        if (!parentTaskId.HasValue)
        {
            return null;
        }

        return await dbContext.AiTasks
            .SingleOrDefaultAsync(
                t => t.TenantId == context.TenantId
                     && t.FacilityId == context.FacilityId
                     && t.Id == parentTaskId.Value,
                ct);
    }

    private void AppendPlaybookAuditAsync(
        AiTaskOperationContext context,
        AiTask task,
        string eventType,
        string actorId,
        string actorLabel,
        DateTimeOffset occurredAtUtc,
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
            ActorType = AiTaskActorTypes.System,
            ActorId = actorId,
            ActorLabel = actorLabel,
            Note = TrimOrNull(note),
            CorrelationId = context.CorrelationId,
            MetadataJson = metadata is null ? null : Serialize(metadata),
        });
    }

    private static void UpdateRunStepForTaskTransition(
        AiTaskPlaybookRunStep runStep,
        string targetStatus,
        DateTimeOffset now)
    {
        if (string.Equals(targetStatus, AiTaskStatuses.AwaitingApproval, StringComparison.Ordinal))
        {
            runStep.Status = AiTaskPlaybookRunStepStatuses.AwaitingApproval;
            return;
        }

        if (string.Equals(targetStatus, AiTaskStatuses.Approved, StringComparison.Ordinal)
            || string.Equals(targetStatus, AiTaskStatuses.InProgress, StringComparison.Ordinal))
        {
            runStep.Status = AiTaskPlaybookRunStepStatuses.InProgress;
            runStep.StartedAtUtc ??= now;
        }
    }

    private void UpdateCriticalPath(
        AiTaskPlaybookRun run,
        IReadOnlyList<AiTaskPlaybookStep> templateSteps,
        IReadOnlyList<AiTaskPlaybookStepDependency> dependencies)
    {
        var criticalPath = playbookEngine.CalculateCriticalPath(templateSteps, dependencies, run.Steps.ToList());
        run.CriticalPathJson = Serialize(criticalPath);
        run.AtRisk = criticalPath.BlockingStepKeys.Count > 0;
    }

    private static AiTask BuildPlaybookChildTask(
        AiTaskOperationContext context,
        AiTaskPlaybookRun run,
        AiTaskPlaybookRunStep runStep,
        AiTaskPlaybookStep templateStep,
        string playbookCategory,
        AiTask? sourceTask,
        string idempotencyKey,
        DateTimeOffset now,
        string actor)
    {
        var isApprovalStep = templateStep.StepType == AiTaskPlaybookStepTypes.Approval;
        var status = isApprovalStep ? AiTaskStatuses.AwaitingApproval : AiTaskStatuses.Approved;
        var category = ResolveTaskCategory(playbookCategory);
        var closurePolicy = DeserializeClosurePolicy(templateStep.ClosurePolicyJson);

        return new AiTask
        {
            Id = Guid.NewGuid(),
            TenantId = context.TenantId,
            FacilityId = context.FacilityId,
            Title = templateStep.Name.Trim(),
            Summary = string.IsNullOrWhiteSpace(templateStep.Description)
                ? $"Playbook step '{runStep.StepKey}'."
                : templateStep.Description.Trim(),
            Category = category,
            Priority = templateStep.DefaultPriority.Trim(),
            RiskLevel = sourceTask?.RiskLevel ?? AiTaskRiskLevels.Medium,
            Status = status,
            DetectedIssue = sourceTask?.DetectedIssue ?? $"Playbook step: {templateStep.Name}",
            Reasoning = sourceTask?.Reasoning ?? "Generated by playbook orchestration.",
            RecommendationJson = Serialize(new AiTaskRecommendationModel(
                templateStep.Name,
                templateStep.Description ?? string.Empty,
                AiTaskActionTypes.Review,
                isApprovalStep,
                true,
                null)),
            ConfidenceJson = sourceTask?.ConfidenceJson ?? Serialize(new AiTaskConfidenceModel(1m, "Playbook step", null)),
            ImpactJson = sourceTask?.ImpactJson ?? Serialize(new AiTaskImpactModel(null, null, null, null, null, null, null, "Playbook step")),
            SourcesJson = sourceTask?.SourcesJson ?? "[]",
            ApprovalState = isApprovalStep ? AiTaskApprovalStates.Pending : AiTaskApprovalStates.Approved,
            SiteId = sourceTask?.SiteId,
            ProductionLineId = sourceTask?.ProductionLineId,
            MachineId = sourceTask?.MachineId,
            MoldId = sourceTask?.MoldId,
            ProductionOrderId = sourceTask?.ProductionOrderId,
            SalesOrderId = sourceTask?.SalesOrderId,
            MaterialId = sourceTask?.MaterialId,
            SupplierId = sourceTask?.SupplierId,
            CustomerId = sourceTask?.CustomerId,
            SourceModule = PlaybookRunSourceModule,
            SourceEntityId = runStep.Id.ToString(),
            IdempotencyKey = idempotencyKey,
            ParentTaskId = run.ParentTaskId ?? run.SourceTaskId,
            PlaybookRunId = run.Id,
            PlaybookRunStepId = runStep.Id,
            DependencyState = runStep.DependencyState,
            DependencyBlockReason = runStep.BlockReason,
            ClosureApprovalRequired = isApprovalStep || closurePolicy.ApprovalRequired,
            FourEyesRequired = isApprovalStep && closurePolicy.FourEyesRequired,
            RuleSetVersion = AiTaskConstants.RuleSetVersion,
            Version = 1,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
            CreatedBy = actor,
            UpdatedBy = actor,
        };
    }

    private static string ResolveTaskCategory(string playbookCategory) =>
        playbookCategory switch
        {
            AiTaskPlaybookCategories.MachineIncident => AiTaskCategories.Maintenance,
            AiTaskPlaybookCategories.QualityNonconformance => AiTaskCategories.Quality,
            AiTaskPlaybookCategories.MaterialShortage => AiTaskCategories.MaterialRisk,
            AiTaskPlaybookCategories.CriticalOrderDelay => AiTaskCategories.DeliveryRisk,
            AiTaskPlaybookCategories.EnergyAnomaly => AiTaskCategories.Energy,
            AiTaskPlaybookCategories.ShiftHandoverCritical => AiTaskCategories.LiveProduction,
            _ => AiTaskCategories.Maintenance,
        };

    private async Task<RevisionGraph?> LoadRevisionGraphAsync(
        AiTaskOperationContext context,
        Guid revisionId,
        CancellationToken ct)
    {
        var revision = await dbContext.AiTaskPlaybookRevisions.AsNoTracking()
            .SingleOrDefaultAsync(r => r.TenantId == context.TenantId && r.Id == revisionId, ct);
        if (revision is null)
        {
            return null;
        }

        var steps = await dbContext.AiTaskPlaybookSteps.AsNoTracking()
            .Where(s => s.TenantId == context.TenantId && s.PlaybookRevisionId == revisionId)
            .OrderBy(s => s.SortOrder)
            .ToListAsync(ct);

        var dependencies = await dbContext.AiTaskPlaybookStepDependencies.AsNoTracking()
            .Where(d => d.TenantId == context.TenantId && d.PlaybookRevisionId == revisionId)
            .ToListAsync(ct);

        return new RevisionGraph(revision, steps, dependencies);
    }

    private static void PersistRevisionGraph(
        CrevaFactOSDbContext dbContext,
        AiTaskOperationContext context,
        AiTaskPlaybookRevision revision,
        IReadOnlyList<AiTaskPlaybookStepDefinition> steps,
        IReadOnlyList<AiTaskPlaybookDependencyDefinition> dependencies)
    {
        var stepEntities = steps
            .Select(step => new AiTaskPlaybookStep
            {
                Id = Guid.NewGuid(),
                TenantId = context.TenantId,
                PlaybookRevisionId = revision.Id,
                StepKey = step.StepKey.Trim(),
                Name = step.Name.Trim(),
                Description = TrimOrNull(step.Description),
                SortOrder = step.SortOrder,
                StepType = step.StepType.Trim(),
                Required = step.Required,
                DefaultPriority = step.DefaultPriority.Trim(),
                AssignmentPolicyJson = TrimOrNull(step.AssignmentPolicyJson),
                SlaPolicyJson = TrimOrNull(step.SlaPolicyJson),
                EvidencePolicyJson = TrimOrNull(step.EvidencePolicyJson),
                ClosurePolicyJson = TrimOrNull(step.ClosurePolicyJson),
                ConditionJson = AiTaskPlaybookConditionSerializer.Serialize(step.Condition),
                TargetDurationMinutes = step.TargetDurationMinutes,
            })
            .ToList();

        var stepIdByKey = stepEntities.ToDictionary(s => s.StepKey, s => s.Id, StringComparer.OrdinalIgnoreCase);
        var dependencyEntities = dependencies
            .Select(dependency => new AiTaskPlaybookStepDependency
            {
                Id = Guid.NewGuid(),
                TenantId = context.TenantId,
                PlaybookRevisionId = revision.Id,
                StepId = stepIdByKey[dependency.StepKey.Trim()],
                DependsOnStepId = stepIdByKey[dependency.DependsOnStepKey.Trim()],
                DependencyType = dependency.DependencyType.Trim(),
            })
            .ToList();

        dbContext.AiTaskPlaybookSteps.AddRange(stepEntities);
        dbContext.AiTaskPlaybookStepDependencies.AddRange(dependencyEntities);
        revision.Steps = stepEntities;
        revision.Dependencies = dependencyEntities;
    }

    private static AiTaskPlaybookRevision BuildRevision(
        AiTaskPlaybook playbook,
        Guid revisionId,
        string actor,
        DateTimeOffset now,
        string reason,
        IReadOnlyList<AiTaskPlaybookStepDefinition> steps,
        IReadOnlyList<AiTaskPlaybookDependencyDefinition> dependencies) =>
        new()
        {
            Id = revisionId,
            TenantId = playbook.TenantId,
            PlaybookId = playbook.Id,
            Version = playbook.Version,
            Name = playbook.Name,
            Description = playbook.Description,
            Category = playbook.Category,
            TriggerContext = playbook.TriggerContext,
            DefinitionSnapshotJson = SerializeDefinitionSnapshot(steps, dependencies),
            ChangedBy = actor,
            ChangedAtUtc = now,
            ChangeReason = reason,
        };

    private static string SerializeDefinitionSnapshot(
        IReadOnlyList<AiTaskPlaybookStepDefinition> steps,
        IReadOnlyList<AiTaskPlaybookDependencyDefinition> dependencies) =>
        Serialize(new { steps, dependencies });

    private static AiTaskPlaybookSummaryReadModel MapSummary(
        AiTaskPlaybook playbook,
        int stepCount,
        DateTimeOffset? lastRunAtUtc) =>
        new(
            playbook.Id,
            playbook.PlaybookKey,
            playbook.Name,
            playbook.Category,
            playbook.FacilityId,
            playbook.Version,
            playbook.Enabled,
            stepCount,
            playbook.UpdatedAtUtc,
            lastRunAtUtc);

    private static AiTaskPlaybookDetailReadModel MapDetail(
        AiTaskPlaybook playbook,
        IReadOnlyList<AiTaskPlaybookStep> steps,
        IReadOnlyList<AiTaskPlaybookStepDependency> dependencies)
    {
        var stepIdToKey = steps.ToDictionary(s => s.Id, s => s.StepKey);
        return new AiTaskPlaybookDetailReadModel(
            playbook.Id,
            playbook.PlaybookKey,
            playbook.Name,
            playbook.Description,
            playbook.Category,
            playbook.TriggerContext,
            playbook.FacilityId,
            playbook.Version,
            playbook.Enabled,
            playbook.EffectiveFromUtc,
            playbook.EffectiveUntilUtc,
            steps.Select(MapStep).ToList(),
            dependencies.Select(d => new AiTaskPlaybookDependencyReadModel(
                d.Id,
                stepIdToKey[d.StepId],
                stepIdToKey[d.DependsOnStepId],
                d.DependencyType)).ToList(),
            playbook.CreatedAtUtc,
            playbook.UpdatedAtUtc);
    }

    private static AiTaskPlaybookStepReadModel MapStep(AiTaskPlaybookStep step) =>
        new(
            step.Id,
            step.StepKey,
            step.Name,
            step.Description,
            step.SortOrder,
            step.StepType,
            step.Required,
            step.DefaultPriority,
            step.AssignmentPolicyJson,
            step.SlaPolicyJson,
            step.EvidencePolicyJson,
            step.ClosurePolicyJson,
            AiTaskPlaybookConditionSerializer.Deserialize(step.ConditionJson),
            step.TargetDurationMinutes);

    private static AiTaskPlaybookRunDetailReadModel MapRunDetail(AiTaskPlaybookRun run, string playbookName) =>
        new(
            run.Id,
            run.PlaybookId,
            playbookName,
            run.PlaybookVersion,
            run.Status,
            run.FacilityId,
            run.TriggerSource,
            run.SourceTaskId,
            run.SourceEntityJson,
            run.ParentTaskId,
            run.RequiredStepsCompleted,
            run.RequiredStepsTotal,
            run.OptionalStepsCompleted,
            run.OptionalStepsTotal,
            run.BlockedStepsCount,
            run.ApprovalsPending,
            run.AtRisk,
            run.BlockReason,
            run.CriticalPathJson,
            run.Steps
                .OrderBy(s => s.SortOrder)
                .Select(s => new AiTaskPlaybookRunStepReadModel(
                    s.Id,
                    s.StepKey,
                    s.Name,
                    s.Required,
                    s.StepType,
                    s.Status,
                    s.DependencyState,
                    s.LinkedTaskId,
                    s.BlockReason,
                    s.SortOrder,
                    s.ReadyAtUtc,
                    s.StartedAtUtc,
                    s.CompletedAtUtc))
                .ToList(),
            run.StartedAtUtc,
            run.CompletedAtUtc,
            run.CancelledAtUtc,
            run.CancellationReason);

    private static string? ValidatePlaybookPayload(
        string category,
        IReadOnlyList<AiTaskPlaybookStepDefinition> steps,
        IReadOnlyList<AiTaskPlaybookDependencyDefinition> dependencies)
    {
        if (!AiTaskPlaybookCategories.All.Contains(category.Trim()))
        {
            return "A valid playbook category is required.";
        }

        if (steps.Count == 0)
        {
            return "At least one step is required.";
        }

        foreach (var step in steps)
        {
            if (!AiTaskPlaybookStepTypes.All.Contains(step.StepType.Trim()))
            {
                return $"Unsupported step type: {step.StepType}";
            }

            if (!AiTaskPriorities.All.Contains(step.DefaultPriority.Trim()))
            {
                return $"Unsupported default priority: {step.DefaultPriority}";
            }
        }

        foreach (var dependency in dependencies)
        {
            if (!AiTaskPlaybookDependencyTypes.All.Contains(dependency.DependencyType.Trim()))
            {
                return $"Unsupported dependency type: {dependency.DependencyType}";
            }
        }

        return null;
    }

    private static (bool ApprovalRequired, bool FourEyesRequired) DeserializeClosurePolicy(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return (false, false);
        }

        try
        {
            var policy = JsonSerializer.Deserialize<ClosurePolicyConfig>(json, JsonOptions);
            return (policy?.ApprovalRequired ?? false, policy?.FourEyesRequired ?? false);
        }
        catch (JsonException)
        {
            return (false, false);
        }
    }

    private async Task<string> GenerateTaskReferenceAsync(AiTaskOperationContext context, CancellationToken ct)
    {
        for (var attempt = 0; attempt < 5; attempt++)
        {
            var suffix = Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
            var reference = $"AT-{suffix}";
            var exists = await dbContext.AiTasks.AsNoTracking()
                .AnyAsync(t => t.TenantId == context.TenantId && t.TaskReference == reference, ct);
            if (!exists)
            {
                return reference;
            }
        }

        return $"AT-{Guid.NewGuid():N}".ToUpperInvariant();
    }

    private static bool IsTerminalRunStepStatus(string status) =>
        status is AiTaskPlaybookRunStepStatuses.Completed
            or AiTaskPlaybookRunStepStatuses.Cancelled
            or AiTaskPlaybookRunStepStatuses.Failed
            or AiTaskPlaybookRunStepStatuses.Skipped;

    private static void AppendAuditEvent(
        AiTaskOperationContext context,
        AiTask task,
        string eventType,
        string actorId,
        string actorLabel,
        DateTimeOffset occurredAtUtc,
        string? previousStatus,
        string? nextStatus,
        string? note)
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
            Note = TrimOrNull(note),
            PreviousStatus = previousStatus,
            NextStatus = nextStatus,
            CorrelationId = context.CorrelationId,
        });
    }

    private static string Serialize<T>(T value) => JsonSerializer.Serialize(value, JsonOptions);

    private static string? TrimOrNull(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private sealed record RevisionGraph(
        AiTaskPlaybookRevision Revision,
        IReadOnlyList<AiTaskPlaybookStep> Steps,
        IReadOnlyList<AiTaskPlaybookStepDependency> Dependencies);

    private sealed record ClosurePolicyConfig(bool? ApprovalRequired, bool? FourEyesRequired);
}
