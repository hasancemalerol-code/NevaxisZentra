namespace CrevaFactOS.Persistence.CommercialIntelligence;

public sealed record CommercialSalesPriceProfileReadModel(
    Guid Id,
    string ProfileCode,
    string DisplayName,
    string Status,
    string ComparisonDimension,
    int ReferenceWindowDurationDays,
    int MinimumComparableObservations,
    decimal MaximumPositiveVarianceRatio,
    decimal? CustomerSpreadThreshold,
    string AllowedEvidenceStatuses,
    string AllowedSourceEvidenceKinds,
    string RequiredTaxInclusion,
    string RequiredDiscountInclusion,
    string RequiredFreightInclusion,
    string RequiredGrossNetBasis,
    string ReferenceCenter,
    string RuleSetVersion,
    string SourceProvenance,
    uint Version,
    bool IsPilotData);

public sealed record CommercialSalesPriceAssessmentSummaryReadModel(
    Guid Id,
    string AssessmentReference,
    Guid ProfileId,
    string ProfileCode,
    Guid MaterialId,
    string? CustomerReference,
    string SourceEvidenceKind,
    string ComparisonDimension,
    string AssessmentOutcome,
    decimal CurrentUnitPrice,
    decimal? ReferenceMedianUnitPrice,
    decimal? VarianceRatio,
    int CandidateObservationCount,
    int ComparableObservationCount,
    int ExcludedObservationCount,
    string CurrencyCode,
    string NormalizedUnit,
    DateTimeOffset WindowStartUtc,
    DateTimeOffset WindowEndUtc,
    string? LeadingDriverCode,
    string RuleSetVersion,
    DateTimeOffset EvaluatedAtUtc,
    DateTimeOffset CreatedAtUtc,
    bool IsPilotData);

public sealed record CommercialSalesPriceAssessmentDriverReadModel(
    Guid Id,
    string ReasonCode,
    string Severity,
    decimal? ActualValue,
    decimal? ThresholdValue,
    string? Unit,
    decimal? NormalizedVariance,
    int AffectedObservationCount,
    string SupportingText,
    int SortOrder,
    bool IsLeadingDriver);

public sealed record CommercialSalesPriceAssessmentDetailReadModel(
    Guid Id,
    string AssessmentReference,
    Guid ProfileId,
    string ProfileCode,
    Guid MaterialId,
    string? CustomerReference,
    string SourceEvidenceKind,
    string ComparisonDimension,
    decimal CurrentUnitPrice,
    decimal? ReferenceMedianUnitPrice,
    decimal? AbsoluteVariance,
    decimal? VarianceRatio,
    int CandidateObservationCount,
    int ComparableObservationCount,
    int ExcludedObservationCount,
    string CurrencyCode,
    string NormalizedUnit,
    DateTimeOffset WindowStartUtc,
    DateTimeOffset WindowEndUtc,
    string PriceBasisGrossNet,
    string PriceBasisTaxInclusion,
    string PriceBasisDiscountInclusion,
    string PriceBasisFreightInclusion,
    string EvidenceStatus,
    string AssessmentOutcome,
    string? LeadingDriverCode,
    string RuleSetVersion,
    string SourceProvenance,
    DateTimeOffset EvaluatedAtUtc,
    DateTimeOffset CreatedAtUtc,
    IReadOnlyList<CommercialSalesPriceAssessmentDriverReadModel> Drivers,
    bool IsPilotData);

public sealed record CommercialSalesPriceWorkspaceReadModel(
    string RuleSetVersion,
    string ProvenanceDisclaimer,
    string ReferenceCenter,
    int DefaultMinimumComparableObservations,
    bool RealTransactionMappingComplete,
    bool CanRead,
    bool CanManage);

public sealed record CreateCommercialSalesPriceProfileCommand(
    string ProfileCode,
    string DisplayName,
    string ComparisonDimension,
    int ReferenceWindowDurationDays,
    int MinimumComparableObservations,
    decimal MaximumPositiveVarianceRatio,
    decimal? CustomerSpreadThreshold,
    string AllowedEvidenceStatuses,
    string AllowedSourceEvidenceKinds,
    string RequiredTaxInclusion,
    string RequiredDiscountInclusion,
    string RequiredFreightInclusion,
    string RequiredGrossNetBasis,
    string SourceProvenance,
    string IdempotencyKey);

