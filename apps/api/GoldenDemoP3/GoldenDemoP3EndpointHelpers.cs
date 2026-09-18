using NevaxisZentra.Api.Facilities;
using NevaxisZentra.Api.PresentationMode;
using NevaxisZentra.Persistence.Authorization;
using NevaxisZentra.Persistence.GoldenDemoP1;
using NevaxisZentra.Persistence.GoldenDemoP3;

namespace NevaxisZentra.Api.GoldenDemoP3;

internal static class GoldenDemoP3EndpointHelpers
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

    internal static object MapOutcome(OutcomeSnapshot snapshot) => new
    {
        productionEventId = snapshot.ProductionEventId,
        machineCode = snapshot.MachineCode,
        interventionStatus = snapshot.InterventionStatus,
        outcomeStatus = snapshot.OutcomeStatus,
        verificationConfidence = snapshot.VerificationConfidence,
        validationWindow = MapValidationWindow(snapshot.ValidationWindow),
        baselineWindow = MapWindow(snapshot.BaselineWindow),
        validationMeasurementWindow = snapshot.ValidationMeasurementWindow is null
            ? null
            : MapWindow(snapshot.ValidationMeasurementWindow),
        hero = MapHero(snapshot.Hero),
        metricComparisons = snapshot.MetricComparisons.Select(MapMetric),
        verificationChecks = snapshot.VerificationChecks.Select(MapCheck),
        insufficientEvidenceReasons = snapshot.InsufficientEvidenceReasons,
        energyImpact = MapEnergy(snapshot.EnergyImpact),
        businessImpact = MapBusiness(snapshot.BusinessImpact),
        traceability = MapTraceability(snapshot.Traceability),
    };

    internal static IResult MapP1Exception(GoldenDemoP1Exception ex) =>
        ex.FailureCode switch
        {
            GoldenDemoP1FailureCodes.EventNotFound => TypedResults.NotFound(new { code = ex.FailureCode, message = ex.Message }),
            _ => TypedResults.UnprocessableEntity(new { code = ex.FailureCode, message = ex.Message }),
        };

    private static object MapValidationWindow(ValidationWindowModel window) => new
    {
        window.Status,
        statusLabel = window.StatusLabel,
        validationStartedAtUtc = window.ValidationStartedAtUtc,
        samplesCollected = window.SamplesCollected,
        minimumSamplesRequired = window.MinimumSamplesRequired,
        evidenceCompleteness = window.EvidenceCompleteness,
        window.Summary,
        readyForVerification = window.ReadyForVerification,
    };

    private static object MapWindow(MeasurementWindowModel window) => new
    {
        windowKind = window.WindowKind,
        windowStartUtc = window.WindowStartUtc,
        windowEndUtc = window.WindowEndUtc,
        goodParts = window.GoodParts,
        rejectParts = window.RejectParts,
        sourceLabel = window.SourceLabel,
        productionContext = window.ProductionContext,
        cycleSeconds = window.CycleSeconds,
        goodPartsPerHour = window.GoodPartsPerHour,
        availabilityPercent = window.AvailabilityPercent,
        microStopCount = window.MicroStopCount,
        kwhPerGoodPart = window.KwhPerGoodPart,
    };

    private static object MapHero(OutcomeHeroModel hero) => new
    {
        hero.Headline,
        hero.Subheadline,
        recoveredGoodPartsPerShift = hero.RecoveredGoodPartsPerShift,
        recoveredGoodPartsClassification = hero.RecoveredGoodPartsClassification,
        verificationResult = hero.VerificationResult,
        showSuccessHero = hero.ShowSuccessHero,
        highlights = hero.Highlights.Select(h => new
        {
            h.Label,
            beforeValue = h.BeforeValue,
            afterValue = h.AfterValue,
            deltaLabel = h.DeltaLabel,
            h.Status,
        }),
    };

    private static object MapMetric(MetricComparisonModel metric) => new
    {
        metricCode = metric.MetricCode,
        metric.Label,
        beforeValue = metric.BeforeValue,
        afterValue = metric.AfterValue,
        deltaLabel = metric.DeltaLabel,
        metric.Status,
        classification = metric.Classification,
    };

    private static object MapCheck(VerificationCheckModel check) => new
    {
        check.Code,
        check.Label,
        check.Passed,
        measuredValue = check.MeasuredValue,
        referenceValue = check.ReferenceValue,
        check.Status,
        check.Explanation,
        classification = check.Classification,
    };

    private static object MapEnergy(EnergyImpactModel energy) => new
    {
        hasData = energy.HasData,
        classification = energy.Classification,
        beforeKwhPerGoodPart = energy.BeforeKwhPerGoodPart,
        afterKwhPerGoodPart = energy.AfterKwhPerGoodPart,
        changePercent = energy.ChangePercent,
        energy.Summary,
        relationshipNote = energy.RelationshipNote,
    };

    private static object MapBusiness(BusinessImpactClosureModel business) => new
    {
        operationalGoodPartsPerShift = business.OperationalGoodPartsPerShift,
        operationalClassification = business.OperationalClassification,
        capacityImpactPercent = business.CapacityImpactPercent,
        estimatedBusinessImpactTry = business.EstimatedBusinessImpactTry,
        estimatedBusinessClassification = business.EstimatedBusinessClassification,
        modeledVerifiedBusinessImpactTry = business.ModeledVerifiedBusinessImpactTry,
        modeledVerifiedBusinessClassification = business.ModeledVerifiedBusinessClassification,
        categories = business.Categories.Select(c => new
        {
            categoryCode = c.CategoryCode,
            c.Label,
            beforeLabel = c.BeforeLabel,
            afterLabel = c.AfterLabel,
            protectedOrRecoveredLabel = c.ProtectedOrRecoveredLabel,
            classification = c.Classification,
        }),
    };

    private static object MapTraceability(OutcomeTraceabilityModel traceability) => new
    {
        planReferenceId = traceability.PlanReferenceId,
        verificationReferenceId = traceability.VerificationReferenceId,
        interventionReferenceId = traceability.InterventionReferenceId,
        sourceAgentReferences = traceability.SourceAgentReferences,
        workflowSummary = traceability.WorkflowSummary,
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
