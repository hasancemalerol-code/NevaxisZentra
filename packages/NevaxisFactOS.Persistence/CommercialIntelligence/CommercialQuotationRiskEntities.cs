namespace NevaxisFactOS.Persistence.CommercialIntelligence;

public sealed class CommercialQuotationRiskProfile
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public string ProfileCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = CommercialIntelligenceProfileStatuses.Draft;
    public int ReferenceWindowDurationDays { get; set; } = 90;
    public int MinimumComparableObservations { get; set; } = CommercialIntelligenceConstants.DefaultMinimumComparableObservations;
    public decimal MaximumNegativePriceVarianceRatio { get; set; } =
        CommercialIntelligenceConstants.DefaultQuotationRiskMaximumNegativeVarianceRatio;
    public decimal MaximumPositivePriceVarianceRatio { get; set; } =
        CommercialIntelligenceConstants.DefaultQuotationRiskMaximumPositiveVarianceRatio;
    public int MaximumPaymentTermDays { get; set; } =
        CommercialIntelligenceConstants.DefaultQuotationRiskMaximumPaymentTermDays;
    public int ExpiryWarningDays { get; set; } =
        CommercialIntelligenceConstants.DefaultQuotationRiskExpiryWarningDays;
    public int MaximumEvidenceAgeDays { get; set; } =
        CommercialIntelligenceConstants.DefaultQuotationRiskMaximumEvidenceAgeDays;
    public decimal MaximumRiskIncidenceDelta { get; set; } =
        CommercialIntelligenceConstants.DefaultQuotationRiskMaximumRiskIncidenceDelta;
    public string AllowedEvidenceStatuses { get; set; } = string.Empty;
    public string AllowedSalesEvidenceKinds { get; set; } = string.Empty;
    public string RequiredTaxInclusion { get; set; } = CommercialIntelligencePriceBasisInclusionStates.Excluded;
    public string RequiredDiscountInclusion { get; set; } = CommercialIntelligencePriceBasisInclusionStates.Excluded;
    public string RequiredFreightInclusion { get; set; } = CommercialIntelligencePriceBasisInclusionStates.Excluded;
    public string RequiredGrossNetBasis { get; set; } = CommercialSalesPriceGrossNetBasis.Net;
    public string RuleSetVersion { get; set; } = CommercialIntelligenceConstants.QuotationRiskRuleSetVersion;
    public string SourceProvenance { get; set; } = string.Empty;
    public string? CreateIdempotencyKey { get; set; }
    public string? CreateIdempotencyPayloadFingerprint { get; set; }
    public uint Version { get; set; } = 1;
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public string UpdatedBy { get; set; } = string.Empty;
}

