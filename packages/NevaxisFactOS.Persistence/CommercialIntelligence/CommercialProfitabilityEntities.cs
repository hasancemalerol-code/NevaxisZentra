namespace NevaxisFactOS.Persistence.CommercialIntelligence;

public sealed class CommercialProfitabilityProfile
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public string ProfileCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = CommercialIntelligenceProfileStatuses.Draft;
    public decimal MinimumContributionMarginRatio { get; set; } =
        CommercialIntelligenceConstants.DefaultProfitabilityMinimumContributionMarginRatio;
    public int MinimumComparableAssessmentCountPerWindow { get; set; } =
        CommercialIntelligenceConstants.DefaultProfitabilityMinimumComparableAssessmentCountPerWindow;
    public decimal MaximumAdverseContributionMarginDelta { get; set; } =
        CommercialIntelligenceConstants.DefaultProfitabilityMaximumAdverseContributionMarginDelta;
    public decimal MinimumFavorableContributionMarginDelta { get; set; } =
        CommercialIntelligenceConstants.DefaultProfitabilityMinimumFavorableContributionMarginDelta;
    public decimal MaximumCostShareIncrease { get; set; } =
        CommercialIntelligenceConstants.DefaultProfitabilityMaximumCostShareIncrease;
    public int MaximumEvidenceAgeDays { get; set; } =
        CommercialIntelligenceConstants.DefaultProfitabilityMaximumEvidenceAgeDays;
    public string RequiredCostComponentKinds { get; set; } = string.Empty;
    public string RequiredCurrencyCode { get; set; } = "TRY";
    public string RequiredUnitOfMeasure { get; set; } = "EA";
    public string RequiredGrossNetBasis { get; set; } = CommercialSalesPriceGrossNetBasis.Net;
    public string RequiredTaxInclusion { get; set; } = CommercialIntelligencePriceBasisInclusionStates.Excluded;
    public string RequiredDiscountInclusion { get; set; } = CommercialIntelligencePriceBasisInclusionStates.Excluded;
    public string RequiredFreightInclusion { get; set; } = CommercialIntelligencePriceBasisInclusionStates.Excluded;
    public string AllowedEvidenceStatuses { get; set; } = string.Empty;
    public string RuleSetVersion { get; set; } = CommercialIntelligenceConstants.ProfitabilityRuleSetVersion;
    public string SourceProvenance { get; set; } = string.Empty;
    public string? CreateIdempotencyKey { get; set; }
    public string? CreateIdempotencyPayloadFingerprint { get; set; }
    public uint Version { get; set; } = 1;
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public string UpdatedBy { get; set; } = string.Empty;
}

