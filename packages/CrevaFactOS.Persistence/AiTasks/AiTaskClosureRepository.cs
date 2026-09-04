using System.Text.Json;
using CrevaFactOS.Persistence.Teams;
using CrevaFactOS.Persistence.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace CrevaFactOS.Persistence.AiTasks;

public interface IAiTaskClosureRepository
{
    Task<IReadOnlyList<AiTaskEvidenceReadModel>> ListEvidenceAsync(
        AiTaskOperationContext context,
        Guid taskId,
        CancellationToken cancellationToken = default);

    Task<AiTaskClosureValidationResult> GetClosureValidationAsync(
        AiTaskOperationContext context,
        Guid taskId,
        CancellationToken cancellationToken = default);

    Task<AiTaskWriteResult> SubmitEvidenceAsync(
        AiTaskOperationContext context,
        SubmitAiTaskEvidenceCommand command,
        CancellationToken cancellationToken = default);

    Task<AiTaskWriteResult> UpdateResolutionAsync(
        AiTaskOperationContext context,
        UpdateAiTaskResolutionCommand command,
        CancellationToken cancellationToken = default);

    Task<AiTaskWriteResult> RequestClosureAsync(
        AiTaskOperationContext context,
        RequestAiTaskClosureCommand command,
        CancellationToken cancellationToken = default);

    Task<AiTaskWriteResult> ApproveClosureAsync(
        AiTaskOperationContext context,
        ApproveAiTaskClosureCommand command,
        CancellationToken cancellationToken = default);

    Task<AiTaskWriteResult> RejectClosureAsync(
        AiTaskOperationContext context,
        RejectAiTaskClosureCommand command,
        CancellationToken cancellationToken = default);

    Task<AiTaskWriteResult> ReopenAsync(
        AiTaskOperationContext context,
        ReopenAiTaskCommand command,
        CancellationToken cancellationToken = default);
}

