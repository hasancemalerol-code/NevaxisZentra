using CrevaFactOS.Api.Facilities;
using CrevaFactOS.Persistence;
using CrevaFactOS.Persistence.Audit;
using CrevaFactOS.Persistence.CommercialIntelligence;
using CrevaFactOS.Persistence.Tenancy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace CrevaFactOS.Api.CommercialIntelligence;

internal static class CommercialQuotationRiskEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapCommercialQuotationRiskEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/commercial-intelligence/quotation-risks").RequireAuthorization();

        group.MapGet("/workspace", GetWorkspaceAsync);
        group.MapGet("/profiles", ListProfilesAsync);
        group.MapPost("/profiles", CreateProfileAsync);
        group.MapPut("/profiles/{profileId:guid}", UpdateProfileAsync);
        group.MapGet("/assessments", ListAssessmentsAsync);
        group.MapGet("/assessments/{assessmentId:guid}", GetAssessmentAsync);
        group.MapPost("/assessments/evaluate", EvaluateAssessmentAsync);

        return endpoints;
    }

    private static async Task<IResult> GetWorkspaceAsync(
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        ICommercialQuotationRiskRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await CommercialIntelligenceEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var canManage = await CommercialIntelligenceEndpointHelpers.HasManageAccessAsync(
            facilityId!.Value, facilityAccessResolver, cancellationToken);
        var workspace = await repository.GetWorkspaceAsync(context, canManage, cancellationToken);
        return TypedResults.Ok(new
        {
            ruleSetVersion = workspace.RuleSetVersion,
            provenanceDisclaimer = workspace.ProvenanceDisclaimer,
            defaultMinimumComparableObservations = workspace.DefaultMinimumComparableObservations,
            defaultMaximumNegativePriceVarianceRatio = workspace.DefaultMaximumNegativePriceVarianceRatio,
            defaultMaximumPositivePriceVarianceRatio = workspace.DefaultMaximumPositivePriceVarianceRatio,
            defaultMaximumPaymentTermDays = workspace.DefaultMaximumPaymentTermDays,
            defaultExpiryWarningDays = workspace.DefaultExpiryWarningDays,
            realQuotationMappingComplete = workspace.RealQuotationMappingComplete,
            capabilities = new { canRead = workspace.CanRead, canManage = workspace.CanManage },
        });
    }

    private static async Task<IResult> ListProfilesAsync(
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        ICommercialQuotationRiskRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await CommercialIntelligenceEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var profiles = await repository.ListProfilesAsync(context, cancellationToken);
        return TypedResults.Ok(new
        {
            items = profiles.Select(MapProfile),
            ruleSetVersion = CommercialIntelligenceConstants.QuotationRiskRuleSetVersion,
            provenanceDisclaimer = CommercialIntelligenceConstants.QuotationRiskDevelopmentPilotProvenance,
        });
    }

    private static async Task<IResult> CreateProfileAsync(
        [FromQuery] Guid? facilityId,
        [FromBody] CreateCommercialQuotationRiskProfileRequest body,
        IFacilityAccessResolver facilityAccessResolver,
        ICommercialQuotationRiskRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await CommercialIntelligenceEndpointHelpers.ResolveManageAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var result = await repository.CreateProfileAsync(
            context,
            new CreateCommercialQuotationRiskProfileCommand(
                body.ProfileCode,
                body.Name,
                body.ReferenceWindowDurationDays,
                body.MinimumComparableObservations,
                body.MaximumNegativePriceVarianceRatio,
                body.MaximumPositivePriceVarianceRatio,
                body.MaximumPaymentTermDays,
                body.ExpiryWarningDays,
                body.MaximumEvidenceAgeDays,
                body.AllowedEvidenceStatuses,
                body.AllowedSalesEvidenceKinds,
                body.RequiredTaxInclusion,
                body.RequiredDiscountInclusion,
                body.RequiredFreightInclusion,
                body.RequiredGrossNetBasis,
                body.SourceProvenance,
                body.IdempotencyKey),
            cancellationToken);

        return MapWriteResult(result, isCreate: true);
    }

    private static async Task<IResult> UpdateProfileAsync(
        Guid profileId,
        [FromQuery] Guid? facilityId,
        [FromBody] UpdateCommercialQuotationRiskProfileRequest body,
        IFacilityAccessResolver facilityAccessResolver,
        ICommercialQuotationRiskRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await CommercialIntelligenceEndpointHelpers.ResolveManageAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var result = await repository.UpdateProfileAsync(
            context,
            new UpdateCommercialQuotationRiskProfileCommand(
                profileId,
                body.ExpectedVersion,
                body.Name,
                body.Status,
                body.ReferenceWindowDurationDays,
                body.MinimumComparableObservations,
                body.MaximumNegativePriceVarianceRatio,
                body.MaximumPositivePriceVarianceRatio,
                body.MaximumPaymentTermDays,
                body.ExpiryWarningDays,
                body.MaximumEvidenceAgeDays,
                body.AllowedEvidenceStatuses,
                body.AllowedSalesEvidenceKinds,
                body.RequiredTaxInclusion,
                body.RequiredDiscountInclusion,
                body.RequiredFreightInclusion,
                body.RequiredGrossNetBasis,
                body.SourceProvenance),
            cancellationToken);

        return MapWriteResult(result);
    }

    private static async Task<IResult> ListAssessmentsAsync(
        [FromQuery] Guid? facilityId,
        [FromQuery] Guid? materialId,
        [FromQuery] string? outcome,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        IFacilityAccessResolver facilityAccessResolver = null!,
        ICommercialQuotationRiskRepository repository = null!,
        CorrelationContext correlationContext = null!,
        CancellationToken cancellationToken = default)
    {
        var (context, failResult) = await CommercialIntelligenceEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var assessments = await repository.ListAssessmentsAsync(
            context,
            new CommercialQuotationRiskAssessmentListFilters(materialId, outcome, page, pageSize),
            cancellationToken);

        return TypedResults.Ok(new
        {
            items = assessments.Select(MapAssessmentSummary),
            page,
            pageSize,
            ruleSetVersion = CommercialIntelligenceConstants.QuotationRiskRuleSetVersion,
        });
    }

    private static async Task<IResult> GetAssessmentAsync(
        Guid assessmentId,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        ICommercialQuotationRiskRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await CommercialIntelligenceEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var assessment = await repository.GetAssessmentByIdAsync(context, assessmentId, cancellationToken);
        return assessment is null
            ? CommercialIntelligenceEndpointHelpers.MapFailure(
                CommercialIntelligenceFailureCodes.NotFound,
                "Quotation risk assessment not found.")
            : TypedResults.Ok(MapAssessmentDetail(assessment));
    }

    private static async Task<IResult> EvaluateAssessmentAsync(
        [FromQuery] Guid? facilityId,
        [FromBody] EvaluateCommercialQuotationRiskAssessmentRequest body,
        IFacilityAccessResolver facilityAccessResolver,
        ICommercialQuotationRiskRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await CommercialIntelligenceEndpointHelpers.ResolveManageAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var result = await repository.EvaluateAssessmentAsync(
            context,
            new EvaluateCommercialQuotationRiskAssessmentCommand(
                body.ProfileId,
                body.ExpectedProfileVersion,
                body.EvidenceObservationId,
                body.ReferenceWindowStartUtc,
                body.ReferenceWindowEndUtc,
                body.SourceProvenance,
                body.IdempotencyKey),
            cancellationToken);

        return MapWriteResult(result, isCreate: true);
    }

    private static IResult MapWriteResult(CommercialQuotationRiskWriteResult result, bool isCreate = false)
    {
        if (!result.Succeeded)
        {
            return CommercialIntelligenceEndpointHelpers.MapFailure(result.FailureCode!, result.FailureReason);
        }

        if (result.Assessment is not null)
        {
            var assessmentDto = MapAssessmentDetail(result.Assessment);
            if (result.IdempotentReplay)
            {
                return TypedResults.Ok(new { id = result.EntityId, idempotentReplay = true, assessment = assessmentDto });
            }

            return isCreate
                ? TypedResults.Created((string?)null, new { id = result.EntityId, idempotentReplay = false, assessment = assessmentDto })
                : TypedResults.Ok(new { id = result.EntityId, version = result.Version, idempotentReplay = false, assessment = assessmentDto });
        }

        var dto = new CommercialIntelligenceWriteSuccessDto(result.EntityId!.Value, result.Version, result.IdempotentReplay);
        return result.IdempotentReplay
            ? TypedResults.Ok(dto)
            : isCreate ? TypedResults.Created((string?)null, dto) : TypedResults.Ok(dto);
    }

    private static object MapProfile(CommercialQuotationRiskProfileReadModel profile) => new
    {
        id = profile.Id,
        profileCode = profile.ProfileCode,
        name = profile.Name,
        status = profile.Status,
        referenceWindowDurationDays = profile.ReferenceWindowDurationDays,
        minimumComparableObservations = profile.MinimumComparableObservations,
        maximumNegativePriceVarianceRatio = profile.MaximumNegativePriceVarianceRatio,
        maximumPositivePriceVarianceRatio = profile.MaximumPositivePriceVarianceRatio,
        maximumPaymentTermDays = profile.MaximumPaymentTermDays,
        expiryWarningDays = profile.ExpiryWarningDays,
        maximumEvidenceAgeDays = profile.MaximumEvidenceAgeDays,
        allowedEvidenceStatuses = profile.AllowedEvidenceStatuses,
        allowedSalesEvidenceKinds = profile.AllowedSalesEvidenceKinds,
        requiredTaxInclusion = profile.RequiredTaxInclusion,
        requiredDiscountInclusion = profile.RequiredDiscountInclusion,
        requiredFreightInclusion = profile.RequiredFreightInclusion,
        requiredGrossNetBasis = profile.RequiredGrossNetBasis,
        ruleSetVersion = profile.RuleSetVersion,
        sourceProvenance = profile.SourceProvenance,
        version = profile.Version,
        isPilotData = profile.IsPilotData,
    };

    private static object MapAssessmentSummary(CommercialQuotationRiskAssessmentSummaryReadModel assessment) => new
    {
        id = assessment.Id,
        assessmentReference = assessment.AssessmentReference,
        profileId = assessment.ProfileId,
        profileCode = assessment.ProfileCode,
        evidenceObservationId = assessment.EvidenceObservationId,
        quotationReference = assessment.QuotationReference,
        customerReference = assessment.CustomerReference,
        materialId = assessment.MaterialId,
        outcome = assessment.Outcome,
        quotedComparableUnitPrice = assessment.QuotedComparableUnitPrice,
        referenceMedianUnitPrice = assessment.ReferenceMedianUnitPrice,
        signedPriceVarianceRatio = assessment.SignedPriceVarianceRatio,
        candidateObservationCount = assessment.CandidateObservationCount,
        comparableObservationCount = assessment.ComparableObservationCount,
        excludedObservationCount = assessment.ExcludedObservationCount,
        paymentTermDays = assessment.PaymentTermDays,
        remainingValidityDays = assessment.RemainingValidityDays,
        currencyCode = assessment.CurrencyCode,
        unitOfMeasure = assessment.UnitOfMeasure,
        referenceWindowStartUtc = assessment.ReferenceWindowStartUtc,
        referenceWindowEndUtc = assessment.ReferenceWindowEndUtc,
        leadingDriverCode = assessment.LeadingDriverCode,
        ruleSetVersion = assessment.RuleSetVersion,
        evaluatedAtUtc = assessment.EvaluatedAtUtc,
        createdAtUtc = assessment.CreatedAtUtc,
        isPilotData = assessment.IsPilotData,
    };

    private static object MapAssessmentDetail(CommercialQuotationRiskAssessmentDetailReadModel assessment) => new
    {
        id = assessment.Id,
        assessmentReference = assessment.AssessmentReference,
        profileId = assessment.ProfileId,
        profileCode = assessment.ProfileCode,
        profileVersion = assessment.ProfileVersion,
        evidenceObservationId = assessment.EvidenceObservationId,
        quotationReference = assessment.QuotationReference,
        customerReference = assessment.CustomerReference,
        materialId = assessment.MaterialId,
        referenceWindowStartUtc = assessment.ReferenceWindowStartUtc,
        referenceWindowEndUtc = assessment.ReferenceWindowEndUtc,
        referenceMedianUnitPrice = assessment.ReferenceMedianUnitPrice,
        quotedComparableUnitPrice = assessment.QuotedComparableUnitPrice,
        signedPriceVarianceRatio = assessment.SignedPriceVarianceRatio,
        candidateObservationCount = assessment.CandidateObservationCount,
        comparableObservationCount = assessment.ComparableObservationCount,
        excludedObservationCount = assessment.ExcludedObservationCount,
        paymentTermDays = assessment.PaymentTermDays,
        remainingValidityDays = assessment.RemainingValidityDays,
        currencyCode = assessment.CurrencyCode,
        unitOfMeasure = assessment.UnitOfMeasure,
        grossNetBasis = assessment.GrossNetBasis,
        taxBasis = assessment.TaxBasis,
        discountBasis = assessment.DiscountBasis,
        freightBasis = assessment.FreightBasis,
        evidenceStatus = assessment.EvidenceStatus,
        outcome = assessment.Outcome,
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
}

