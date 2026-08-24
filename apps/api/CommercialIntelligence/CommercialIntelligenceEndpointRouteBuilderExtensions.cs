using CrevaFactOS.Api.Facilities;
using CrevaFactOS.Persistence;
using CrevaFactOS.Persistence.Audit;
using CrevaFactOS.Persistence.CommercialIntelligence;
using CrevaFactOS.Persistence.Tenancy;
using Microsoft.AspNetCore.Mvc;

namespace CrevaFactOS.Api.CommercialIntelligence;

public static class CommercialIntelligenceEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapCommercialIntelligenceEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/commercial-intelligence/purchase-prices").RequireAuthorization();

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
        ICommercialIntelligenceRepository repository,
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
            capabilities = new { canRead = workspace.CanRead, canManage = workspace.CanManage },
        });
    }

    private static async Task<IResult> ListProfilesAsync(
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        ICommercialIntelligenceRepository repository,
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
            ruleSetVersion = CommercialIntelligenceConstants.RuleSetVersion,
            provenanceDisclaimer = CommercialIntelligenceConstants.DevelopmentPilotProvenance,
        });
    }

    private static async Task<IResult> CreateProfileAsync(
        [FromQuery] Guid? facilityId,
        [FromBody] CreateCommercialPurchasePriceProfileRequest body,
        IFacilityAccessResolver facilityAccessResolver,
        ICommercialIntelligenceRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await CommercialIntelligenceEndpointHelpers.ResolveManageAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var result = await repository.CreateProfileAsync(
            context,
            new CreateCommercialPurchasePriceProfileCommand(
                body.ProfileCode,
                body.DisplayName,
                body.ComparisonDimension,
                body.ReferenceWindowDurationDays,
                body.MinimumComparableObservations,
                body.MaximumPositiveVarianceRatio,
                body.SupplierSpreadThreshold,
                body.AllowedEvidenceStatuses,
                body.RequiredTaxInclusion,
                body.RequiredDiscountInclusion,
                body.RequiredFreightInclusion,
                body.SourceProvenance,
                body.IdempotencyKey),
            cancellationToken);

        return CommercialIntelligenceEndpointHelpers.MapWriteResult(result, isCreate: true);
    }

    private static async Task<IResult> UpdateProfileAsync(
        Guid profileId,
        [FromQuery] Guid? facilityId,
        [FromBody] UpdateCommercialPurchasePriceProfileRequest body,
        IFacilityAccessResolver facilityAccessResolver,
        ICommercialIntelligenceRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await CommercialIntelligenceEndpointHelpers.ResolveManageAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var result = await repository.UpdateProfileAsync(
            context,
            new UpdateCommercialPurchasePriceProfileCommand(
                profileId,
                body.ExpectedVersion,
                body.DisplayName,
                body.Status,
                body.ComparisonDimension,
                body.ReferenceWindowDurationDays,
                body.MinimumComparableObservations,
                body.MaximumPositiveVarianceRatio,
                body.SupplierSpreadThreshold,
                body.AllowedEvidenceStatuses,
                body.RequiredTaxInclusion,
                body.RequiredDiscountInclusion,
                body.RequiredFreightInclusion,
                body.SourceProvenance),
            cancellationToken);

        return CommercialIntelligenceEndpointHelpers.MapWriteResult(result);
    }

    private static async Task<IResult> ListAssessmentsAsync(
        [FromQuery] Guid? facilityId,
        [FromQuery] Guid? materialId,
        [FromQuery] string? assessmentOutcome,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        IFacilityAccessResolver facilityAccessResolver = null!,
        ICommercialIntelligenceRepository repository = null!,
        CorrelationContext correlationContext = null!,
        CancellationToken cancellationToken = default)
    {
        var (context, failResult) = await CommercialIntelligenceEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var assessments = await repository.ListAssessmentsAsync(
            context,
            new CommercialPurchasePriceAssessmentListFilters(materialId, assessmentOutcome, page, pageSize),
            cancellationToken);

        return TypedResults.Ok(new
        {
            items = assessments.Select(MapAssessmentSummary),
            page,
            pageSize,
            ruleSetVersion = CommercialIntelligenceConstants.RuleSetVersion,
        });
    }

    private static async Task<IResult> GetAssessmentAsync(
        Guid assessmentId,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        ICommercialIntelligenceRepository repository,
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
                "Purchase price assessment not found.")
            : TypedResults.Ok(CommercialIntelligenceEndpointHelpers.MapAssessmentDetail(assessment));
    }

    private static async Task<IResult> EvaluateAssessmentAsync(
        [FromQuery] Guid? facilityId,
        [FromBody] EvaluateCommercialPurchasePriceAssessmentRequest body,
        IFacilityAccessResolver facilityAccessResolver,
        ICommercialIntelligenceRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await CommercialIntelligenceEndpointHelpers.ResolveManageAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var result = await repository.EvaluateAssessmentAsync(
            context,
            new EvaluateCommercialPurchasePriceAssessmentCommand(
                body.ProfileId,
                body.MaterialId,
                body.SupplierReference,
                body.ComparisonDimension,
                body.CurrentUnitPrice,
                body.CurrencyCode,
                body.NormalizedUnit,
                body.WindowStartUtc,
                body.WindowEndUtc,
                body.PriceBasisTaxInclusion,
                body.PriceBasisDiscountInclusion,
                body.PriceBasisFreightInclusion,
                body.SourceProvenance,
                body.IdempotencyKey),
            cancellationToken);

        return CommercialIntelligenceEndpointHelpers.MapWriteResult(result, isCreate: true);
    }

    private static object MapProfile(CommercialPurchasePriceProfileReadModel profile) => new
    {
        id = profile.Id,
        profileCode = profile.ProfileCode,
        displayName = profile.DisplayName,
        status = profile.Status,
        comparisonDimension = profile.ComparisonDimension,
        referenceWindowDurationDays = profile.ReferenceWindowDurationDays,
        minimumComparableObservations = profile.MinimumComparableObservations,
        maximumPositiveVarianceRatio = profile.MaximumPositiveVarianceRatio,
        supplierSpreadThreshold = profile.SupplierSpreadThreshold,
        allowedEvidenceStatuses = profile.AllowedEvidenceStatuses,
        requiredTaxInclusion = profile.RequiredTaxInclusion,
        requiredDiscountInclusion = profile.RequiredDiscountInclusion,
        requiredFreightInclusion = profile.RequiredFreightInclusion,
        referenceCenter = profile.ReferenceCenter,
        ruleSetVersion = profile.RuleSetVersion,
        sourceProvenance = profile.SourceProvenance,
        version = profile.Version,
        isPilotData = profile.IsPilotData,
    };

    private static object MapAssessmentSummary(CommercialPurchasePriceAssessmentSummaryReadModel assessment) => new
    {
        id = assessment.Id,
        assessmentReference = assessment.AssessmentReference,
        profileId = assessment.ProfileId,
        profileCode = assessment.ProfileCode,
        materialId = assessment.MaterialId,
        supplierReference = assessment.SupplierReference,
        comparisonDimension = assessment.ComparisonDimension,
        assessmentOutcome = assessment.AssessmentOutcome,
        currentUnitPrice = assessment.CurrentUnitPrice,
        referenceMedianUnitPrice = assessment.ReferenceMedianUnitPrice,
        varianceRatio = assessment.VarianceRatio,
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
}

