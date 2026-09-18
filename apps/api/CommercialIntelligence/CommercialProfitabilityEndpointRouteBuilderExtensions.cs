using NevaxisZentra.Api.Facilities;
using NevaxisZentra.Persistence;
using NevaxisZentra.Persistence.Audit;
using NevaxisZentra.Persistence.CommercialIntelligence;
using NevaxisZentra.Persistence.Tenancy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace NevaxisZentra.Api.CommercialIntelligence;

internal static class CommercialProfitabilityEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapCommercialProfitabilityEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/commercial-intelligence/profitability").RequireAuthorization();

        group.MapGet("/workspace", GetWorkspaceAsync);
        group.MapGet("/profiles", ListProfilesAsync);
        group.MapPost("/profiles", CreateProfileAsync);
        group.MapPut("/profiles/{profileId:guid}", UpdateProfileAsync);
        group.MapGet("/assessments", ListAssessmentsAsync);
        group.MapGet("/assessments/{assessmentId:guid}", GetAssessmentAsync);
        group.MapPost("/assessments/evaluate", EvaluateAssessmentAsync);
        group.MapGet("/comparisons", ListComparisonsAsync);
        group.MapGet("/comparisons/{comparisonRunId:guid}", GetComparisonAsync);
        group.MapPost("/comparisons/evaluate", EvaluateComparisonAsync);

        return endpoints;
    }

    private static async Task<IResult> GetWorkspaceAsync(
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        ICommercialProfitabilityRepository repository,
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
            defaultMinimumContributionMarginRatio = workspace.DefaultMinimumContributionMarginRatio,
            defaultMaximumEvidenceAgeDays = workspace.DefaultMaximumEvidenceAgeDays,
            defaultRequiredCostComponentKinds = workspace.DefaultRequiredCostComponentKinds,
            realRevenueMappingComplete = workspace.RealRevenueMappingComplete,
            realCostMappingComplete = workspace.RealCostMappingComplete,
            fullCostMappingComplete = workspace.FullCostMappingComplete,
            realProfitabilityMappingComplete = workspace.RealProfitabilityMappingComplete,
            capabilities = new { canRead = workspace.CanRead, canManage = workspace.CanManage },
        });
    }

    private static async Task<IResult> ListProfilesAsync(
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        ICommercialProfitabilityRepository repository,
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
            ruleSetVersion = CommercialIntelligenceConstants.ProfitabilityRuleSetVersion,
            provenanceDisclaimer = CommercialIntelligenceConstants.ProfitabilityDevelopmentPilotProvenance,
        });
    }

    private static async Task<IResult> CreateProfileAsync(
        [FromQuery] Guid? facilityId,
        [FromBody] CreateCommercialProfitabilityProfileRequest body,
        IFacilityAccessResolver facilityAccessResolver,
        ICommercialProfitabilityRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await CommercialIntelligenceEndpointHelpers.ResolveManageAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var result = await repository.CreateProfileAsync(
            context,
            new CreateCommercialProfitabilityProfileCommand(
                body.ProfileCode,
                body.Name,
                body.MinimumContributionMarginRatio,
                body.MaximumEvidenceAgeDays,
                body.RequiredCostComponentKinds,
                body.RequiredCurrencyCode,
                body.RequiredUnitOfMeasure,
                body.RequiredGrossNetBasis,
                body.RequiredTaxInclusion,
                body.RequiredDiscountInclusion,
                body.RequiredFreightInclusion,
                body.AllowedEvidenceStatuses,
                body.SourceProvenance,
                body.IdempotencyKey),
            cancellationToken);

        return MapWriteResult(result, isCreate: true);
    }

    private static async Task<IResult> UpdateProfileAsync(
        Guid profileId,
        [FromQuery] Guid? facilityId,
        [FromBody] UpdateCommercialProfitabilityProfileRequest body,
        IFacilityAccessResolver facilityAccessResolver,
        ICommercialProfitabilityRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await CommercialIntelligenceEndpointHelpers.ResolveManageAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var result = await repository.UpdateProfileAsync(
            context,
            new UpdateCommercialProfitabilityProfileCommand(
                profileId,
                body.ExpectedVersion,
                body.Name,
                body.Status,
                body.MinimumContributionMarginRatio,
                body.MaximumEvidenceAgeDays,
                body.RequiredCostComponentKinds,
                body.RequiredCurrencyCode,
                body.RequiredUnitOfMeasure,
                body.RequiredGrossNetBasis,
                body.RequiredTaxInclusion,
                body.RequiredDiscountInclusion,
                body.RequiredFreightInclusion,
                body.AllowedEvidenceStatuses,
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
        ICommercialProfitabilityRepository repository = null!,
        CorrelationContext correlationContext = null!,
        CancellationToken cancellationToken = default)
    {
        var (context, failResult) = await CommercialIntelligenceEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var assessments = await repository.ListAssessmentsAsync(
            context,
            new CommercialProfitabilityAssessmentListFilters(materialId, outcome, page, pageSize),
            cancellationToken);

        return TypedResults.Ok(new
        {
            items = assessments.Select(MapAssessmentSummary),
            ruleSetVersion = CommercialIntelligenceConstants.ProfitabilityRuleSetVersion,
        });
    }

    private static async Task<IResult> GetAssessmentAsync(
        Guid assessmentId,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        ICommercialProfitabilityRepository repository,
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
                "Profitability assessment not found.")
            : TypedResults.Ok(MapAssessmentDetail(assessment));
    }

    private static async Task<IResult> EvaluateAssessmentAsync(
        [FromQuery] Guid? facilityId,
        [FromBody] EvaluateCommercialProfitabilityAssessmentRequest body,
        IFacilityAccessResolver facilityAccessResolver,
        ICommercialProfitabilityRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await CommercialIntelligenceEndpointHelpers.ResolveManageAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var result = await repository.EvaluateAssessmentAsync(
            context,
            new EvaluateCommercialProfitabilityAssessmentCommand(
                body.ProfileId,
                body.ExpectedProfileVersion,
                body.SalesEvidenceObservationId,
                body.SourceProvenance,
                body.IdempotencyKey),
            cancellationToken);

        return MapWriteResult(result, isCreate: true);
    }

    private static async Task<IResult> ListComparisonsAsync(
        [FromQuery] Guid? facilityId,
        [FromQuery] string? comparisonType,
        [FromQuery] Guid? profileId,
        [FromQuery] Guid? materialId,
        [FromQuery] string? customerReference,
        [FromQuery] string? outcome,
        [FromQuery] string? evidenceStatus,
        [FromQuery] DateTimeOffset? windowFromUtc,
        [FromQuery] DateTimeOffset? windowToUtc,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        IFacilityAccessResolver facilityAccessResolver = null!,
        ICommercialProfitabilityRepository repository = null!,
        CorrelationContext correlationContext = null!,
        CancellationToken cancellationToken = default)
    {
        var (context, failResult) = await CommercialIntelligenceEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var comparisons = await repository.ListComparisonsAsync(
            context,
            new CommercialProfitabilityComparisonListFilters(
                comparisonType,
                profileId,
                materialId,
                customerReference,
                outcome,
                evidenceStatus,
                windowFromUtc,
                windowToUtc,
                page,
                pageSize),
            cancellationToken);

        return TypedResults.Ok(new
        {
            items = comparisons.Select(MapComparisonSummary),
            page,
            pageSize,
            ruleSetVersion = CommercialIntelligenceConstants.ProfitabilityComparisonRuleSetVersion,
            realProfitabilityMappingComplete = false,
        });
    }

    private static async Task<IResult> GetComparisonAsync(
        Guid comparisonRunId,
        [FromQuery] Guid? facilityId,
        IFacilityAccessResolver facilityAccessResolver,
        ICommercialProfitabilityRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await CommercialIntelligenceEndpointHelpers.ResolveReadAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var comparison = await repository.GetComparisonByIdAsync(context, comparisonRunId, cancellationToken);
        return comparison is null
            ? CommercialIntelligenceEndpointHelpers.MapFailure(
                CommercialIntelligenceFailureCodes.NotFound,
                "Profitability comparison not found.")
            : TypedResults.Ok(MapComparisonDetail(comparison));
    }

    private static async Task<IResult> EvaluateComparisonAsync(
        [FromQuery] Guid? facilityId,
        [FromBody] EvaluateCommercialProfitabilityComparisonRequest body,
        IFacilityAccessResolver facilityAccessResolver,
        ICommercialProfitabilityRepository repository,
        CorrelationContext correlationContext,
        CancellationToken cancellationToken)
    {
        var (context, failResult) = await CommercialIntelligenceEndpointHelpers.ResolveManageAccessAsync(
            facilityId, facilityAccessResolver, correlationContext, cancellationToken);
        if (context is null) return failResult!;

        var result = await repository.EvaluateComparisonAsync(
            context,
            new EvaluateCommercialProfitabilityComparisonCommand(
                body.ProfileId,
                body.ExpectedProfileVersion,
                body.ComparisonType,
                body.MaterialId,
                body.CustomerReference,
                body.CurrentWindowStartUtc,
                body.CurrentWindowEndUtc,
                body.ReferenceWindowStartUtc,
                body.ReferenceWindowEndUtc,
                body.SourceProvenance,
                body.IdempotencyKey),
            cancellationToken);

        return MapWriteResult(result, isCreate: true);
    }

    private static IResult MapWriteResult(CommercialProfitabilityWriteResult result, bool isCreate = false)
    {
        if (!result.Succeeded)
        {
            return CommercialIntelligenceEndpointHelpers.MapFailure(result.FailureCode!, result.FailureReason!);
        }

        if (result.Assessment is not null)
        {
            return result.IdempotentReplay
                ? TypedResults.Ok(new
                {
                    id = result.EntityId,
                    idempotentReplay = true,
                    assessment = MapAssessmentDetail(result.Assessment),
                })
                : TypedResults.Created(
                    $"/api/commercial-intelligence/profitability/assessments/{result.EntityId}",
                    new
                    {
                        id = result.EntityId,
                        idempotentReplay = false,
                        assessment = MapAssessmentDetail(result.Assessment),
                    });
        }

        if (result.Comparison is not null)
        {
            var comparisonDto = MapComparisonDetail(result.Comparison);
            return result.IdempotentReplay
                ? TypedResults.Ok(new { id = result.EntityId, idempotentReplay = true, comparison = comparisonDto })
                : TypedResults.Created((string?)null, new { id = result.EntityId, idempotentReplay = false, comparison = comparisonDto });
        }

        return result.IdempotentReplay
            ? TypedResults.Ok(new { id = result.EntityId, version = result.Version, idempotentReplay = true })
            : isCreate
                ? TypedResults.Created(
                    $"/api/commercial-intelligence/profitability/profiles/{result.EntityId}",
                    new { id = result.EntityId, version = result.Version, idempotentReplay = false })
                : TypedResults.Ok(new { id = result.EntityId, version = result.Version, idempotentReplay = false });
    }

    private static object MapProfile(CommercialProfitabilityProfileReadModel profile) => new
    {
        id = profile.Id,
        profileCode = profile.ProfileCode,
        name = profile.Name,
        status = profile.Status,
        minimumContributionMarginRatio = profile.MinimumContributionMarginRatio,
        maximumEvidenceAgeDays = profile.MaximumEvidenceAgeDays,
        requiredCostComponentKinds = profile.RequiredCostComponentKinds,
        requiredCurrencyCode = profile.RequiredCurrencyCode,
        requiredUnitOfMeasure = profile.RequiredUnitOfMeasure,
        requiredGrossNetBasis = profile.RequiredGrossNetBasis,
        requiredTaxInclusion = profile.RequiredTaxInclusion,
        requiredDiscountInclusion = profile.RequiredDiscountInclusion,
        requiredFreightInclusion = profile.RequiredFreightInclusion,
        allowedEvidenceStatuses = profile.AllowedEvidenceStatuses,
        ruleSetVersion = profile.RuleSetVersion,
        sourceProvenance = profile.SourceProvenance,
        version = profile.Version,
        isPilotData = profile.IsPilotData,
    };

    private static object MapAssessmentSummary(CommercialProfitabilityAssessmentSummaryReadModel assessment) => new
    {
        id = assessment.Id,
        assessmentReference = assessment.AssessmentReference,
        profileId = assessment.ProfileId,
        profileCode = assessment.ProfileCode,
        salesEvidenceObservationId = assessment.SalesEvidenceObservationId,
        customerReference = assessment.CustomerReference,
        materialId = assessment.MaterialId,
        outcome = assessment.Outcome,
        comparableNetSalesUnitPrice = assessment.ComparableNetSalesUnitPrice,
        totalIncludedUnitCost = assessment.TotalIncludedUnitCost,
        unitContribution = assessment.UnitContribution,
        contributionMarginRatio = assessment.ContributionMarginRatio,
        markupRatio = assessment.MarkupRatio,
        costCoverageStatus = assessment.CostCoverageStatus,
        candidateCostComponentCount = assessment.CandidateCostComponentCount,
        includedCostComponentCount = assessment.IncludedCostComponentCount,
        excludedCostComponentCount = assessment.ExcludedCostComponentCount,
        leadingDriverCode = assessment.LeadingDriverCode,
        ruleSetVersion = assessment.RuleSetVersion,
        evaluatedAtUtc = assessment.EvaluatedAtUtc,
        createdAtUtc = assessment.CreatedAtUtc,
        isPilotData = assessment.IsPilotData,
    };

    private static object MapAssessmentDetail(CommercialProfitabilityAssessmentDetailReadModel assessment) => new
    {
        id = assessment.Id,
        assessmentReference = assessment.AssessmentReference,
        profileId = assessment.ProfileId,
        profileCode = assessment.ProfileCode,
        profileVersion = assessment.ProfileVersion,
        salesEvidenceObservationId = assessment.SalesEvidenceObservationId,
        customerReference = assessment.CustomerReference,
        materialId = assessment.MaterialId,
        orderReference = assessment.OrderReference,
        quotationReference = assessment.QuotationReference,
        evidenceSetReference = assessment.EvidenceSetReference,
        comparableNetSalesUnitPrice = assessment.ComparableNetSalesUnitPrice,
        totalIncludedUnitCost = assessment.TotalIncludedUnitCost,
        unitContribution = assessment.UnitContribution,
        contributionMarginRatio = assessment.ContributionMarginRatio,
        markupRatio = assessment.MarkupRatio,
        candidateCostComponentCount = assessment.CandidateCostComponentCount,
        includedCostComponentCount = assessment.IncludedCostComponentCount,
        excludedCostComponentCount = assessment.ExcludedCostComponentCount,
        requiredCostComponentCount = assessment.RequiredCostComponentCount,
        availableRequiredCostComponentCount = assessment.AvailableRequiredCostComponentCount,
        missingRequiredCostComponentCount = assessment.MissingRequiredCostComponentCount,
        missingRequiredComponentKinds = assessment.MissingRequiredComponentKinds,
        costCoverageStatus = assessment.CostCoverageStatus,
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
        costComponents = assessment.CostComponents.Select(c => new
        {
            id = c.Id,
            componentKind = c.ComponentKind,
            unitCost = c.UnitCost,
            currencyCode = c.CurrencyCode,
            unitOfMeasure = c.UnitOfMeasure,
            costBasis = c.CostBasis,
            allocationBasis = c.AllocationBasis,
            evidenceTimestampUtc = c.EvidenceTimestampUtc,
            sourceKind = c.SourceKind,
            sourceReference = c.SourceReference,
            evidenceStatus = c.EvidenceStatus,
            inclusionStatus = c.InclusionStatus,
            exclusionReason = c.ExclusionReason,
            sortOrder = c.SortOrder,
        }),
        drivers = assessment.Drivers.Select(d => new
        {
            id = d.Id,
            reasonCode = d.ReasonCode,
            severity = d.Severity,
            actualValue = d.ActualValue,
            thresholdValue = d.ThresholdValue,
            unit = d.Unit,
            normalizedVariance = d.NormalizedVariance,
            affectedComponentCount = d.AffectedComponentCount,
            supportingText = d.SupportingText,
            sortOrder = d.SortOrder,
            isLeadingDriver = d.IsLeadingDriver,
        }),
        isPilotData = assessment.IsPilotData,
    };

    private static object MapComparisonSummary(CommercialProfitabilityComparisonSummaryReadModel comparison) => new
    {
        id = comparison.Id,
        comparisonReference = comparison.ComparisonReference,
        profileId = comparison.ProfileId,
        profileCode = comparison.ProfileCode,
        comparisonType = comparison.ComparisonType,
        materialId = comparison.MaterialId,
        customerReference = comparison.CustomerReference,
        comparisonOutcome = comparison.ComparisonOutcome,
        evidenceStatus = comparison.EvidenceStatus,
        aggregationMethod = comparison.AggregationMethod,
        candidateAssessmentCount = comparison.CandidateAssessmentCount,
        comparableAssessmentCount = comparison.ComparableAssessmentCount,
        excludedAssessmentCount = comparison.ExcludedAssessmentCount,
        referenceMedianContributionMarginRatio = comparison.ReferenceMedianContributionMarginRatio,
        currentMedianContributionMarginRatio = comparison.CurrentMedianContributionMarginRatio,
        contributionMarginDelta = comparison.ContributionMarginDelta,
        referenceWindowStartUtc = comparison.ReferenceWindowStartUtc,
        referenceWindowEndUtc = comparison.ReferenceWindowEndUtc,
        currentWindowStartUtc = comparison.CurrentWindowStartUtc,
        currentWindowEndUtc = comparison.CurrentWindowEndUtc,
        leadingDriverCode = comparison.LeadingDriverCode,
        ruleSetVersion = comparison.RuleSetVersion,
        evaluatedAtUtc = comparison.EvaluatedAtUtc,
        createdAtUtc = comparison.CreatedAtUtc,
        isPilotData = comparison.IsPilotData,
        realProfitabilityMappingComplete = false,
    };

    private static object MapComparisonDetail(CommercialProfitabilityComparisonDetailReadModel comparison) => new
    {
        id = comparison.Id,
        comparisonReference = comparison.ComparisonReference,
        profileId = comparison.ProfileId,
        profileCode = comparison.ProfileCode,
        profileVersion = comparison.ProfileVersion,
        comparisonType = comparison.ComparisonType,
        materialId = comparison.MaterialId,
        customerReference = comparison.CustomerReference,
        aggregationMethod = comparison.AggregationMethod,
        candidateAssessmentCount = comparison.CandidateAssessmentCount,
        comparableAssessmentCount = comparison.ComparableAssessmentCount,
        excludedAssessmentCount = comparison.ExcludedAssessmentCount,
        completeCoverageCount = comparison.CompleteCoverageCount,
        partialCoverageCount = comparison.PartialCoverageCount,
        insufficientDataCount = comparison.InsufficientDataCount,
        sourceConflictCount = comparison.SourceConflictCount,
        negativeContributionCount = comparison.NegativeContributionCount,
        belowThresholdCount = comparison.BelowThresholdCount,
        referenceCandidateAssessmentCount = comparison.ReferenceCandidateAssessmentCount,
        referenceComparableAssessmentCount = comparison.ReferenceComparableAssessmentCount,
        referenceExcludedAssessmentCount = comparison.ReferenceExcludedAssessmentCount,
        referenceCompleteCoverageCount = comparison.ReferenceCompleteCoverageCount,
        referencePartialCoverageCount = comparison.ReferencePartialCoverageCount,
        referenceInsufficientDataCount = comparison.ReferenceInsufficientDataCount,
        referenceSourceConflictCount = comparison.ReferenceSourceConflictCount,
        referenceNegativeContributionCount = comparison.ReferenceNegativeContributionCount,
        referenceBelowThresholdCount = comparison.ReferenceBelowThresholdCount,
        currentCandidateAssessmentCount = comparison.CurrentCandidateAssessmentCount,
        currentComparableAssessmentCount = comparison.CurrentComparableAssessmentCount,
        currentExcludedAssessmentCount = comparison.CurrentExcludedAssessmentCount,
        currentCompleteCoverageCount = comparison.CurrentCompleteCoverageCount,
        currentPartialCoverageCount = comparison.CurrentPartialCoverageCount,
        currentInsufficientDataCount = comparison.CurrentInsufficientDataCount,
        currentSourceConflictCount = comparison.CurrentSourceConflictCount,
        currentNegativeContributionCount = comparison.CurrentNegativeContributionCount,
        currentBelowThresholdCount = comparison.CurrentBelowThresholdCount,
        referenceMedianContributionMarginRatio = comparison.ReferenceMedianContributionMarginRatio,
        currentMedianContributionMarginRatio = comparison.CurrentMedianContributionMarginRatio,
        contributionMarginDelta = comparison.ContributionMarginDelta,
        referenceMedianUnitContribution = comparison.ReferenceMedianUnitContribution,
        currentMedianUnitContribution = comparison.CurrentMedianUnitContribution,
        referenceWindowStartUtc = comparison.ReferenceWindowStartUtc,
        referenceWindowEndUtc = comparison.ReferenceWindowEndUtc,
        currentWindowStartUtc = comparison.CurrentWindowStartUtc,
        currentWindowEndUtc = comparison.CurrentWindowEndUtc,
        evidenceStatus = comparison.EvidenceStatus,
        comparisonOutcome = comparison.ComparisonOutcome,
        leadingDriverCode = comparison.LeadingDriverCode,
        ruleSetVersion = comparison.RuleSetVersion,
        sourceProvenance = comparison.SourceProvenance,
        evaluatedAtUtc = comparison.EvaluatedAtUtc,
        createdAtUtc = comparison.CreatedAtUtc,
        results = comparison.Results.Select(r => new
        {
            id = r.Id,
            resultDimension = r.ResultDimension,
            canonicalResultReference = r.CanonicalResultReference,
            customerReference = r.CustomerReference,
            materialId = r.MaterialId,
            componentKind = r.ComponentKind,
            candidateAssessmentCount = r.CandidateAssessmentCount,
            comparableAssessmentCount = r.ComparableAssessmentCount,
            excludedAssessmentCount = r.ExcludedAssessmentCount,
            completeCoverageCount = r.CompleteCoverageCount,
            partialCoverageCount = r.PartialCoverageCount,
            negativeContributionCount = r.NegativeContributionCount,
            belowThresholdCount = r.BelowThresholdCount,
            referenceComparableAssessmentCount = r.ReferenceComparableAssessmentCount,
            currentComparableAssessmentCount = r.CurrentComparableAssessmentCount,
            referenceExcludedAssessmentCount = r.ReferenceExcludedAssessmentCount,
            currentExcludedAssessmentCount = r.CurrentExcludedAssessmentCount,
            referenceMedianContributionMarginRatio = r.ReferenceMedianContributionMarginRatio,
            currentMedianContributionMarginRatio = r.CurrentMedianContributionMarginRatio,
            contributionMarginDelta = r.ContributionMarginDelta,
            referenceMedianUnitContribution = r.ReferenceMedianUnitContribution,
            currentMedianUnitContribution = r.CurrentMedianUnitContribution,
            referenceMedianUnitCost = r.ReferenceMedianUnitCost,
            currentMedianUnitCost = r.CurrentMedianUnitCost,
            signedUnitCostChange = r.SignedUnitCostChange,
            signedUnitCostChangeRatio = r.SignedUnitCostChangeRatio,
            referenceMedianCostShare = r.ReferenceMedianCostShare,
            currentMedianCostShare = r.CurrentMedianCostShare,
            costShareDelta = r.CostShareDelta,
            positionState = r.PositionState,
            contributionRank = r.ContributionRank,
            isTied = r.IsTied,
            evidenceStatus = r.EvidenceStatus,
            sortOrder = r.SortOrder,
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
            affectedAssessmentCount = d.AffectedAssessmentCount,
            supportingText = d.SupportingText,
            sortOrder = d.SortOrder,
            isLeadingDriver = d.IsLeadingDriver,
        }),
        isPilotData = comparison.IsPilotData,
        realProfitabilityMappingComplete = false,
    };
}