public sealed record UpdateCommercialSalesPriceProfileCommand(
    Guid ProfileId,
    uint ExpectedVersion,
    string DisplayName,
    string Status,
    string ComparisonDimension,
    int ReferenceWindowDurationDays,
    int MinimumComparableObservations,
    decimal MaximumPositiveVarianceRatio,
    decimal? CustomerSpreadThreshold,
    string AllowedEvidenceStatuses,
    string AllowedSourceEvidenceKinds,
    string RequiredTaxInclusion,
    string RequiredDiscountInclusion,
    string RequiredFreightInclusion,
    string RequiredGrossNetBasis,
    string SourceProvenance);

public sealed record EvaluateCommercialSalesPriceAssessmentCommand(
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

public sealed record CommercialSalesPriceAssessmentListFilters(
    Guid? MaterialId,
    string? AssessmentOutcome,
    int Page,
    int PageSize);

public sealed record CommercialSalesPriceComparisonSummaryReadModel(
    Guid Id,
    string ComparisonReference,
    Guid ProfileId,
    string ProfileCode,
    string ComparisonType,
    Guid MaterialId,
    string? CustomerReference,
    string ComparisonOutcome,
    decimal? CurrentMedianUnitPrice,
    decimal? ReferenceMedianUnitPrice,
    decimal? SignedVarianceRatio,
    decimal? CustomerSpreadRatio,
    int CandidateObservationCount,
    int ComparableObservationCount,
    int ExcludedObservationCount,
    string CurrencyCode,
    string NormalizedUnit,
    DateTimeOffset CurrentWindowStartUtc,
    DateTimeOffset CurrentWindowEndUtc,
    DateTimeOffset? ReferenceWindowStartUtc,
    DateTimeOffset? ReferenceWindowEndUtc,
    string? LeadingDriverCode,
    string RuleSetVersion,
    DateTimeOffset EvaluatedAtUtc,
    DateTimeOffset CreatedAtUtc,
    bool IsPilotData);

public sealed record CommercialSalesPriceComparisonResultReadModel(
    Guid Id,
    string? CustomerReference,
    int ComparableObservationCount,
    decimal? CustomerMedianUnitPrice,
    decimal? LowestComparableMedianUnitPrice,
    decimal? RelativePricePositionRatio,
    int PriceRank,
    bool IsTied,
    string PositionState,
    string EvidenceStatus);

public sealed record CommercialSalesPriceComparisonDriverReadModel(
    Guid Id,
    Guid? ComparisonResultId,
    string ReasonCode,
    string Severity,
    decimal? ActualValue,
    decimal? ReferenceValue,
    decimal? ThresholdValue,
    string? Unit,
    decimal? NormalizedVariance,
    int AffectedObservationCount,
    string SupportingText,
    int SortOrder,
    bool IsLeadingDriver);

public sealed record CommercialSalesPriceComparisonDetailReadModel(
    Guid Id,
    string ComparisonReference,
    Guid ProfileId,
    string ProfileCode,
    string ProfileVersion,
    string ComparisonType,
    Guid MaterialId,
    string? CustomerReference,
    string SourceEvidenceKind,
    decimal? CurrentMedianUnitPrice,
    decimal? ReferenceMedianUnitPrice,
    decimal? SignedVarianceRatio,
    decimal? CustomerSpreadRatio,
    int CandidateObservationCount,
    int ComparableObservationCount,
    int ExcludedObservationCount,
    string CurrencyCode,
    string NormalizedUnit,
    DateTimeOffset CurrentWindowStartUtc,
    DateTimeOffset CurrentWindowEndUtc,
    DateTimeOffset? ReferenceWindowStartUtc,
    DateTimeOffset? ReferenceWindowEndUtc,
    string PriceBasisGrossNet,
    string PriceBasisTaxInclusion,
    string PriceBasisDiscountInclusion,
    string PriceBasisFreightInclusion,
    string EvidenceStatus,
    string ComparisonOutcome,
    string? LeadingDriverCode,
    string RuleSetVersion,
    string SourceProvenance,
    DateTimeOffset EvaluatedAtUtc,
    DateTimeOffset CreatedAtUtc,
    IReadOnlyList<CommercialSalesPriceComparisonResultReadModel> CustomerResults,
    IReadOnlyList<CommercialSalesPriceComparisonDriverReadModel> Drivers,
    bool IsPilotData);

public sealed record EvaluateCommercialSalesPriceComparisonCommand(
    Guid ProfileId,
    uint ExpectedProfileVersion,
    string ComparisonType,
    Guid MaterialId,
    string? CustomerReference,
    string SourceEvidenceKind,
    DateTimeOffset CurrentWindowStartUtc,
    DateTimeOffset CurrentWindowEndUtc,
    DateTimeOffset? ReferenceWindowStartUtc,
    DateTimeOffset? ReferenceWindowEndUtc,
    string CurrencyCode,
    string NormalizedUnit,
    string PriceBasisGrossNet,
    string PriceBasisTaxInclusion,
    string PriceBasisDiscountInclusion,
    string PriceBasisFreightInclusion,
    string SourceProvenance,
    string IdempotencyKey);

public sealed record CommercialSalesPriceComparisonListFilters(
    string? ComparisonType,
    Guid? MaterialId,
    string? CustomerReference,
    string? Outcome,
    DateTimeOffset? WindowFromUtc,
    DateTimeOffset? WindowToUtc,
    int Page,
    int PageSize);

public sealed class CommercialSalesPriceWriteResult
{
    public bool Succeeded { get; init; }
    public Guid? EntityId { get; init; }
    public uint? Version { get; init; }
    public bool IdempotentReplay { get; init; }
    public string? FailureCode { get; init; }
    public string? FailureReason { get; init; }
    public CommercialSalesPriceAssessmentDetailReadModel? Assessment { get; init; }
    public CommercialSalesPriceComparisonDetailReadModel? Comparison { get; init; }

    public static CommercialSalesPriceWriteResult Success(
        Guid entityId,
        uint version,
        bool idempotentReplay = false,
        CommercialSalesPriceAssessmentDetailReadModel? assessment = null,
        CommercialSalesPriceComparisonDetailReadModel? comparison = null) =>
        new()
        {
            Succeeded = true,
            EntityId = entityId,
            Version = version,
            IdempotentReplay = idempotentReplay,
            Assessment = assessment,
            Comparison = comparison,
        };

    public static CommercialSalesPriceWriteResult Fail(string code, string reason) =>
        new()
        {
            Succeeded = false,
            FailureCode = code,
            FailureReason = reason,
        };
}

public interface ICommercialSalesPriceRepository
{
    Task<CommercialSalesPriceWorkspaceReadModel> GetWorkspaceAsync(
        CommercialIntelligenceOperationContext context,
        bool canManage,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CommercialSalesPriceProfileReadModel>> ListProfilesAsync(
        CommercialIntelligenceOperationContext context,
        CancellationToken cancellationToken = default);

    Task<CommercialSalesPriceProfileReadModel?> GetProfileByIdAsync(
        CommercialIntelligenceOperationContext context,
        Guid profileId,
        CancellationToken cancellationToken = default);

    Task<CommercialSalesPriceWriteResult> CreateProfileAsync(
        CommercialIntelligenceOperationContext context,
        CreateCommercialSalesPriceProfileCommand command,
        CancellationToken cancellationToken = default);

    Task<CommercialSalesPriceWriteResult> UpdateProfileAsync(
        CommercialIntelligenceOperationContext context,
        UpdateCommercialSalesPriceProfileCommand command,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CommercialSalesPriceAssessmentSummaryReadModel>> ListAssessmentsAsync(
        CommercialIntelligenceOperationContext context,
        CommercialSalesPriceAssessmentListFilters filters,
        CancellationToken cancellationToken = default);

    Task<CommercialSalesPriceAssessmentDetailReadModel?> GetAssessmentByIdAsync(
        CommercialIntelligenceOperationContext context,
        Guid assessmentId,
        CancellationToken cancellationToken = default);

    Task<CommercialSalesPriceWriteResult> EvaluateAssessmentAsync(
        CommercialIntelligenceOperationContext context,
        EvaluateCommercialSalesPriceAssessmentCommand command,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CommercialSalesPriceComparisonSummaryReadModel>> ListComparisonsAsync(
        CommercialIntelligenceOperationContext context,
        CommercialSalesPriceComparisonListFilters filters,
        CancellationToken cancellationToken = default);

    Task<CommercialSalesPriceComparisonDetailReadModel?> GetComparisonByIdAsync(
        CommercialIntelligenceOperationContext context,
        Guid comparisonRunId,
        CancellationToken cancellationToken = default);

    Task<CommercialSalesPriceWriteResult> EvaluateComparisonAsync(
        CommercialIntelligenceOperationContext context,
        EvaluateCommercialSalesPriceComparisonCommand command,
        CancellationToken cancellationToken = default);
}
