using NevaxisFactOS.Api.Facilities;
using NevaxisFactOS.Api.PresentationMode;
using NevaxisFactOS.Persistence.Authorization;
using NevaxisFactOS.Persistence.GoldenDemoP1;
using NevaxisFactOS.Persistence.GoldenDemoP2B;

namespace NevaxisFactOS.Api.GoldenDemoP2B;

internal static class GoldenDemoP2BEndpointHelpers
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

        var presentationFailure = await PresentationModeEndpointHelpers.ResolvePresentationAccessFailureAsync(
            facilityId, facilityAccessResolver, cancellationToken);
        if (presentationFailure is not null)
        {
            return (null, presentationFailure);
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

    internal static object MapSnapshot(DigitalFactoryTeamSnapshot snapshot) => new
    {
        productionEventId = snapshot.ProductionEventId,
        machineCode = snapshot.MachineCode,
        agents = snapshot.Agents.Select(MapAgent),
        orchestrator = MapOrchestrator(snapshot.Orchestrator),
        activityTimeline = snapshot.ActivityTimeline.Select(MapActivity),
        approvalSheet = MapApprovalSheet(snapshot.ApprovalSheet),
        planActions = snapshot.PlanActions.Select(MapPlanAction),
        interventionTimeline = snapshot.InterventionTimeline.Select(MapIntervention),
    };

    internal static IResult MapP1Exception(GoldenDemoP1Exception ex) =>
        ex.FailureCode switch
        {
            GoldenDemoP1FailureCodes.EventNotFound => TypedResults.NotFound(new { code = ex.FailureCode, message = ex.Message }),
            _ => TypedResults.UnprocessableEntity(new { code = ex.FailureCode, message = ex.Message }),
        };

    private static object MapAgent(AgentEvidenceDetail agent) => new
    {
        agent.FindingId,
        agent.AgentCode,
        agent.WorkerCode,
        agent.AgentLabel,
        agent.Summary,
        agent.EvidenceClassification,
        generatedAtUtc = agent.GeneratedAtUtc,
        dataInputsReviewed = agent.DataInputsReviewed,
        calculationInputs = agent.CalculationInputs,
        calculationResults = agent.CalculationResults,
        agent.Conclusion,
        agent.Confidence,
        insufficientEvidenceState = agent.InsufficientEvidenceState,
        productionEventId = agent.ProductionEventId,
        machineAssetId = agent.MachineAssetId,
        machineCode = agent.MachineCode,
        resultingAction = agent.ResultingAction,
        planReferenceId = agent.PlanReferenceId,
        workReferenceId = agent.WorkReferenceId,
    };

    private static object MapOrchestrator(OrchestratorAssessment o) => new
    {
        o.WhatIsHappening,
        o.WhySummary,
        o.CurrentExpectedShiftGoodParts,
        o.RecoveredExpectedShiftGoodParts,
        o.ProductionRecoveryGoodParts,
        o.WhatShouldHappenNext,
        o.HumanApprovalRequired,
        o.ApprovingRole,
        o.RequiredControlReason,
        o.ExpectedOperationalOutcome,
        inputsUsed = o.InputsUsed,
        insufficientSpecialistEvidence = o.InsufficientSpecialistEvidence,
    };

    private static object MapActivity(GoldenDemoActivityItem item) => new
    {
        timestampUtc = item.TimestampUtc,
        item.Actor,
        item.Action,
        item.Status,
        sourceReference = item.SourceReference,
        evidenceType = item.EvidenceType,
        resultSummary = item.ResultSummary,
    };

    private static object MapApprovalSheet(ApprovalSheetModel sheet) => new
    {
        isApprovalRequired = sheet.IsApprovalRequired,
        isApproved = sheet.IsApproved,
        recommendedIntervention = sheet.RecommendedIntervention,
        whyRecommended = sheet.WhyRecommended,
        expectedInterruptionMinutes = sheet.ExpectedInterruptionMinutes,
        currentProductionExposureGoodParts = sheet.CurrentProductionExposureGoodParts,
        currentExpectedShiftGoodParts = sheet.CurrentExpectedShiftGoodParts,
        recoveredExpectedShiftGoodParts = sheet.RecoveredExpectedShiftGoodParts,
        recoveryOpportunityGoodParts = sheet.RecoveryOpportunityGoodParts,
        operationalRisk = sheet.OperationalRisk,
        approvingRole = sheet.ApprovingRole,
        evidenceReferences = sheet.EvidenceReferences,
        requestedAtUtc = sheet.RequestedAtUtc,
        requestedByActor = sheet.RequestedByActor,
        approvedBySubjectId = sheet.ApprovedBySubjectId,
        approvedAtUtc = sheet.ApprovedAtUtc,
        approvalStatus = sheet.ApprovalStatus,
        auditReference = sheet.AuditReference,
    };

    private static object MapPlanAction(PlanActionTraceability action) => new
    {
        actionId = action.ActionId,
        planId = action.PlanId,
        action.SortOrder,
        action.Title,
        action.ResponsibleRole,
        action.Status,
        action.EvidenceRequired,
        action.ValidationCriteria,
        sourceAgentCode = action.SourceAgentCode,
        sourceAgentLabel = action.SourceAgentLabel,
        relatedTechnicalFinding = action.RelatedTechnicalFinding,
        completedAtUtc = action.CompletedAtUtc,
        workReferenceId = action.WorkReferenceId,
    };

    private static object MapIntervention(InterventionTimelineItem item) => new
    {
        stageCode = item.StageCode,
        stageLabel = item.StageLabel,
        item.Status,
        occurredAtUtc = item.OccurredAtUtc,
        item.Actor,
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
