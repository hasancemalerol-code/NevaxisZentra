using System.Text.Json;
using NevaxisZentra.Persistence.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace NevaxisZentra.Persistence.Maintenance;

public sealed class MaintenanceAiDraftRepository(
    NevaxisZentraDbContext dbContext,
    TenantDatabaseSession tenantDatabaseSession,
    IMaintenanceAiProvider aiProvider) : IMaintenanceAiDraftRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public Task<MaintenanceAiDraftDetailReadModel?> GetCurrentDraftAsync(
        MaintenanceOperationContext context,
        string assetReference,
        CancellationToken cancellationToken) =>
        tenantDatabaseSession.ExecuteReadAsync(async ct =>
        {
            var draft = await dbContext.MaintenanceAiActionDrafts.AsNoTracking()
                .Where(d => d.FacilityId == context.FacilityId
                    && d.AssetReference == assetReference
                    && d.Status != MaintenanceAiDraftStatuses.Stale
                    && d.Status != MaintenanceAiDraftStatuses.Rejected
                    && d.Status != MaintenanceAiDraftStatuses.Executed)
                .OrderByDescending(d => d.GeneratedAtUtc)
                .FirstOrDefaultAsync(ct);

            return draft is null ? null : await MapDetailAsync(dbContext, context, draft, ct);
        }, cancellationToken);

    public Task<IReadOnlyList<MaintenanceAiDraftSummaryReadModel>> ListDraftsAsync(
        MaintenanceOperationContext context,
        string assetReference,
        int? limit,
        CancellationToken cancellationToken) =>
        tenantDatabaseSession.ExecuteReadAsync(async ct =>
        {
            var take = Math.Clamp(limit ?? 10, 1, 50);
            var drafts = await dbContext.MaintenanceAiActionDrafts.AsNoTracking()
                .Where(d => d.FacilityId == context.FacilityId && d.AssetReference == assetReference)
                .OrderByDescending(d => d.GeneratedAtUtc)
                .Take(take)
                .ToListAsync(ct);

            return (IReadOnlyList<MaintenanceAiDraftSummaryReadModel>)drafts
                .Select(MapSummary)
                .ToList();
        }, cancellationToken);

    public Task<MaintenanceAiDraftDetailReadModel?> GetDraftByIdAsync(
        MaintenanceOperationContext context,
        Guid draftId,
        CancellationToken cancellationToken) =>
        tenantDatabaseSession.ExecuteReadAsync(async ct =>
        {
            var draft = await dbContext.MaintenanceAiActionDrafts.AsNoTracking()
                .SingleOrDefaultAsync(
                    d => d.TenantId == context.TenantId
                        && d.FacilityId == context.FacilityId
                        && d.Id == draftId,
                    ct);

            return draft is null ? null : await MapDetailAsync(dbContext, context, draft, ct);
        }, cancellationToken);

    public Task<MaintenanceAiDraftWriteResult> GenerateDraftAsync(
        MaintenanceOperationContext context,
        string assetReference,
        CancellationToken cancellationToken) =>
        tenantDatabaseSession.ExecuteWriteAsync(
            ct => GenerateDraftCoreAsync(context, assetReference, ct),
            cancellationToken);

    public Task<MaintenanceAiDraftWriteResult> UpdateDraftAsync(
        MaintenanceOperationContext context,
        UpdateMaintenanceAiDraftCommand command,
        CancellationToken cancellationToken) =>
        tenantDatabaseSession.ExecuteWriteAsync(
            async ct =>
            {
                var draft = await dbContext.MaintenanceAiActionDrafts
                    .SingleOrDefaultAsync(
                        d => d.TenantId == context.TenantId
                            && d.FacilityId == context.FacilityId
                            && d.Id == command.DraftId,
                        ct);

                if (draft is null)
                {
                    return MaintenanceAiDraftWriteResult.Fail(MaintenanceAiFailureCodes.NotFound, "Draft not found.");
                }

                if (draft.Version != command.ExpectedVersion)
                {
                    return MaintenanceAiDraftWriteResult.Fail(
                        MaintenanceAiFailureCodes.ConcurrencyConflict,
                        "Draft version mismatch.");
                }

                if (draft.Status is MaintenanceAiDraftStatuses.Approved
                    or MaintenanceAiDraftStatuses.Rejected
                    or MaintenanceAiDraftStatuses.Executed
                    or MaintenanceAiDraftStatuses.ExecutionFailed
                    or MaintenanceAiDraftStatuses.Stale)
                {
                    return MaintenanceAiDraftWriteResult.Fail(
                        MaintenanceAiFailureCodes.InvalidStatus,
                        "Draft cannot be edited in current status.");
                }

                var now = DateTimeOffset.UtcNow;
                var actor = context.IdentitySubjectId.ToString();
                if (!string.IsNullOrWhiteSpace(command.Title))
                {
                    draft.HumanEditedTitle = command.Title.Trim();
                }

                if (command.Description is not null)
                {
                    draft.HumanEditedDescription = command.Description.Trim();
                }

                if (!string.IsNullOrWhiteSpace(command.Priority))
                {
                    draft.HumanEditedPriority = command.Priority.Trim();
                }

                if (!string.IsNullOrWhiteSpace(command.RequestType))
                {
                    draft.HumanEditedRequestType = command.RequestType.Trim();
                }

                draft.HumanEditorSubjectId = actor;
                draft.HumanEditedAtUtc = now;
                draft.Status = MaintenanceAiDraftStatuses.UnderReview;
                draft.Version += 1;
                draft.UpdatedAtUtc = now;
                draft.LastReviewedAtUtc = now;

                AddDraftAudit(dbContext, context, draft, MaintenanceAiAuditActions.DraftEdited, "success", now, new
                {
                    draftId = draft.Id,
                    draft.Version,
                });

                await dbContext.SaveChangesAsync(ct);
                return MaintenanceAiDraftWriteResult.Success(draft.Id, draft.Version);
            },
            cancellationToken);

    public Task<MaintenanceAiDraftWriteResult> ApproveDraftAsync(
        MaintenanceOperationContext context,
        ApproveMaintenanceAiDraftCommand command,
        CancellationToken cancellationToken) =>
        tenantDatabaseSession.ExecuteWriteAsync(
            ct => ApproveDraftCoreAsync(context, command, ct),
            cancellationToken);

    public Task<MaintenanceAiDraftWriteResult> RejectDraftAsync(
        MaintenanceOperationContext context,
        RejectMaintenanceAiDraftCommand command,
        CancellationToken cancellationToken) =>
        tenantDatabaseSession.ExecuteWriteAsync(
            async ct =>
            {
                var draft = await dbContext.MaintenanceAiActionDrafts
                    .SingleOrDefaultAsync(
                        d => d.TenantId == context.TenantId
                            && d.FacilityId == context.FacilityId
                            && d.Id == command.DraftId,
                        ct);

                if (draft is null)
                {
                    return MaintenanceAiDraftWriteResult.Fail(MaintenanceAiFailureCodes.NotFound, "Draft not found.");
                }

                if (draft.Status == MaintenanceAiDraftStatuses.Rejected)
                {
                    return MaintenanceAiDraftWriteResult.Success(draft.Id, draft.Version);
                }

                if (draft.Status is MaintenanceAiDraftStatuses.Executed
                    or MaintenanceAiDraftStatuses.Approved)
                {
                    return MaintenanceAiDraftWriteResult.Fail(
                        MaintenanceAiFailureCodes.InvalidStatus,
                        "Draft cannot be rejected in current status.");
                }

                if (draft.Version != command.ExpectedVersion)
                {
                    return MaintenanceAiDraftWriteResult.Fail(
                        MaintenanceAiFailureCodes.ConcurrencyConflict,
                        "Draft version mismatch.");
                }

                if (string.IsNullOrWhiteSpace(command.RejectionReason))
                {
                    return MaintenanceAiDraftWriteResult.Fail(
                        MaintenanceAiFailureCodes.ValidationFailed,
                        "Rejection reason is required.");
                }

                var now = DateTimeOffset.UtcNow;
                draft.Status = MaintenanceAiDraftStatuses.Rejected;
                draft.RejectedBySubjectId = context.IdentitySubjectId.ToString();
                draft.RejectedAtUtc = now;
                draft.RejectionReason = command.RejectionReason.Trim();
                draft.LastReviewedAtUtc = now;
                draft.UpdatedAtUtc = now;

                AddDraftAudit(dbContext, context, draft, MaintenanceAiAuditActions.DraftRejected, "success", now, new
                {
                    draftId = draft.Id,
                    reason = draft.RejectionReason,
                });

                await dbContext.SaveChangesAsync(ct);
                return MaintenanceAiDraftWriteResult.Success(draft.Id, draft.Version);
            },
            cancellationToken);

    private async Task<MaintenanceAiDraftWriteResult> GenerateDraftCoreAsync(
        MaintenanceOperationContext context,
        string assetReference,
        CancellationToken cancellationToken)
    {
        var (snapshot, errorCode) = await MaintenanceAiInputBuilder.BuildAsync(
            dbContext, context, assetReference, cancellationToken);

        if (snapshot is null)
        {
            return MaintenanceAiDraftWriteResult.Fail(
                errorCode ?? MaintenanceAiFailureCodes.SourceUnavailable,
                "Trusted input snapshot is unavailable.");
        }

        var fingerprint = MaintenanceAiInputBuilder.ComputeFingerprint(snapshot);
        var now = DateTimeOffset.UtcNow;
        var correlationId = context.CorrelationId ?? Guid.NewGuid();

        var existingActive = await dbContext.MaintenanceAiActionDrafts
            .Where(d => d.FacilityId == context.FacilityId
                && d.AssetReference == assetReference
                && d.InputFingerprint == fingerprint
                && (d.Status == MaintenanceAiDraftStatuses.Draft || d.Status == MaintenanceAiDraftStatuses.UnderReview))
            .OrderByDescending(d => d.GeneratedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        if (existingActive is not null)
        {
            return MaintenanceAiDraftWriteResult.Success(existingActive.Id, existingActive.Version);
        }

        await MarkStaleDraftsAsync(context, assetReference, now, cancellationToken);

        var draftId = Guid.NewGuid();
        var placeholder = new MaintenanceAiActionDraft
        {
            Id = draftId,
            TenantId = context.TenantId,
            FacilityId = context.FacilityId,
            AssetReference = assetReference,
            AssetName = snapshot.AssetName,
            ConditionEvaluationReference = snapshot.ConditionEvaluationReference,
            RiskAssessmentId = snapshot.RiskAssessmentId,
            InputFingerprint = fingerprint,
            RuleSetVersion = snapshot.RuleSetVersion,
            ProviderIdentifier = aiProvider.ProviderName,
            ModelIdentifier = aiProvider.ModelName,
            PromptVersion = MaintenanceAiAgentPromptVersions.V1,
            InputSnapshotJson = JsonSerializer.Serialize(snapshot, JsonOptions),
            Status = MaintenanceAiDraftStatuses.Draft,
            Version = 1,
            GeneratedBySubjectId = context.IdentitySubjectId.ToString(),
            GeneratedAtUtc = now,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
        };

        dbContext.MaintenanceAiActionDrafts.Add(placeholder);
        AddDraftAudit(dbContext, context, placeholder, MaintenanceAiAuditActions.GenerationRequested, "started", now, new
        {
            assetReference,
            fingerprint,
        });

        var providerResult = await aiProvider.GenerateDraftAsync(
            new MaintenanceAiProviderRequest(
                MaintenanceAiAgentPromptVersions.V1,
                snapshot,
                correlationId.ToString()),
            cancellationToken);

        if (!providerResult.Succeeded || providerResult.Output is null)
        {
            placeholder.Status = MaintenanceAiDraftStatuses.ExecutionFailed;
            placeholder.FailureCode = providerResult.FailureCode ?? MaintenanceAiFailureCodes.ProviderFailed;
            placeholder.FailureMessage = providerResult.FailureMessage ?? "Provider failed.";
            placeholder.ExecutionResultAtUtc = now;
            AddDraftAudit(dbContext, context, placeholder, MaintenanceAiAuditActions.ProviderFailed, "failed", now, new
            {
                code = placeholder.FailureCode,
            });
            await dbContext.SaveChangesAsync(cancellationToken);
            return MaintenanceAiDraftWriteResult.Fail(placeholder.FailureCode, placeholder.FailureMessage);
        }

        var validation = MaintenanceAiOutputValidator.Validate(providerResult.Output, snapshot);
        if (!validation.Valid)
        {
            placeholder.Status = MaintenanceAiDraftStatuses.ExecutionFailed;
            placeholder.FailureCode = validation.FailureCode ?? MaintenanceAiFailureCodes.InvalidOutput;
            placeholder.FailureMessage = validation.Reason ?? "Invalid structured output.";
            placeholder.RawProviderResponseJson = providerResult.RawResponseJson;
            placeholder.ExecutionResultAtUtc = now;
            AddDraftAudit(dbContext, context, placeholder, MaintenanceAiAuditActions.OutputRejected, "rejected", now, new
            {
                code = placeholder.FailureCode,
            });
            await dbContext.SaveChangesAsync(cancellationToken);
            return MaintenanceAiDraftWriteResult.Fail(placeholder.FailureCode, placeholder.FailureMessage);
        }

        var output = providerResult.Output;
        placeholder.ValidatedOutputJson = JsonSerializer.Serialize(output, JsonOptions);
        placeholder.AiOriginalOutputJson = placeholder.ValidatedOutputJson;
        placeholder.RawProviderResponseJson = providerResult.RawResponseJson;
        placeholder.QualitativeConfidence = output.QualitativeConfidence;
        placeholder.SuggestedTitle = output.SuggestedTitle;
        placeholder.SuggestedDescription = output.SuggestedDescription;
        placeholder.SuggestedPriority = output.SuggestedPriority;
        placeholder.SuggestedRequestType = output.SuggestedRequestType;
        placeholder.Status = MaintenanceAiDraftStatuses.Draft;

        AddDraftAudit(dbContext, context, placeholder, MaintenanceAiAuditActions.ProviderCompleted, "success", now, new
        {
            provider = providerResult.ProviderIdentifier,
        });
        AddDraftAudit(dbContext, context, placeholder, MaintenanceAiAuditActions.OutputValidated, "success", now, new
        {
            confidence = output.QualitativeConfidence,
        });
        AddDraftAudit(dbContext, context, placeholder, MaintenanceAiAuditActions.DraftCreated, "success", now, new
        {
            draftId = placeholder.Id,
        });

        await dbContext.SaveChangesAsync(cancellationToken);
        return MaintenanceAiDraftWriteResult.Success(placeholder.Id, placeholder.Version);
    }

    private async Task<MaintenanceAiDraftWriteResult> ApproveDraftCoreAsync(
        MaintenanceOperationContext context,
        ApproveMaintenanceAiDraftCommand command,
        CancellationToken cancellationToken)
    {
        var draft = await dbContext.MaintenanceAiActionDrafts
            .SingleOrDefaultAsync(
                d => d.TenantId == context.TenantId
                    && d.FacilityId == context.FacilityId
                    && d.Id == command.DraftId,
                cancellationToken);

        if (draft is null)
        {
            return MaintenanceAiDraftWriteResult.Fail(MaintenanceAiFailureCodes.NotFound, "Draft not found.");
        }

        if (draft.CreatedMaintenanceRequestId.HasValue)
        {
            AddDraftAudit(dbContext, context, draft, MaintenanceAiAuditActions.DuplicateApprovalPrevented, "duplicate", DateTimeOffset.UtcNow, new
            {
                requestId = draft.CreatedMaintenanceRequestId,
            });
            await dbContext.SaveChangesAsync(cancellationToken);
            return MaintenanceAiDraftWriteResult.SuccessWithRequest(
                draft.Id,
                draft.Version,
                draft.CreatedMaintenanceRequestId.Value,
                null);
        }

        if (draft.Status == MaintenanceAiDraftStatuses.Rejected)
        {
            return MaintenanceAiDraftWriteResult.Fail(MaintenanceAiFailureCodes.AlreadyRejected, "Draft was rejected.");
        }

        if (draft.Version != command.ExpectedVersion)
        {
            return MaintenanceAiDraftWriteResult.Fail(
                MaintenanceAiFailureCodes.ConcurrencyConflict,
                "Draft version mismatch.");
        }

        var freshness = await ValidateFreshnessAsync(context, draft, cancellationToken);
        if (!freshness.IsFresh)
        {
            draft.Status = MaintenanceAiDraftStatuses.Stale;
            draft.UpdatedAtUtc = DateTimeOffset.UtcNow;
            AddDraftAudit(dbContext, context, draft, MaintenanceAiAuditActions.DraftMarkedStale, "stale", DateTimeOffset.UtcNow, new
            {
                reason = freshness.Reason,
            });
            await dbContext.SaveChangesAsync(cancellationToken);
            return MaintenanceAiDraftWriteResult.Fail(MaintenanceAiFailureCodes.StaleDraft, freshness.Reason ?? "Draft is stale.");
        }

        if (draft.Status is not MaintenanceAiDraftStatuses.Draft and not MaintenanceAiDraftStatuses.UnderReview)
        {
            return MaintenanceAiDraftWriteResult.Fail(MaintenanceAiFailureCodes.InvalidStatus, "Draft cannot be approved.");
        }

        var now = DateTimeOffset.UtcNow;
        draft.Status = MaintenanceAiDraftStatuses.Approved;
        draft.ApprovedBySubjectId = context.IdentitySubjectId.ToString();
        draft.ApprovedAtUtc = now;
        draft.LastReviewedAtUtc = now;
        draft.UpdatedAtUtc = now;

        AddDraftAudit(dbContext, context, draft, MaintenanceAiAuditActions.DraftApproved, "approved", now, new
        {
            version = draft.Version,
        });
        AddDraftAudit(dbContext, context, draft, MaintenanceAiAuditActions.RequestCreationAttempted, "started", now, new
        {
            draftId = draft.Id,
        });

        var mapping = await dbContext.MaintenanceAssetMachineMappings.AsNoTracking()
            .SingleAsync(
                m => m.FacilityId == context.FacilityId && m.AssetReference == draft.AssetReference,
                cancellationToken);

        var title = draft.HumanEditedTitle ?? draft.SuggestedTitle;
        var description = draft.HumanEditedDescription ?? draft.SuggestedDescription;
        if (string.IsNullOrWhiteSpace(title))
        {
            draft.Status = MaintenanceAiDraftStatuses.ExecutionFailed;
            draft.FailureCode = MaintenanceAiFailureCodes.ValidationFailed;
            draft.FailureMessage = "Draft title is required.";
            draft.ExecutionResultAtUtc = now;
            await dbContext.SaveChangesAsync(cancellationToken);
            return MaintenanceAiDraftWriteResult.Fail(draft.FailureCode, draft.FailureMessage);
        }

        var request = new MaintenanceRequest
        {
            Id = Guid.NewGuid(),
            TenantId = context.TenantId,
            FacilityId = context.FacilityId,
            RequestNumber = await MaintenanceRepositorySupport.NextRequestNumberAsync(dbContext, context, cancellationToken),
            AssetType = mapping.AssetType,
            AssetId = mapping.AssetId,
            AssetReference = draft.AssetReference,
            AssetName = draft.AssetName,
            ProductionOrderId = null,
            Type = MaintenanceValueConversions.RequestTypeFromStorage(draft.HumanEditedRequestType ?? draft.SuggestedRequestType),
            Priority = MaintenanceValueConversions.PriorityFromStorage(draft.HumanEditedPriority ?? draft.SuggestedPriority),
            Status = MaintenanceRequestStatus.Open,
            Title = title.Trim(),
            Description = description?.Trim() ?? string.Empty,
            ReportedAtUtc = now,
            ReportedBySubjectId = context.IdentitySubjectId.ToString(),
            Source = "maintenance-ai-agent",
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
            Version = 1,
        };

        dbContext.MaintenanceRequests.Add(request);
        MaintenanceRepositorySupport.AddAudit(
            dbContext,
            context,
            MaintenanceAuditActions.RequestCreated,
            MaintenanceAuditEventTypes.RequestCreated,
            "maintenance.request",
            request.Id,
            now,
            new { requestId = request.Id, requestNumber = request.RequestNumber, aiDraftId = draft.Id });
        MaintenanceRepositorySupport.AddOutbox(
            dbContext,
            context,
            "maintenance-request",
            request.Id,
            request.Version,
            MaintenanceOutboxMessageTypes.RequestCreated,
            now,
            new { requestId = request.Id.ToString(), requestNumber = request.RequestNumber });

        draft.Status = MaintenanceAiDraftStatuses.Executed;
        draft.CreatedMaintenanceRequestId = request.Id;
        draft.ExecutionResultAtUtc = now;
        AddDraftAudit(dbContext, context, draft, MaintenanceAiAuditActions.RequestCreated, "success", now, new
        {
            requestId = request.Id,
        });

        await dbContext.SaveChangesAsync(cancellationToken);

        return MaintenanceAiDraftWriteResult.SuccessWithRequest(
            draft.Id,
            draft.Version,
            request.Id,
            request.RequestNumber);
    }

    private async Task MarkStaleDraftsAsync(
        MaintenanceOperationContext context,
        string assetReference,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var staleCandidates = await dbContext.MaintenanceAiActionDrafts
            .Where(d => d.FacilityId == context.FacilityId
                && d.AssetReference == assetReference
                && (d.Status == MaintenanceAiDraftStatuses.Draft || d.Status == MaintenanceAiDraftStatuses.UnderReview))
            .ToListAsync(cancellationToken);

        foreach (var draft in staleCandidates)
        {
            draft.Status = MaintenanceAiDraftStatuses.Stale;
            draft.UpdatedAtUtc = now;
            AddDraftAudit(dbContext, context, draft, MaintenanceAiAuditActions.DraftMarkedStale, "superseded", now, new
            {
                reason = "regenerated",
            });
        }
    }

    private async Task<(bool IsFresh, string? Reason)> ValidateFreshnessAsync(
        MaintenanceOperationContext context,
        MaintenanceAiActionDraft draft,
        CancellationToken cancellationToken)
    {
        var (snapshot, _) = await MaintenanceAiInputBuilder.BuildAsync(
            dbContext, context, draft.AssetReference, cancellationToken);

        if (snapshot is null)
        {
            return (false, "Current trusted input is unavailable.");
        }

        var currentFingerprint = MaintenanceAiInputBuilder.ComputeFingerprint(snapshot);
        if (!string.Equals(currentFingerprint, draft.InputFingerprint, StringComparison.Ordinal))
        {
            return (false, "Source condition/risk fingerprint changed.");
        }

        if (snapshot.RiskAssessmentId != draft.RiskAssessmentId)
        {
            return (false, "Risk assessment changed.");
        }

        return (true, null);
    }

    private static void AddDraftAudit(
        NevaxisZentraDbContext dbContext,
        MaintenanceOperationContext context,
        MaintenanceAiActionDraft draft,
        string action,
        string resultCode,
        DateTimeOffset now,
        object details)
    {
        dbContext.MaintenanceAiDraftAuditEvents.Add(new MaintenanceAiDraftAuditEvent
        {
            Id = Guid.NewGuid(),
            TenantId = context.TenantId,
            FacilityId = context.FacilityId,
            DraftId = draft.Id,
            Action = action,
            ActorSubjectId = context.IdentitySubjectId.ToString(),
            DraftVersion = draft.Version,
            CorrelationId = context.CorrelationId ?? Guid.NewGuid(),
            ResultCode = resultCode,
            DetailsJson = JsonSerializer.Serialize(details, JsonOptions),
            OccurredAtUtc = now,
        });
    }

    private static MaintenanceAiDraftSummaryReadModel MapSummary(MaintenanceAiActionDraft draft) =>
        new(
            draft.Id,
            draft.AssetReference,
            draft.AssetName,
            draft.Status,
            draft.QualitativeConfidence,
            draft.HumanEditedTitle ?? draft.SuggestedTitle,
            draft.Version,
            draft.GeneratedAtUtc,
            draft.UpdatedAtUtc,
            draft.CreatedMaintenanceRequestId,
            draft.ProviderIdentifier == MaintenanceAiAgentProviders.DeterministicDevelopment);

    private static async Task<MaintenanceAiDraftDetailReadModel> MapDetailAsync(
        NevaxisZentraDbContext dbContext,
        MaintenanceOperationContext context,
        MaintenanceAiActionDraft draft,
        CancellationToken cancellationToken)
    {
        var validated = JsonSerializer.Deserialize<MaintenanceAiStructuredOutput>(draft.ValidatedOutputJson, JsonOptions)
            ?? new MaintenanceAiStructuredOutput(
                string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty,
                [], [], string.Empty, MaintenanceAiQualitativeConfidenceLevels.Low, [], false);
        var original = JsonSerializer.Deserialize<MaintenanceAiStructuredOutput>(draft.AiOriginalOutputJson, JsonOptions)
            ?? validated;

        var snapshot = JsonSerializer.Deserialize<MaintenanceAiInputSnapshot>(draft.InputSnapshotJson, JsonOptions);
        var audit = await dbContext.MaintenanceAiDraftAuditEvents.AsNoTracking()
            .Where(e => e.DraftId == draft.Id)
            .OrderByDescending(e => e.OccurredAtUtc)
            .Take(20)
            .ToListAsync(cancellationToken);

        string? requestNumber = null;
        if (draft.CreatedMaintenanceRequestId.HasValue)
        {
            requestNumber = await dbContext.MaintenanceRequests.AsNoTracking()
                .Where(r => r.Id == draft.CreatedMaintenanceRequestId.Value)
                .Select(r => r.RequestNumber)
                .SingleOrDefaultAsync(cancellationToken);
        }

        return new MaintenanceAiDraftDetailReadModel(
            draft.Id,
            draft.AssetReference,
            draft.AssetName,
            draft.Status,
            draft.QualitativeConfidence,
            draft.ConditionEvaluationReference,
            draft.RiskAssessmentId,
            draft.InputFingerprint,
            draft.RuleSetVersion,
            draft.ProviderIdentifier,
            draft.ModelIdentifier,
            draft.PromptVersion,
            draft.ProviderIdentifier == MaintenanceAiAgentProviders.DeterministicDevelopment,
            snapshot?.DataFreshness ?? MaintenanceConditionDataFreshnessLevels.Unknown,
            snapshot?.DataSufficiency ?? MaintenanceConditionDataSufficiencyLevels.Insufficient,
            snapshot?.RiskBand ?? MaintenancePredictiveRiskBands.InsufficientData,
            snapshot?.DeterministicRecommendationCategory ?? MaintenancePredictiveRecommendationCategories.ReviewDataQuality,
            validated,
            original,
            draft.SuggestedTitle,
            draft.SuggestedDescription,
            draft.SuggestedPriority,
            draft.SuggestedRequestType,
            draft.HumanEditedTitle,
            draft.HumanEditedDescription,
            draft.HumanEditedPriority,
            draft.HumanEditedRequestType,
            draft.HumanEditorSubjectId,
            draft.HumanEditedAtUtc,
            draft.GeneratedBySubjectId,
            draft.GeneratedAtUtc,
            draft.ApprovedBySubjectId,
            draft.ApprovedAtUtc,
            draft.RejectedBySubjectId,
            draft.RejectedAtUtc,
            draft.RejectionReason,
            draft.CreatedMaintenanceRequestId,
            requestNumber,
            draft.FailureCode,
            draft.FailureMessage,
            draft.Version,
            audit.Select(a => new MaintenanceAiDraftAuditReadModel(
                a.Action,
                a.ActorSubjectId,
                a.DraftVersion,
                a.ResultCode,
                a.OccurredAtUtc)).ToList());
    }
}
