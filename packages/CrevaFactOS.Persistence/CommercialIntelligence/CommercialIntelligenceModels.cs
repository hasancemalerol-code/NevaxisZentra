namespace CrevaFactOS.Persistence.CommercialIntelligence;

public sealed record CommercialIntelligenceOperationContext(
    Guid TenantId,
    Guid FacilityId,
    Guid MembershipId,
    Guid IdentitySubjectId,
    string PermissionKey,
    Guid? CorrelationId);

public sealed record CommercialPurchasePriceProfileReadModel(
    Guid Id,
    string ProfileCode,
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
    string ReferenceCenter,
    string RuleSetVersion,
    string SourceProvenance,
    uint Version,
    bool IsPilotData);

public sealed record CommercialPurchasePriceAssessmentSummaryReadModel(
    Guid Id,
    string AssessmentReference,
    Guid ProfileId,
    string ProfileCode,
    Guid MaterialId,
    string? SupplierReference,
    string ComparisonDimension,
    string AssessmentOutcome,
    decimal CurrentUnitPrice,
    decimal? ReferenceMedianUnitPrice,
    decimal? VarianceRatio,
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

public sealed record CommercialPurchasePriceAssessmentDetailReadModel(
    Guid Id,
    string AssessmentReference,
    Guid ProfileId,
    string ProfileCode,
    Guid MaterialId,
    string? SupplierReference,
    string ComparisonDimension,
    decimal CurrentUnitPrice,
    decimal? ReferenceMedianUnitPrice,
    decimal? AbsoluteVariance,
    decimal? VarianceRatio,
    int ComparableObservationCount,
    int ExcludedObservationCount,
    string CurrencyCode,
    string NormalizedUnit,
    DateTimeOffset WindowStartUtc,
    DateTimeOffset WindowEndUtc,
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
    IReadOnlyList<CommercialPurchasePriceAssessmentDriverReadModel> Drivers,
    bool IsPilotData);

public sealed record CommercialPurchasePriceAssessmentDriverReadModel(
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

public sealed record CommercialPurchasePriceWorkspaceReadModel(
    string RuleSetVersion,
    string ProvenanceDisclaimer,
    string ReferenceCenter,
    int DefaultMinimumComparableObservations,
    bool CanRead,
    bool CanManage);

public sealed record CreateCommercialPurchasePriceProfileCommand(
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

public sealed record UpdateCommercialPurchasePriceProfileCommand(
    Guid ProfileId,
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

public sealed record EvaluateCommercialPurchasePriceAssessmentCommand(
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

public sealed record CommercialPurchasePriceAssessmentListFilters(
    Guid? MaterialId,
    string? AssessmentOutcome,
    int Page,
    int PageSize);

public sealed class CommercialIntelligenceWriteResult
{
    public bool Succeeded { get; init; }
    public Guid? EntityId { get; init; }
    public uint? Version { get; init; }
    public bool IdempotentReplay { get; init; }
    public string? FailureCode { get; init; }
    public string? FailureReason { get; init; }
    public CommercialPurchasePriceAssessmentDetailReadModel? Assessment { get; init; }

    public static CommercialIntelligenceWriteResult Success(
        Guid entityId,
        uint version,
        bool idempotentReplay = false,
        CommercialPurchasePriceAssessmentDetailReadModel? assessment = null) =>
        new()
        {
            Succeeded = true,
            EntityId = entityId,
            Version = version,
            IdempotentReplay = idempotentReplay,
            Assessment = assessment,
        };

    public static CommercialIntelligenceWriteResult Fail(string code, string reason) =>
        new()
        {
            Succeeded = false,
            FailureCode = code,
            FailureReason = reason,
        };
}

public interface ICommercialIntelligenceRepository
{
    Task<CommercialPurchasePriceWorkspaceReadModel> GetWorkspaceAsync(
        CommercialIntelligenceOperationContext context,
        bool canManage,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CommercialPurchasePriceProfileReadModel>> ListProfilesAsync(
        CommercialIntelligenceOperationContext context,
        CancellationToken cancellationToken = default);

    Task<CommercialPurchasePriceProfileReadModel?> GetProfileByIdAsync(
        CommercialIntelligenceOperationContext context,
        Guid profileId,
        CancellationToken cancellationToken = default);

    Task<CommercialIntelligenceWriteResult> CreateProfileAsync(
        CommercialIntelligenceOperationContext context,
        CreateCommercialPurchasePriceProfileCommand command,
        CancellationToken cancellationToken = default);

    Task<CommercialIntelligenceWriteResult> UpdateProfileAsync(
        CommercialIntelligenceOperationContext context,
        UpdateCommercialPurchasePriceProfileCommand command,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CommercialPurchasePriceAssessmentSummaryReadModel>> ListAssessmentsAsync(
        CommercialIntelligenceOperationContext context,
        CommercialPurchasePriceAssessmentListFilters filters,
        CancellationToken cancellationToken = default);

    Task<CommercialPurchasePriceAssessmentDetailReadModel?> GetAssessmentByIdAsync(
        CommercialIntelligenceOperationContext context,
        Guid assessmentId,
        CancellationToken cancellationToken = default);

    Task<CommercialIntelligenceWriteResult> EvaluateAssessmentAsync(
        CommercialIntelligenceOperationContext context,
        EvaluateCommercialPurchasePriceAssessmentCommand command,
        CancellationToken cancellationToken = default);
}
