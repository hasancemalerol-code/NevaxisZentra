using System.Globalization;
using CrevaFactOS.Persistence.AiUsage;
using CrevaFactOS.Persistence.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace CrevaFactOS.Persistence.Export;

public sealed class ExportAiWriteRepository(
    CrevaFactOSDbContext dbContext,
    TenantDatabaseSession tenantDatabaseSession,
    ExportAiContextBuilder contextBuilder,
    IExportAiProvider aiProvider)
    : IExportAiWriteRepository
{
    public async Task<ExportAiWriteResult> StartAnalysisAsync(
        ExportOperationContext context,
        StartExportAiAnalysisCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(command);

        if (command.ExportCaseId is null && command.CountryResearchCaseId is null)
        {
            return ExportAiWriteResult.Fail(
                ExportFailureCodes.ValidationFailed,
                "Either an export case or country research case is required.");
        }

        try
        {
            return await tenantDatabaseSession.ExecuteWriteAsync(
                async ct => await RunAnalysisAsync(context, command, ct),
                cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return ExportAiWriteResult.Fail(
                ExportFailureCodes.ConcurrencyConflict,
                "The analysis run has been modified. Please reload and try again.");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return ExportAiWriteResult.Fail(
                ExportFailureCodes.SourceUnavailable,
                "Export AI service is unavailable.");
        }
    }

    public async Task<ExportAiRecommendationWriteResult> ReviewRecommendationAsync(
        ExportOperationContext context,
        ReviewExportAiRecommendationCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(command);

        try
        {
            return await tenantDatabaseSession.ExecuteWriteAsync(
                async ct =>
                {
                    var exportCase = await dbContext.ExportCases
                        .SingleOrDefaultAsync(
                            c => c.TenantId == context.TenantId
                                && c.FacilityId == context.FacilityId
                                && c.Id == command.ExportCaseId,
                            ct);

                    if (exportCase is null)
                    {
                        return ExportAiRecommendationWriteResult.Fail(
                            ExportFailureCodes.NotFound,
                            "Export case was not found.");
                    }

                    var recommendation = await (
                        from rec in dbContext.ExportAiRecommendations
                        join run in dbContext.ExportAiAnalysisRuns
                            on rec.AnalysisRunId equals run.Id
                        where rec.TenantId == context.TenantId
                            && rec.FacilityId == context.FacilityId
                            && rec.Id == command.RecommendationId
                            && run.ExportCaseId == command.ExportCaseId
                        select rec).SingleOrDefaultAsync(ct);

                    if (recommendation is null)
                    {
                        return ExportAiRecommendationWriteResult.Fail(
                            Export4LFailureCodes.RecommendationNotFound,
                            "AI recommendation was not found.");
                    }

                    if (recommendation.Version != command.ExpectedVersion)
                    {
                        return ExportAiRecommendationWriteResult.Fail(
                            ExportFailureCodes.ConcurrencyConflict,
                            "The recommendation has been modified. Please reload and try again.");
                    }

                    var targetStatus = MapReviewDecisionToStatus(command.Decision);
                    if (recommendation.Status is ExportAiRecommendationStatus.Accepted
                        or ExportAiRecommendationStatus.Rejected
                        or ExportAiRecommendationStatus.Dismissed)
                    {
                        if (recommendation.Status == targetStatus)
                        {
                            return ExportAiRecommendationWriteResult.Success(
                                recommendation.Version,
                                exportCase.Version);
                        }

                        return ExportAiRecommendationWriteResult.Fail(
                            Export4LFailureCodes.InvalidRecommendationStatus,
                            "The recommendation has already been reviewed.");
                    }

                    if (recommendation.Status != ExportAiRecommendationStatus.Proposed)
                    {
                        return ExportAiRecommendationWriteResult.Fail(
                            Export4LFailureCodes.InvalidRecommendationStatus,
                            "Only proposed recommendations can be reviewed.");
                    }

                    var now = DateTimeOffset.UtcNow;
                    var correlationId = context.CorrelationId ?? Guid.NewGuid();
                    var actorSubjectId = context.IdentitySubjectId.ToString();

                    recommendation.Status = targetStatus;
                    recommendation.ReviewDecision = command.Decision;
                    recommendation.ReviewNote = string.IsNullOrWhiteSpace(command.ReviewNote)
                        ? null
                        : command.ReviewNote.Trim();
                    recommendation.ReviewedAtUtc = now;
                    recommendation.ReviewedBySubjectId = actorSubjectId;
                    recommendation.Version += 1;

                    exportCase.Version += 1;
                    exportCase.UpdatedAtUtc = now;
                    exportCase.LastModifiedBySubjectId = actorSubjectId;

                    var helper = new ExportReadinessPersistence(dbContext);
                    helper.AddAudit(
                        context,
                        ExportAuditEventTypes.ExportAiRecommendationReviewed,
                        ExportAuditActions.ExportAiRecommendationReviewed,
                        "export.ai.recommendation",
                        recommendation.Id.ToString(),
                        correlationId,
                        now,
                        new
                        {
                            exportCaseId = command.ExportCaseId.ToString(),
                            recommendationId = recommendation.Id.ToString(),
                            analysisRunId = recommendation.AnalysisRunId.ToString(),
                            decision = ExportValueConversions.ToStorage(command.Decision),
                        });

                    if (command.Decision == ExportAiReviewDecision.Accepted)
                    {
                        helper.AddOutbox(
                            context.TenantId,
                            ExportAiOutboxMessageTypes.ExportAiRecommendationAccepted,
                            "export-case",
                            exportCase.Id.ToString(),
                            correlationId,
                            $"{exportCase.Id}:{exportCase.Version}:{recommendation.Id}:{ExportAiOutboxMessageTypes.ExportAiRecommendationAccepted}",
                            now,
                            new Dictionary<string, string?>
                            {
                                ["tenantId"] = context.TenantId.ToString(),
                                ["facilityId"] = context.FacilityId.ToString(),
                                ["exportCaseId"] = exportCase.Id.ToString(),
                                ["recommendationId"] = recommendation.Id.ToString(),
                                ["analysisRunId"] = recommendation.AnalysisRunId.ToString(),
                                ["proposedActionType"] = recommendation.ProposedActionType.HasValue
                                    ? ExportValueConversions.ToStorage(recommendation.ProposedActionType.Value)
                                    : null,
                                ["occurredAtUtc"] = now.ToString("O"),
                            });

                        if (recommendation.ProposedActionType == ExportAiProposedActionType.CreateTask)
                        {
                            helper.AddOutbox(
                                context.TenantId,
                                ExportAiOutboxMessageTypes.ExportAiTaskRequested,
                                "export-case",
                                exportCase.Id.ToString(),
                                correlationId,
                                $"{exportCase.Id}:{exportCase.Version}:{recommendation.Id}:{ExportAiOutboxMessageTypes.ExportAiTaskRequested}",
                                now,
                                new Dictionary<string, string?>
                                {
                                    ["tenantId"] = context.TenantId.ToString(),
                                    ["facilityId"] = context.FacilityId.ToString(),
                                    ["exportCaseId"] = exportCase.Id.ToString(),
                                    ["recommendationId"] = recommendation.Id.ToString(),
                                    ["sourceModule"] = ExportAiTaskIntegrationContract.SourceModule,
                                    ["taskType"] = ExportAiTaskIntegrationContract.TaskType,
                                    ["title"] = recommendation.Title,
                                    ["description"] = recommendation.Description,
                                    ["payloadJson"] = recommendation.ProposedActionPayloadJson,
                                    ["occurredAtUtc"] = now.ToString("O"),
                                });
                        }
                    }

                    await dbContext.SaveChangesAsync(ct);
                    return ExportAiRecommendationWriteResult.Success(
                        recommendation.Version,
                        exportCase.Version);
                },
                cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return ExportAiRecommendationWriteResult.Fail(
                ExportFailureCodes.ConcurrencyConflict,
                "The recommendation has been modified. Please reload and try again.");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return ExportAiRecommendationWriteResult.Fail(
                ExportFailureCodes.SourceUnavailable,
                "Export AI service is unavailable.");
        }
    }

    private async Task<ExportAiWriteResult> RunAnalysisAsync(
        ExportOperationContext context,
        StartExportAiAnalysisCommand command,
        CancellationToken cancellationToken)
    {
        ExportAiContextSnapshotModel? contextModel;
        Guid? exportCaseId = command.ExportCaseId;
        Guid? researchCaseId = command.CountryResearchCaseId;

        if (exportCaseId.HasValue)
        {
            var exportCase = await dbContext.ExportCases
                .SingleOrDefaultAsync(
                    c => c.TenantId == context.TenantId
                        && c.FacilityId == context.FacilityId
                        && c.Id == exportCaseId.Value,
                    cancellationToken);

            if (exportCase is null)
            {
                return ExportAiWriteResult.Fail(
                    ExportFailureCodes.NotFound,
                    "Export case was not found.");
            }

            if (exportCase.Status == ExportCaseStatus.Closed)
            {
                return ExportAiWriteResult.Fail(
                    ExportFailureCodes.CaseClosed,
                    "Cannot run AI analysis for a closed export case.");
            }

            contextModel = await contextBuilder.BuildForExportCaseAsync(
                context,
                exportCaseId.Value,
                cancellationToken);
        }
        else
        {
            var researchCase = await dbContext.ExportCountryResearchCases
                .SingleOrDefaultAsync(
                    r => r.TenantId == context.TenantId
                        && r.FacilityId == context.FacilityId
                        && r.Id == researchCaseId!.Value,
                    cancellationToken);

            if (researchCase is null)
            {
                return ExportAiWriteResult.Fail(
                    ExportFailureCodes.NotFound,
                    "Country research case was not found.");
            }

            exportCaseId = researchCase.LinkedExportCaseId;
            contextModel = await contextBuilder.BuildForResearchCaseAsync(
                context,
                researchCaseId!.Value,
                cancellationToken);
        }

        if (contextModel is null)
        {
            return ExportAiWriteResult.Fail(
                Export4LFailureCodes.ContextUnavailable,
                "Export AI context could not be built.");
        }

        var now = DateTimeOffset.UtcNow;
        var correlationId = context.CorrelationId ?? Guid.NewGuid();
        var actorSubjectId = context.IdentitySubjectId.ToString();
        var promptVersion = ResolvePromptVersion(command.RunType);
        var contextJson = ExportAiContextBuilder.Serialize(contextModel);
        var contextHash = ExportAiContextBuilder.ComputeHash(contextJson);

        var snapshot = new ExportAiContextSnapshot
        {
            Id = Guid.NewGuid(),
            TenantId = context.TenantId,
            FacilityId = context.FacilityId,
            ExportCaseId = exportCaseId,
            CountryResearchCaseId = researchCaseId,
            SnapshotVersion = 1,
            ContextHash = contextHash,
            ContextJson = contextJson,
            CreatedAtUtc = now,
            CreatedBySubjectId = actorSubjectId,
        };

        dbContext.ExportAiContextSnapshots.Add(snapshot);

        var run = new ExportAiAnalysisRun
        {
            Id = Guid.NewGuid(),
            TenantId = context.TenantId,
            FacilityId = context.FacilityId,
            ExportCaseId = exportCaseId,
            CountryResearchCaseId = researchCaseId,
            ContextSnapshotId = snapshot.Id,
            RunType = command.RunType,
            Status = ExportAiAnalysisRunStatus.Running,
            Provider = aiProvider.ProviderName,
            Model = aiProvider.ModelName,
            PromptVersion = promptVersion,
            StartedAtUtc = now,
            RequestedBySubjectId = actorSubjectId,
            Version = 1,
        };

        dbContext.ExportAiAnalysisRuns.Add(run);

        var helper = new ExportReadinessPersistence(dbContext);
        helper.AddAudit(
            context,
            ExportAuditEventTypes.ExportAiAnalysisStarted,
            ExportAuditActions.ExportAiAnalysisStarted,
            "export.ai.run",
            run.Id.ToString(),
            correlationId,
            now,
            new
            {
                analysisRunId = run.Id.ToString(),
                exportCaseId = exportCaseId?.ToString(),
                countryResearchCaseId = researchCaseId?.ToString(),
                runType = ExportValueConversions.ToStorage(command.RunType),
                promptVersion,
            });

        ExportAiProviderOutput output;
        try
        {
            output = await aiProvider.AnalyzeExportCaseAsync(
                command.RunType,
                promptVersion,
                contextModel,
                cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            run.Status = ExportAiAnalysisRunStatus.Failed;
            run.CompletedAtUtc = DateTimeOffset.UtcNow;
            run.FailureCode = Export4LFailureCodes.ProviderUnavailable;
            run.FailureMessage = "The AI provider failed to complete analysis.";
            run.Version += 1;

            helper.AddAudit(
                context,
                ExportAuditEventTypes.ExportAiAnalysisFailed,
                ExportAuditActions.ExportAiAnalysisFailed,
                "export.ai.run",
                run.Id.ToString(),
                correlationId,
                run.CompletedAtUtc.Value,
                new
                {
                    analysisRunId = run.Id.ToString(),
                    failureCode = run.FailureCode,
                });

            await dbContext.SaveChangesAsync(cancellationToken);
            return ExportAiWriteResult.Fail(
                Export4LFailureCodes.ProviderUnavailable,
                "The AI provider failed to complete analysis.");
        }

        foreach (var findingDraft in output.Findings)
        {
            dbContext.ExportAiFindings.Add(new ExportAiFinding
            {
                Id = Guid.NewGuid(),
                TenantId = context.TenantId,
                FacilityId = context.FacilityId,
                AnalysisRunId = run.Id,
                Category = findingDraft.Category,
                Title = findingDraft.Title,
                Summary = findingDraft.Summary,
                Severity = findingDraft.Severity,
                Confidence = findingDraft.Confidence,
                SourceType = findingDraft.SourceType,
                SourceReferencesJson = Export4LMapping.SerializeSources(findingDraft.SourceReferences),
                RelatedRequirementId = findingDraft.RelatedRequirementId,
                RelatedDocumentId = findingDraft.RelatedDocumentId,
                RelatedShipmentId = findingDraft.RelatedShipmentId,
                Actionable = findingDraft.Actionable,
                HumanReviewRequired = findingDraft.HumanReviewRequired,
                CreatedAtUtc = now,
            });
        }

        foreach (var recommendationDraft in output.Recommendations)
        {
            dbContext.ExportAiRecommendations.Add(new ExportAiRecommendation
            {
                Id = Guid.NewGuid(),
                TenantId = context.TenantId,
                FacilityId = context.FacilityId,
                AnalysisRunId = run.Id,
                Title = recommendationDraft.Title,
                Description = recommendationDraft.Description,
                Priority = recommendationDraft.Priority,
                Confidence = recommendationDraft.Confidence,
                ReasoningSummary = recommendationDraft.ReasoningSummary,
                ProposedActionType = recommendationDraft.ProposedActionType,
                ProposedActionPayloadJson = recommendationDraft.ProposedActionPayloadJson,
                Status = ExportAiRecommendationStatus.Proposed,
                CreatedAtUtc = now,
                Version = 1,
            });
        }

        run.Status = ExportAiAnalysisRunStatus.Completed;
        run.CompletedAtUtc = DateTimeOffset.UtcNow;
        run.InputUsageUnits = output.InputUsageUnits;
        run.OutputUsageUnits = output.OutputUsageUnits;
        run.AiCreditsConsumed = output.AiCreditsConsumed;
        run.ReasoningSummary = output.ReasoningSummary;
        run.Version += 1;

        var hasHighRisk = output.Findings.Any(f =>
            f.Severity == ExportAiFindingSeverity.Critical
            || f.HumanReviewRequired)
            || output.Recommendations.Any(r =>
                r.Priority is ExportAiRecommendationPriority.Critical or ExportAiRecommendationPriority.High);

        helper.AddAudit(
            context,
            ExportAuditEventTypes.ExportAiAnalysisCompleted,
            ExportAuditActions.ExportAiAnalysisCompleted,
            "export.ai.run",
            run.Id.ToString(),
            correlationId,
            run.CompletedAtUtc.Value,
            new
            {
                analysisRunId = run.Id.ToString(),
                exportCaseId = exportCaseId?.ToString(),
                runType = ExportValueConversions.ToStorage(command.RunType),
                findingCount = output.Findings.Count,
                recommendationCount = output.Recommendations.Count,
                hasHighRisk,
            });

        helper.AddOutbox(
            context.TenantId,
            ExportAiOutboxMessageTypes.ExportAiAnalysisCompleted,
            exportCaseId.HasValue ? "export-case" : "export-country-research-case",
            (exportCaseId ?? researchCaseId)!.Value.ToString(),
            correlationId,
            $"{run.Id}:{run.Version}:{ExportAiOutboxMessageTypes.ExportAiAnalysisCompleted}",
            run.CompletedAtUtc.Value,
            new Dictionary<string, string?>
            {
                ["tenantId"] = context.TenantId.ToString(),
                ["facilityId"] = context.FacilityId.ToString(),
                ["analysisRunId"] = run.Id.ToString(),
                ["exportCaseId"] = exportCaseId?.ToString(),
                ["countryResearchCaseId"] = researchCaseId?.ToString(),
                ["runType"] = ExportValueConversions.ToStorage(command.RunType),
                ["findingCount"] = output.Findings.Count.ToString(CultureInfo.InvariantCulture),
                ["recommendationCount"] = output.Recommendations.Count.ToString(CultureInfo.InvariantCulture),
                ["occurredAtUtc"] = run.CompletedAtUtc.Value.ToString("O"),
            });

        if (hasHighRisk)
        {
            helper.AddOutbox(
                context.TenantId,
                ExportAiOutboxMessageTypes.ExportAiHighRiskFound,
                exportCaseId.HasValue ? "export-case" : "export-country-research-case",
                (exportCaseId ?? researchCaseId)!.Value.ToString(),
                correlationId,
                $"{run.Id}:{run.Version}:{ExportAiOutboxMessageTypes.ExportAiHighRiskFound}",
                run.CompletedAtUtc.Value,
                new Dictionary<string, string?>
                {
                    ["tenantId"] = context.TenantId.ToString(),
                    ["facilityId"] = context.FacilityId.ToString(),
                    ["analysisRunId"] = run.Id.ToString(),
                    ["exportCaseId"] = exportCaseId?.ToString(),
                    ["occurredAtUtc"] = run.CompletedAtUtc.Value.ToString("O"),
                });
        }

        if (exportCaseId.HasValue)
        {
            var exportCase = await dbContext.ExportCases
                .SingleAsync(
                    c => c.TenantId == context.TenantId
                        && c.FacilityId == context.FacilityId
                        && c.Id == exportCaseId.Value,
                    cancellationToken);

            helper.AppendActivity(
                exportCase,
                ExportActivityEventTypes.AiAnalysisCompleted,
                "AI analysis completed",
                $"{ExportValueConversions.ToStorage(command.RunType)} ({output.Findings.Count} findings)",
                run.CompletedAtUtc.Value,
                ExportActivitySourceType.User,
                actorSubjectId,
                correlationId,
                "export.ai.run",
                run.Id);

            if (hasHighRisk)
            {
                helper.AppendActivity(
                    exportCase,
                    ExportActivityEventTypes.AiHighRiskFound,
                    "AI high-risk finding",
                    output.ReasoningSummary,
                    run.CompletedAtUtc.Value,
                    ExportActivitySourceType.System,
                    actorSubjectId,
                    correlationId,
                    "export.ai.run",
                    run.Id);
            }
        }

        if (run.Status == ExportAiAnalysisRunStatus.Completed && run.AiCreditsConsumed > 0)
        {
            dbContext.AiUsageRecords.Add(new AiUsageRecord
            {
                Id = Guid.NewGuid(),
                TenantId = context.TenantId,
                FacilityId = context.FacilityId,
                UserSubjectId = actorSubjectId,
                Module = "export",
                Agent = ExportValueConversions.ToStorage(command.RunType),
                RunId = run.Id,
                Provider = run.Provider,
                Model = run.Model,
                InputUsage = run.InputUsageUnits,
                OutputUsage = run.OutputUsageUnits,
                CreditsConsumed = run.AiCreditsConsumed,
                CreatedAtUtc = run.CompletedAtUtc ?? now,
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return ExportAiWriteResult.Success(run.Id, run.Version);
    }

    private static string ResolvePromptVersion(ExportAiAnalysisRunType runType) =>
        runType switch
        {
            ExportAiAnalysisRunType.CaseOverview => ExportAiPromptVersions.CaseOverviewV1,
            ExportAiAnalysisRunType.ComplianceExplanation => ExportAiPromptVersions.ComplianceExplainV1,
            ExportAiAnalysisRunType.ReadinessAnalysis => ExportAiPromptVersions.ReadinessAnalysisV1,
            ExportAiAnalysisRunType.MissingDataAnalysis => ExportAiPromptVersions.MissingDataV1,
            ExportAiAnalysisRunType.CountryResearchAssist => ExportAiPromptVersions.CountryResearchV1,
            ExportAiAnalysisRunType.NextActions => ExportAiPromptVersions.NextActionsV1,
            ExportAiAnalysisRunType.DocumentGapAnalysis => ExportAiPromptVersions.DocumentGapV1,
            ExportAiAnalysisRunType.CrossModuleRiskAnalysis => ExportAiPromptVersions.CrossModuleRiskV1,
            _ => ExportAiPromptVersions.CaseOverviewV1,
        };

    private static ExportAiRecommendationStatus MapReviewDecisionToStatus(ExportAiReviewDecision decision) =>
        decision switch
        {
            ExportAiReviewDecision.Accepted => ExportAiRecommendationStatus.Accepted,
            ExportAiReviewDecision.Rejected => ExportAiRecommendationStatus.Rejected,
            ExportAiReviewDecision.Dismissed => ExportAiRecommendationStatus.Dismissed,
            _ => ExportAiRecommendationStatus.Proposed,
        };
}

public sealed class UnavailableExportAiWriteRepository : IExportAiWriteRepository
{
    private static ExportAiWriteResult AnalysisUnavailable() =>
        ExportAiWriteResult.Fail(
            ExportFailureCodes.SourceUnavailable,
            "Export AI persistence is disabled.");

    private static ExportAiRecommendationWriteResult RecommendationUnavailable() =>
        ExportAiRecommendationWriteResult.Fail(
            ExportFailureCodes.SourceUnavailable,
            "Export AI persistence is disabled.");

    public Task<ExportAiWriteResult> StartAnalysisAsync(
        ExportOperationContext context,
        StartExportAiAnalysisCommand command,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(AnalysisUnavailable());

    public Task<ExportAiRecommendationWriteResult> ReviewRecommendationAsync(
        ExportOperationContext context,
        ReviewExportAiRecommendationCommand command,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(RecommendationUnavailable());
}
