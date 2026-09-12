namespace NevaxisFactOS.Persistence.CommercialIntelligence;

public sealed class CommercialSalesPriceProfile
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public string ProfileCode { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Status { get; set; } = CommercialIntelligenceProfileStatuses.Draft;
    public string ComparisonDimension { get; set; } = CommercialSalesPriceComparisonDimensions.Material;
    public int ReferenceWindowDurationDays { get; set; } = 90;
    public int MinimumComparableObservations { get; set; } = CommercialIntelligenceConstants.DefaultMinimumComparableObservations;
    public decimal MaximumPositiveVarianceRatio { get; set; }
    public decimal? CustomerSpreadThreshold { get; set; }
    public string AllowedEvidenceStatuses { get; set; } = string.Empty;
    public string AllowedSourceEvidenceKinds { get; set; } = string.Empty;
    public string RequiredTaxInclusion { get; set; } = CommercialIntelligencePriceBasisInclusionStates.Excluded;
    public string RequiredDiscountInclusion { get; set; } = CommercialIntelligencePriceBasisInclusionStates.Excluded;
    public string RequiredFreightInclusion { get; set; } = CommercialIntelligencePriceBasisInclusionStates.Excluded;
    public string RequiredGrossNetBasis { get; set; } = CommercialSalesPriceGrossNetBasis.Net;
    public string ReferenceCenter { get; set; } = CommercialIntelligenceConstants.ReferenceCenterMedian;
    public string RuleSetVersion { get; set; } = CommercialIntelligenceConstants.SalesRuleSetVersion;
    public string SourceProvenance { get; set; } = string.Empty;
    public string? CreateIdempotencyKey { get; set; }
    public string? CreateIdempotencyPayloadFingerprint { get; set; }
    public uint Version { get; set; } = 1;
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public string UpdatedBy { get; set; } = string.Empty;
}

