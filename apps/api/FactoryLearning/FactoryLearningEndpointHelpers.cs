using NevaxisFactOS.Api.Facilities;
using NevaxisFactOS.Persistence.Authorization;
using NevaxisFactOS.Persistence.FactoryLearning;

namespace NevaxisFactOS.Api.FactoryLearning;

internal static class FactoryLearningEndpointHelpers
{
    internal static async Task<(FacilityAccessContext? Access, IResult? Failure)> ResolveAccessAsync(
        Guid? facilityId,
        string permissionKey,
        IFacilityAccessResolver facilityAccessResolver,
        CancellationToken cancellationToken)
    {
        if (facilityId is null || facilityId == Guid.Empty)
        {
            return (null, TypedResults.BadRequest(new { code = "facility-missing", message = "Tesis tanımlayıcısı zorunludur." }));
        }

        var accessResult = await facilityAccessResolver.ResolveAsync(
            new FacilityAccessRequest(facilityId.Value, permissionKey),
            cancellationToken);

        if (!accessResult.Succeeded || accessResult.Access is null)
        {
            return (null, MapFacilityAccessFailure(accessResult));
        }

        return (accessResult.Access, null);
    }

    internal static IResult MapException(FactoryLearningException ex) =>
        ex.FailureCode switch
        {
            FactoryLearningFailureCodes.NotFound => TypedResults.NotFound(new { code = ex.FailureCode, message = ex.Message }),
            FactoryLearningFailureCodes.ConcurrencyConflict => TypedResults.Conflict(new { code = ex.FailureCode, message = ex.Message }),
            FactoryLearningFailureCodes.Forbidden => TypedResults.Forbid(),
            _ => TypedResults.UnprocessableEntity(new { code = ex.FailureCode, message = ex.Message }),
        };

    internal static object MapWorkspace(FactoryLearningWorkspaceSnapshot workspace) => new
    {
        commandSummary = workspace.CommandSummary,
        paretoRows = workspace.ParetoRows.Select(r => new
        {
            category = r.Category,
            label = r.Label,
            totalMinutes = r.TotalMinutes,
            contributionPercent = r.ContributionPercent,
            occurrenceCount = r.OccurrenceCount,
            dataClassification = r.DataClassification,
        }),
        losses = workspace.Losses.Select(MapLoss).ToList(),
        patterns = workspace.Patterns.Select(MapPattern).ToList(),
        opportunities = workspace.Opportunities.Select(MapOpportunity).ToList(),
        activeExperiments = workspace.ActiveExperiments.Select(MapExperiment).ToList(),
        verifiedImprovements = workspace.VerifiedImprovements.Select(MapVerifiedImprovement).ToList(),
        learningMemories = workspace.LearningMemories.Select(MapLearningMemory).ToList(),
        insights = workspace.Insights.Select(i => new
        {
            insightType = i.InsightType,
            title = i.Title,
            summary = i.Summary,
            confidenceScore = i.ConfidenceScore,
            evidenceSummary = i.EvidenceSummary,
            linkedMemoryId = i.LinkedMemoryId,
        }),
        dataFreshnessUtc = workspace.DataFreshnessUtc,
    };

    internal static object MapLoss(FactoryLoss loss) => new
    {
        id = loss.Id,
        category = loss.Category,
        title = loss.Title,
        summary = loss.Summary,
        machineId = loss.MachineId,
        durationMinutes = loss.DurationMinutes,
        sourceModule = loss.SourceModule,
        evidenceJson = loss.EvidenceJson,
        dataClassification = loss.DataClassification,
        confidenceScore = loss.ConfidenceScore,
        recurrenceCount = loss.RecurrenceCount,
        lossTreePathJson = loss.LossTreePathJson,
        lastSeenAtUtc = loss.LastSeenAtUtc,
    };

