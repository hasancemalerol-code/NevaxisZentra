using NevaxisFactOS.Api.Facilities;
using NevaxisFactOS.Persistence.FactoryDecisionCenter;

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
        generatedAtUtc = workspace.GeneratedAtUtc,
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
        concurrencyToken = detail.ConcurrencyToken,
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