public sealed class CommercialQuotationRiskEvidenceObservation
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public string QuotationReference { get; set; } = string.Empty;
    public string CustomerReference { get; set; } = string.Empty;
    public Guid MaterialId { get; set; }
    public string SourceKind { get; set; } = string.Empty;
    public string EvidenceStatus { get; set; } = CommercialQuotationRiskEvidenceStatuses.Simulated;
    public DateTimeOffset QuotedAtUtc { get; set; }
    public DateTimeOffset? ExpiresAtUtc { get; set; }
    public decimal QuotedUnitPrice { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public string UnitOfMeasure { get; set; } = string.Empty;
    public string GrossNetBasis { get; set; } = CommercialSalesPriceGrossNetBasis.Net;
    public string TaxBasis { get; set; } = CommercialIntelligencePriceBasisInclusionStates.Excluded;
    public string DiscountBasis { get; set; } = CommercialIntelligencePriceBasisInclusionStates.Excluded;
    public string FreightBasis { get; set; } = CommercialIntelligencePriceBasisInclusionStates.Excluded;
    public int PaymentTermDays { get; set; }
    public string SourceReference { get; set; } = string.Empty;
    public string SourceProvenance { get; set; } = string.Empty;
    public string SourceFingerprint { get; set; } = string.Empty;
    public string? IngestIdempotencyKey { get; set; }
    public string? IngestIdempotencyPayloadFingerprint { get; set; }
    public DateTimeOffset ReceivedAtUtc { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
}

public sealed class CommercialQuotationRiskAssessment
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public string AssessmentReference { get; set; } = string.Empty;
    public Guid ProfileId { get; set; }
    public string ProfileVersion { get; set; } = string.Empty;
    public Guid EvidenceObservationId { get; set; }
    public string QuotationReference { get; set; } = string.Empty;
    public string CustomerReference { get; set; } = string.Empty;
    public Guid MaterialId { get; set; }
    public DateTimeOffset ReferenceWindowStartUtc { get; set; }
    public DateTimeOffset ReferenceWindowEndUtc { get; set; }
    public decimal? ReferenceMedianUnitPrice { get; set; }
    public decimal QuotedComparableUnitPrice { get; set; }
    public decimal? SignedPriceVarianceRatio { get; set; }
    public int CandidateObservationCount { get; set; }
    public int ComparableObservationCount { get; set; }
    public int ExcludedObservationCount { get; set; }
    public int PaymentTermDays { get; set; }
    public int? RemainingValidityDays { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public string UnitOfMeasure { get; set; } = string.Empty;
    public string GrossNetBasis { get; set; } = string.Empty;
    public string TaxBasis { get; set; } = string.Empty;
    public string DiscountBasis { get; set; } = string.Empty;
    public string FreightBasis { get; set; } = string.Empty;
    public string EvidenceStatus { get; set; } = string.Empty;
    public string Outcome { get; set; } = string.Empty;
    public string? LeadingDriverCode { get; set; }
    public string RuleSetVersion { get; set; } = CommercialIntelligenceConstants.QuotationRiskRuleSetVersion;
    public string? IdempotencyKey { get; set; }
    public string? IdempotencyPayloadFingerprint { get; set; }
    public string SourceProvenance { get; set; } = string.Empty;
    public DateTimeOffset EvaluatedAtUtc { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public string CreatedBy { get; set; } = string.Empty;

    public CommercialQuotationRiskProfile? Profile { get; set; }
    public CommercialQuotationRiskEvidenceObservation? EvidenceObservation { get; set; }
    public ICollection<CommercialQuotationRiskAssessmentDriver> Drivers { get; set; } = [];
}

public sealed class CommercialQuotationRiskAssessmentDriver
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid AssessmentId { get; set; }
    public string ReasonCode { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public decimal? ActualValue { get; set; }
    public decimal? ThresholdValue { get; set; }
    public string? Unit { get; set; }
    public decimal? NormalizedVariance { get; set; }
    public int AffectedObservationCount { get; set; }
    public string SupportingText { get; set; } = string.Empty;
    public string SourceProvenance { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public bool IsLeadingDriver { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }

    public CommercialQuotationRiskAssessment? Assessment { get; set; }
}

public sealed class CommercialQuotationRiskComparisonRun
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public string ComparisonReference { get; set; } = string.Empty;
    public Guid ProfileId { get; set; }
    public string ProfileVersion { get; set; } = string.Empty;
    public string ComparisonType { get; set; } = string.Empty;
    public Guid? MaterialId { get; set; }
    public string? CustomerReference { get; set; }
    public DateTimeOffset? ReferenceWindowStartUtc { get; set; }
    public DateTimeOffset? ReferenceWindowEndUtc { get; set; }
    public DateTimeOffset CurrentWindowStartUtc { get; set; }
    public DateTimeOffset CurrentWindowEndUtc { get; set; }
    public string EvidenceStatus { get; set; } = string.Empty;
    public string ComparisonOutcome { get; set; } = string.Empty;
    public int CandidateAssessmentCount { get; set; }
    public int ComparableAssessmentCount { get; set; }
    public int ExcludedAssessmentCount { get; set; }
    public int RiskBearingAssessmentCount { get; set; }
    public int InsufficientDataCount { get; set; }
    public int SourceConflictCount { get; set; }
    public int PriceRiskCount { get; set; }
    public int ExpiryRiskCount { get; set; }
    public int PaymentTermRiskCount { get; set; }
    public int? ReferenceCandidateAssessmentCount { get; set; }
    public int? ReferenceComparableAssessmentCount { get; set; }
    public int? ReferenceExcludedAssessmentCount { get; set; }
    public int? ReferenceRiskBearingAssessmentCount { get; set; }
    public int? CurrentCandidateAssessmentCount { get; set; }
    public int? CurrentComparableAssessmentCount { get; set; }
    public int? CurrentExcludedAssessmentCount { get; set; }
    public int? CurrentRiskBearingAssessmentCount { get; set; }
    public decimal? ReferenceRiskIncidence { get; set; }
    public decimal? CurrentRiskIncidence { get; set; }
    public decimal? RiskIncidenceDelta { get; set; }
    public string? LeadingDriverCode { get; set; }
    public string RuleSetVersion { get; set; } = CommercialIntelligenceConstants.QuotationRiskComparisonRuleSetVersion;
    public string? IdempotencyKey { get; set; }
    public string? IdempotencyPayloadFingerprint { get; set; }
    public string SourceProvenance { get; set; } = string.Empty;
    public DateTimeOffset EvaluatedAtUtc { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public string CreatedBy { get; set; } = string.Empty;

    public CommercialQuotationRiskProfile? Profile { get; set; }
    public ICollection<CommercialQuotationRiskComparisonResult> Results { get; set; } = [];
    public ICollection<CommercialQuotationRiskComparisonDriver> Drivers { get; set; } = [];
}

public sealed class CommercialQuotationRiskComparisonResult
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid ComparisonRunId { get; set; }
    public string CustomerReference { get; set; } = string.Empty;
    public int CandidateAssessmentCount { get; set; }
    public int ComparableAssessmentCount { get; set; }
    public int ExcludedAssessmentCount { get; set; }
    public int RiskBearingAssessmentCount { get; set; }
    public decimal? RiskIncidenceFraction { get; set; }
    public int PriceRiskCount { get; set; }
    public int ExpiryRiskCount { get; set; }
    public int PaymentTermRiskCount { get; set; }
    public string PositionState { get; set; } = string.Empty;
    public int? RiskRank { get; set; }
    public bool IsTied { get; set; }
    public string EvidenceStatus { get; set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; set; }

    public CommercialQuotationRiskComparisonRun? ComparisonRun { get; set; }
    public ICollection<CommercialQuotationRiskComparisonDriver> Drivers { get; set; } = [];
}

public sealed class CommercialQuotationRiskComparisonDriver
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid ComparisonRunId { get; set; }
    public Guid? ComparisonResultId { get; set; }
    public string ReasonCode { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public decimal? ActualValue { get; set; }
    public decimal? ReferenceValue { get; set; }
    public decimal? ThresholdValue { get; set; }
    public string? Unit { get; set; }
    public decimal? NormalizedVariance { get; set; }
    public int AffectedAssessmentCount { get; set; }
    public string SupportingText { get; set; } = string.Empty;
    public string SourceProvenance { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public bool IsLeadingDriver { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }

    public CommercialQuotationRiskComparisonRun? ComparisonRun { get; set; }
    public CommercialQuotationRiskComparisonResult? ComparisonResult { get; set; }
}