    internal static object MapPattern(FactoryLossPattern pattern) => new
    {
        id = pattern.Id,
        patternType = pattern.PatternType,
        title = pattern.Title,
        summary = pattern.Summary,
        patternKey = pattern.PatternKey,
        occurrenceCount = pattern.OccurrenceCount,
        confidenceScore = pattern.ConfidenceScore,
        dataClassification = pattern.DataClassification,
    };

    internal static object MapOpportunity(ImprovementOpportunity opportunity) => new
    {
        id = opportunity.Id,
        title = opportunity.Title,
        problemSummary = opportunity.ProblemSummary,
        status = opportunity.Status,
        priorityScore = opportunity.PriorityScore,
        recoverablePotentialScore = opportunity.RecoverablePotentialScore,
        impactClassification = opportunity.ImpactClassification,
        domain = opportunity.Domain,
        confidenceScore = opportunity.ConfidenceScore,
        ownerSubjectId = opportunity.OwnerSubjectId,
        concurrencyToken = opportunity.ConcurrencyToken,
    };

    internal static object MapExperiment(FactoryExperiment experiment) => new
    {
        id = experiment.Id,
        opportunityId = experiment.OpportunityId,
        experimentType = experiment.ExperimentType,
        title = experiment.Title,
        hypothesis = experiment.Hypothesis,
        status = experiment.Status,
        outcome = experiment.Outcome,
        approvalState = experiment.ApprovalState,
        startedAtUtc = experiment.StartedAtUtc,
        completedAtUtc = experiment.CompletedAtUtc,
        verifiedAtUtc = experiment.VerifiedAtUtc,
        concurrencyToken = experiment.ConcurrencyToken,
    };

    internal static object MapVerifiedImprovement(VerifiedImprovement verified) => new
    {
        id = verified.Id,
        title = verified.Title,
        outcomeSummary = verified.OutcomeSummary,
        beforeJson = verified.BeforeJson,
        afterJson = verified.AfterJson,
        deltaJson = verified.DeltaJson,
        effectivenessScore = verified.EffectivenessScore,
        impactClassification = verified.ImpactClassification,
        standardizationState = verified.StandardizationState,
        sustainabilityStatus = verified.SustainabilityStatus,
        verifiedAtUtc = verified.VerifiedAtUtc,
        concurrencyToken = verified.ConcurrencyToken,
    };

    internal static object MapLearningMemory(FactoryLearningMemory memory) => new
    {
        id = memory.Id,
        problemPattern = memory.ProblemPattern,
        resultSummary = memory.ResultSummary,
        effectivenessScore = memory.EffectivenessScore,
        memoryQuality = memory.MemoryQuality,
        evidenceJson = memory.EvidenceJson,
        recordedAtUtc = memory.RecordedAtUtc,
    };

    internal static object MapRootCause(FactoryRootCauseAnalysis analysis) => new
    {
        id = analysis.Id,
        method = analysis.Method,
        summary = analysis.Summary,
        candidateCause = analysis.CandidateCause,
        confidenceLevel = analysis.ConfidenceLevel,
        isConfirmedCause = analysis.IsConfirmedCause,
        concurrencyToken = analysis.ConcurrencyToken,
    };

    private static Microsoft.AspNetCore.Http.HttpResults.ProblemHttpResult MapFacilityAccessFailure(
        FacilityAccessResolutionResult accessResult) =>
        accessResult.FailureCode switch
        {
            FacilityAccessFailureCodes.Unauthenticated => TypedResults.Problem(
                title: "Kimlik doğrulama gerekli.",
                statusCode: StatusCodes.Status401Unauthorized),
            FacilityAccessFailureCodes.PermissionDenied => TypedResults.Problem(
                title: "Yetki reddedildi.",
                statusCode: StatusCodes.Status403Forbidden),
            _ => TypedResults.Problem(
                title: accessResult.InternalReason,
                statusCode: StatusCodes.Status403Forbidden),
        };
}