internal sealed record CreateCommercialQuotationRiskProfileRequest(
    string ProfileCode,
    string Name,
    int ReferenceWindowDurationDays,
    int MinimumComparableObservations,
    decimal MaximumNegativePriceVarianceRatio,
    decimal MaximumPositivePriceVarianceRatio,
    int MaximumPaymentTermDays,
    int ExpiryWarningDays,
    int MaximumEvidenceAgeDays,
    string AllowedEvidenceStatuses,
    string AllowedSalesEvidenceKinds,
    string RequiredTaxInclusion,
    string RequiredDiscountInclusion,
    string RequiredFreightInclusion,
    string RequiredGrossNetBasis,
    string SourceProvenance,
    string IdempotencyKey);

internal sealed record UpdateCommercialQuotationRiskProfileRequest(
    uint ExpectedVersion,
    string Name,
    string Status,
    int ReferenceWindowDurationDays,
    int MinimumComparableObservations,
    decimal MaximumNegativePriceVarianceRatio,
    decimal MaximumPositivePriceVarianceRatio,
    int MaximumPaymentTermDays,
    int ExpiryWarningDays,
    int MaximumEvidenceAgeDays,
    string AllowedEvidenceStatuses,
    string AllowedSalesEvidenceKinds,
    string RequiredTaxInclusion,
    string RequiredDiscountInclusion,
    string RequiredFreightInclusion,
    string RequiredGrossNetBasis,
    string SourceProvenance);

internal sealed record EvaluateCommercialQuotationRiskAssessmentRequest(
    Guid ProfileId,
    uint ExpectedProfileVersion,
    Guid EvidenceObservationId,
    DateTimeOffset ReferenceWindowStartUtc,
    DateTimeOffset ReferenceWindowEndUtc,
    string SourceProvenance,
    string IdempotencyKey);

public static class CommercialQuotationRiskServiceCollectionExtensions
{
    public static IServiceCollection AddCrevaFactOSCommercialQuotationRisk(this IServiceCollection services)
    {
        services.AddScoped<ICommercialQuotationRiskRepository>(serviceProvider =>
        {
            var dbContext = serviceProvider.GetService<CrevaFactOSDbContext>();
            return dbContext is null
                ? new UnavailableCommercialQuotationRiskRepository()
                : new CommercialQuotationRiskRepository(
                    dbContext,
                    serviceProvider.GetRequiredService<TenantDatabaseSession>());
        });

        return services;
    }
}
