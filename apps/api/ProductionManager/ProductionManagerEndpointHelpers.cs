using NevaxisFactOS.Api.Facilities;
using NevaxisFactOS.Persistence.ProductionManager;

namespace NevaxisFactOS.Api.ProductionManager;

internal static class ProductionManagerEndpointHelpers
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

    internal static IResult MapException(ProductionManagerException ex) =>
        ex.FailureCode switch
        {
            ProductionManagerFailureCodes.NotFound => TypedResults.NotFound(new { code = ex.FailureCode, message = ex.Message }),
            ProductionManagerFailureCodes.ConcurrencyConflict => TypedResults.Conflict(new { code = ex.FailureCode, message = ex.Message }),
            ProductionManagerFailureCodes.Forbidden or ProductionManagerFailureCodes.AutonomyDenied =>
                TypedResults.Forbid(),
            _ => TypedResults.UnprocessableEntity(new { code = ex.FailureCode, message = ex.Message }),
        };

    internal static object MapWorkspace(ProductionManagerWorkspaceSnapshot workspace) => new
    {
        executive = workspace.Executive,
        predictions = workspace.Predictions.Select(MapPrediction).ToList(),
        opportunities = workspace.Opportunities.Select(o => new
        {
            decisionType = o.DecisionType,
            title = o.Title,
            problemSummary = o.ProblemSummary,
            correlationKey = o.CorrelationKey,
            confidenceScore = o.ConfidenceScore,
            confidenceBand = o.ConfidenceBand,
        }).ToList(),
        pendingDecisions = workspace.PendingDecisions.Select(MapDecision).ToList(),
        recentDecisions = workspace.RecentDecisions.Select(MapDecision).ToList(),
        bottlenecks = workspace.Bottlenecks,
        policy = new
        {
            autonomyMode = workspace.Policy.AutonomyMode,
            lowConfidenceThreshold = workspace.Policy.LowConfidenceThreshold,
            staleDataMaxMinutes = workspace.Policy.StaleDataMaxMinutes,
        },
        stateCapturedAtUtc = workspace.State.CapturedAtUtc,
    };

    internal static object MapDecision(FactoryDecision decision) => new
    {
        id = decision.Id,
        factoryCaseId = decision.FactoryCaseId,
        decisionType = decision.DecisionType,
        title = decision.Title,
        problemSummary = decision.ProblemSummary,
        status = decision.Status,
        autonomyMode = decision.AutonomyMode,
        confidenceBand = decision.ConfidenceBand,
        confidenceScore = decision.ConfidenceScore,
        approvalState = decision.ApprovalState,
        selectedOptionId = decision.SelectedOptionId,
        effectivenessScore = decision.EffectivenessScore,
        executedAtUtc = decision.ExecutedAtUtc,
        verifiedAtUtc = decision.VerifiedAtUtc,
        concurrencyToken = decision.ConcurrencyToken,
        options = decision.Options.Select(o => new
        {
            id = o.Id,
            optionCode = o.OptionCode,
            title = o.Title,
            description = o.Description,
            score = o.Score,
            requiresApproval = o.RequiresApproval,
            isRejected = o.IsRejected,
            rejectionReason = o.RejectionReason,
            rank = o.Rank,
        }).ToList(),
    };

    internal static object MapPrediction(FactoryPrediction prediction) => new
    {
        id = prediction.Id,
        predictionType = prediction.PredictionType,
        title = prediction.Title,
        summary = prediction.Summary,
        severity = prediction.Severity,
        confidenceBand = prediction.ConfidenceBand,
        confidenceScore = prediction.ConfidenceScore,
        horizon = prediction.Horizon,
        dataClassification = prediction.DataClassification,
        correlationKey = prediction.CorrelationKey,
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
