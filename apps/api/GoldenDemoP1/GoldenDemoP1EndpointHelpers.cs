using NevaxisFactOS.Api.Facilities;
using NevaxisFactOS.Persistence.Authorization;
using NevaxisFactOS.Persistence.GoldenDemoP1;
using Microsoft.AspNetCore.Mvc;

namespace NevaxisFactOS.Api.GoldenDemoP1;

internal static class GoldenDemoP1EndpointHelpers
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

    internal static IResult MapException(GoldenDemoP1Exception ex) =>
        ex.FailureCode switch
        {
            GoldenDemoP1FailureCodes.EventNotFound => TypedResults.NotFound(new { code = ex.FailureCode, message = ex.Message }),
            GoldenDemoP1FailureCodes.Unauthorized => TypedResults.Forbid(),
            _ => TypedResults.UnprocessableEntity(new { code = ex.FailureCode, message = ex.Message }),
        };

    internal static object MapWorkspace(GoldenDemoWorkspaceSnapshot snapshot) => new
    {
        @event = new
        {
            id = snapshot.Event.Id,
            machineAssetId = snapshot.Event.MachineAssetId,
            machineCode = snapshot.Event.MachineCode,
            title = snapshot.Event.Title,
            status = snapshot.Event.Status,
            moldReference = snapshot.Event.MoldReference,
            productReference = snapshot.Event.ProductReference,
            shiftTargetGoodParts = snapshot.Event.ShiftTargetGoodParts,
            shiftStartUtc = snapshot.Event.ShiftStartUtc,
            shiftEndUtc = snapshot.Event.ShiftEndUtc,
        },
        baseline = snapshot.Baseline is null
            ? null
            : new
            {
                referenceSource = snapshot.Baseline.ReferenceSource,
                referenceSourceLabel = snapshot.Baseline.ReferenceSourceLabel,
                totalCycleSeconds = snapshot.Baseline.TotalCycleSeconds,
                referenceKwhPerGoodPart = snapshot.Baseline.ReferenceKwhPerGoodPart,
            },
        cycleLoss = new
        {
            snapshot.CycleLoss.HasBaseline,
            snapshot.CycleLoss.BaselineFailureReason,
            snapshot.CycleLoss.ReferenceCycleSeconds,
            snapshot.CycleLoss.CurrentCycleSeconds,
            snapshot.CycleLoss.CycleLossSeconds,
            snapshot.CycleLoss.MicroStopEquivalentLossSeconds,
            phases = snapshot.CycleLoss.Phases.Select(p => new
            {
                p.PhaseCode,
                p.Label,
                p.ReferenceSeconds,
                p.CurrentSeconds,
                p.LossSeconds,
                p.IsLoss,
                p.IsImprovement,
            }),
            losses = snapshot.CycleLoss.Losses.Select(p => new { p.PhaseCode, p.Label, p.LossSeconds }),
            improvements = snapshot.CycleLoss.Improvements.Select(p => new { p.PhaseCode, p.Label, p.LossSeconds }),
        },
        contributors = snapshot.Contributors.Select(c => new
        {
            c.Rank,
            c.PhaseCode,
            c.Label,
            c.ContributorKind,
            c.MeasuredDeviationSeconds,
            c.ContributionToCycleLossSeconds,
            c.Confidence,
            c.SuggestedNextStep,
            c.EvidenceWindow,
        }),
        recovery = new
        {
            snapshot.Recovery.CurrentExpectedShiftGoodParts,
            snapshot.Recovery.RecoveredExpectedShiftGoodParts,
            snapshot.Recovery.ProductionRecoveryGoodParts,
            snapshot.Recovery.ProductionRecoveryPercent,
            snapshot.Recovery.RecoverableCycleSeconds,
            snapshot.Recovery.RemainingShiftMinutes,
            snapshot.Recovery.ConfidenceLevel,
            snapshot.Recovery.Assumptions,
            snapshot.Recovery.SafetyMessage,
        },
        whatIfScenarios = snapshot.WhatIfScenarios.Select(s => new
        {
            s.ScenarioCode,
            s.Title,
            s.ProjectedEndOfShiftGoodParts,
            s.ProductionGapGoodParts,
            s.ContinuedCycleLossSeconds,
            s.InterventionDurationMinutes,
            s.RecoveredProductionOpportunity,
            s.RiskExposure,
            s.IsRecommended,
            s.RecommendationReason,
        }),
        plan = snapshot.Plan is null
            ? null
            : new
            {
                id = snapshot.Plan.Id,
                status = snapshot.Plan.Status,
                recommendationSummary = snapshot.Plan.RecommendationSummary,
                recommendedScenarioCode = snapshot.Plan.RecommendedScenarioCode,
                requiresApproval = snapshot.Plan.RequiresApproval,
                approvedBySubjectId = snapshot.Plan.ApprovedBySubjectId,
                approvedAtUtc = snapshot.Plan.ApprovedAtUtc,
            },
        planActions = snapshot.PlanActions.Select(a => new
        {
            id = a.Id,
            a.SortOrder,
            a.ActionCode,
            a.Title,
            a.ResponsibleRole,
            a.Priority,
            a.Status,
            a.EvidenceRequired,
            a.EstimatedDowntimeMinutes,
            a.RequiresApproval,
            a.ValidationCriteria,
            a.CompletedAtUtc,
        }),
        agentFindings = snapshot.AgentFindings.Select(f => new
        {
            f.AgentCode,
            f.WorkerCode,
            f.Summary,
            f.EvidenceClassification,
            f.GeneratedAtUtc,
        }),
        verification = snapshot.Verification is null
            ? null
            : new
            {
                snapshot.Verification.Result,
                snapshot.Verification.Summary,
                snapshot.Verification.RecoveredGoodPartsPerShift,
                snapshot.Verification.VerifiedAtUtc,
            },
        beforeAfter = snapshot.BeforeAfter is null
            ? null
            : new
            {
                snapshot.BeforeAfter.BeforeCycleSeconds,
                snapshot.BeforeAfter.AfterCycleSeconds,
                snapshot.BeforeAfter.BeforeGoodPartsPerHour,
                snapshot.BeforeAfter.AfterGoodPartsPerHour,
                snapshot.BeforeAfter.BeforeMicroStopCount,
                snapshot.BeforeAfter.AfterMicroStopCount,
                snapshot.BeforeAfter.BeforeRejectRatePercent,
                snapshot.BeforeAfter.AfterRejectRatePercent,
                snapshot.BeforeAfter.BeforeKwhPerGoodPart,
                snapshot.BeforeAfter.AfterKwhPerGoodPart,
                snapshot.BeforeAfter.RecoveredGoodPartsPerShift,
                snapshot.BeforeAfter.OutcomeLabel,
            },
        metrics = snapshot.Metrics,
        businessImpact = new
        {
            snapshot.BusinessImpact.OperationalGoodPartsPerShift,
            snapshot.BusinessImpact.CapacityImpactPercent,
            snapshot.BusinessImpact.EnergyIntensityImpactPercent,
            snapshot.BusinessImpact.DowntimeMinutesAvoided,
            verifiedBusinessImpactTry = snapshot.BusinessImpact.VerifiedBusinessImpactTry,
            estimatedBusinessImpactTry = snapshot.BusinessImpact.EstimatedBusinessImpactTry,
        },
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