public sealed class AiTaskClosureRepository(
    CrevaFactOSDbContext dbContext,
    TenantDatabaseSession tenantDatabaseSession,
    IAiTaskClosureEngine closureEngine,
    ITeamsNotificationRepository teamsNotificationRepository,
    IAiTaskPlaybookOrchestrator playbookOrchestrator) : IAiTaskClosureRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public Task<IReadOnlyList<AiTaskEvidenceReadModel>> ListEvidenceAsync(
        AiTaskOperationContext context,
        Guid taskId,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(
            async ct =>
            {
                var items = await dbContext.AiTaskEvidence.AsNoTracking()
                    .Where(e => e.TenantId == context.TenantId
                        && e.FacilityId == context.FacilityId
                        && e.TaskId == taskId)
                    .OrderByDescending(e => e.SubmittedAtUtc)
                    .ToListAsync(ct);

                return (IReadOnlyList<AiTaskEvidenceReadModel>)items
                    .Select(MapEvidence)
                    .ToList();
            },
            cancellationToken);

    public Task<AiTaskClosureValidationResult> GetClosureValidationAsync(
        AiTaskOperationContext context,
        Guid taskId,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(
            async ct =>
            {
                var task = await LoadTaskAsync(context, taskId, ct);
                if (task is null)
                {
                    return AiTaskClosureValidationResult.Fail(
                        new AiTaskClosureValidationIssue("not-found", "Görev bulunamadı."));
                }

                var evidence = await LoadEvidenceAsync(context, taskId, ct);
                var policies = await LoadPoliciesAsync(context, ct);
                var policy = closureEngine.ResolvePolicy(task, policies);
                return closureEngine.ValidateClosureRequest(task, evidence, policy);
            },
            cancellationToken);

    public Task<AiTaskWriteResult> SubmitEvidenceAsync(
        AiTaskOperationContext context,
        SubmitAiTaskEvidenceCommand command,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteWriteAsync(
            async ct =>
            {
                var task = await LoadTrackedTaskAsync(context, command.TaskId, ct);
                if (task is null)
                {
                    return AiTaskWriteResult.Fail(AiTaskFailureCodes.NotFound, "Görev bulunamadı.");
                }

                if (task.Version != command.ExpectedVersion)
                {
                    return AiTaskWriteResult.Fail(
                        AiTaskFailureCodes.ConcurrencyConflict,
                        "Görev sürümü güncel değil.");
                }

                if (!AiTaskEvidenceTypes.All.Contains(command.EvidenceType.Trim()))
                {
                    return AiTaskWriteResult.Fail(
                        AiTaskFailureCodes.ValidationFailed,
                        "Geçersiz kanıt türü.");
                }

                if (string.IsNullOrWhiteSpace(command.Title))
                {
                    return AiTaskWriteResult.Fail(
                        AiTaskFailureCodes.ValidationFailed,
                        "Kanıt başlığı zorunludur.");
                }

                var now = DateTimeOffset.UtcNow;
                var actorLabel = await ResolveActorLabelAsync(context, ct);
                var evidence = new AiTaskEvidence
                {
                    Id = Guid.NewGuid(),
                    TenantId = context.TenantId,
                    FacilityId = context.FacilityId,
                    TaskId = task.Id,
                    EvidenceType = command.EvidenceType.Trim(),
                    Title = command.Title.Trim(),
                    Description = TrimOrNull(command.Description),
                    ReferenceMetadataJson = TrimOrNull(command.ReferenceMetadataJson),
                    SourceModule = TrimOrNull(command.SourceModule),
                    SourceEntityId = TrimOrNull(command.SourceEntityId),
                    SubmittedByMembershipId = context.MembershipId,
                    SubmittedAtUtc = now,
                    SubmittedByLabel = actorLabel,
                };

                dbContext.AiTaskEvidence.Add(evidence);
                task.UpdatedAtUtc = now;
                task.UpdatedBy = actorLabel;
                task.Version += 1;

                AppendAuditEvent(
                    context,
                    task,
                    AiTaskAuditEventTypes.EvidenceSubmitted,
                    context.IdentitySubjectId.ToString(),
                    actorLabel,
                    now,
                    metadata: new
                    {
                        evidenceId = evidence.Id,
                        evidenceType = evidence.EvidenceType,
                        title = evidence.Title,
                    },
                    note: command.Description);

                await dbContext.SaveChangesAsync(ct);
                return AiTaskWriteResult.Success(task.Id, task.Version);
            },
            cancellationToken);

    public Task<AiTaskWriteResult> UpdateResolutionAsync(
        AiTaskOperationContext context,
        UpdateAiTaskResolutionCommand command,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteWriteAsync(
            async ct =>
            {
                var task = await LoadTrackedTaskAsync(context, command.TaskId, ct);
                if (task is null)
                {
                    return AiTaskWriteResult.Fail(AiTaskFailureCodes.NotFound, "Görev bulunamadı.");
                }

                if (task.Version != command.ExpectedVersion)
                {
                    return AiTaskWriteResult.Fail(
                        AiTaskFailureCodes.ConcurrencyConflict,
                        "Görev sürümü güncel değil.");
                }

                if (string.IsNullOrWhiteSpace(command.ResolutionSummary))
                {
                    return AiTaskWriteResult.Fail(
                        AiTaskFailureCodes.ValidationFailed,
                        "Çözüm özeti zorunludur.");
                }

                if (!AiTaskResolutionCodes.All.Contains(command.ResolutionCode.Trim()))
                {
                    return AiTaskWriteResult.Fail(
                        AiTaskFailureCodes.ValidationFailed,
                        "Geçersiz çözüm kodu.");
                }

                var now = DateTimeOffset.UtcNow;
                var actorLabel = await ResolveActorLabelAsync(context, ct);
                task.ResolutionSummary = command.ResolutionSummary.Trim();
                task.ResolutionCode = command.ResolutionCode.Trim();
                task.ClosureReason = TrimOrNull(command.ClosureReason);
                task.UpdatedAtUtc = now;
                task.UpdatedBy = actorLabel;
                task.Version += 1;

                AppendAuditEvent(
                    context,
                    task,
                    AiTaskAuditEventTypes.ResolutionUpdated,
                    context.IdentitySubjectId.ToString(),
                    actorLabel,
                    now,
                    metadata: new
                    {
                        resolutionCode = task.ResolutionCode,
                    },
                    note: task.ResolutionSummary);

                await dbContext.SaveChangesAsync(ct);
                return AiTaskWriteResult.Success(task.Id, task.Version);
            },
            cancellationToken);

    public Task<AiTaskWriteResult> RequestClosureAsync(
        AiTaskOperationContext context,
        RequestAiTaskClosureCommand command,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteWriteAsync(
            async ct =>
            {
                var task = await LoadTrackedTaskAsync(context, command.TaskId, ct);
                if (task is null)
                {
                    return AiTaskWriteResult.Fail(AiTaskFailureCodes.NotFound, "Görev bulunamadı.");
                }

                if (task.Version != command.ExpectedVersion)
                {
                    return AiTaskWriteResult.Fail(
                        AiTaskFailureCodes.ConcurrencyConflict,
                        "Görev sürümü güncel değil.");
                }

                var evidence = await LoadEvidenceAsync(context, task.Id, ct);
                var policies = await LoadPoliciesAsync(context, ct);
                var policy = closureEngine.ResolvePolicy(task, policies);
                var validation = closureEngine.ValidateClosureRequest(task, evidence, policy);
                if (!validation.IsValid)
                {
                    return AiTaskWriteResult.Fail(
                        AiTaskFailureCodes.ClosureValidationFailed,
                        string.Join(" ", validation.Issues.Select(issue => issue.Message)));
                }

                var now = DateTimeOffset.UtcNow;
                var actorLabel = await ResolveActorLabelAsync(context, ct);
                task.ClosureApprovalRequired = policy.ApprovalRequired;
                task.ClosureRequestedAtUtc = now;
                task.ClosureRequestedByMembershipId = context.MembershipId;
                task.UpdatedAtUtc = now;
                task.UpdatedBy = actorLabel;
                task.Version += 1;

                if (policy.ApprovalRequired)
                {
                    task.ClosureState = AiTaskClosureStates.AwaitingClosureApproval;
                    AppendAuditEvent(
                        context,
                        task,
                        AiTaskAuditEventTypes.ClosureRequested,
                        context.IdentitySubjectId.ToString(),
                        actorLabel,
                        now,
                        note: command.Note,
                        metadata: new { approvalRequired = true, policyKey = policy.PolicyKey });

                    await NotifyClosureApprovalRequestedAsync(context, task, ct);
                }
                else
                {
                    await CompleteClosureAsync(context, task, actorLabel, now, command.Note, ct);
                }

                await dbContext.SaveChangesAsync(ct);
                return AiTaskWriteResult.Success(task.Id, task.Version);
            },
            cancellationToken);

    public Task<AiTaskWriteResult> ApproveClosureAsync(
        AiTaskOperationContext context,
        ApproveAiTaskClosureCommand command,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteWriteAsync(
            async ct =>
            {
                var task = await LoadTrackedTaskAsync(context, command.TaskId, ct);
                if (task is null)
                {
                    return AiTaskWriteResult.Fail(AiTaskFailureCodes.NotFound, "Görev bulunamadı.");
                }

                if (task.Version != command.ExpectedVersion)
                {
                    return AiTaskWriteResult.Fail(
                        AiTaskFailureCodes.ConcurrencyConflict,
                        "Görev sürümü güncel değil.");
                }

                if (task.ClosureState != AiTaskClosureStates.AwaitingClosureApproval)
                {
                    return AiTaskWriteResult.Fail(
                        AiTaskFailureCodes.ClosureStateConflict,
                        "Görev kapanış onayı beklemiyor.");
                }

                var policies = await LoadPoliciesAsync(context, ct);
                var policy = closureEngine.ResolvePolicy(task, policies);
                if (policy.FourEyesRequired
                    && task.ClosureRequestedByMembershipId == context.MembershipId)
                {
                    return AiTaskWriteResult.Fail(
                        AiTaskFailureCodes.FourEyesViolation,
                        "Talep eden kişi kendi kapanış talebini onaylayamaz.");
                }

                var now = DateTimeOffset.UtcNow;
                var actorLabel = await ResolveActorLabelAsync(context, ct);
                task.ClosureApprovedAtUtc = now;
                task.ClosureApprovedByMembershipId = context.MembershipId;
                await CompleteClosureAsync(context, task, actorLabel, now, command.Note, ct);

                await dbContext.SaveChangesAsync(ct);
                return AiTaskWriteResult.Success(task.Id, task.Version);
            },
            cancellationToken);

    public Task<AiTaskWriteResult> RejectClosureAsync(
        AiTaskOperationContext context,
        RejectAiTaskClosureCommand command,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteWriteAsync(
            async ct =>
            {
                var task = await LoadTrackedTaskAsync(context, command.TaskId, ct);
                if (task is null)
                {
                    return AiTaskWriteResult.Fail(AiTaskFailureCodes.NotFound, "Görev bulunamadı.");
                }

                if (task.Version != command.ExpectedVersion)
                {
                    return AiTaskWriteResult.Fail(
                        AiTaskFailureCodes.ConcurrencyConflict,
                        "Görev sürümü güncel değil.");
                }

                if (task.ClosureState != AiTaskClosureStates.AwaitingClosureApproval)
                {
                    return AiTaskWriteResult.Fail(
                        AiTaskFailureCodes.ClosureStateConflict,
                        "Görev kapanış onayı beklemiyor.");
                }

                if (string.IsNullOrWhiteSpace(command.RejectionReason))
                {
                    return AiTaskWriteResult.Fail(
                        AiTaskFailureCodes.ValidationFailed,
                        "Ret gerekçesi zorunludur.");
                }

                var now = DateTimeOffset.UtcNow;
                var actorLabel = await ResolveActorLabelAsync(context, ct);
                task.ClosureState = AiTaskClosureStates.ClosureRejected;
                task.ClosureRejectedAtUtc = now;
                task.ClosureRejectedByMembershipId = context.MembershipId;
                task.ClosureRejectionReason = command.RejectionReason.Trim();
                task.Status = AiTaskStatuses.InProgress;
                task.UpdatedAtUtc = now;
                task.UpdatedBy = actorLabel;
                task.Version += 1;

                AppendAuditEvent(
                    context,
                    task,
                    AiTaskAuditEventTypes.ClosureRejected,
                    context.IdentitySubjectId.ToString(),
                    actorLabel,
                    now,
                    previousStatus: AiTaskStatuses.InProgress,
                    nextStatus: AiTaskStatuses.InProgress,
                    note: task.ClosureRejectionReason);

                if (task.ClosureRequestedByMembershipId.HasValue)
                {
                    await NotifyClosureRejectedAsync(
                        context,
                        task,
                        task.ClosureRequestedByMembershipId.Value,
                        ct);
                }

                await dbContext.SaveChangesAsync(ct);
                return AiTaskWriteResult.Success(task.Id, task.Version);
            },
            cancellationToken);

    public Task<AiTaskWriteResult> ReopenAsync(
        AiTaskOperationContext context,
        ReopenAiTaskCommand command,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteWriteAsync(
            async ct =>
            {
                var task = await LoadTrackedTaskAsync(context, command.TaskId, ct);
                if (task is null)
                {
                    return AiTaskWriteResult.Fail(AiTaskFailureCodes.NotFound, "Görev bulunamadı.");
                }

                if (task.Version != command.ExpectedVersion)
                {
                    return AiTaskWriteResult.Fail(
                        AiTaskFailureCodes.ConcurrencyConflict,
                        "Görev sürümü güncel değil.");
                }

                if (task.Status != AiTaskStatuses.Completed)
                {
                    return AiTaskWriteResult.Fail(
                        AiTaskFailureCodes.InvalidTransition,
                        "Yalnızca tamamlanan görevler yeniden açılabilir.");
                }

                if (string.IsNullOrWhiteSpace(command.ReopenReason))
                {
                    return AiTaskWriteResult.Fail(
                        AiTaskFailureCodes.ValidationFailed,
                        "Yeniden açma gerekçesi zorunludur.");
                }

                var now = DateTimeOffset.UtcNow;
                var actorLabel = await ResolveActorLabelAsync(context, ct);
                var previousStatus = task.Status;
                var priorFinalClosedAtUtc = task.FinalClosedAtUtc;
                task.Status = AiTaskStatuses.InProgress;
                task.ClosureState = AiTaskClosureStates.Reopened;
                task.ReopenedAtUtc = now;
                task.ReopenedByMembershipId = context.MembershipId;
                task.ReopenReason = command.ReopenReason.Trim();
                task.CompletedAtUtc = null;
                task.FinalClosedAtUtc = null;
                task.ClosureRequestedAtUtc = null;
                task.ClosureRequestedByMembershipId = null;
                task.ClosureApprovedAtUtc = null;
                task.ClosureApprovedByMembershipId = null;
                task.ClosureRejectedAtUtc = null;
                task.ClosureRejectedByMembershipId = null;
                task.ClosureRejectionReason = null;
                task.UpdatedAtUtc = now;
                task.UpdatedBy = actorLabel;
                task.Version += 1;

                AppendAuditEvent(
                    context,
                    task,
                    AiTaskAuditEventTypes.TaskReopened,
                    context.IdentitySubjectId.ToString(),
                    actorLabel,
                    now,
                    previousStatus: previousStatus,
                    nextStatus: task.Status,
                    note: task.ReopenReason,
                    metadata: new
                    {
                        priorFinalClosedAtUtc,
                    });

                await dbContext.SaveChangesAsync(ct);

                await playbookOrchestrator.NotifyTaskReopenedAsync(context, task, ct);

                return AiTaskWriteResult.Success(task.Id, task.Version);
            },
            cancellationToken);

    private async Task CompleteClosureAsync(
        AiTaskOperationContext context,
        AiTask task,
        string actorLabel,
        DateTimeOffset now,
        string? note,
        CancellationToken ct)
    {
        var previousStatus = task.Status;
        task.Status = AiTaskStatuses.Completed;
        task.ClosureState = AiTaskClosureStates.Closed;
        task.CompletedAtUtc = now;
        task.ResolvedAtUtc = now;
        task.FinalClosedAtUtc = now;
        task.UpdatedAtUtc = now;
        task.UpdatedBy = actorLabel;

        AppendAuditEvent(
            context,
            task,
            AiTaskAuditEventTypes.ClosureApproved,
            context.IdentitySubjectId.ToString(),
            actorLabel,
            now,
            previousStatus: previousStatus,
            nextStatus: task.Status,
            note: note,
            metadata: new
            {
                resolutionCode = task.ResolutionCode,
                finalClosedAtUtc = task.FinalClosedAtUtc,
            });

        if (task.ClosureRequestedByMembershipId.HasValue
            && task.ClosureRequestedByMembershipId != context.MembershipId)
        {
            await NotifyClosureApprovedAsync(
                context,
                task,
                task.ClosureRequestedByMembershipId.Value,
                ct);
        }
    }

    private async Task<AiTask?> LoadTaskAsync(
        AiTaskOperationContext context,
        Guid taskId,
        CancellationToken ct) =>
        await dbContext.AiTasks.AsNoTracking()
            .SingleOrDefaultAsync(
                t => t.TenantId == context.TenantId
                    && t.FacilityId == context.FacilityId
                    && t.Id == taskId,
                ct);

    private async Task<AiTask?> LoadTrackedTaskAsync(
        AiTaskOperationContext context,
        Guid taskId,
        CancellationToken ct) =>
        await dbContext.AiTasks
            .SingleOrDefaultAsync(
                t => t.TenantId == context.TenantId
                    && t.FacilityId == context.FacilityId
                    && t.Id == taskId,
                ct);

    private async Task<List<AiTaskEvidence>> LoadEvidenceAsync(
        AiTaskOperationContext context,
        Guid taskId,
        CancellationToken ct) =>
        await dbContext.AiTaskEvidence.AsNoTracking()
            .Where(e => e.TenantId == context.TenantId
                && e.FacilityId == context.FacilityId
                && e.TaskId == taskId)
            .ToListAsync(ct);

    private async Task<List<AiTaskClosurePolicy>> LoadPoliciesAsync(
        AiTaskOperationContext context,
        CancellationToken ct) =>
        await dbContext.AiTaskClosurePolicies.AsNoTracking()
            .Where(p => p.TenantId == context.TenantId
                && (p.FacilityId == null || p.FacilityId == context.FacilityId))
            .ToListAsync(ct);

    private async Task<string> ResolveActorLabelAsync(
        AiTaskOperationContext context,
        CancellationToken ct)
    {
        var membership = await dbContext.TenantMemberships.AsNoTracking()
            .SingleAsync(m => m.TenantId == context.TenantId && m.Id == context.MembershipId, ct);
        var account = await dbContext.SaasIdentityAccounts.AsNoTracking()
            .SingleOrDefaultAsync(a => a.IdentitySubjectId == membership.IdentitySubjectId, ct);
        return !string.IsNullOrWhiteSpace(account?.DisplayName)
            ? account!.DisplayName
            : membership.MembershipReference;
    }

    private static AiTaskEvidenceReadModel MapEvidence(AiTaskEvidence evidence) =>
        new(
            evidence.Id,
            evidence.TaskId,
            evidence.EvidenceType,
            evidence.Title,
            evidence.Description,
            evidence.ReferenceMetadataJson,
            evidence.SourceModule,
            evidence.SourceEntityId,
            evidence.SubmittedByMembershipId,
            evidence.SubmittedByLabel,
            evidence.SubmittedAtUtc);

    private async Task NotifyClosureApprovalRequestedAsync(
        AiTaskOperationContext context,
        AiTask task,
        CancellationToken ct)
    {
        var recipient = task.AssignedMembershipId ?? task.ClosureRequestedByMembershipId;
        if (!recipient.HasValue || recipient == context.MembershipId)
        {
            return;
        }

        var teamsContext = BuildTeamsContext(context);
        await teamsNotificationRepository.TryCreateTaskClosureApprovalRequestedAsync(
            teamsContext,
            recipient.Value,
            task.Id,
            task.TaskReference,
            ct);
    }

    private async Task NotifyClosureApprovedAsync(
        AiTaskOperationContext context,
        AiTask task,
        Guid recipientMembershipId,
        CancellationToken ct)
    {
        var teamsContext = BuildTeamsContext(context);
        await teamsNotificationRepository.TryCreateTaskClosureApprovedAsync(
            teamsContext,
            recipientMembershipId,
            task.Id,
            task.TaskReference,
            ct);
    }

    private async Task NotifyClosureRejectedAsync(
        AiTaskOperationContext context,
        AiTask task,
        Guid recipientMembershipId,
        CancellationToken ct)
    {
        var teamsContext = BuildTeamsContext(context);
        await teamsNotificationRepository.TryCreateTaskClosureRejectedAsync(
            teamsContext,
            recipientMembershipId,
            task.Id,
            task.TaskReference,
            task.ClosureRejectionReason,
            ct);
    }

    private static TeamsOperationContext BuildTeamsContext(AiTaskOperationContext context) =>
        new(
            context.TenantId,
            context.FacilityId,
            context.MembershipId,
            context.IdentitySubjectId,
            context.PermissionKey,
            context.CorrelationId);

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
            MetadataJson = metadata is null ? null : JsonSerializer.Serialize(metadata, JsonOptions),
        });
    }

    private static string? TrimOrNull(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

public sealed class UnavailableAiTaskClosureRepository : IAiTaskClosureRepository
{
    private static readonly Task<AiTaskWriteResult> UnavailableResult =
        Task.FromResult(AiTaskWriteResult.Fail(AiTaskFailureCodes.Unavailable, "AI görev kapanış servisi kullanılamıyor."));

    public Task<IReadOnlyList<AiTaskEvidenceReadModel>> ListEvidenceAsync(
        AiTaskOperationContext context,
        Guid taskId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<AiTaskEvidenceReadModel>>(Array.Empty<AiTaskEvidenceReadModel>());

    public Task<AiTaskClosureValidationResult> GetClosureValidationAsync(
        AiTaskOperationContext context,
        Guid taskId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(AiTaskClosureValidationResult.Fail(
            new AiTaskClosureValidationIssue("unavailable", "Kapanış doğrulaması kullanılamıyor.")));

    public Task<AiTaskWriteResult> SubmitEvidenceAsync(
        AiTaskOperationContext context,
        SubmitAiTaskEvidenceCommand command,
        CancellationToken cancellationToken = default) => UnavailableResult;

    public Task<AiTaskWriteResult> UpdateResolutionAsync(
        AiTaskOperationContext context,
        UpdateAiTaskResolutionCommand command,
        CancellationToken cancellationToken = default) => UnavailableResult;

    public Task<AiTaskWriteResult> RequestClosureAsync(
        AiTaskOperationContext context,
        RequestAiTaskClosureCommand command,
        CancellationToken cancellationToken = default) => UnavailableResult;

    public Task<AiTaskWriteResult> ApproveClosureAsync(
        AiTaskOperationContext context,
        ApproveAiTaskClosureCommand command,
        CancellationToken cancellationToken = default) => UnavailableResult;

    public Task<AiTaskWriteResult> RejectClosureAsync(
        AiTaskOperationContext context,
        RejectAiTaskClosureCommand command,
        CancellationToken cancellationToken = default) => UnavailableResult;

    public Task<AiTaskWriteResult> ReopenAsync(
        AiTaskOperationContext context,
        ReopenAiTaskCommand command,
        CancellationToken cancellationToken = default) => UnavailableResult;
}
