using CrevaFactOS.Api.Facilities;
using CrevaFactOS.Persistence;
using CrevaFactOS.Persistence.Audit;
using CrevaFactOS.Persistence.CommercialIntelligence;
using CrevaFactOS.Persistence.Tenancy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace CrevaFactOS.Api.CommercialIntelligence;

internal static class CommercialSalesPriceEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapCommercialSalesPriceEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/commercial-intelligence/sales-prices").RequireAuthorization();

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
        ICommercialSalesPriceRepository repository,
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
            referenceCenter = workspace.ReferenceCenter,
            defaultMinimumComparableObservations = workspace.DefaultMinimumComparableObservations,
            realTransactionMappingComplete = workspace.RealTransactionMappingComplete,
            capabilities = new { canRead = workspace.CanRead, canManage = workspace.CanManage },
        });
    }

    private static async Task<IResult> ListProfilesAsync(
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        ICommercialSalesPriceRepository repository,
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
            ruleSetVersion = CommercialIntelligenceConstants.SalesRuleSetVersion,
            provenanceDisclaimer = CommercialIntelligenceConstants.SalesDevelopmentPilotProvenance,
        });
    }

    private static async Task<IResult> CreateProfileAsync(
        [FromQuery] Guid? facilityId,
        [FromBody] CreateCommercialSalesPriceProfileRequest body,
        IFacilityAccessResolver facilityAccessResolver,
        ICommercialSalesPriceRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await CommercialIntelligenceEndpointHelpers.ResolveManageAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var result = await repository.CreateProfileAsync(
            context,
            new CreateCommercialSalesPriceProfileCommand(
                body.ProfileCode,
                body.DisplayName,
                body.ComparisonDimension,
                body.ReferenceWindowDurationDays,
                body.MinimumComparableObservations,
                body.MaximumPositiveVarianceRatio,
                body.AllowedEvidenceStatuses,
                body.AllowedSourceEvidenceKinds,
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
        [FromBody] UpdateCommercialSalesPriceProfileRequest body,
        IFacilityAccessResolver facilityAccessResolver,
        ICommercialSalesPriceRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await CommercialIntelligenceEndpointHelpers.ResolveManageAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var result = await repository.UpdateProfileAsync(
            context,
            new UpdateCommercialSalesPriceProfileCommand(
                profileId,
                body.ExpectedVersion,
                body.DisplayName,
                body.Status,
                body.ComparisonDimension,
                body.ReferenceWindowDurationDays,
                body.MinimumComparableObservations,
                body.MaximumPositiveVarianceRatio,
                body.AllowedEvidenceStatuses,
                body.AllowedSourceEvidenceKinds,
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
        [FromQuery] string? assessmentOutcome,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        IFacilityAccessResolver facilityAccessResolver = null!,
        ICommercialSalesPriceRepository repository = null!,
        CorrelationContext correlationContext = null!,
        CancellationToken cancellationToken = default)
    {
        var (context, failResult) = await CommercialIntelligenceEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var assessments = await repository.ListAssessmentsAsync(
            context,
            new CommercialSalesPriceAssessmentListFilters(materialId, assessmentOutcome, page, pageSize),
            cancellationToken);

        return TypedResults.Ok(new
        {
            items = assessments.Select(MapAssessmentSummary),
            page,
            pageSize,
            ruleSetVersion = CommercialIntelligenceConstants.SalesRuleSetVersion,
        });
    }

    private static async Task<IResult> GetAssessmentAsync(
        Guid assessmentId,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        ICommercialSalesPriceRepository repository,
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
                "Sales price assessment not found.")
            : TypedResults.Ok(MapAssessmentDetail(assessment));
    }

    private static async Task<IResult> EvaluateAssessmentAsync(
        [FromQuery] Guid? facilityId,
        [FromBody] EvaluateCommercialSalesPriceAssessmentRequest body,
        IFacilityAccessResolver facilityAccessResolver,
        ICommercialSalesPriceRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await CommercialIntelligenceEndpointHelpers.ResolveManageAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var result = await repository.EvaluateAssessmentAsync(
            context,
            new EvaluateCommercialSalesPriceAssessmentCommand(
                body.ProfileId,
                body.MaterialId,
                body.CustomerReference,
                body.SourceEvidenceKind,
                body.ComparisonDimension,
                body.CurrentUnitPrice,
                body.CurrencyCode,
                body.NormalizedUnit,
                body.WindowStartUtc,
                body.WindowEndUtc,
                body.PriceBasisGrossNet,
                body.PriceBasisTaxInclusion,
                body.PriceBasisDiscountInclusion,
                body.PriceBasisFreightInclusion,
                body.SourceProvenance,
                body.IdempotencyKey),
            cancellationToken);

        return MapWriteResult(result, isCreate: true);
    }

    private static IResult MapWriteResult(CommercialSalesPriceWriteResult result, bool isCreate = false)
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

    private static object MapProfile(CommercialSalesPriceProfileReadModel profile) => new
    {
        id = profile.Id,
        profileCode = profile.ProfileCode,
        displayName = profile.DisplayName,
        status = profile.Status,
        comparisonDimension = profile.ComparisonDimension,
        referenceWindowDurationDays = profile.ReferenceWindowDurationDays,
        minimumComparableObservations = profile.MinimumComparableObservations,
        maximumPositiveVarianceRatio = profile.MaximumPositiveVarianceRatio,
        allowedEvidenceStatuses = profile.AllowedEvidenceStatuses,
        allowedSourceEvidenceKinds = profile.AllowedSourceEvidenceKinds,
        requiredTaxInclusion = profile.RequiredTaxInclusion,
        requiredDiscountInclusion = profile.RequiredDiscountInclusion,
        requiredFreightInclusion = profile.RequiredFreightInclusion,
        requiredGrossNetBasis = profile.RequiredGrossNetBasis,
        referenceCenter = profile.ReferenceCenter,
        ruleSetVersion = profile.RuleSetVersion,
        sourceProvenance = profile.SourceProvenance,
        version = profile.Version,
        isPilotData = profile.IsPilotData,
    };

    private static object MapAssessmentSummary(CommercialSalesPriceAssessmentSummaryReadModel assessment) => new
    {
        id = assessment.Id,
        assessmentReference = assessment.AssessmentReference,
        profileId = assessment.ProfileId,
        profileCode = assessment.ProfileCode,
        materialId = assessment.MaterialId,
        customerReference = assessment.CustomerReference,
        sourceEvidenceKind = assessment.SourceEvidenceKind,
        comparisonDimension = assessment.ComparisonDimension,
        assessmentOutcome = assessment.AssessmentOutcome,
        currentUnitPrice = assessment.CurrentUnitPrice,
        referenceMedianUnitPrice = assessment.ReferenceMedianUnitPrice,
        varianceRatio = assessment.VarianceRatio,
        candidateObservationCount = assessment.CandidateObservationCount,
        comparableObservationCount = assessment.ComparableObservationCount,
        excludedObservationCount = assessment.ExcludedObservationCount,
        currencyCode = assessment.CurrencyCode,
        normalizedUnit = assessment.NormalizedUnit,
        windowStartUtc = assessment.WindowStartUtc,
        windowEndUtc = assessment.WindowEndUtc,
        leadingDriverCode = assessment.LeadingDriverCode,
        ruleSetVersion = assessment.RuleSetVersion,
        evaluatedAtUtc = assessment.EvaluatedAtUtc,
        createdAtUtc = assessment.CreatedAtUtc,
        isPilotData = assessment.IsPilotData,
    };

    private static object MapAssessmentDetail(CommercialSalesPriceAssessmentDetailReadModel assessment) => new
    {
        id = assessment.Id,
        assessmentReference = assessment.AssessmentReference,
        profileId = assessment.ProfileId,
        profileCode = assessment.ProfileCode,
        materialId = assessment.MaterialId,
        customerReference = assessment.CustomerReference,
        sourceEvidenceKind = assessment.SourceEvidenceKind,
        comparisonDimension = assessment.ComparisonDimension,
        currentUnitPrice = assessment.CurrentUnitPrice,
        referenceMedianUnitPrice = assessment.ReferenceMedianUnitPrice,
        absoluteVariance = assessment.AbsoluteVariance,
        varianceRatio = assessment.VarianceRatio,
        candidateObservationCount = assessment.CandidateObservationCount,
        comparableObservationCount = assessment.ComparableObservationCount,
        excludedObservationCount = assessment.ExcludedObservationCount,
        currencyCode = assessment.CurrencyCode,
        normalizedUnit = assessment.NormalizedUnit,
        windowStartUtc = assessment.WindowStartUtc,
        windowEndUtc = assessment.WindowEndUtc,
        priceBasisGrossNet = assessment.PriceBasisGrossNet,
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
}

internal sealed record CreateCommercialSalesPriceProfileRequest(
    string ProfileCode,
    string DisplayName,
    string ComparisonDimension,
    int ReferenceWindowDurationDays,
    int MinimumComparableObservations,
    decimal MaximumPositiveVarianceRatio,
    string AllowedEvidenceStatuses,
    string AllowedSourceEvidenceKinds,
    string RequiredTaxInclusion,
    string RequiredDiscountInclusion,
    string RequiredFreightInclusion,
    string RequiredGrossNetBasis,
    string SourceProvenance,
    string IdempotencyKey);

internal sealed record UpdateCommercialSalesPriceProfileRequest(
    uint ExpectedVersion,
    string DisplayName,
    string Status,
    string ComparisonDimension,
    int ReferenceWindowDurationDays,
    int MinimumComparableObservations,
    decimal MaximumPositiveVarianceRatio,
    string AllowedEvidenceStatuses,
    string AllowedSourceEvidenceKinds,
    string RequiredTaxInclusion,
    string RequiredDiscountInclusion,
    string RequiredFreightInclusion,
    string RequiredGrossNetBasis,
    string SourceProvenance);

internal sealed record EvaluateCommercialSalesPriceAssessmentRequest(
    Guid ProfileId,
    Guid MaterialId,
    string? CustomerReference,
    string SourceEvidenceKind,
    string ComparisonDimension,
    decimal CurrentUnitPrice,
    string CurrencyCode,
    string NormalizedUnit,
    DateTimeOffset WindowStartUtc,
    DateTimeOffset WindowEndUtc,
    string PriceBasisGrossNet,
    string PriceBasisTaxInclusion,
    string PriceBasisDiscountInclusion,
    string PriceBasisFreightInclusion,
    string SourceProvenance,
    string IdempotencyKey);

public static class CommercialSalesPriceServiceCollectionExtensions
{
    public static IServiceCollection AddCrevaFactOSCommercialSalesPrice(this IServiceCollection services)
    {
        services.AddScoped<ICommercialSalesPriceRepository>(serviceProvider =>
        {
            var dbContext = serviceProvider.GetService<CrevaFactOSDbContext>();
            return dbContext is null
                ? new UnavailableCommercialSalesPriceRepository()
                : new CommercialSalesPriceRepository(
                    dbContext,
                    serviceProvider.GetRequiredService<TenantDatabaseSession>());
        });

        return services;
    }
}
