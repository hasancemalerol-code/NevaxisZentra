using System.Text.Json;
using CrevaFactOS.Persistence.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace CrevaFactOS.Persistence.AiTasks;

public interface IAiTaskRepository
{
    Task<AiTaskListReadModel> ListTasksAsync(
        AiTaskOperationContext context,
        AiTaskListFilters filters,
        CancellationToken cancellationToken = default);

    Task<AiTaskDetailReadModel?> GetTaskByIdAsync(
        AiTaskOperationContext context,
        Guid taskId,
        CancellationToken cancellationToken = default);

    Task<AiTaskDetailReadModel?> GetTaskByIdempotencyKeyAsync(
        AiTaskOperationContext context,
        string idempotencyKey,
        CancellationToken cancellationToken = default);

    Task<AiTaskDetailReadModel?> GetTaskBySourceAsync(
        AiTaskOperationContext context,
        string sourceModule,
        string sourceEntityId,
        CancellationToken cancellationToken = default);

    Task<AiTaskWriteResult> CreateTaskAsync(
        AiTaskOperationContext context,
        CreateAiTaskCommand command,
        CancellationToken cancellationToken = default);

    Task<AiTaskWriteResult> CreateSourceLinkedTaskAsync(
        AiTaskOperationContext context,
        CreateSourceLinkedAiTaskCommand command,
        CancellationToken cancellationToken = default);

    Task<AiTaskWriteResult> TransitionTaskAsync(
        AiTaskOperationContext context,
        TransitionAiTaskCommand command,
        CancellationToken cancellationToken = default);
}

