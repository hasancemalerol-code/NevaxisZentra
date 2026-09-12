namespace NevaxisFactOS.Persistence.CommercialIntelligence;

public sealed record CommercialProfitabilityProfileReadModel(
    Guid Id,
    string ProfileCode,
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
    string RuleSetVersion,
    string SourceProvenance,
    uint Version,
    bool IsPilotData);

public sealed record CommercialProfitabilityAssessmentCostComponentReadModel(
    Guid Id,
    string ComponentKind,
    decimal UnitCost,
    string CurrencyCode,
    string UnitOfMeasure,
    string CostBasis,
    string? AllocationBasis,
    DateTimeOffset EvidenceTimestampUtc,
    string SourceKind,
    string SourceReference,
    string EvidenceStatus,
    string InclusionStatus,
    string? ExclusionReason,
    int SortOrder);

public sealed record CommercialProfitabilityAssessmentDriverReadModel(
    Guid Id,
    string ReasonCode,
    string Severity,
    decimal? ActualValue,
    decimal? ThresholdValue,
    string? Unit,
    decimal? NormalizedVariance,
    int AffectedComponentCount,
    string SupportingText,
    int SortOrder,
    bool IsLeadingDriver);

public sealed record CommercialProfitabilityAssessmentSummaryReadModel(
    Guid Id,
    string AssessmentReference,
    Guid ProfileId,
    string ProfileCode,
    Guid SalesEvidenceObservationId,
    string CustomerReference,
    Guid MaterialId,
    string Outcome,
    decimal ComparableNetSalesUnitPrice,
    decimal TotalIncludedUnitCost,
    decimal UnitContribution,
    decimal? ContributionMarginRatio,
    decimal? MarkupRatio,
    string CostCoverageStatus,
    int CandidateCostComponentCount,
    int IncludedCostComponentCount,
    int ExcludedCostComponentCount,
    string? LeadingDriverCode,
    string RuleSetVersion,
    DateTimeOffset EvaluatedAtUtc,
    DateTimeOffset CreatedAtUtc,
    bool IsPilotData);

public sealed record CommercialProfitabilityAssessmentDetailReadModel(
    Guid Id,
    string AssessmentReference,
    Guid ProfileId,
    string ProfileCode,
    string ProfileVersion,
    Guid SalesEvidenceObservationId,
    string CustomerReference,
    Guid MaterialId,
    string? OrderReference,
    string? QuotationReference,
    string EvidenceSetReference,
    decimal ComparableNetSalesUnitPrice,
    decimal TotalIncludedUnitCost,
    decimal UnitContribution,
    decimal? ContributionMarginRatio,
    decimal? MarkupRatio,
    int CandidateCostComponentCount,
    int IncludedCostComponentCount,
    int ExcludedCostComponentCount,
    int RequiredCostComponentCount,
    int AvailableRequiredCostComponentCount,
    int MissingRequiredCostComponentCount,
    string MissingRequiredComponentKinds,
    string CostCoverageStatus,
    string CurrencyCode,
    string UnitOfMeasure,
    string GrossNetBasis,
    string TaxBasis,
    string DiscountBasis,
    string FreightBasis,
    string EvidenceStatus,
    string Outcome,
    string? LeadingDriverCode,
    string RuleSetVersion,
    string SourceProvenance,
    DateTimeOffset EvaluatedAtUtc,
    DateTimeOffset CreatedAtUtc,
    IReadOnlyList<CommercialProfitabilityAssessmentCostComponentReadModel> CostComponents,
    IReadOnlyList<CommercialProfitabilityAssessmentDriverReadModel> Drivers,
    bool IsPilotData);

public sealed record CommercialProfitabilityWorkspaceReadModel(
    string RuleSetVersion,
    string ProvenanceDisclaimer,
    decimal DefaultMinimumContributionMarginRatio,
    int DefaultMaximumEvidenceAgeDays,
    IReadOnlyList<string> DefaultRequiredCostComponentKinds,
    bool RealRevenueMappingComplete,
    bool RealCostMappingComplete,
    bool FullCostMappingComplete,
    bool RealProfitabilityMappingComplete,
    bool CanRead,
    bool CanManage);

