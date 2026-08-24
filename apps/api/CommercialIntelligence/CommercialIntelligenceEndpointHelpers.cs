using CrevaFactOS.Api.Facilities;
using CrevaFactOS.Persistence.Audit;
using CrevaFactOS.Persistence.CommercialIntelligence;
using CrevaFactOS.Persistence.Tenancy;

namespace CrevaFactOS.Api.CommercialIntelligence;

internal static class CommercialIntelligenceEndpointHelpers
{
    internal static async Task<(CommercialIntelligenceOperationContext? Context, IResult? Failure)> ResolveReadAccessAsync(
        Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        if (facilityId is null || facilityId == Guid.Empty)
        {
            return (null, MapAccessDenied(
                FacilityAccessFailureCodes.FacilityMissing,
                "A facility identifier is required."));
        }

        var accessResult = await facilityAccessResolver.ResolveAsync(
            new FacilityAccessRequest(facilityId.Value, CommercialIntelligencePermissionKeys.Read),
            cancellationToken);

        if (!accessResult.Succeeded || accessResult.Access is null)
        {
            return (null, MapFacilityAccessFailure(accessResult));
        }

        return (BuildContext(accessResult.Access, correlationContext), null);
    }

    internal static async Task<(CommercialIntelligenceOperationContext? Context, IResult? Failure)> ResolveManageAccessAsync(
        Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        if (facilityId is null || facilityId == Guid.Empty)
        {
            return (null, MapAccessDenied(
                FacilityAccessFailureCodes.FacilityMissing,
                "A facility identifier is required."));
        }

        var accessResult = await facilityAccessResolver.ResolveAsync(
            new FacilityAccessRequest(facilityId.Value, CommercialIntelligencePermissionKeys.Manage),
            cancellationToken);

        if (!accessResult.Succeeded || accessResult.Access is null)
        {
            return (null, MapFacilityAccessFailure(accessResult));
        }

        return (BuildContext(accessResult.Access, correlationContext), null);
    }

    internal static async Task<bool> HasManageAccessAsync(
        Guid facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        CancellationToken cancellationToken)
    {
        var accessResult = await facilityAccessResolver.ResolveAsync(
            new FacilityAccessRequest(facilityId, CommercialIntelligencePermissionKeys.Manage),
            cancellationToken);

        return accessResult.Succeeded && accessResult.Access is not null;
    }

    internal static CommercialIntelligenceOperationContext BuildContext(
        FacilityAccessContext access,
        CorrelationContext correlationContext) =>
        new(
            access.TenantId,
            access.FacilityId,
            access.MembershipId,
            access.IdentitySubjectId,
            access.PermissionKey,
            correlationContext.IsAvailable ? correlationContext.CorrelationId : null);

    internal static IResult MapWriteResult(CommercialIntelligenceWriteResult result, bool isCreate = false)
    {
        if (!result.Succeeded)
        {
            return MapFailure(result.FailureCode!, result.FailureReason);
        }

        if (result.Assessment is not null)
        {
            var assessmentDto = MapAssessmentDetail(result.Assessment);
            if (result.IdempotentReplay)
            {
                return TypedResults.Ok(new
                {
                    id = result.EntityId,
                    idempotentReplay = true,
                    assessment = assessmentDto,
                });
            }

            return isCreate
                ? TypedResults.Created((string?)null, new
                {
                    id = result.EntityId,
                    idempotentReplay = false,
                    assessment = assessmentDto,
                })
                : TypedResults.Ok(new
                {
                    id = result.EntityId,
                    version = result.Version,
                    idempotentReplay = false,
                    assessment = assessmentDto,
                });
        }

        var dto = new CommercialIntelligenceWriteSuccessDto(result.EntityId!.Value, result.Version, result.IdempotentReplay);
        if (result.IdempotentReplay)
        {
            return TypedResults.Ok(dto);
        }

        return isCreate ? TypedResults.Created((string?)null, dto) : TypedResults.Ok(dto);
    }