public sealed class CommercialSalesPriceEvidenceObservation
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid MaterialId { get; set; }
    public string? CustomerReference { get; set; }
    public string SourceEvidenceKind { get; set; } = string.Empty;
    public string SourceDocumentReference { get; set; } = string.Empty;
    public string SourceLineReference { get; set; } = string.Empty;
    public DateTimeOffset CommercialEventAtUtc { get; set; }
    public DateTimeOffset ReceivedAtUtc { get; set; }
    public decimal Quantity { get; set; }
    public string SourceUnit { get; set; } = string.Empty;
    public decimal? NormalizedQuantity { get; set; }
    public string? NormalizedUnit { get; set; }
    public decimal OriginalUnitPrice { get; set; }
    public decimal ComparableUnitPrice { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public string PriceBasisGrossNet { get; set; } = CommercialSalesPriceGrossNetBasis.Net;
    public string PriceBasisTaxInclusion { get; set; } = CommercialIntelligencePriceBasisInclusionStates.Excluded;
    public string PriceBasisDiscountInclusion { get; set; } = CommercialIntelligencePriceBasisInclusionStates.Excluded;
    public string PriceBasisFreightInclusion { get; set; } = CommercialIntelligencePriceBasisInclusionStates.Excluded;
    public bool IsCancelledOrReturned { get; set; }
    public string EvidenceStatus { get; set; } = CommercialIntelligenceEvidenceStatuses.Simulated;
    public string SourceFingerprint { get; set; } = string.Empty;
    public string SourceProvenance { get; set; } = string.Empty;
    public string ObservationSetCode { get; set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
}

public sealed class CommercialSalesPriceAssessment
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public string AssessmentReference { get; set; } = string.Empty;
    public Guid ProfileId { get; set; }
    public string ProfileVersion { get; set; } = string.Empty;
    public Guid MaterialId { get; set; }
    public string? CustomerReference { get; set; }
    public string SourceEvidenceKind { get; set; } = string.Empty;
    public string ComparisonDimension { get; set; } = string.Empty;
    public decimal CurrentUnitPrice { get; set; }
    public decimal? ReferenceMedianUnitPrice { get; set; }
    public decimal? AbsoluteVariance { get; set; }
    public decimal? VarianceRatio { get; set; }
    public int CandidateObservationCount { get; set; }
    public int ComparableObservationCount { get; set; }
    public int ExcludedObservationCount { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public string NormalizedUnit { get; set; } = string.Empty;
    public DateTimeOffset WindowStartUtc { get; set; }
    public DateTimeOffset WindowEndUtc { get; set; }
    public string PriceBasisGrossNet { get; set; } = string.Empty;
    public string PriceBasisTaxInclusion { get; set; } = string.Empty;
    public string PriceBasisDiscountInclusion { get; set; } = string.Empty;
    public string PriceBasisFreightInclusion { get; set; } = string.Empty;
    public string EvidenceStatus { get; set; } = string.Empty;
    public string AssessmentOutcome { get; set; } = string.Empty;
    public string? LeadingDriverCode { get; set; }
    public string RuleSetVersion { get; set; } = CommercialIntelligenceConstants.SalesRuleSetVersion;
    public string? IdempotencyKey { get; set; }
    public string? IdempotencyPayloadFingerprint { get; set; }
    public string SourceProvenance { get; set; } = string.Empty;
    public DateTimeOffset EvaluatedAtUtc { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public string CreatedBy { get; set; } = string.Empty;

    public CommercialSalesPriceProfile? Profile { get; set; }
    public ICollection<CommercialSalesPriceAssessmentDriver> Drivers { get; set; } = [];
}

public sealed class CommercialSalesPriceAssessmentDriver
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

    public CommercialSalesPriceAssessment? Assessment { get; set; }
}

public sealed class CommercialSalesPriceComparisonRun
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public string ComparisonReference { get; set; } = string.Empty;
    public Guid ProfileId { get; set; }
    public string ProfileVersion { get; set; } = string.Empty;
    public string ComparisonType { get; set; } = string.Empty;
    public Guid MaterialId { get; set; }
    public string? CustomerReference { get; set; }
    public string SourceEvidenceKind { get; set; } = string.Empty;
    public DateTimeOffset CurrentWindowStartUtc { get; set; }
    public DateTimeOffset CurrentWindowEndUtc { get; set; }
    public DateTimeOffset? ReferenceWindowStartUtc { get; set; }
    public DateTimeOffset? ReferenceWindowEndUtc { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public string NormalizedUnit { get; set; } = string.Empty;
    public string PriceBasisGrossNet { get; set; } = string.Empty;
    public string PriceBasisTaxInclusion { get; set; } = string.Empty;
    public string PriceBasisDiscountInclusion { get; set; } = string.Empty;
    public string PriceBasisFreightInclusion { get; set; } = string.Empty;
    public string EvidenceStatus { get; set; } = string.Empty;
    public string ComparisonOutcome { get; set; } = string.Empty;
    public decimal? CurrentMedianUnitPrice { get; set; }
    public decimal? ReferenceMedianUnitPrice { get; set; }
    public decimal? SignedVarianceRatio { get; set; }
    public decimal? CustomerSpreadRatio { get; set; }
    public int CandidateObservationCount { get; set; }
    public int ComparableObservationCount { get; set; }
    public int ExcludedObservationCount { get; set; }
    public string? LeadingDriverCode { get; set; }
    public string RuleSetVersion { get; set; } = CommercialIntelligenceConstants.SalesComparisonRuleSetVersion;
    public string? IdempotencyKey { get; set; }
    public string? IdempotencyPayloadFingerprint { get; set; }
    public string SourceProvenance { get; set; } = string.Empty;
    public DateTimeOffset EvaluatedAtUtc { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public string CreatedBy { get; set; } = string.Empty;

    public CommercialSalesPriceProfile? Profile { get; set; }
    public ICollection<CommercialSalesPriceComparisonResult> Results { get; set; } = [];
    public ICollection<CommercialSalesPriceComparisonDriver> Drivers { get; set; } = [];
}

public sealed class CommercialSalesPriceComparisonResult
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid ComparisonRunId { get; set; }
    public string? CustomerReference { get; set; }
    public int ComparableObservationCount { get; set; }
    public decimal? CustomerMedianUnitPrice { get; set; }
    public decimal? LowestComparableMedianUnitPrice { get; set; }
    public decimal? RelativePricePositionRatio { get; set; }
    public int PriceRank { get; set; }
    public bool IsTied { get; set; }
    public string PositionState { get; set; } = string.Empty;
    public string EvidenceStatus { get; set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; set; }

    public CommercialSalesPriceComparisonRun? ComparisonRun { get; set; }
    public ICollection<CommercialSalesPriceComparisonDriver> Drivers { get; set; } = [];
}

public sealed class CommercialSalesPriceComparisonDriver
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
    public int AffectedObservationCount { get; set; }
    public string SupportingText { get; set; } = string.Empty;
    public string SourceProvenance { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public bool IsLeadingDriver { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }

    public CommercialSalesPriceComparisonRun? ComparisonRun { get; set; }
    public CommercialSalesPriceComparisonResult? ComparisonResult { get; set; }
}
