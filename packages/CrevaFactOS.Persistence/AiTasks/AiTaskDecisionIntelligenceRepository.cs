#pragma warning disable CA1859

using System.Text.Json;
using CrevaFactOS.Persistence.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace CrevaFactOS.Persistence.AiTasks;

public sealed class AiTaskDecisionIntelligenceRepository(
    CrevaFactOSDbContext dbContext,
    TenantDatabaseSession tenantDatabaseSession,
    IAiTaskDecisionProvider decisionProvider,
    IAiTaskOperationsRepository operationsRepository) : IAiTaskDecisionIntelligenceRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public Task<AiTaskDecisionWorkspaceReadModel> GetWorkspaceAsync(
        AiTaskOperationContext context,
        Guid taskId,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(
            async ct =>
            {
                var task = await LoadTaskAsync(context, taskId, ct);
                if (task is null)
                {
                    return new AiTaskDecisionWorkspaceReadModel(
                        null, [], [], [], decisionProvider.IsAvailable,
                        decisionProvider.IsAvailable ? null : "AI değerlendirmesi şu anda kullanılamıyor.");
                }

                var analyses = await LoadAnalysesAsync(context, taskId, ct);
                var relationships = await LoadRelationshipsAsync(context, taskId, ct);
                var snapshot = await BuildInputSnapshotAsync(task, ct);
                var candidates = await FindRelatedCandidatesAsync(context, task, ct);
                var suggested = decisionProvider.IsAvailable
                    ? DeterministicAiTaskDecisionProviderExtensions.BuildRelatedCandidatesFromProvider(snapshot, candidates)
                    : [];

                return new AiTaskDecisionWorkspaceReadModel(
                    analyses.Count > 0 ? analyses[0] : null,
                    analyses,
                    relationships,
                    suggested,
                    decisionProvider.IsAvailable,
                    decisionProvider.IsAvailable ? null : "AI değerlendirmesi şu anda kullanılamıyor.");
            },
            cancellationToken);

    public Task<AiTaskDecisionWriteResult> AnalyzeAsync(
        AiTaskOperationContext context,
        AnalyzeAiTaskDecisionCommand command,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteWriteAsync(
            async ct => await AnalyzeInternalAsync(context, command, ct),
            cancellationToken);

    public Task<AiTaskDecisionWriteResult> AcceptRecommendationAsync(
        AiTaskOperationContext context,
        AcceptAiTaskRecommendationCommand command,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteWriteAsync(
            async ct => await AcceptRecommendationInternalAsync(context, command, ct),
            cancellationToken);

    public Task<AiTaskDecisionWriteResult> RejectRecommendationAsync(
        AiTaskOperationContext context,
        RejectAiTaskRecommendationCommand command,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteWriteAsync(
            async ct => await RejectRecommendationInternalAsync(context, command, ct),
            cancellationToken);

    public Task<AiTaskDecisionWriteResult> ConfirmRelationshipAsync(
        AiTaskOperationContext context,
        ConfirmAiTaskRelationshipCommand command,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteWriteAsync(
            async ct => await ConfirmRelationshipInternalAsync(context, command, ct),
            cancellationToken);

    public Task<AiTaskDecisionClosureReviewResult?> ReviewClosureAsync(
        AiTaskOperationContext context,
        Guid taskId,
        string idempotencyKey,
        CancellationToken cancellationToken = default) =>
        tenantDatabaseSession.ExecuteReadAsync(
            async ct =>
            {
                var existing = await dbContext.AiTaskAiAnalyses.AsNoTracking()
                    .Where(a => a.TenantId == context.TenantId
                                && a.TaskId == taskId
                                && a.AnalysisType == AiTaskDecisionAnalysisTypes.ClosureReview
                                && a.IdempotencyKey == idempotencyKey)
                    .OrderByDescending(a => a.CreatedAtUtc)
                    .FirstOrDefaultAsync(ct);

                if (existing is not null)
                {
                    return DeserializeClosure(existing.ResultJson);
                }

                var task = await LoadTaskAsync(context, taskId, ct);
                if (task is null || !decisionProvider.IsAvailable)
                {
                    return null;
                }

                var snapshot = await BuildInputSnapshotAsync(task, ct);
                var candidates = await FindRelatedCandidatesAsync(context, task, ct);
                var providerResult = await decisionProvider.AnalyzeAsync(
                    new AiTaskDecisionProviderRequest(
                        snapshot,
                        candidates,
                        AiTaskDecisionAnalysisTypes.ClosureReview,
                        AiTaskDecisionIntelligenceProviders.PromptTemplateVersion,
                        "closure-review"),
                    ct);

                return providerResult.ClosureReview;
            },
            cancellationToken);

    private async Task<AiTaskDecisionWriteResult> AnalyzeInternalAsync(
        AiTaskOperationContext context,
        AnalyzeAiTaskDecisionCommand command,
        CancellationToken ct)
    {
        if (!AiTaskDecisionAnalysisTypes.All.Contains(command.AnalysisType))
        {
            return AiTaskDecisionWriteResult.Fail(
                AiTaskFailureCodes.ValidationFailed,
                "Geçersiz analiz tipi.");
        }

        if (!decisionProvider.IsAvailable)
        {
            return AiTaskDecisionWriteResult.Fail(
                AiTaskDecisionIntelligenceFailureCodes.ProviderUnavailable,
                "AI değerlendirmesi şu anda kullanılamıyor.");
        }

        var existing = await dbContext.AiTaskAiAnalyses.AsNoTracking()
            .FirstOrDefaultAsync(
                a => a.TenantId == context.TenantId
                     && a.TaskId == command.TaskId
                     && a.IdempotencyKey == command.IdempotencyKey,
                ct);

        if (existing is not null)
        {
            var replay = await MapAnalysisReadModelAsync(existing, ct);
            return AiTaskDecisionWriteResult.Success(existing.Id, idempotentReplay: true, analysis: replay);
        }

        var task = await dbContext.AiTasks
            .SingleOrDefaultAsync(
                t => t.TenantId == context.TenantId
                     && t.FacilityId == context.FacilityId
                     && t.Id == command.TaskId,
                ct);

        if (task is null)
        {
            return AiTaskDecisionWriteResult.Fail(AiTaskFailureCodes.NotFound, "Görev bulunamadı.");
        }

        var snapshot = await BuildInputSnapshotAsync(task, ct);
        var candidates = await FindRelatedCandidatesAsync(context, task, ct);
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(15));

        AiTaskDecisionProviderResult providerResult;
        try
        {
            providerResult = await decisionProvider.AnalyzeAsync(
                new AiTaskDecisionProviderRequest(
                    snapshot,
                    candidates,
                    command.AnalysisType,
                    AiTaskDecisionIntelligenceProviders.PromptTemplateVersion,
                    command.TriggerReason),
                timeoutCts.Token);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            return AiTaskDecisionWriteResult.Fail(
                AiTaskDecisionIntelligenceFailureCodes.ProviderFailed,
                "AI sağlayıcı zaman aşımına uğradı.");
        }

        if (!providerResult.Succeeded)
        {
            return AiTaskDecisionWriteResult.Fail(
                providerResult.FailureCode ?? AiTaskDecisionIntelligenceFailureCodes.ProviderFailed,
                providerResult.FailureReason ?? "AI analizi başarısız.");
        }

        var now = DateTimeOffset.UtcNow;
        var analysisId = Guid.NewGuid();
        var triage = providerResult.Triage;
        var closure = providerResult.ClosureReview;
        var confidence = triage?.ConfidenceBand ?? closure?.ConfidenceBand ?? AiTaskDecisionConfidenceBands.Medium;
        var resultJson = command.AnalysisType == AiTaskDecisionAnalysisTypes.ClosureReview
            ? Serialize(closure)
            : Serialize(triage);

        await dbContext.AiTaskAiAnalyses
            .Where(a => a.TenantId == context.TenantId && a.TaskId == command.TaskId && !a.Superseded)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(a => a.Superseded, true)
                    .SetProperty(a => a.SupersededByAnalysisId, analysisId),
                ct);

        var analysis = new AiTaskAiAnalysis
        {
            Id = analysisId,
            TenantId = context.TenantId,
            FacilityId = context.FacilityId,
            TaskId = command.TaskId,
            AnalysisType = command.AnalysisType,
            ProviderName = providerResult.ProviderName,
            ModelName = providerResult.ModelName,
            PromptTemplateVersion = AiTaskDecisionIntelligenceProviders.PromptTemplateVersion,
            ConfidenceBand = confidence,
            ResultJson = resultJson,
            IdempotencyKey = command.IdempotencyKey.Trim(),
            CorrelationId = context.CorrelationId,
            Superseded = false,
            CreatedAtUtc = now,
            CreatedBy = context.MembershipId.ToString("D"),
        };

        dbContext.AiTaskAiAnalyses.Add(analysis);

        if (triage is not null)
        {
            foreach (var action in triage.NextActions)
            {
                dbContext.AiTaskAiRecommendations.Add(new AiTaskAiRecommendation
                {
                    Id = Guid.NewGuid(),
                    TenantId = context.TenantId,
                    FacilityId = context.FacilityId,
                    TaskId = command.TaskId,
                    AnalysisId = analysisId,
                    ActionType = action.ActionType,
                    Title = action.Title,
                    Rationale = action.Rationale,
                    ConfidenceBand = action.ConfidenceBand,
                    UrgencyBand = action.UrgencyBand,
                    RelatedEntityType = action.RelatedEntityType,
                    RelatedEntityId = action.RelatedEntityId,
                    PayloadJson = action.PayloadJson,
                    Status = AiTaskDecisionRecommendationStatuses.Pending,
                    CreatedAtUtc = now,
                });
            }
        }

        AppendAuditEvent(
            context,
            task,
            AiTaskAuditEventTypes.AiAnalyzed,
            context.MembershipId.ToString("D"),
            "AI Decision Intelligence",
            now,
            new { analysisId, command.AnalysisType, confidence },
            note: triage?.Explanation ?? closure?.Explanation);

        await dbContext.SaveChangesAsync(ct);
        var readModel = await MapAnalysisReadModelAsync(analysis, ct);
        return AiTaskDecisionWriteResult.Success(analysisId, task.Version, analysis: readModel);
    }

    private async Task<AiTaskDecisionWriteResult> AcceptRecommendationInternalAsync(
        AiTaskOperationContext context,
        AcceptAiTaskRecommendationCommand command,
        CancellationToken ct)
    {
        var recommendation = await dbContext.AiTaskAiRecommendations
            .SingleOrDefaultAsync(
                r => r.TenantId == context.TenantId
                     && r.TaskId == command.TaskId
                     && r.Id == command.RecommendationId,
                ct);

        if (recommendation is null)
        {
            return AiTaskDecisionWriteResult.Fail(AiTaskFailureCodes.NotFound, "Öneri bulunamadı.");
        }

        if (recommendation.Status != AiTaskDecisionRecommendationStatuses.Pending)
        {
            return AiTaskDecisionWriteResult.Fail(
                AiTaskFailureCodes.ValidationFailed,
                "Öneri zaten işlendi.");
        }

        var task = await dbContext.AiTasks
            .SingleOrDefaultAsync(
                t => t.TenantId == context.TenantId
                     && t.FacilityId == context.FacilityId
                     && t.Id == command.TaskId,
                ct);

        if (task is null)
        {
            return AiTaskDecisionWriteResult.Fail(AiTaskFailureCodes.NotFound, "Görev bulunamadı.");
        }

        if (task.Version != command.ExpectedTaskVersion)
        {
            return AiTaskDecisionWriteResult.Fail(
                AiTaskFailureCodes.ConcurrencyConflict,
                "Görev sürümü güncel değil.");
        }

        var now = DateTimeOffset.UtcNow;
        AiTaskWriteResult? applyResult = null;
        var payloadJson = command.EditedPayloadJson ?? recommendation.PayloadJson;

        switch (recommendation.ActionType)
        {
            case AiTaskDecisionRecommendationActionTypes.AssignTeam:
            {
                var payload = DeserializePayload(payloadJson);
                if (payload.TryGetValue("teamChannelId", out var teamRaw)
                    && Guid.TryParse(teamRaw?.ToString(), out var teamId)
                    && !task.AssignedTeamChannelId.HasValue)
                {
                    applyResult = await operationsRepository.AssignTaskAsync(
                        context,
                        new AssignAiTaskCommand(
                            command.TaskId,
                            command.ExpectedTaskVersion,
                            null,
                            teamId,
                            command.Reason ?? recommendation.Rationale,
                            "ai-recommendation"),
                        ct);
                }

                break;
            }

            case AiTaskDecisionRecommendationActionTypes.SetPriority:
            {
                var payload = DeserializePayload(payloadJson);
                if (payload.TryGetValue("priority", out var priorityRaw)
                    && priorityRaw is string priority
                    && AiTaskPriorities.All.Contains(priority))
                {
                    applyResult = await operationsRepository.ChangePriorityAsync(
                        context,
                        new ChangeAiTaskPriorityCommand(
                            command.TaskId,
                            command.ExpectedTaskVersion,
                            priority,
                            command.Reason ?? recommendation.Rationale),
                        ct);
                }

                break;
            }

            case AiTaskDecisionRecommendationActionTypes.Escalate:
                applyResult = await operationsRepository.EscalateTaskAsync(
                    context,
                    new EscalateAiTaskCommand(
                        command.TaskId,
                        command.ExpectedTaskVersion,
                        "sla-risk",
                        command.Reason ?? recommendation.Rationale),
                    ct);
                break;
        }

        recommendation.Status = AiTaskDecisionRecommendationStatuses.Accepted;
        recommendation.FeedbackDecision = AiTaskDecisionFeedbackDecisions.Accepted;
        recommendation.FeedbackReason = command.Reason;
        recommendation.FeedbackByMembershipId = context.MembershipId;
        recommendation.FeedbackAtUtc = now;
        recommendation.AppliedResultJson = applyResult is null
            ? Serialize(new { advisoryOnly = true })
            : Serialize(new { applyResult.Succeeded, applyResult.Version, applyResult.FailureCode });

        AppendAuditEvent(
            context,
            task,
            AiTaskAuditEventTypes.AiRecommendationAccepted,
            context.MembershipId.ToString("D"),
            "AI recommendation accepted",
            now,
            new { recommendationId = recommendation.Id, recommendation.ActionType },
            note: command.Reason ?? recommendation.Rationale);

        await dbContext.SaveChangesAsync(ct);
        return AiTaskDecisionWriteResult.Success(
            recommendation.Id,
            applyResult?.Version ?? task.Version);
    }

    private async Task<AiTaskDecisionWriteResult> RejectRecommendationInternalAsync(
        AiTaskOperationContext context,
        RejectAiTaskRecommendationCommand command,
        CancellationToken ct)
    {
        var recommendation = await dbContext.AiTaskAiRecommendations
            .SingleOrDefaultAsync(
                r => r.TenantId == context.TenantId
                     && r.TaskId == command.TaskId
                     && r.Id == command.RecommendationId,
                ct);

        if (recommendation is null)
        {
            return AiTaskDecisionWriteResult.Fail(AiTaskFailureCodes.NotFound, "Öneri bulunamadı.");
        }

        if (recommendation.Status != AiTaskDecisionRecommendationStatuses.Pending)
        {
            return AiTaskDecisionWriteResult.Fail(
                AiTaskFailureCodes.ValidationFailed,
                "Öneri zaten işlendi.");
        }

        var task = await dbContext.AiTasks
            .SingleOrDefaultAsync(
                t => t.TenantId == context.TenantId
                     && t.FacilityId == context.FacilityId
                     && t.Id == command.TaskId,
                ct);

        if (task is null)
        {
            return AiTaskDecisionWriteResult.Fail(AiTaskFailureCodes.NotFound, "Görev bulunamadı.");
        }

        var now = DateTimeOffset.UtcNow;
        recommendation.Status = AiTaskDecisionRecommendationStatuses.Rejected;
        recommendation.FeedbackDecision = AiTaskDecisionFeedbackDecisions.Rejected;
        recommendation.FeedbackReason = command.Reason.Trim();
        recommendation.FeedbackByMembershipId = context.MembershipId;
        recommendation.FeedbackAtUtc = now;

        AppendAuditEvent(
            context,
            task,
            AiTaskAuditEventTypes.AiRecommendationRejected,
            context.MembershipId.ToString("D"),
            "AI recommendation rejected",
            now,
            new { recommendationId = recommendation.Id },
            note: command.Reason.Trim());

        await dbContext.SaveChangesAsync(ct);
        return AiTaskDecisionWriteResult.Success(recommendation.Id, task.Version);
    }

    private async Task<AiTaskDecisionWriteResult> ConfirmRelationshipInternalAsync(
        AiTaskOperationContext context,
        ConfirmAiTaskRelationshipCommand command,
        CancellationToken ct)
    {
        if (!AiTaskDecisionRelationshipTypes.All.Contains(command.RelationshipType))
        {
            return AiTaskDecisionWriteResult.Fail(
                AiTaskFailureCodes.ValidationFailed,
                "Geçersiz ilişki tipi.");
        }

        var source = await LoadTaskAsync(context, command.TaskId, ct);
        var target = await LoadTaskAsync(context, command.TargetTaskId, ct);
        if (source is null || target is null)
        {
            return AiTaskDecisionWriteResult.Fail(AiTaskFailureCodes.NotFound, "Görev bulunamadı.");
        }

        var now = DateTimeOffset.UtcNow;
        var existing = await dbContext.AiTaskRelationships
            .SingleOrDefaultAsync(
                r => r.TenantId == context.TenantId
                     && r.SourceTaskId == command.TaskId
                     && r.TargetTaskId == command.TargetTaskId
                     && r.RelationshipType == command.RelationshipType,
                ct);

        if (existing is null)
        {
            existing = new AiTaskRelationship
            {
                Id = Guid.NewGuid(),
                TenantId = context.TenantId,
                FacilityId = context.FacilityId,
                SourceTaskId = command.TaskId,
                TargetTaskId = command.TargetTaskId,
                RelationshipType = command.RelationshipType,
                Status = command.Status,
                ConfidenceBand = AiTaskDecisionConfidenceBands.Medium,
                Explanation = command.Explanation,
                ConfirmedByMembershipId = context.MembershipId,
                ConfirmedAtUtc = now,
                CreatedAtUtc = now,
                CreatedBy = context.MembershipId.ToString("D"),
            };
            dbContext.AiTaskRelationships.Add(existing);
        }
        else
        {
            existing.Status = command.Status;
            existing.Explanation = command.Explanation;
            existing.ConfirmedByMembershipId = context.MembershipId;
            existing.ConfirmedAtUtc = now;
        }

        var sourceTracked = await dbContext.AiTasks
            .SingleAsync(t => t.Id == command.TaskId, ct);

        AppendAuditEvent(
            context,
            sourceTracked,
            AiTaskAuditEventTypes.AiRelationshipConfirmed,
            context.MembershipId.ToString("D"),
            "AI relationship confirmed",
            now,
            new
            {
                targetTaskId = command.TargetTaskId,
                command.RelationshipType,
                command.Status,
            },
            note: command.Explanation);

        await dbContext.SaveChangesAsync(ct);
        return AiTaskDecisionWriteResult.Success(existing.Id, sourceTracked.Version);
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

    private async Task<AiTaskDecisionInputSnapshot> BuildInputSnapshotAsync(AiTask task, CancellationToken ct)
    {
        var policies = await dbContext.AiTaskSlaPolicies.AsNoTracking()
            .Where(p => p.TenantId == task.TenantId && p.Enabled)
            .ToListAsync(ct);
        var policy = AiTaskSlaEngine.ResolvePolicy(task, policies);
        var sla = AiTaskSlaEngine.DeriveSlaState(task, policy, DateTimeOffset.UtcNow);
        var evidenceCount = await dbContext.AiTaskEvidence.AsNoTracking()
            .CountAsync(
                e => e.TenantId == task.TenantId
                     && e.FacilityId == task.FacilityId
                     && e.TaskId == task.Id,
                ct);

        return new AiTaskDecisionInputSnapshot(
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
            task.SourceModule,
            task.SourceEntityId,
            task.MachineId,
            task.ProductionOrderId,
            task.MoldId,
            task.MaterialId,
            task.SalesOrderId,
            sla.State,
            task.EscalationLevel,
            task.EscalationState,
            evidenceCount,
            task.MinimumEvidenceRequired,
            task.ClosureApprovalRequired,
            task.FourEyesRequired,
            task.ClosureState,
            task.ResolutionSummary,
            task.AssignedTeamChannelId,
            task.AssignedMembershipId,
            task.AutomationRuleId,
            task.AutomationMetadataJson,
            task.CreatedAtUtc,
            task.UpdatedAtUtc,
            task.Version);
    }

    private async Task<IReadOnlyList<AiTaskDecisionRelatedCandidateSnapshot>> FindRelatedCandidatesAsync(
        AiTaskOperationContext context,
        AiTask task,
        CancellationToken ct)
    {
        var query = dbContext.AiTasks.AsNoTracking()
            .Where(t => t.TenantId == context.TenantId
                        && t.FacilityId == context.FacilityId
                        && t.Id != task.Id);

        query = query.Where(t =>
            (task.MachineId != null && t.MachineId == task.MachineId)
            || (task.ProductionOrderId != null && t.ProductionOrderId == task.ProductionOrderId)
            || (task.SourceEntityId != null && t.SourceEntityId == task.SourceEntityId)
            || (task.SourceModule != null && t.SourceModule == task.SourceModule));

        var items = await query
            .OrderByDescending(t => t.CreatedAtUtc)
            .Take(20)
            .Select(t => new AiTaskDecisionRelatedCandidateSnapshot(
                t.Id,
                t.TaskReference,
                t.Title,
                t.Status,
                t.Priority,
                t.SourceModule,
                t.SourceEntityId,
                t.MachineId,
                t.ProductionOrderId,
                t.CreatedAtUtc))
            .ToListAsync(ct);

        return items;
    }

    private async Task<IReadOnlyList<AiTaskDecisionAnalysisReadModel>> LoadAnalysesAsync(
        AiTaskOperationContext context,
        Guid taskId,
        CancellationToken ct)
    {
        var entities = await dbContext.AiTaskAiAnalyses.AsNoTracking()
            .Where(a => a.TenantId == context.TenantId && a.TaskId == taskId)
            .OrderByDescending(a => a.CreatedAtUtc)
            .Take(20)
            .ToListAsync(ct);

        var results = new List<AiTaskDecisionAnalysisReadModel>();
        foreach (var entity in entities)
        {
            results.Add(await MapAnalysisReadModelAsync(entity, ct));
        }

        return results;
    }

    private async Task<AiTaskDecisionAnalysisReadModel> MapAnalysisReadModelAsync(
        AiTaskAiAnalysis entity,
        CancellationToken ct)
    {
        var recommendations = await dbContext.AiTaskAiRecommendations.AsNoTracking()
            .Where(r => r.TenantId == entity.TenantId && r.AnalysisId == entity.Id)
            .OrderBy(r => r.CreatedAtUtc)
            .Select(r => new AiTaskDecisionRecommendationReadModel(
                r.Id,
                r.AnalysisId,
                r.ActionType,
                r.Title,
                r.Rationale,
                r.ConfidenceBand,
                r.UrgencyBand,
                r.RelatedEntityType,
                r.RelatedEntityId,
                r.PayloadJson,
                r.Status,
                r.FeedbackDecision,
                r.FeedbackReason,
                r.FeedbackAtUtc,
                r.AppliedResultJson,
                r.CreatedAtUtc))
            .ToListAsync(ct);

        return new AiTaskDecisionAnalysisReadModel(
            entity.Id,
            entity.TaskId,
            entity.AnalysisType,
            entity.ProviderName,
            entity.ModelName,
            entity.PromptTemplateVersion,
            entity.ConfidenceBand,
            entity.AnalysisType == AiTaskDecisionAnalysisTypes.ClosureReview
                ? null
                : DeserializeTriage(entity.ResultJson),
            entity.AnalysisType == AiTaskDecisionAnalysisTypes.ClosureReview
                ? DeserializeClosure(entity.ResultJson)
                : null,
            entity.Superseded,
            entity.SupersededByAnalysisId,
            entity.CreatedAtUtc,
            entity.CreatedBy,
            recommendations);
    }

    private async Task<IReadOnlyList<AiTaskDecisionRelationshipReadModel>> LoadRelationshipsAsync(
        AiTaskOperationContext context,
        Guid taskId,
        CancellationToken ct)
    {
        var relationships = await dbContext.AiTaskRelationships.AsNoTracking()
            .Where(r => r.TenantId == context.TenantId
                        && (r.SourceTaskId == taskId || r.TargetTaskId == taskId))
            .OrderByDescending(r => r.CreatedAtUtc)
            .ToListAsync(ct);

        var targetIds = relationships
            .Select(r => r.SourceTaskId == taskId ? r.TargetTaskId : r.SourceTaskId)
            .Distinct()
            .ToList();

        var tasks = await dbContext.AiTasks.AsNoTracking()
            .Where(t => t.TenantId == context.TenantId && targetIds.Contains(t.Id))
            .ToDictionaryAsync(t => t.Id, ct);

        return relationships.Select(r =>
        {
            var otherId = r.SourceTaskId == taskId ? r.TargetTaskId : r.SourceTaskId;
            tasks.TryGetValue(otherId, out var other);
            return new AiTaskDecisionRelationshipReadModel(
                r.Id,
                r.SourceTaskId,
                r.TargetTaskId,
                other?.Title ?? "Görev",
                other?.Status ?? "unknown",
                other?.Priority ?? "medium",
                other?.SourceModule,
                r.RelationshipType,
                r.Status,
                r.ConfidenceBand,
                r.Explanation,
                r.ConfirmedAtUtc,
                r.CreatedAtUtc);
        }).ToList();
    }

    private void AppendAuditEvent(
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
            ActorType = eventType.StartsWith("ai-", StringComparison.Ordinal) && eventType.Contains("analyzed", StringComparison.Ordinal)
                ? AiTaskActorTypes.Ai
                : AiTaskActorTypes.User,
            ActorId = actorId,
            ActorLabel = actorLabel,
            Note = note,
            CorrelationId = context.CorrelationId,
            MetadataJson = metadata is null ? null : Serialize(metadata),
        });
    }

    private static string Serialize<T>(T value) => JsonSerializer.Serialize(value, JsonOptions);

    private static AiTaskDecisionTriageResult? DeserializeTriage(string json) =>
        JsonSerializer.Deserialize<AiTaskDecisionTriageResult>(json, JsonOptions);

    private static AiTaskDecisionClosureReviewResult? DeserializeClosure(string json) =>
        JsonSerializer.Deserialize<AiTaskDecisionClosureReviewResult>(json, JsonOptions);

    private static Dictionary<string, object?> DeserializePayload(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, object?>>(json, JsonOptions) ?? [];
        }
        catch
        {
            return [];
        }
    }
}

internal static class DeterministicAiTaskDecisionProviderExtensions
{
    internal static IReadOnlyList<AiTaskDecisionRelatedCandidateResult> BuildRelatedCandidatesFromProvider(
        AiTaskDecisionInputSnapshot snapshot,
        IReadOnlyList<AiTaskDecisionRelatedCandidateSnapshot> candidates) =>
        candidates
            .Select(candidate => new AiTaskDecisionRelatedCandidateResult(
                candidate.TaskId,
                DeterministicAiTaskDecisionProvider.ResolveRelationshipType(snapshot, candidate),
                DeterministicAiTaskDecisionProvider.ResolveRelationshipConfidence(snapshot, candidate),
                candidate.Title))
            .Where(c => c.ConfidenceBand != AiTaskDecisionConfidenceBands.Low)
            .Take(5)
            .ToList();
}