public sealed record CreateCommercialProfitabilityProfileCommand(
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

public sealed record UpdateCommercialProfitabilityProfileCommand(
    Guid ProfileId,
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

public sealed record EvaluateCommercialProfitabilityAssessmentCommand(
    Guid ProfileId,
    uint ExpectedProfileVersion,
    Guid SalesEvidenceObservationId,
    string SourceProvenance,
    string IdempotencyKey);

public sealed record CommercialProfitabilityAssessmentListFilters(
    Guid? MaterialId,
    string? Outcome,
    int Page,
    int PageSize);

public sealed record CommercialProfitabilityComparisonSummaryReadModel(
    Guid Id,
    string ComparisonReference,
    Guid ProfileId,
    string ProfileCode,
    string ComparisonType,
    Guid? MaterialId,
    string? CustomerReference,
    string ComparisonOutcome,
    string EvidenceStatus,
    string AggregationMethod,
    int CandidateAssessmentCount,
    int ComparableAssessmentCount,
    int ExcludedAssessmentCount,
    decimal? ReferenceMedianContributionMarginRatio,
    decimal? CurrentMedianContributionMarginRatio,
    decimal? ContributionMarginDelta,
    DateTimeOffset? ReferenceWindowStartUtc,
    DateTimeOffset? ReferenceWindowEndUtc,
    DateTimeOffset CurrentWindowStartUtc,
    DateTimeOffset CurrentWindowEndUtc,
    string? LeadingDriverCode,
    string RuleSetVersion,
    DateTimeOffset EvaluatedAtUtc,
    DateTimeOffset CreatedAtUtc,
    bool IsPilotData);

public sealed record CommercialProfitabilityComparisonResultReadModel(
    Guid Id,
    string ResultDimension,
    string CanonicalResultReference,
    string? CustomerReference,
    Guid? MaterialId,
    string? ComponentKind,
    int CandidateAssessmentCount,
    int ComparableAssessmentCount,
    int ExcludedAssessmentCount,
    int CompleteCoverageCount,
    int PartialCoverageCount,
    int NegativeContributionCount,
    int BelowThresholdCount,
    int? ReferenceComparableAssessmentCount,
    int? CurrentComparableAssessmentCount,
    int? ReferenceExcludedAssessmentCount,
    int? CurrentExcludedAssessmentCount,
    decimal? ReferenceMedianContributionMarginRatio,
    decimal? CurrentMedianContributionMarginRatio,
    decimal? ContributionMarginDelta,
    decimal? ReferenceMedianUnitContribution,
    decimal? CurrentMedianUnitContribution,
    decimal? ReferenceMedianUnitCost,
    decimal? CurrentMedianUnitCost,
    decimal? SignedUnitCostChange,
    decimal? SignedUnitCostChangeRatio,
    decimal? ReferenceMedianCostShare,
    decimal? CurrentMedianCostShare,
    decimal? CostShareDelta,
    string PositionState,
    int? ContributionRank,
    bool IsTied,
    string EvidenceStatus,
    int SortOrder);

public sealed record CommercialProfitabilityComparisonDriverReadModel(
    Guid Id,
    Guid? ComparisonResultId,
    string ReasonCode,
    string Severity,
    decimal? ActualValue,
    decimal? ReferenceValue,
    decimal? ThresholdValue,
    string? Unit,
    decimal? NormalizedVariance,
    int AffectedAssessmentCount,
    string SupportingText,
    int SortOrder,
    bool IsLeadingDriver);

public sealed record CommercialProfitabilityComparisonDetailReadModel(
    Guid Id,
    string ComparisonReference,
    Guid ProfileId,
    string ProfileCode,
    string ProfileVersion,
    string ComparisonType,
    Guid? MaterialId,
    string? CustomerReference,
    string AggregationMethod,
    int CandidateAssessmentCount,
    int ComparableAssessmentCount,
    int ExcludedAssessmentCount,
    int CompleteCoverageCount,
    int PartialCoverageCount,
    int InsufficientDataCount,
    int SourceConflictCount,
    int NegativeContributionCount,
    int BelowThresholdCount,
    int? ReferenceCandidateAssessmentCount,
    int? ReferenceComparableAssessmentCount,
    int? ReferenceExcludedAssessmentCount,
    int? ReferenceCompleteCoverageCount,
    int? ReferencePartialCoverageCount,
    int? ReferenceInsufficientDataCount,
    int? ReferenceSourceConflictCount,
    int? ReferenceNegativeContributionCount,
    int? ReferenceBelowThresholdCount,
    int? CurrentCandidateAssessmentCount,
    int? CurrentComparableAssessmentCount,
    int? CurrentExcludedAssessmentCount,
    int? CurrentCompleteCoverageCount,
    int? CurrentPartialCoverageCount,
    int? CurrentInsufficientDataCount,
    int? CurrentSourceConflictCount,
    int? CurrentNegativeContributionCount,
    int? CurrentBelowThresholdCount,
    decimal? ReferenceMedianContributionMarginRatio,
    decimal? CurrentMedianContributionMarginRatio,
    decimal? ContributionMarginDelta,
    decimal? ReferenceMedianUnitContribution,
    decimal? CurrentMedianUnitContribution,
    DateTimeOffset? ReferenceWindowStartUtc,
    DateTimeOffset? ReferenceWindowEndUtc,
    DateTimeOffset CurrentWindowStartUtc,
    DateTimeOffset CurrentWindowEndUtc,
    string EvidenceStatus,
    string ComparisonOutcome,
    string? LeadingDriverCode,
    string RuleSetVersion,
    string SourceProvenance,
    DateTimeOffset EvaluatedAtUtc,
    DateTimeOffset CreatedAtUtc,
    IReadOnlyList<CommercialProfitabilityComparisonResultReadModel> Results,
    IReadOnlyList<CommercialProfitabilityComparisonDriverReadModel> Drivers,
    bool IsPilotData);

public sealed record EvaluateCommercialProfitabilityComparisonCommand(
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

public sealed record CommercialProfitabilityComparisonListFilters(
    string? ComparisonType,
    Guid? ProfileId,
    Guid? MaterialId,
    string? CustomerReference,
    string? Outcome,
    string? EvidenceStatus,
    DateTimeOffset? WindowFromUtc,
    DateTimeOffset? WindowToUtc,
    int Page,
    int PageSize);

public sealed class CommercialProfitabilityWriteResult
{
    public bool Succeeded { get; init; }
    public Guid? EntityId { get; init; }
    public uint Version { get; init; }
    public bool IdempotentReplay { get; init; }
    public string? FailureCode { get; init; }
    public string? FailureReason { get; init; }
    public CommercialProfitabilityAssessmentDetailReadModel? Assessment { get; init; }
    public CommercialProfitabilityComparisonDetailReadModel? Comparison { get; init; }

    public static CommercialProfitabilityWriteResult Success(
        Guid entityId,
        uint version,
        bool idempotentReplay = false,
        CommercialProfitabilityAssessmentDetailReadModel? assessment = null,
        CommercialProfitabilityComparisonDetailReadModel? comparison = null) =>
        new()
        {
            Succeeded = true,
            EntityId = entityId,
            Version = version,
            IdempotentReplay = idempotentReplay,
            Assessment = assessment,
            Comparison = comparison,
        };

    public static CommercialProfitabilityWriteResult Fail(string code, string reason) =>
        new()
        {
            Succeeded = false,
            FailureCode = code,
            FailureReason = reason,
        };
}

public interface ICommercialProfitabilityRepository
{
    Task<CommercialProfitabilityWorkspaceReadModel> GetWorkspaceAsync(
        CommercialIntelligenceOperationContext context,
        bool canManage,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CommercialProfitabilityProfileReadModel>> ListProfilesAsync(
        CommercialIntelligenceOperationContext context,
        CancellationToken cancellationToken = default);

    Task<CommercialProfitabilityProfileReadModel?> GetProfileByIdAsync(
        CommercialIntelligenceOperationContext context,
        Guid profileId,
        CancellationToken cancellationToken = default);

    Task<CommercialProfitabilityWriteResult> CreateProfileAsync(
        CommercialIntelligenceOperationContext context,
        CreateCommercialProfitabilityProfileCommand command,
        CancellationToken cancellationToken = default);

    Task<CommercialProfitabilityWriteResult> UpdateProfileAsync(
        CommercialIntelligenceOperationContext context,
        UpdateCommercialProfitabilityProfileCommand command,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CommercialProfitabilityAssessmentSummaryReadModel>> ListAssessmentsAsync(
        CommercialIntelligenceOperationContext context,
        CommercialProfitabilityAssessmentListFilters filters,
        CancellationToken cancellationToken = default);

    Task<CommercialProfitabilityAssessmentDetailReadModel?> GetAssessmentByIdAsync(
        CommercialIntelligenceOperationContext context,
        Guid assessmentId,
        CancellationToken cancellationToken = default);

    Task<CommercialProfitabilityWriteResult> EvaluateAssessmentAsync(
        CommercialIntelligenceOperationContext context,
        EvaluateCommercialProfitabilityAssessmentCommand command,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CommercialProfitabilityComparisonSummaryReadModel>> ListComparisonsAsync(
        CommercialIntelligenceOperationContext context,
        CommercialProfitabilityComparisonListFilters filters,
        CancellationToken cancellationToken = default);

    Task<CommercialProfitabilityComparisonDetailReadModel?> GetComparisonByIdAsync(
        CommercialIntelligenceOperationContext context,
        Guid comparisonRunId,
        CancellationToken cancellationToken = default);

    Task<CommercialProfitabilityWriteResult> EvaluateComparisonAsync(
        CommercialIntelligenceOperationContext context,
        EvaluateCommercialProfitabilityComparisonCommand command,
        CancellationToken cancellationToken = default);
}
