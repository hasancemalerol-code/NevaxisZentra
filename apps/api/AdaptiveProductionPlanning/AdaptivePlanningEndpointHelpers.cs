using Microsoft.AspNetCore.Mvc;
using NevaxisZentra.Api.Facilities;
using NevaxisZentra.Persistence.AdaptiveProductionPlanning;
using NevaxisZentra.Persistence.Authorization;

namespace NevaxisZentra.Api.AdaptiveProductionPlanning;

internal static class AdaptivePlanningEndpointHelpers
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

    internal static IResult MapException(AdaptivePlanningException ex) =>
        ex.FailureCode switch
        {
            AdaptivePlanningFailureCodes.NotFound => TypedResults.NotFound(new { code = ex.FailureCode, message = ex.Message }),
            AdaptivePlanningFailureCodes.ConcurrencyConflict => TypedResults.Conflict(new { code = ex.FailureCode, message = ex.Message }),
            AdaptivePlanningFailureCodes.DuplicatePublish => TypedResults.Conflict(new { code = ex.FailureCode, message = ex.Message }),
            AdaptivePlanningFailureCodes.Forbidden => TypedResults.Forbid(),
            AdaptivePlanningFailureCodes.FrozenWindow => TypedResults.UnprocessableEntity(new { code = ex.FailureCode, message = ex.Message }),
            AdaptivePlanningFailureCodes.ApprovalRequired => TypedResults.UnprocessableEntity(new { code = ex.FailureCode, message = ex.Message }),
            _ => TypedResults.UnprocessableEntity(new { code = ex.FailureCode, message = ex.Message }),
        };

    internal static object MapWorkspace(AdaptivePlanningWorkspaceSnapshot workspace) => new
    {
        activePlan = workspace.ActivePlan is null ? null : new
        {
            id = workspace.ActivePlan.Id,
            referenceCode = workspace.ActivePlan.ReferenceCode,
            title = workspace.ActivePlan.Title,
            adaptiveLifecycleStatus = workspace.ActivePlan.AdaptiveLifecycleStatus,
            activePlanVersionId = workspace.ActivePlan.ActivePlanVersionId,
        },
        activeVersion = workspace.ActiveVersion is null ? null : MapVersion(workspace.ActiveVersion),
        operations = workspace.Operations.Select(MapOperation).ToList(),
        scenarios = workspace.Scenarios.Select(MapScenario).ToList(),
        deviations = workspace.Deviations.Select(MapDeviation).ToList(),
        recentChanges = workspace.RecentChanges.Select(MapChange).ToList(),
        activityFeed = workspace.ActivityFeed.Select(a => new
        {
            eventType = a.EventType,
            summary = a.Summary,
            occurredAtUtc = a.OccurredAtUtc,
        }),
        policy = MapPolicy(workspace.Policy),
        metrics = workspace.Metrics,
        risks = workspace.Risks,
        dataFreshnessUtc = workspace.DataFreshnessUtc,
    };

    internal static object MapVersion(ProductionPlanVersion version) => new
    {
        id = version.Id,
        versionNumber = version.VersionNumber,
        planningHorizon = version.PlanningHorizon,
        lifecycleStatus = version.LifecycleStatus,
        score = version.Score,
        confidenceScore = version.ConfidenceScore,
        publishedAtUtc = version.PublishedAtUtc,
        concurrencyToken = version.ConcurrencyToken,
    };

    internal static object MapOperation(ProductionPlanOperation op) => new
    {
        id = op.Id,
        orderId = op.OrderId,
        machineId = op.MachineId,
        lineId = op.LineId,
        moldId = op.MoldId,
        plannedStartUtc = op.PlannedStartUtc,
        plannedEndUtc = op.PlannedEndUtc,
        setupMinutes = op.SetupMinutes,
        materialReadinessState = op.MaterialReadinessState,
        sequenceRank = op.SequenceRank,
        status = op.Status,
    };

    internal static object MapScenario(PlanningScenario scenario) => new
    {
        id = scenario.Id,
        scenarioCode = scenario.ScenarioCode,
        title = scenario.Title,
        triggerType = scenario.TriggerType,
        score = scenario.Score,
        rank = scenario.Rank,
        comparisonJson = scenario.ComparisonJson,
        status = scenario.Status,
    };

    internal static object MapDeviation(PlanDeviation deviation) => new
    {
        id = deviation.Id,
        deviationType = deviation.DeviationType,
        severity = deviation.Severity,
        summary = deviation.Summary,
        detectedAtUtc = deviation.DetectedAtUtc,
    };

    internal static object MapChange(PlanChange change) => new
    {
        id = change.Id,
        changeType = change.ChangeType,
        triggerType = change.TriggerType,
        diffJson = change.DiffJson,
        createdAtUtc = change.CreatedAtUtc,
    };

    internal static object MapPolicy(AdaptivePlanningPolicy policy) => new
    {
        id = policy.Id,
        frozenWindowMinutes = policy.FrozenWindowMinutes,
        nervousnessCooldownMinutes = policy.NervousnessCooldownMinutes,
        minBenefitThreshold = policy.MinBenefitThreshold,
        autonomyMode = policy.AutonomyMode,
        policyEnvelopeJson = policy.PolicyEnvelopeJson,
        concurrencyToken = policy.ConcurrencyToken,
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
