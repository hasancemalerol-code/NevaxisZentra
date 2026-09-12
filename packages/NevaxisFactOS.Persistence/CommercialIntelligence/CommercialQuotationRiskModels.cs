namespace NevaxisFactOS.Persistence.CommercialIntelligence;

public sealed record CommercialQuotationRiskProfileReadModel(
    Guid Id,
    string ProfileCode,
    string Name,
    string Status,
    int ReferenceWindowDurationDays,
    int MinimumComparableObservations,
    decimal MaximumNegativePriceVarianceRatio,
    decimal MaximumPositivePriceVarianceRatio,
    int MaximumPaymentTermDays,
    int ExpiryWarningDays,
    int MaximumEvidenceAgeDays,
    decimal MaximumRiskIncidenceDelta,
    string AllowedEvidenceStatuses,
    string AllowedSalesEvidenceKinds,
    string RequiredTaxInclusion,
    string RequiredDiscountInclusion,
    string RequiredFreightInclusion,
    string RequiredGrossNetBasis,
    string RuleSetVersion,
    string SourceProvenance,
    uint Version,
    bool IsPilotData);

public sealed record CommercialQuotationRiskAssessmentSummaryReadModel(
    Guid Id,
    string AssessmentReference,
    Guid ProfileId,
    string ProfileCode,
    Guid EvidenceObservationId,
    string QuotationReference,
    string CustomerReference,
    Guid MaterialId,
    string Outcome,
    decimal QuotedComparableUnitPrice,
    decimal? ReferenceMedianUnitPrice,
    decimal? SignedPriceVarianceRatio,
    int CandidateObservationCount,
    int ComparableObservationCount,
    int ExcludedObservationCount,
    int PaymentTermDays,
    int? RemainingValidityDays,
    string CurrencyCode,
    string UnitOfMeasure,
    DateTimeOffset ReferenceWindowStartUtc,
    DateTimeOffset ReferenceWindowEndUtc,
    string? LeadingDriverCode,
    string RuleSetVersion,
    DateTimeOffset EvaluatedAtUtc,
    DateTimeOffset CreatedAtUtc,
    bool IsPilotData);