public sealed record CreateCommercialPurchasePriceProfileRequest(
    string ProfileCode,
    string DisplayName,
    string ComparisonDimension,
    int ReferenceWindowDurationDays,
    int MinimumComparableObservations,
    decimal MaximumPositiveVarianceRatio,
    decimal? SupplierSpreadThreshold,
    string AllowedEvidenceStatuses,
    string RequiredTaxInclusion,
    string RequiredDiscountInclusion,
    string RequiredFreightInclusion,
    string SourceProvenance,
    string IdempotencyKey);

public sealed record UpdateCommercialPurchasePriceProfileRequest(
    uint ExpectedVersion,
    string DisplayName,
    string Status,
    string ComparisonDimension,
    int ReferenceWindowDurationDays,
    int MinimumComparableObservations,
    decimal MaximumPositiveVarianceRatio,
    decimal? SupplierSpreadThreshold,
    string AllowedEvidenceStatuses,
    string RequiredTaxInclusion,
    string RequiredDiscountInclusion,
    string RequiredFreightInclusion,
    string SourceProvenance);

public sealed record EvaluateCommercialPurchasePriceAssessmentRequest(
    Guid ProfileId,
    Guid MaterialId,
    string? SupplierReference,
    string ComparisonDimension,
    decimal CurrentUnitPrice,
    string CurrencyCode,
    string NormalizedUnit,
    DateTimeOffset WindowStartUtc,
    DateTimeOffset WindowEndUtc,
    string PriceBasisTaxInclusion,
    string PriceBasisDiscountInclusion,
    string PriceBasisFreightInclusion,
    string SourceProvenance,
    string IdempotencyKey);

public static class CommercialIntelligenceServiceCollectionExtensions
{
    public static IServiceCollection AddCrevaFactOSCommercialIntelligence(this IServiceCollection services)
    {
        services.AddScoped<ICommercialIntelligenceRepository>(serviceProvider =>
        {
            var dbContext = serviceProvider.GetService<CrevaFactOSDbContext>();
            return dbContext is null
                ? new UnavailableCommercialIntelligenceRepository()
                : new CommercialIntelligenceRepository(
                    dbContext,
                    serviceProvider.GetRequiredService<TenantDatabaseSession>());
        });

        return services;
    }
}
