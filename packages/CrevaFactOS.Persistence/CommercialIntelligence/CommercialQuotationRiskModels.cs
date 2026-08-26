namespace CrevaFactOS.Persistence.CommercialIntelligence;

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

public sealed class CommercialQuotationRiskWriteResult
{
    public bool Succeeded { get; init; }
    public Guid? EntityId { get; init; }
    public uint Version { get; init; }
    public bool IdempotentReplay { get; init; }
    public string? FailureCode { get; init; }
    public string? FailureReason { get; init; }
    public CommercialQuotationRiskAssessmentDetailReadModel? Assessment { get; init; }

    public static CommercialQuotationRiskWriteResult Success(
        Guid entityId,
        uint version,
        bool idempotentReplay = false,
        CommercialQuotationRiskAssessmentDetailReadModel? assessment = null) =>
        new()
        {
            Succeeded = true,
            EntityId = entityId,
            Version = version,
            IdempotentReplay = idempotentReplay,
            Assessment = assessment,
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
}