public sealed class AiTaskRepository(
    CrevaFactOSDbContext dbContext,
    TenantDatabaseSession tenantDatabaseSession) : IAiTaskRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public Task<AiTaskListReadModel> ListTasksAsync(
        AiTaskOperationContext context,
        AiTaskListFilters filters,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(
            async ct =>
            {
                var page = filters.Page < 1 ? 1 : filters.Page;
                var pageSize = filters.PageSize is < 1 or > 100 ? 25 : filters.PageSize;

                var query = dbContext.AiTasks.AsNoTracking()
                    .Where(t => t.TenantId == context.TenantId && t.FacilityId == context.FacilityId);

                if (!string.IsNullOrWhiteSpace(filters.Status))
                {
                    query = query.Where(t => t.Status == filters.Status.Trim());
                }

                if (!string.IsNullOrWhiteSpace(filters.Category))
                {
                    query = query.Where(t => t.Category == filters.Category.Trim());
                }

                if (!string.IsNullOrWhiteSpace(filters.Priority))
                {
                    query = query.Where(t => t.Priority == filters.Priority.Trim());
                }

                if (!string.IsNullOrWhiteSpace(filters.SourceModule))
                {
                    query = query.Where(t => t.SourceModule == filters.SourceModule.Trim());
                }

                var totalCount = await query.CountAsync(ct);
                var items = await query
                    .OrderByDescending(t => t.UpdatedAtUtc)
                    .ThenByDescending(t => t.CreatedAtUtc)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync(ct);

                return new AiTaskListReadModel(
                    items.Select(MapSummary).ToList(),
                    totalCount,
                    page,
                    pageSize);
            },
            cancellationToken);

    public Task<AiTaskDetailReadModel?> GetTaskByIdAsync(
        AiTaskOperationContext context,
        Guid taskId,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(
            async ct => await LoadDetailAsync(context, t => t.Id == taskId, ct),
            cancellationToken);

    public Task<AiTaskDetailReadModel?> GetTaskByIdempotencyKeyAsync(
        AiTaskOperationContext context,
        string idempotencyKey,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(
            async ct =>
            {
                if (string.IsNullOrWhiteSpace(idempotencyKey))
                {
                    return null;
                }

                var normalizedKey = idempotencyKey.Trim();
                return await LoadDetailAsync(
                    context,
                    t => t.IdempotencyKey == normalizedKey,
                    ct);
            },
            cancellationToken);

    public Task<AiTaskDetailReadModel?> GetTaskBySourceAsync(
        AiTaskOperationContext context,
        string sourceModule,
        string sourceEntityId,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(
            async ct =>
            {
                if (string.IsNullOrWhiteSpace(sourceModule) || string.IsNullOrWhiteSpace(sourceEntityId))
                {
                    return null;
                }

                var normalizedModule = sourceModule.Trim();
                var normalizedEntityId = sourceEntityId.Trim();
                return await LoadDetailAsync(
                    context,
                    t => t.SourceModule == normalizedModule && t.SourceEntityId == normalizedEntityId,
                    ct);
            },
            cancellationToken);

    public Task<AiTaskWriteResult> CreateTaskAsync(
        AiTaskOperationContext context,
        CreateAiTaskCommand command,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteWriteAsync(
            async ct =>
            {
                var validationError = ValidateCreateCommand(command);
                if (validationError is not null)
                {
                    return AiTaskWriteResult.Fail(AiTaskFailureCodes.ValidationFailed, validationError);
                }

                var facilityExists = await dbContext.Facilities.AsNoTracking()
                    .AnyAsync(
                        f => f.TenantId == context.TenantId && f.Id == context.FacilityId,
                        ct);
                if (!facilityExists)
                {
                    return AiTaskWriteResult.Fail(
                        AiTaskFailureCodes.FacilityScopeDenied,
                        "Facility not found.");
                }

                var now = DateTimeOffset.UtcNow;
                var actor = context.IdentitySubjectId.ToString();
                var task = BuildTaskEntity(context, command, now, actor);
                task.TaskReference = await GenerateTaskReferenceAsync(context, ct);

                dbContext.AiTasks.Add(task);
                AppendAuditEvent(
                    context,
                    task,
                    AiTaskAuditEventTypes.Created,
                    actor,
                    "AI Task Center",
                    now,
                    null,
                    task.Status,
                    null);

                await dbContext.SaveChangesAsync(ct);
                return AiTaskWriteResult.Success(task.Id, task.Version);
            },
            cancellationToken);

    public Task<AiTaskWriteResult> CreateSourceLinkedTaskAsync(
        AiTaskOperationContext context,
        CreateSourceLinkedAiTaskCommand command,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteWriteAsync(
            async ct =>
            {
                var validationError = ValidateSourceLinkedCommand(command);
                if (validationError is not null)
                {
                    return AiTaskWriteResult.Fail(AiTaskFailureCodes.ValidationFailed, validationError);
                }

                var fingerprint = AiTaskIdempotencyFingerprint.ComputeSourceLinkedFingerprint(context, command);
                var normalizedKey = command.IdempotencyKey.Trim();
                var existing = await dbContext.AiTasks.AsNoTracking()
                    .SingleOrDefaultAsync(
                        t => t.TenantId == context.TenantId && t.IdempotencyKey == normalizedKey,
                        ct);

                if (existing is not null)
                {
                    return ResolveIdempotentReplay(existing, fingerprint);
                }

                var facilityExists = await dbContext.Facilities.AsNoTracking()
                    .AnyAsync(
                        f => f.TenantId == context.TenantId && f.Id == context.FacilityId,
                        ct);
                if (!facilityExists)
                {
                    return AiTaskWriteResult.Fail(
                        AiTaskFailureCodes.FacilityScopeDenied,
                        "Facility not found.");
                }

                var now = DateTimeOffset.UtcNow;
                var actor = context.IdentitySubjectId.ToString();
                var task = BuildTaskEntity(context, command, now, actor);
                task.TaskReference = await GenerateTaskReferenceAsync(context, ct);
                task.SourceModule = command.SourceModule.Trim();
                task.SourceEntityId = command.SourceEntityId.Trim();
                task.IdempotencyKey = normalizedKey;
                task.IdempotencyPayloadFingerprint = fingerprint;

                dbContext.AiTasks.Add(task);
                AppendAuditEvent(
                    context,
                    task,
                    AiTaskAuditEventTypes.Created,
                    actor,
                    "AI Task Center",
                    now,
                    null,
                    task.Status,
                    null);

                await dbContext.SaveChangesAsync(ct);
                return AiTaskWriteResult.Success(task.Id, task.Version);
            },
            cancellationToken);

    public Task<AiTaskWriteResult> TransitionTaskAsync(
        AiTaskOperationContext context,
        TransitionAiTaskCommand command,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteWriteAsync(
            async ct =>
            {
                if (string.IsNullOrWhiteSpace(command.TargetStatus)
                    || !AiTaskStatuses.All.Contains(command.TargetStatus.Trim()))
                {
                    return AiTaskWriteResult.Fail(
                        AiTaskFailureCodes.ValidationFailed,
                        "A valid target status is required.");
                }

                var targetStatus = command.TargetStatus.Trim();
                var noteError = AiTaskLifecycle.ValidateTransitionNote(targetStatus, command.Note);
                if (noteError is not null)
                {
                    return AiTaskWriteResult.Fail(AiTaskFailureCodes.ValidationFailed, noteError);
                }

                var task = await dbContext.AiTasks
                    .SingleOrDefaultAsync(
                        t => t.TenantId == context.TenantId
                             && t.FacilityId == context.FacilityId
                             && t.Id == command.TaskId,
                        ct);

                if (task is null)
                {
                    return AiTaskWriteResult.Fail(AiTaskFailureCodes.NotFound, "AI task not found.");
                }

                if (task.Version != command.ExpectedVersion)
                {
                    return AiTaskWriteResult.Fail(
                        AiTaskFailureCodes.ConcurrencyConflict,
                        "AI task was modified by another request.");
                }

                if (!AiTaskLifecycle.CanTransition(task.Status, targetStatus))
                {
                    return AiTaskWriteResult.Fail(
                        AiTaskFailureCodes.InvalidTransition,
                        $"Transition from '{task.Status}' to '{targetStatus}' is not allowed.");
                }

                var now = DateTimeOffset.UtcNow;
                var actor = context.IdentitySubjectId.ToString();
                var previousStatus = task.Status;
                task.Status = targetStatus;
                task.UpdatedAtUtc = now;
                task.UpdatedBy = actor;
                task.Version++;

                ApplyApprovalTransition(task, targetStatus, actor, now, command.Note);

                if (string.Equals(targetStatus, AiTaskStatuses.Completed, StringComparison.Ordinal))
                {
                    task.CompletedAtUtc = now;
                }

                AppendAuditEvent(
                    context,
                    task,
                    AiTaskLifecycle.ResolveTransitionAuditEventType(targetStatus),
                    actor,
                    actor,
                    now,
                    previousStatus,
                    targetStatus,
                    command.Note);

                await dbContext.SaveChangesAsync(ct);
                return AiTaskWriteResult.Success(task.Id, task.Version);
            },
            cancellationToken);

    private async Task<AiTaskDetailReadModel?> LoadDetailAsync(
        AiTaskOperationContext context,
        System.Linq.Expressions.Expression<Func<AiTask, bool>> predicate,
        CancellationToken ct)
    {
        var task = await dbContext.AiTasks.AsNoTracking()
            .Include(t => t.AuditEvents)
            .Where(t => t.TenantId == context.TenantId && t.FacilityId == context.FacilityId)
            .Where(predicate)
            .SingleOrDefaultAsync(ct);

        return task is null ? null : MapDetail(task);
    }

    private static AiTaskWriteResult ResolveIdempotentReplay(AiTask existing, string fingerprint)
    {
        if (string.IsNullOrWhiteSpace(existing.IdempotencyPayloadFingerprint))
        {
            return AiTaskWriteResult.Fail(
                AiTaskFailureCodes.IdempotencyLegacyUnverifiable,
                "Existing task cannot be verified for idempotent replay.");
        }

        if (!string.Equals(existing.IdempotencyPayloadFingerprint, fingerprint, StringComparison.Ordinal))
        {
            return AiTaskWriteResult.Fail(
                AiTaskFailureCodes.IdempotencyConflict,
                "Idempotency key was already used with a different payload.");
        }

        return AiTaskWriteResult.Success(existing.Id, existing.Version, idempotentReplay: true);
    }

    private static AiTask BuildTaskEntity(
        AiTaskOperationContext context,
        CreateSourceLinkedAiTaskCommand command,
        DateTimeOffset now,
        string actor) =>
        BuildTaskEntity(
            context,
            command.Title,
            command.Summary,
            command.Category,
            command.Priority,
            command.RiskLevel,
            command.DetectedIssue,
            command.Reasoning,
            command.Recommendation,
            command.Confidence,
            command.Impact,
            command.Sources,
            command.SiteId,
            command.ProductionLineId,
            command.MachineId,
            command.MoldId,
            command.ProductionOrderId,
            command.SalesOrderId,
            command.MaterialId,
            command.SupplierId,
            command.CustomerId,
            command.DueAtUtc,
            now,
            actor);

    private static AiTask BuildTaskEntity(
        AiTaskOperationContext context,
        CreateAiTaskCommand command,
        DateTimeOffset now,
        string actor) =>
        BuildTaskEntity(
            context,
            command.Title,
            command.Summary,
            command.Category,
            command.Priority,
            command.RiskLevel,
            command.DetectedIssue,
            command.Reasoning,
            command.Recommendation,
            command.Confidence,
            command.Impact,
            command.Sources,
            command.SiteId,
            command.ProductionLineId,
            command.MachineId,
            command.MoldId,
            command.ProductionOrderId,
            command.SalesOrderId,
            command.MaterialId,
            command.SupplierId,
            command.CustomerId,
            command.DueAtUtc,
            now,
            actor);

    private static AiTask BuildTaskEntity(
        AiTaskOperationContext context,
        string title,
        string summary,
        string category,
        string priority,
        string riskLevel,
        string detectedIssue,
        string reasoning,
        AiTaskRecommendationModel recommendation,
        AiTaskConfidenceModel confidence,
        AiTaskImpactModel impact,
        IReadOnlyList<AiTaskSourceReferenceModel> sources,
        string? siteId,
        string? productionLineId,
        string? machineId,
        string? moldId,
        string? productionOrderId,
        string? salesOrderId,
        string? materialId,
        string? supplierId,
        string? customerId,
        DateTimeOffset? dueAtUtc,
        DateTimeOffset now,
        string actor)
    {
        var approvalState = AiTaskLifecycle.ResolveInitialApprovalState(
            recommendation.ActionType.Trim(),
            recommendation.RequiresApproval);

        return new AiTask
        {
            Id = Guid.NewGuid(),
            TenantId = context.TenantId,
            FacilityId = context.FacilityId,
            Title = title.Trim(),
            Summary = summary.Trim(),
            Category = category.Trim(),
            Priority = priority.Trim(),
            RiskLevel = riskLevel.Trim(),
            Status = AiTaskStatuses.Draft,
            DetectedIssue = detectedIssue.Trim(),
            Reasoning = reasoning.Trim(),
            RecommendationJson = Serialize(recommendation),
            ConfidenceJson = Serialize(confidence),
            ImpactJson = Serialize(impact),
            SourcesJson = Serialize(sources),
            ApprovalState = approvalState,
            SiteId = TrimOrNull(siteId),
            ProductionLineId = TrimOrNull(productionLineId),
            MachineId = TrimOrNull(machineId),
            MoldId = TrimOrNull(moldId),
            ProductionOrderId = TrimOrNull(productionOrderId),
            SalesOrderId = TrimOrNull(salesOrderId),
            MaterialId = TrimOrNull(materialId),
            SupplierId = TrimOrNull(supplierId),
            CustomerId = TrimOrNull(customerId),
            RuleSetVersion = AiTaskConstants.RuleSetVersion,
            DueAtUtc = dueAtUtc,
            Version = 1,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
            CreatedBy = actor,
            UpdatedBy = actor,
        };
    }

    private static void ApplyApprovalTransition(
        AiTask task,
        string targetStatus,
        string actor,
        DateTimeOffset now,
        string? note)
    {
        var nextApprovalState = AiTaskLifecycle.ResolveApprovalStateForTransition(task.ApprovalState, targetStatus);
        if (nextApprovalState is null)
        {
            return;
        }

        task.ApprovalState = nextApprovalState;

        if (string.Equals(targetStatus, AiTaskStatuses.AwaitingApproval, StringComparison.Ordinal))
        {
            task.ApprovalRequestedAtUtc = now;
            task.ApprovalRequestedBy = actor;
            return;
        }

        if (string.Equals(targetStatus, AiTaskStatuses.Approved, StringComparison.Ordinal)
            || string.Equals(targetStatus, AiTaskStatuses.Rejected, StringComparison.Ordinal))
        {
            task.ApprovalDecidedAtUtc = now;
            task.ApprovalDecidedBy = actor;
            task.ApprovalDecisionNote = TrimOrNull(note);
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

    private void AppendAuditEvent(
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
        });
    }

    private static string? ValidateCreateCommand(CreateAiTaskCommand command) =>
        ValidateSharedCreateFields(
            command.Title,
            command.Summary,
            command.Category,
            command.Priority,
            command.RiskLevel,
            command.DetectedIssue,
            command.Reasoning,
            command.Recommendation,
            command.Confidence,
            command.Impact,
            command.Sources);

    private static string? ValidateSourceLinkedCommand(CreateSourceLinkedAiTaskCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.IdempotencyKey))
        {
            return "Idempotency key is required.";
        }

        if (string.IsNullOrWhiteSpace(command.SourceModule))
        {
            return "Source module is required.";
        }

        if (string.IsNullOrWhiteSpace(command.SourceEntityId))
        {
            return "Source entity identifier is required.";
        }

        return ValidateSharedCreateFields(
            command.Title,
            command.Summary,
            command.Category,
            command.Priority,
            command.RiskLevel,
            command.DetectedIssue,
            command.Reasoning,
            command.Recommendation,
            command.Confidence,
            command.Impact,
            command.Sources);
    }

    private static string? ValidateSharedCreateFields(
        string title,
        string summary,
        string category,
        string priority,
        string riskLevel,
        string detectedIssue,
        string reasoning,
        AiTaskRecommendationModel recommendation,
        AiTaskConfidenceModel confidence,
        AiTaskImpactModel impact,
        IReadOnlyList<AiTaskSourceReferenceModel> sources)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return "Title is required.";
        }

        if (string.IsNullOrWhiteSpace(summary))
        {
            return "Summary is required.";
        }

        if (string.IsNullOrWhiteSpace(category) || !AiTaskCategories.All.Contains(category.Trim()))
        {
            return "A valid category is required.";
        }

        if (string.IsNullOrWhiteSpace(priority) || !AiTaskPriorities.All.Contains(priority.Trim()))
        {
            return "A valid priority is required.";
        }

        if (string.IsNullOrWhiteSpace(riskLevel) || !AiTaskRiskLevels.All.Contains(riskLevel.Trim()))
        {
            return "A valid risk level is required.";
        }

        if (string.IsNullOrWhiteSpace(detectedIssue))
        {
            return "Detected issue is required.";
        }

        if (string.IsNullOrWhiteSpace(reasoning))
        {
            return "Reasoning is required.";
        }

        if (string.IsNullOrWhiteSpace(recommendation.Title)
            || string.IsNullOrWhiteSpace(recommendation.Description)
            || string.IsNullOrWhiteSpace(recommendation.ActionType)
            || !AiTaskActionTypes.All.Contains(recommendation.ActionType.Trim()))
        {
            return "A valid recommendation is required.";
        }

        if (string.IsNullOrWhiteSpace(confidence.Explanation))
        {
            return "Confidence explanation is required.";
        }

        if (string.IsNullOrWhiteSpace(impact.Summary))
        {
            return "Impact summary is required.";
        }

        if (sources.Count == 0)
        {
            return "At least one source reference is required.";
        }

        return null;
    }

    private static AiTaskSummaryReadModel MapSummary(AiTask task) =>
        new(
            task.Id,
            task.TaskReference,
            task.Title,
            task.Summary,
            task.Category,
            task.Priority,
            task.RiskLevel,
            task.Status,
            task.ApprovalState,
            task.SourceModule,
            task.SourceEntityId,
            task.DueAtUtc,
            task.CreatedAtUtc,
            task.UpdatedAtUtc,
            task.Version);

    private static AiTaskDetailReadModel MapDetail(AiTask task) =>
        new(
            task.Id,
            task.TaskReference,
            task.Title,
            task.Summary,
            task.Category,
            task.Priority,
            task.RiskLevel,
            task.Status,
            task.DetectedIssue,
            task.Reasoning,
            DeserializeRequired<AiTaskRecommendationModel>(task.RecommendationJson),
            DeserializeRequired<AiTaskConfidenceModel>(task.ConfidenceJson),
            DeserializeRequired<AiTaskImpactModel>(task.ImpactJson),
            DeserializeRequiredList<AiTaskSourceReferenceModel>(task.SourcesJson),
            DeserializeOptional<AiTaskAssigneeModel>(task.AssigneeJson),
            new AiTaskApprovalReadModel(
                task.ApprovalState,
                task.ApprovalRequestedAtUtc,
                task.ApprovalRequestedBy,
                task.ApprovalDecidedAtUtc,
                task.ApprovalDecidedBy,
                task.ApprovalDecisionNote),
            DeserializeOptional<AiTaskExecutionReadModel>(task.ExecutionJson),
            task.SiteId,
            task.ProductionLineId,
            task.MachineId,
            task.MoldId,
            task.ProductionOrderId,
            task.SalesOrderId,
            task.MaterialId,
            task.SupplierId,
            task.CustomerId,
            task.SourceModule,
            task.SourceEntityId,
            task.RuleSetVersion,
            task.DueAtUtc,
            task.CompletedAtUtc,
            task.CreatedAtUtc,
            task.UpdatedAtUtc,
            task.Version,
            task.AuditEvents
                .OrderBy(e => e.OccurredAtUtc)
                .Select(e => new AiTaskAuditEventReadModel(
                    e.Id,
                    e.TaskId,
                    e.EventType,
                    e.OccurredAtUtc,
                    e.ActorType,
                    e.ActorId,
                    e.ActorLabel,
                    e.Note,
                    e.PreviousStatus,
                    e.NextStatus))
                .ToList());

    private static string Serialize<T>(T value) =>
        JsonSerializer.Serialize(value, JsonOptions);

    private static T DeserializeRequired<T>(string json) =>
        JsonSerializer.Deserialize<T>(json, JsonOptions)
        ?? throw new InvalidOperationException($"Unable to deserialize {typeof(T).Name}.");

    private static T? DeserializeOptional<T>(string? json) =>
        string.IsNullOrWhiteSpace(json) ? default : JsonSerializer.Deserialize<T>(json, JsonOptions);

    private static List<T> DeserializeRequiredList<T>(string json) =>
        JsonSerializer.Deserialize<List<T>>(json, JsonOptions) ?? [];

    private static string? TrimOrNull(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

public sealed class UnavailableAiTaskRepository : IAiTaskRepository
{
    private static AiTaskWriteResult Unavailable() =>
        AiTaskWriteResult.Fail(AiTaskFailureCodes.Unavailable, "AI task persistence is unavailable.");

    public Task<AiTaskListReadModel> ListTasksAsync(
        AiTaskOperationContext context,
        AiTaskListFilters filters,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(new AiTaskListReadModel([], 0, 1, 25));

    public Task<AiTaskDetailReadModel?> GetTaskByIdAsync(
        AiTaskOperationContext context,
        Guid taskId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<AiTaskDetailReadModel?>(null);

    public Task<AiTaskDetailReadModel?> GetTaskByIdempotencyKeyAsync(
        AiTaskOperationContext context,
        string idempotencyKey,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<AiTaskDetailReadModel?>(null);

    public Task<AiTaskDetailReadModel?> GetTaskBySourceAsync(
        AiTaskOperationContext context,
        string sourceModule,
        string sourceEntityId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<AiTaskDetailReadModel?>(null);

    public Task<AiTaskWriteResult> CreateTaskAsync(
        AiTaskOperationContext context,
        CreateAiTaskCommand command,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Unavailable());

    public Task<AiTaskWriteResult> CreateSourceLinkedTaskAsync(
        AiTaskOperationContext context,
        CreateSourceLinkedAiTaskCommand command,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Unavailable());

    public Task<AiTaskWriteResult> TransitionTaskAsync(
        AiTaskOperationContext context,
        TransitionAiTaskCommand command,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Unavailable());
}