internal sealed record CreateCommercialProfitabilityProfileRequest(
    string ProfileCode,
    string Name,
    decimal MinimumContributionMarginRatio,
    int MaximumEvidenceAgeDays,
    string RequiredCostComponentKinds,
    string RequiredCurrencyCode,
    string RequiredUnitOfMeasure,
    string RequiredGrossNetBasis,
    string RequiredTaxInclusion,
    string RequiredDiscountInclusion,
    string RequiredFreightInclusion,
    string AllowedEvidenceStatuses,
    string SourceProvenance,
    string IdempotencyKey);

internal sealed record UpdateCommercialProfitabilityProfileRequest(
    uint ExpectedVersion,
    string Name,
    string Status,
    decimal MinimumContributionMarginRatio,
    int MaximumEvidenceAgeDays,
    string RequiredCostComponentKinds,
    string RequiredCurrencyCode,
    string RequiredUnitOfMeasure,
    string RequiredGrossNetBasis,
    string RequiredTaxInclusion,
    string RequiredDiscountInclusion,
    string RequiredFreightInclusion,
    string AllowedEvidenceStatuses,
    string SourceProvenance);

internal sealed record EvaluateCommercialProfitabilityAssessmentRequest(
    Guid ProfileId,
    uint ExpectedProfileVersion,
    Guid SalesEvidenceObservationId,
    string SourceProvenance,
    string IdempotencyKey);

internal sealed record EvaluateCommercialProfitabilityComparisonRequest(
    Guid ProfileId,
    uint ExpectedProfileVersion,
    string ComparisonType,
    Guid? MaterialId,
    string? CustomerReference,
    DateTimeOffset CurrentWindowStartUtc,
    DateTimeOffset CurrentWindowEndUtc,
    DateTimeOffset? ReferenceWindowStartUtc,
    DateTimeOffset? ReferenceWindowEndUtc,
    string SourceProvenance,
    string IdempotencyKey);

public static class CommercialProfitabilityServiceCollectionExtensions
{
    public static IServiceCollection AddNevaxisZentraCommercialProfitability(this IServiceCollection services)
    {
        services.AddScoped<ICommercialProfitabilityRepository>(serviceProvider =>
        {
            var dbContext = serviceProvider.GetService<NevaxisZentraDbContext>();
            return dbContext is null
                ? new UnavailableCommercialProfitabilityRepository()
                : new CommercialProfitabilityRepository(
                    dbContext,
                    serviceProvider.GetRequiredService<TenantDatabaseSession>());
        });

        return services;
    }
}