    internal static object MapAssessmentDetail(CommercialPurchasePriceAssessmentDetailReadModel assessment) => new
    {
        id = assessment.Id,
        assessmentReference = assessment.AssessmentReference,
        profileId = assessment.ProfileId,
        profileCode = assessment.ProfileCode,
        materialId = assessment.MaterialId,
        supplierReference = assessment.SupplierReference,
        comparisonDimension = assessment.ComparisonDimension,
        currentUnitPrice = assessment.CurrentUnitPrice,
        referenceMedianUnitPrice = assessment.ReferenceMedianUnitPrice,
        absoluteVariance = assessment.AbsoluteVariance,
        varianceRatio = assessment.VarianceRatio,
        comparableObservationCount = assessment.ComparableObservationCount,
        excludedObservationCount = assessment.ExcludedObservationCount,
        currencyCode = assessment.CurrencyCode,
        normalizedUnit = assessment.NormalizedUnit,
        windowStartUtc = assessment.WindowStartUtc,
        windowEndUtc = assessment.WindowEndUtc,
        priceBasisTaxInclusion = assessment.PriceBasisTaxInclusion,
        priceBasisDiscountInclusion = assessment.PriceBasisDiscountInclusion,
        priceBasisFreightInclusion = assessment.PriceBasisFreightInclusion,
        evidenceStatus = assessment.EvidenceStatus,
        assessmentOutcome = assessment.AssessmentOutcome,
        leadingDriverCode = assessment.LeadingDriverCode,
        ruleSetVersion = assessment.RuleSetVersion,
        sourceProvenance = assessment.SourceProvenance,
        evaluatedAtUtc = assessment.EvaluatedAtUtc,
        createdAtUtc = assessment.CreatedAtUtc,
        drivers = assessment.Drivers.Select(d => new
        {
            id = d.Id,
            reasonCode = d.ReasonCode,
            severity = d.Severity,
            actualValue = d.ActualValue,
            thresholdValue = d.ThresholdValue,
            unit = d.Unit,
            normalizedVariance = d.NormalizedVariance,
            affectedObservationCount = d.AffectedObservationCount,
            supportingText = d.SupportingText,
            sortOrder = d.SortOrder,
            isLeadingDriver = d.IsLeadingDriver,
        }),
        isPilotData = assessment.IsPilotData,
    };

    internal static IResult MapFailure(string? failureCode, string? reason) =>
        failureCode switch
        {
            CommercialIntelligenceFailureCodes.NotFound => TypedResults.Problem(
                title: reason ?? "Record not found.",
                statusCode: StatusCodes.Status404NotFound,
                extensions: FailureExt(failureCode)),
            CommercialIntelligenceFailureCodes.ConcurrencyConflict
                or CommercialIntelligenceFailureCodes.IdempotencyConflict
                or CommercialIntelligenceFailureCodes.IdempotencyLegacyUnverifiable => TypedResults.Problem(
                title: reason ?? "Request conflict.",
                statusCode: StatusCodes.Status409Conflict,
                extensions: FailureExt(failureCode)),
            CommercialIntelligenceFailureCodes.ValidationFailed => TypedResults.Problem(
                title: reason ?? "Validation failed.",
                statusCode: StatusCodes.Status400BadRequest,
                extensions: FailureExt(failureCode)),
            _ => TypedResults.Problem(
                title: reason ?? "Request failed.",
                statusCode: StatusCodes.Status400BadRequest,
                extensions: FailureExt(failureCode!)),
        };

    private static Microsoft.AspNetCore.Http.HttpResults.ProblemHttpResult MapAccessDenied(string code, string reason) =>
        TypedResults.Problem(
            title: reason,
            statusCode: StatusCodes.Status403Forbidden,
            extensions: FailureExt(code));

    private static Microsoft.AspNetCore.Http.HttpResults.ProblemHttpResult MapFacilityAccessFailure(
        FacilityAccessResolutionResult accessResult) =>
        accessResult.FailureCode switch
        {
            FacilityAccessFailureCodes.Unauthenticated => TypedResults.Problem(
                title: "Authentication is required.",
                statusCode: StatusCodes.Status401Unauthorized,
                extensions: FailureExt("commercial-intelligence.unauthenticated")),
            FacilityAccessFailureCodes.PermissionDenied => TypedResults.Problem(
                title: "You do not have permission for this operation.",
                statusCode: StatusCodes.Status403Forbidden,
                extensions: FailureExt("commercial-intelligence.permission-denied")),
            FacilityAccessFailureCodes.FacilityAccessDenied => TypedResults.Problem(
                title: "You do not have access to this facility.",
                statusCode: StatusCodes.Status403Forbidden,
                extensions: FailureExt("commercial-intelligence.facility-access-denied")),
            FacilityAccessFailureCodes.FacilityMissing => TypedResults.Problem(
                title: "A facility identifier is required.",
                statusCode: StatusCodes.Status400BadRequest,
                extensions: FailureExt("commercial-intelligence.invalid-scope")),
            _ => TypedResults.Problem(
                title: "Access denied.",
                statusCode: StatusCodes.Status403Forbidden,
                extensions: FailureExt("commercial-intelligence.facility-access-denied")),
        };

    private static Dictionary<string, object?> FailureExt(string code) =>
        new() { ["failureCode"] = code };
}

internal sealed record CommercialIntelligenceWriteSuccessDto(Guid Id, uint? Version, bool IdempotentReplay);
