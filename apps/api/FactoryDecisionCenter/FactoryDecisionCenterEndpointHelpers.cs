using NevaxisFactOS.Api.Facilities;
using NevaxisFactOS.Persistence.FactoryDecisionCenter;
using NevaxisFactOS.Persistence.FactoryRecoveryIntelligence;

namespace NevaxisFactOS.Api.FactoryDecisionCenter;

internal static class FactoryDecisionCenterEndpointHelpers
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

    internal static IResult MapException(FactoryDecisionCenterException ex) =>
        ex.FailureCode switch
        {
            FactoryDecisionCenterFailureCodes.NotFound => TypedResults.NotFound(new { code = ex.FailureCode, message = ex.Message }),
            FactoryDecisionCenterFailureCodes.ConcurrencyConflict => TypedResults.Conflict(new { code = ex.FailureCode, message = ex.Message }),
            FactoryDecisionCenterFailureCodes.Forbidden => TypedResults.Forbid(),
            FactoryDecisionCenterFailureCodes.ApprovalRequired => TypedResults.UnprocessableEntity(new { code = ex.FailureCode, message = ex.Message }),
            FactoryDecisionCenterFailureCodes.OutcomeNotVerified => TypedResults.UnprocessableEntity(new { code = ex.FailureCode, message = ex.Message }),
            FactoryDecisionCenterFailureCodes.TasksNotComplete => TypedResults.UnprocessableEntity(new { code = ex.FailureCode, message = ex.Message }),
            FactoryDecisionCenterFailureCodes.DevelopmentOnly => TypedResults.Forbid(),
            _ => TypedResults.UnprocessableEntity(new { code = ex.FailureCode, message = ex.Message }),
        };

    internal static object MapWorkspace(FactoryDecisionCenterWorkspaceSnapshot workspace) => new
    {
        summary = new
        {
            criticalDecisions = workspace.Summary.CriticalDecisions,
            outcomesAtRisk = workspace.Summary.OutcomesAtRisk,
            pendingHumanApproval = workspace.Summary.PendingHumanApproval,
            recoveryOperations = workspace.Summary.RecoveryOperations,
            pendingVerification = workspace.Summary.PendingVerification,
        },
        priorityQueue = workspace.PriorityQueue.Select(MapQueueItem).ToList(),
        liveQueues = new
        {
            criticalDecisions = workspace.LiveQueues.CriticalDecisions.Select(MapQueueItem).ToList(),
            activeRecoveryOperations = workspace.LiveQueues.ActiveRecoveryOperations.Select(MapQueueItem).ToList(),
            pendingHumanApproval = workspace.LiveQueues.PendingHumanApproval.Select(MapQueueItem).ToList(),
            blockedOrEscalated = workspace.LiveQueues.BlockedOrEscalated.Select(MapQueueItem).ToList(),
            pendingVerification = workspace.LiveQueues.PendingVerification.Select(MapQueueItem).ToList(),
            verifiedClosedToday = workspace.LiveQueues.VerifiedClosedToday.Select(MapQueueItem).ToList(),
        },
        earlyWarnings = new
        {
            criticalPredictedRisks = workspace.EarlyWarnings.CriticalPredictedRisks.Select(MapEarlyWarningCard).ToList(),
            deliveryRisks = workspace.EarlyWarnings.DeliveryRisks.Select(MapEarlyWarningCard).ToList(),
            machineDegradationTrends = workspace.EarlyWarnings.MachineDegradationTrends.Select(MapEarlyWarningCard).ToList(),
            materialDepletionRisks = workspace.EarlyWarnings.MaterialDepletionRisks.Select(MapEarlyWarningCard).ToList(),
            qualityDeteriorationTrends = workspace.EarlyWarnings.QualityDeteriorationTrends.Select(MapEarlyWarningCard).ToList(),
        },
        generatedAtUtc = workspace.GeneratedAtUtc,
    };

    private static object MapEarlyWarningCard(FactoryDecisionEarlyWarningCard card) => new
    {
        riskId = card.RiskId,
        caseId = card.CaseId,
        riskType = card.RiskType,
        riskState = card.RiskState,
        machineCode = card.MachineCode,
        orderReference = card.OrderReference,
        observedSummary = card.ObservedSummary,
        predictedSummary = card.PredictedSummary,
        timeToImpactLabel = card.TimeToImpactLabel,
        confidenceBand = card.ConfidenceBand,
        horizon = card.Horizon,
        evidence = card.Evidence,
        recommendedActions = card.RecommendedActions,
        dataQuality = card.DataQuality,
        generatedAtUtc = card.GeneratedAtUtc,
    };

    internal static object MapCaseDetail(FactoryDecisionCaseDetailSnapshot detail) => new
    {
        caseId = detail.CaseId,
        title = detail.Title,
        severity = detail.Severity,
        status = detail.Status,
        operationalProblem = detail.OperationalProblem,
        businessOutcomeAtRisk = detail.BusinessOutcomeAtRisk,
        facilityLabel = detail.FacilityLabel,
        orderReference = detail.OrderReference,
        customerName = detail.CustomerName,
        machineCode = detail.MachineCode,
        moldCode = detail.MoldCode,
        materialCode = detail.MaterialCode,
        deliveryCommitmentLabel = detail.DeliveryCommitmentLabel,
        outcomeRisk = detail.OutcomeRisk,
        assessment = detail.Assessment,
        recoveryPlan = detail.RecoveryPlan,
        digitalWorkers = detail.DigitalWorkers,
        humanDecision = detail.HumanDecision,
        executionProgress = detail.ExecutionProgress,
        evidence = detail.Evidence,
        verifiedOutcome = detail.VerifiedOutcome,
        timeline = detail.Timeline,
        recoveryIntelligence = detail.RecoveryIntelligence is null ? null : new
        {
            similarVerifiedCaseCount = detail.RecoveryIntelligence.SimilarVerifiedCaseCount,
            evidenceStrength = detail.RecoveryIntelligence.EvidenceStrength,
            expectedRecoveryMinutesMedian = detail.RecoveryIntelligence.ExpectedRecoveryMinutesMedian,
            knownFailureModes = detail.RecoveryIntelligence.KnownFailureModes,
            similarCases = detail.RecoveryIntelligence.SimilarCases.Select(MapSimilarCase).ToList(),
            strategyPerformance = detail.RecoveryIntelligence.StrategyPerformance.Select(MapStrategyPerformance).ToList(),
            planEvidence = detail.RecoveryIntelligence.PlanEvidence.Select(plan => new
            {
                planKey = plan.PlanKey,
                similarVerifiedCaseCount = plan.SimilarVerifiedCaseCount,
                verifiedSuccesses = plan.VerifiedSuccesses,
                medianRecoveryMinutes = plan.MedianRecoveryMinutes,
                sampleConfidence = plan.SampleConfidence,
                knownRisks = plan.KnownRisks,
                knownFailureModes = plan.KnownFailureModes,
            }).ToList(),
        },
        concurrencyToken = detail.ConcurrencyToken,
    };

    internal static object MapHistoricalLearningCase(FactoryRecoveryHistoricalCaseDetail detail) => new
    {
        learningRecordId = detail.LearningRecordId,
        sourceCaseId = detail.SourceCaseId,
        outcomeType = detail.OutcomeType,
        eventCategory = detail.EventCategory,
        orderReference = detail.OrderReference,
        machineCode = detail.MachineCode,
        materialCode = detail.MaterialCode,
        moldCode = detail.MoldCode,
        severity = detail.Severity,
        problemSummary = detail.ProblemSummary,
        causeSummary = detail.CauseSummary,
        appliedStrategy = detail.AppliedStrategy,
        alternativeStrategy = detail.AlternativeStrategy,
        participatingWorkers = detail.ParticipatingWorkers,
        actionsExecuted = detail.ActionsExecuted,
        blockers = detail.Blockers,
        failureModes = detail.FailureModes,
        deliveryProtected = detail.DeliveryProtected,
        recoveredQuantity = detail.RecoveredQuantity,
        recoveryMinutes = detail.RecoveryMinutes,
        timeToFirstActionMinutes = detail.TimeToFirstActionMinutes,
        evidenceStrength = detail.EvidenceStrength,
        learningDisposition = detail.LearningDisposition,
        verifiedAtUtc = detail.VerifiedAtUtc,
    };

    private static object MapSimilarCase(FactoryRecoverySimilarCaseMatch match) => new
    {
        learningRecordId = match.LearningRecordId,
        sourceCaseId = match.SourceCaseId,
        similarityPercent = match.SimilarityPercent,
        similarityReasons = match.SimilarityReasons,
        orderReference = match.OrderReference,
        machineCode = match.MachineCode,
        problemSummary = match.ProblemSummary,
        appliedStrategy = match.AppliedStrategy,
        outcomeSummary = match.OutcomeSummary,
        recoveryMinutes = match.RecoveryMinutes,
        deliveryProtected = match.DeliveryProtected,
        verifiedAtUtc = match.VerifiedAtUtc,
    };

    private static object MapStrategyPerformance(FactoryRecoveryStrategyPerformance performance) => new
    {
        strategyKey = performance.StrategyKey,
        verifiedUses = performance.VerifiedUses,
        verifiedSuccesses = performance.VerifiedSuccesses,
        medianRecoveryMinutes = performance.MedianRecoveryMinutes,
        deliveryProtectedCount = performance.DeliveryProtectedCount,
        deliveryRelevantCases = performance.DeliveryRelevantCases,
        sampleConfidence = performance.SampleConfidence,
        commonBlockers = performance.CommonBlockers,
        failureModeCount = performance.FailureModeCount,
    };

    private static object MapQueueItem(FactoryDecisionCaseQueueItem item) => new
    {
        caseId = item.CaseId,
        title = item.Title,
        severity = item.Severity,
        status = item.Status,
        orderReference = item.OrderReference,
        customerName = item.CustomerName,
        deliveryCommitmentLabel = item.DeliveryCommitmentLabel,
        riskSummary = item.RiskSummary,
        requiredQuantity = item.RequiredQuantity,
        projectedQuantity = item.ProjectedQuantity,
        gapQuantity = item.GapQuantity,
        recommendedStrategy = item.RecommendedStrategy,
        digitalWorkerNames = item.DigitalWorkerNames,
        decisionRequired = item.DecisionRequired,
        currentBlockerCode = item.CurrentBlockerCode,
        verificationState = item.VerificationState,
        updatedAtUtc = item.UpdatedAtUtc,
        concurrencyToken = item.ConcurrencyToken,
    };

    private static Microsoft.AspNetCore.Http.HttpResults.ProblemHttpResult MapFacilityAccessFailure(FacilityAccessResolutionResult accessResult) =>
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
