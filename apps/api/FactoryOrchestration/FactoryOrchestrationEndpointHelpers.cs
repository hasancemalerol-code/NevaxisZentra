using NevaxisFactOS.Api.Facilities;
using NevaxisFactOS.Persistence.DigitalWorkforce;
using NevaxisFactOS.Persistence.FactoryOrchestration;

namespace NevaxisFactOS.Api.FactoryOrchestration;

internal static class FactoryOrchestrationEndpointHelpers
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

    internal static IResult MapException(FactoryOrchestrationException ex) =>
        ex.FailureCode switch
        {
            FactoryOrchestrationFailureCodes.NotFound => TypedResults.NotFound(new { code = ex.FailureCode, message = ex.Message }),
            FactoryOrchestrationFailureCodes.ConcurrencyConflict => TypedResults.Conflict(new { code = ex.FailureCode, message = ex.Message }),
            FactoryOrchestrationFailureCodes.Forbidden => TypedResults.Forbid(),
            FactoryOrchestrationFailureCodes.OutcomeNotVerified => TypedResults.UnprocessableEntity(new { code = ex.FailureCode, message = ex.Message }),
            _ => TypedResults.UnprocessableEntity(new { code = ex.FailureCode, message = ex.Message }),
        };

    internal static object MapCaseSummary(DigitalWorkCase caseRow) => new
    {
        id = caseRow.Id,
        title = caseRow.Title,
        severity = caseRow.Severity,
        status = caseRow.Status,
        primaryDomain = caseRow.PrimaryDomain,
        participatingDomains = caseRow.ParticipatingDomainsJson,
        currentBlockerCode = caseRow.CurrentBlockerCode,
        primaryWorkerId = caseRow.PrimaryWorkerId,
        primaryWorkerCode = caseRow.PrimaryWorker?.Code,
        primaryWorkerName = caseRow.PrimaryWorker?.Name,
        requiresApproval = caseRow.RequiresApproval,
        verificationState = caseRow.VerificationState,
        startedAtUtc = caseRow.StartedAtUtc,
        updatedAtUtc = caseRow.UpdatedAtUtc,
        targetAtUtc = caseRow.TargetAtUtc,
        businessImpact = caseRow.BusinessImpact,
        correlationKey = caseRow.CorrelationKey,
        reopenCount = caseRow.ReopenCount,
        administrativelyClosed = caseRow.AdministrativelyClosed,
        concurrencyToken = caseRow.ConcurrencyToken,
    };

    internal static object MapCaseDetail(FactoryCaseDetailSnapshot detail) => new
    {
        rootCase = MapCaseSummary(detail.RootCase),
        childCases = detail.ChildCases.Select(MapCaseSummary).ToList(),
        dependencies = detail.Dependencies.Select(d => new
        {
            fromNodeKey = d.FromNodeKey,
            toNodeKey = d.ToNodeKey,
            fromNodeType = d.FromNodeType,
            toNodeType = d.ToNodeType,
            edgeType = d.EdgeType,
            linkedCaseId = d.LinkedCaseId,
            linkedWorkerId = d.LinkedWorkerId,
        }).ToList(),
        blockers = detail.Blockers.Select(b => new
        {
            code = b.Code,
            message = b.Message,
            ownerType = b.OwnerType,
            ownerId = b.OwnerId,
            sinceUtc = b.SinceUtc,
            isStale = b.IsStale,
        }).ToList(),
        recoveryPlan = new
        {
            objective = detail.RecoveryPlan.Objective,
            actions = detail.RecoveryPlan.Actions,
            dependencies = detail.RecoveryPlan.Dependencies,
            verificationCriteria = detail.RecoveryPlan.VerificationCriteria,
            fallback = detail.RecoveryPlan.Fallback,
        },
        timeline = detail.Timeline.Select(a => new
        {
            id = a.Id,
            actionType = a.ActionType,
            description = a.Description,
            createdAtUtc = a.CreatedAtUtc,
        }).ToList(),
        nextAction = detail.NextAction,
        blockerExplanation = detail.BlockerExplanation,
        hasDeadlock = detail.HasDeadlock,
        hasConflict = detail.HasConflict,
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