public sealed record CommercialQuotationRiskAssessmentDriverReadModel(
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

public sealed record CommercialQuotationRiskAssessmentDetailReadModel(
    Guid Id,
    string AssessmentReference,
    Guid ProfileId,
    string ProfileCode,
    string ProfileVersion,
    Guid EvidenceObservationId,
    string QuotationReference,
    string CustomerReference,
    Guid MaterialId,
    DateTimeOffset ReferenceWindowStartUtc,
    DateTimeOffset ReferenceWindowEndUtc,
    decimal? ReferenceMedianUnitPrice,
    decimal QuotedComparableUnitPrice,
    decimal? SignedPriceVarianceRatio,
    int CandidateObservationCount,
    int ComparableObservationCount,
    int ExcludedObservationCount,
    int PaymentTermDays,
    int? RemainingValidityDays,
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
    IReadOnlyList<CommercialQuotationRiskAssessmentDriverReadModel> Drivers,
    bool IsPilotData);

public sealed record CommercialQuotationRiskWorkspaceReadModel(
    string RuleSetVersion,
    string ProvenanceDisclaimer,
    int DefaultMinimumComparableObservations,
    decimal DefaultMaximumNegativePriceVarianceRatio,
    decimal DefaultMaximumPositivePriceVarianceRatio,
    int DefaultMaximumPaymentTermDays,
    int DefaultExpiryWarningDays,
    bool RealQuotationMappingComplete,
    bool CanRead,
    bool CanManage);

public sealed record CreateCommercialQuotationRiskProfileCommand(
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

public sealed record UpdateCommercialQuotationRiskProfileCommand(
    Guid ProfileId,
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

public sealed record EvaluateCommercialQuotationRiskAssessmentCommand(
    Guid ProfileId,
    uint ExpectedProfileVersion,
    Guid EvidenceObservationId,
    DateTimeOffset ReferenceWindowStartUtc,
    DateTimeOffset ReferenceWindowEndUtc,
    string SourceProvenance,
    string IdempotencyKey);

public sealed record CommercialQuotationRiskAssessmentListFilters(
    Guid? MaterialId,
    string? Outcome,
    int Page,
    int PageSize);

public sealed record CommercialQuotationRiskComparisonSummaryReadModel(
    Guid Id,
    string ComparisonReference,
    Guid ProfileId,
    string ProfileCode,
    string ComparisonType,
    Guid? MaterialId,
    string? CustomerReference,
    string ComparisonOutcome,
    string EvidenceStatus,
    int CandidateAssessmentCount,
    int ComparableAssessmentCount,
    int ExcludedAssessmentCount,
    decimal? ReferenceRiskIncidence,
    decimal? CurrentRiskIncidence,
    decimal? RiskIncidenceDelta,
    DateTimeOffset? ReferenceWindowStartUtc,
    DateTimeOffset? ReferenceWindowEndUtc,
    DateTimeOffset CurrentWindowStartUtc,
    DateTimeOffset CurrentWindowEndUtc,
    string? LeadingDriverCode,
    string RuleSetVersion,
    DateTimeOffset EvaluatedAtUtc,
    DateTimeOffset CreatedAtUtc,
    bool IsPilotData);

public sealed record CommercialQuotationRiskComparisonResultReadModel(
    Guid Id,
    string CustomerReference,
    int CandidateAssessmentCount,
    int ComparableAssessmentCount,
    int ExcludedAssessmentCount,
    int RiskBearingAssessmentCount,
    decimal? RiskIncidenceFraction,
    int PriceRiskCount,
    int ExpiryRiskCount,
    int PaymentTermRiskCount,
    string PositionState,
    int? RiskRank,
    bool IsTied,
    string EvidenceStatus);

public sealed record CommercialQuotationRiskComparisonDriverReadModel(
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

public sealed record CommercialQuotationRiskComparisonDetailReadModel(
    Guid Id,
    string ComparisonReference,
    Guid ProfileId,
    string ProfileCode,
    string ProfileVersion,
    string ComparisonType,
    Guid? MaterialId,
    string? CustomerReference,
    int CandidateAssessmentCount,
    int ComparableAssessmentCount,
    int ExcludedAssessmentCount,
    int RiskBearingAssessmentCount,
    int InsufficientDataCount,
    int SourceConflictCount,
    int PriceRiskCount,
    int ExpiryRiskCount,
    int PaymentTermRiskCount,
    int? ReferenceCandidateAssessmentCount,
    int? ReferenceComparableAssessmentCount,
    int? ReferenceExcludedAssessmentCount,
    int? ReferenceRiskBearingAssessmentCount,
    int? CurrentCandidateAssessmentCount,
    int? CurrentComparableAssessmentCount,
    int? CurrentExcludedAssessmentCount,
    int? CurrentRiskBearingAssessmentCount,
    decimal? ReferenceRiskIncidence,
    decimal? CurrentRiskIncidence,
    decimal? RiskIncidenceDelta,
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
    IReadOnlyList<CommercialQuotationRiskComparisonResultReadModel> CustomerResults,
    IReadOnlyList<CommercialQuotationRiskComparisonDriverReadModel> Drivers,
    bool IsPilotData);

public sealed record EvaluateCommercialQuotationRiskComparisonCommand(
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

public sealed record CommercialQuotationRiskComparisonListFilters(
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

public sealed class CommercialQuotationRiskWriteResult
{
    public bool Succeeded { get; init; }
    public Guid? EntityId { get; init; }
    public uint Version { get; init; }
    public bool IdempotentReplay { get; init; }
    public string? FailureCode { get; init; }
    public string? FailureReason { get; init; }
    public CommercialQuotationRiskAssessmentDetailReadModel? Assessment { get; init; }
    public CommercialQuotationRiskComparisonDetailReadModel? Comparison { get; init; }

    public static CommercialQuotationRiskWriteResult Success(
        Guid entityId,
        uint version,
        bool idempotentReplay = false,
        CommercialQuotationRiskAssessmentDetailReadModel? assessment = null,
        CommercialQuotationRiskComparisonDetailReadModel? comparison = null) =>
        new()
        {
            Succeeded = true,
            EntityId = entityId,
            Version = version,
            IdempotentReplay = idempotentReplay,
            Assessment = assessment,
            Comparison = comparison,
        };

    public static CommercialQuotationRiskWriteResult Fail(string code, string reason) =>
        new()
        {
            Succeeded = false,
            FailureCode = code,
            FailureReason = reason,
        };
}

public interface ICommercialQuotationRiskRepository
{
    Task<CommercialQuotationRiskWorkspaceReadModel> GetWorkspaceAsync(
        CommercialIntelligenceOperationContext context,
        bool canManage,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CommercialQuotationRiskProfileReadModel>> ListProfilesAsync(
        CommercialIntelligenceOperationContext context,
        CancellationToken cancellationToken = default);

    Task<CommercialQuotationRiskProfileReadModel?> GetProfileByIdAsync(
        CommercialIntelligenceOperationContext context,
        Guid profileId,
        CancellationToken cancellationToken = default);

    Task<CommercialQuotationRiskWriteResult> CreateProfileAsync(
        CommercialIntelligenceOperationContext context,
        CreateCommercialQuotationRiskProfileCommand command,
        CancellationToken cancellationToken = default);

    Task<CommercialQuotationRiskWriteResult> UpdateProfileAsync(
        CommercialIntelligenceOperationContext context,
        UpdateCommercialQuotationRiskProfileCommand command,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CommercialQuotationRiskAssessmentSummaryReadModel>> ListAssessmentsAsync(
        CommercialIntelligenceOperationContext context,
        CommercialQuotationRiskAssessmentListFilters filters,
        CancellationToken cancellationToken = default);

    Task<CommercialQuotationRiskAssessmentDetailReadModel?> GetAssessmentByIdAsync(
        CommercialIntelligenceOperationContext context,
        Guid assessmentId,
        CancellationToken cancellationToken = default);

    Task<CommercialQuotationRiskWriteResult> EvaluateAssessmentAsync(
        CommercialIntelligenceOperationContext context,
        EvaluateCommercialQuotationRiskAssessmentCommand command,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CommercialQuotationRiskComparisonSummaryReadModel>> ListComparisonsAsync(
        CommercialIntelligenceOperationContext context,
        CommercialQuotationRiskComparisonListFilters filters,
        CancellationToken cancellationToken = default);

    Task<CommercialQuotationRiskComparisonDetailReadModel?> GetComparisonByIdAsync(
        CommercialIntelligenceOperationContext context,
        Guid comparisonRunId,
        CancellationToken cancellationToken = default);

    Task<CommercialQuotationRiskWriteResult> EvaluateComparisonAsync(
        CommercialIntelligenceOperationContext context,
        EvaluateCommercialQuotationRiskComparisonCommand command,
        CancellationToken cancellationToken = default);
}