public sealed class CommercialProfitabilityEvidenceObservation
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public string ObservationKind { get; set; } = CommercialProfitabilityObservationKinds.Sales;
    public string EvidenceSetReference { get; set; } = string.Empty;
    public string CustomerReference { get; set; } = string.Empty;
    public Guid MaterialId { get; set; }
    public string? OrderReference { get; set; }
    public string? QuotationReference { get; set; }
    public decimal? ComparableNetSalesUnitPrice { get; set; }
    public string? ComponentKind { get; set; }
    public decimal? UnitCost { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public string UnitOfMeasure { get; set; } = string.Empty;
    public string GrossNetBasis { get; set; } = CommercialSalesPriceGrossNetBasis.Net;
    public string TaxBasis { get; set; } = CommercialIntelligencePriceBasisInclusionStates.Excluded;
    public string DiscountBasis { get; set; } = CommercialIntelligencePriceBasisInclusionStates.Excluded;
    public string FreightBasis { get; set; } = CommercialIntelligencePriceBasisInclusionStates.Excluded;
    public string? CostBasis { get; set; }
    public string? AllocationBasis { get; set; }
    public string SourceKind { get; set; } = string.Empty;
    public string EvidenceStatus { get; set; } = CommercialProfitabilityEvidenceStatuses.Simulated;
    public DateTimeOffset EvidenceTimestampUtc { get; set; }
    public bool IsCancelledOrReturned { get; set; }
    public string SourceReference { get; set; } = string.Empty;
    public string SourceProvenance { get; set; } = string.Empty;
    public string SourceFingerprint { get; set; } = string.Empty;
    public string? IngestIdempotencyKey { get; set; }
    public string? IngestIdempotencyPayloadFingerprint { get; set; }
    public DateTimeOffset ReceivedAtUtc { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
}

public sealed class CommercialProfitabilityAssessment
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public string AssessmentReference { get; set; } = string.Empty;
    public Guid ProfileId { get; set; }
    public string ProfileVersion { get; set; } = string.Empty;
    public Guid SalesEvidenceObservationId { get; set; }
    public string CustomerReference { get; set; } = string.Empty;
    public Guid MaterialId { get; set; }
    public string? OrderReference { get; set; }
    public string? QuotationReference { get; set; }
    public string EvidenceSetReference { get; set; } = string.Empty;
    public decimal ComparableNetSalesUnitPrice { get; set; }
    public decimal TotalIncludedUnitCost { get; set; }
    public decimal UnitContribution { get; set; }
    public decimal? ContributionMarginRatio { get; set; }
    public decimal? MarkupRatio { get; set; }
    public int CandidateCostComponentCount { get; set; }
    public int IncludedCostComponentCount { get; set; }
    public int ExcludedCostComponentCount { get; set; }
    public int RequiredCostComponentCount { get; set; }
    public int AvailableRequiredCostComponentCount { get; set; }
    public int MissingRequiredCostComponentCount { get; set; }
    public string MissingRequiredComponentKinds { get; set; } = string.Empty;
    public string CostCoverageStatus { get; set; } = string.Empty;
    public string CurrencyCode { get; set; } = string.Empty;
    public string UnitOfMeasure { get; set; } = string.Empty;
    public string GrossNetBasis { get; set; } = string.Empty;
    public string TaxBasis { get; set; } = string.Empty;
    public string DiscountBasis { get; set; } = string.Empty;
    public string FreightBasis { get; set; } = string.Empty;
    public string EvidenceStatus { get; set; } = string.Empty;
    public string Outcome { get; set; } = string.Empty;
    public string? LeadingDriverCode { get; set; }
    public string RuleSetVersion { get; set; } = CommercialIntelligenceConstants.ProfitabilityRuleSetVersion;
    public string? IdempotencyKey { get; set; }
    public string? IdempotencyPayloadFingerprint { get; set; }
    public string SourceProvenance { get; set; } = string.Empty;
    public DateTimeOffset EvaluatedAtUtc { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public string CreatedBy { get; set; } = string.Empty;

    public CommercialProfitabilityProfile? Profile { get; set; }
    public CommercialProfitabilityEvidenceObservation? SalesEvidenceObservation { get; set; }
    public ICollection<CommercialProfitabilityAssessmentCostComponent> CostComponents { get; set; } = [];
    public ICollection<CommercialProfitabilityAssessmentDriver> Drivers { get; set; } = [];
}

public sealed class CommercialProfitabilityAssessmentCostComponent
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid AssessmentId { get; set; }
    public Guid? SourceEvidenceObservationId { get; set; }
    public string ComponentKind { get; set; } = string.Empty;
    public decimal UnitCost { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public string UnitOfMeasure { get; set; } = string.Empty;
    public string CostBasis { get; set; } = string.Empty;
    public string? AllocationBasis { get; set; }
    public DateTimeOffset EvidenceTimestampUtc { get; set; }
    public string SourceKind { get; set; } = string.Empty;
    public string SourceReference { get; set; } = string.Empty;
    public string EvidenceStatus { get; set; } = string.Empty;
    public string InclusionStatus { get; set; } = string.Empty;
    public string? ExclusionReason { get; set; }
    public string SourceProvenance { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }

    public CommercialProfitabilityAssessment? Assessment { get; set; }
}

public sealed class CommercialProfitabilityAssessmentDriver
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
    public int AffectedComponentCount { get; set; }
    public string SupportingText { get; set; } = string.Empty;
    public string SourceProvenance { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public bool IsLeadingDriver { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }

    public CommercialProfitabilityAssessment? Assessment { get; set; }
}

public sealed class CommercialProfitabilityComparisonRun
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
    public string AggregationMethod { get; set; } = CommercialProfitabilityComparisonAggregationMethods.UnweightedMedian;
    public string EvidenceStatus { get; set; } = string.Empty;
    public string ComparisonOutcome { get; set; } = string.Empty;
    public int CandidateAssessmentCount { get; set; }
    public int ComparableAssessmentCount { get; set; }
    public int ExcludedAssessmentCount { get; set; }
    public int CompleteCoverageCount { get; set; }
    public int PartialCoverageCount { get; set; }
    public int InsufficientDataCount { get; set; }
    public int SourceConflictCount { get; set; }
    public int NegativeContributionCount { get; set; }
    public int BelowThresholdCount { get; set; }
    public int? ReferenceCandidateAssessmentCount { get; set; }
    public int? ReferenceComparableAssessmentCount { get; set; }
    public int? ReferenceExcludedAssessmentCount { get; set; }
    public int? ReferenceCompleteCoverageCount { get; set; }
    public int? ReferencePartialCoverageCount { get; set; }
    public int? ReferenceInsufficientDataCount { get; set; }
    public int? ReferenceSourceConflictCount { get; set; }
    public int? ReferenceNegativeContributionCount { get; set; }
    public int? ReferenceBelowThresholdCount { get; set; }
    public int? CurrentCandidateAssessmentCount { get; set; }
    public int? CurrentComparableAssessmentCount { get; set; }
    public int? CurrentExcludedAssessmentCount { get; set; }
    public int? CurrentCompleteCoverageCount { get; set; }
    public int? CurrentPartialCoverageCount { get; set; }
    public int? CurrentInsufficientDataCount { get; set; }
    public int? CurrentSourceConflictCount { get; set; }
    public int? CurrentNegativeContributionCount { get; set; }
    public int? CurrentBelowThresholdCount { get; set; }
    public decimal? ReferenceMedianContributionMarginRatio { get; set; }
    public decimal? CurrentMedianContributionMarginRatio { get; set; }
    public decimal? ContributionMarginDelta { get; set; }
    public decimal? ReferenceMedianUnitContribution { get; set; }
    public decimal? CurrentMedianUnitContribution { get; set; }
    public string? LeadingDriverCode { get; set; }
    public string RuleSetVersion { get; set; } = CommercialIntelligenceConstants.ProfitabilityComparisonRuleSetVersion;
    public string? IdempotencyKey { get; set; }
    public string? IdempotencyPayloadFingerprint { get; set; }
    public string SourceProvenance { get; set; } = string.Empty;
    public DateTimeOffset EvaluatedAtUtc { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public string CreatedBy { get; set; } = string.Empty;

    public CommercialProfitabilityProfile? Profile { get; set; }
    public ICollection<CommercialProfitabilityComparisonResult> Results { get; set; } = [];
    public ICollection<CommercialProfitabilityComparisonDriver> Drivers { get; set; } = [];
}

public sealed class CommercialProfitabilityComparisonResult
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid ComparisonRunId { get; set; }
    public string ResultDimension { get; set; } = string.Empty;
    public string CanonicalResultReference { get; set; } = string.Empty;
    public string? CustomerReference { get; set; }
    public Guid? MaterialId { get; set; }
    public string? ComponentKind { get; set; }
    public int CandidateAssessmentCount { get; set; }
    public int ComparableAssessmentCount { get; set; }
    public int ExcludedAssessmentCount { get; set; }
    public int CompleteCoverageCount { get; set; }
    public int PartialCoverageCount { get; set; }
    public int NegativeContributionCount { get; set; }
    public int BelowThresholdCount { get; set; }
    public int? ReferenceComparableAssessmentCount { get; set; }
    public int? CurrentComparableAssessmentCount { get; set; }
    public int? ReferenceExcludedAssessmentCount { get; set; }
    public int? CurrentExcludedAssessmentCount { get; set; }
    public decimal? ReferenceMedianContributionMarginRatio { get; set; }
    public decimal? CurrentMedianContributionMarginRatio { get; set; }
    public decimal? ContributionMarginDelta { get; set; }
    public decimal? ReferenceMedianUnitContribution { get; set; }
    public decimal? CurrentMedianUnitContribution { get; set; }
    public decimal? ReferenceMedianUnitCost { get; set; }
    public decimal? CurrentMedianUnitCost { get; set; }
    public decimal? SignedUnitCostChange { get; set; }
    public decimal? SignedUnitCostChangeRatio { get; set; }
    public decimal? ReferenceMedianCostShare { get; set; }
    public decimal? CurrentMedianCostShare { get; set; }
    public decimal? CostShareDelta { get; set; }
    public string PositionState { get; set; } = string.Empty;
    public int? ContributionRank { get; set; }
    public bool IsTied { get; set; }
    public string EvidenceStatus { get; set; } = string.Empty;
    public string SourceProvenance { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }

    public CommercialProfitabilityComparisonRun? ComparisonRun { get; set; }
    public ICollection<CommercialProfitabilityComparisonDriver> Drivers { get; set; } = [];
}

public sealed class CommercialProfitabilityComparisonDriver
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

    public CommercialProfitabilityComparisonRun? ComparisonRun { get; set; }
    public CommercialProfitabilityComparisonResult? ComparisonResult { get; set; }
}
