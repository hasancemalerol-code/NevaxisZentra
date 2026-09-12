using NevaxisFactOS.Api.Facilities;
using NevaxisFactOS.Persistence.Audit;
using NevaxisFactOS.Persistence.CommercialIntelligence;
using NevaxisFactOS.Persistence.Tenancy;

namespace NevaxisFactOS.Api.CommercialIntelligence;

internal static class CommercialIntelligenceEndpointHelpers
{
    private static readonly string[] UnavailableComparisonDimensions =
    [
        "delivery",
        "quality",
        "service",
        "credit-terms",
        "lead-time",
    ];

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

        if (result.Comparison is not null)
        {
            var comparisonDto = MapComparisonDetail(result.Comparison);
            if (result.IdempotentReplay)
            {
                return TypedResults.Ok(new
                {
                    id = result.EntityId,
                    idempotentReplay = true,
                    comparison = comparisonDto,
                });
            }

            return isCreate
                ? TypedResults.Created((string?)null, new
                {
                    id = result.EntityId,
                    idempotentReplay = false,
                    comparison = comparisonDto,
                })
                : TypedResults.Ok(new
                {
                    id = result.EntityId,
                    idempotentReplay = false,
                    comparison = comparisonDto,
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

    internal static object MapComparisonDetail(CommercialPurchasePriceComparisonDetailReadModel comparison) => new
    {
        id = comparison.Id,
        comparisonReference = comparison.ComparisonReference,
        profileId = comparison.ProfileId,
        profileCode = comparison.ProfileCode,
        profileVersion = comparison.ProfileVersion,
        comparisonType = comparison.ComparisonType,
        materialId = comparison.MaterialId,
        currentMedianUnitPrice = comparison.CurrentMedianUnitPrice,
        referenceMedianUnitPrice = comparison.ReferenceMedianUnitPrice,
        signedVarianceRatio = comparison.SignedVarianceRatio,
        supplierSpreadRatio = comparison.SupplierSpreadRatio,
        comparableObservationCount = comparison.ComparableObservationCount,
        excludedObservationCount = comparison.ExcludedObservationCount,
        currencyCode = comparison.CurrencyCode,
        normalizedUnit = comparison.NormalizedUnit,
        currentWindowStartUtc = comparison.CurrentWindowStartUtc,
        currentWindowEndUtc = comparison.CurrentWindowEndUtc,
        referenceWindowStartUtc = comparison.ReferenceWindowStartUtc,
        referenceWindowEndUtc = comparison.ReferenceWindowEndUtc,
        priceBasisTaxInclusion = comparison.PriceBasisTaxInclusion,
        priceBasisDiscountInclusion = comparison.PriceBasisDiscountInclusion,
        priceBasisFreightInclusion = comparison.PriceBasisFreightInclusion,
        evidenceStatus = comparison.EvidenceStatus,
        comparisonOutcome = comparison.ComparisonOutcome,
        leadingDriverCode = comparison.LeadingDriverCode,
        ruleSetVersion = comparison.RuleSetVersion,
        sourceProvenance = comparison.SourceProvenance,
        evaluatedAtUtc = comparison.EvaluatedAtUtc,
        createdAtUtc = comparison.CreatedAtUtc,
        supplierResults = comparison.SupplierResults.Select(r => new
        {
            id = r.Id,
            supplierReference = r.SupplierReference,
            comparableObservationCount = r.ComparableObservationCount,
            supplierMedianUnitPrice = r.SupplierMedianUnitPrice,
            lowestComparableMedianUnitPrice = r.LowestComparableMedianUnitPrice,
            relativePricePositionRatio = r.RelativePricePositionRatio,
            priceRank = r.PriceRank,
            isTied = r.IsTied,
            positionState = r.PositionState,
            evidenceStatus = r.EvidenceStatus,
        }),
        drivers = comparison.Drivers.Select(d => new
        {
            id = d.Id,
            comparisonResultId = d.ComparisonResultId,
            reasonCode = d.ReasonCode,
            severity = d.Severity,
            actualValue = d.ActualValue,
            referenceValue = d.ReferenceValue,
            thresholdValue = d.ThresholdValue,
            unit = d.Unit,
            normalizedVariance = d.NormalizedVariance,
            affectedObservationCount = d.AffectedObservationCount,
            supportingText = d.SupportingText,
            sortOrder = d.SortOrder,
            isLeadingDriver = d.IsLeadingDriver,
        }),
        unavailableDimensions = UnavailableComparisonDimensions,
        isPilotData = comparison.IsPilotData,
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
