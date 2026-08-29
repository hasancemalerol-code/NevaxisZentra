namespace CrevaFactOS.Persistence.Material;

public static class MaterialRequirementFailureCodes
{
    public const string InvalidScope = "invalid-scope";
    public const string NotFound = "not-found";
    public const string ConcurrencyConflict = "requirement-concurrency-conflict";
    public const string IdempotencyConflict = "requirement-idempotency-conflict";
    public const string IdempotencyLegacyUnverifiable = "requirement-idempotency-legacy-unverifiable";
    public const string AssessmentConcurrencyConflict = "assessment-concurrency-conflict";
    public const string InvalidQuantity = "invalid-quantity";
    public const string InvalidLot = "invalid-lot";
    public const string InsufficientData = "insufficient-data";
    public const string RegrindLimitExceeded = "regrind-limit-exceeded";
    public const string ValidationFailed = "validation-failed";
    public const string InvalidTransition = "invalid-transition";
    public const string SourceUnavailable = "source-unavailable";
}

public sealed class MaterialRequirementWriteResult
{
    public bool Succeeded { get; init; }
    public string? FailureCode { get; init; }
    public string? FailureReason { get; init; }
    public Guid? EntityId { get; init; }
    public uint? Version { get; init; }
    public bool IdempotentReplay { get; init; }

    public static MaterialRequirementWriteResult Success(Guid entityId, uint version, bool idempotentReplay = false) =>
        new()
        {
            Succeeded = true,
            EntityId = entityId,
            Version = version,
            IdempotentReplay = idempotentReplay,
        };

    public static MaterialRequirementWriteResult Fail(string code, string reason) =>
        new() { Succeeded = false, FailureCode = code, FailureReason = reason };
}

public sealed record MaterialRequirementProfileListFilters(
    Guid? MaterialId,
    string? RequirementType,
    int Page,
    int PageSize);

public sealed record MaterialRequirementProfileSummaryReadModel(
    Guid Id,
    Guid MaterialId,
    string MaterialCode,
    string MaterialName,
    string RequirementType,
    string? ProductionOrderReference,
    string? ProductReference,
    string? MoldReference,
    decimal? GoodPartTargetQuantity,
    decimal? PlannedRegrindPercent,
    string SourceType,
    string? SourceReference,
    DateTimeOffset? DataRecordedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    uint Version);

public sealed record MaterialRequirementProfileDetailReadModel(
    Guid Id,
    Guid FacilityId,
    Guid MaterialId,
    string MaterialCode,
    string MaterialName,
    string RequirementType,
    string? ProductionOrderReference,
    string? ProductReference,
    string? MoldReference,
    decimal? PlannedProductionQuantity,
    decimal? GoodPartTargetQuantity,
    decimal? PartWeightGrams,
    decimal? RunnerWeightGramsPerShot,
    int? EffectiveCavityCount,
    decimal? SetupAllowanceGrams,
    decimal? ProcessLossAllowancePercent,
    decimal? PlannedVirginPercent,
    decimal? PlannedRegrindPercent,
    string SourceType,
    string? SourceReference,
    DateTimeOffset? DataRecordedAtUtc,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    uint Version);

public sealed record MaterialConsumptionObservationListFilters(
    Guid? MaterialId,
    Guid? RequirementProfileId,
    string? ObservationType,
    int Page,
    int PageSize);

public sealed record MaterialConsumptionObservationReadModel(
    Guid Id,
    Guid MaterialId,
    string MaterialCode,
    Guid? MaterialLotId,
    string? LotNumber,
    Guid? RequirementProfileId,
    string ObservationType,
    decimal Quantity,
    string UnitOfMeasure,
    decimal? VirginQuantity,
    decimal? RegrindQuantity,
    decimal? GoodPartQuantity,
    decimal? ScrapQuantity,
    string? ProductionOrderReference,
    string? ProductReference,
    string? MoldReference,
    DateTimeOffset ObservedAtUtc,
    string SourceType,
    string? SourceReference,
    DateTimeOffset? DataRecordedAtUtc,
    string? Notes,
    DateTimeOffset CreatedAtUtc,
    uint Version);

public sealed record MaterialRequirementAssessmentReadModel(
    Guid Id,
    Guid MaterialId,
    Guid? RequirementProfileId,
    string AssessmentType,
    string Status,
    string RuleSetVersion,
    string InputFingerprint,
    IReadOnlyList<MaterialAssessmentDriverReadModel> Drivers,
    IReadOnlyList<MaterialAssessmentEvidenceReadModel> Evidence,
    decimal? TheoreticalRequirementGrams,
    decimal? ObservedConsumptionGrams,
    decimal? VarianceGrams,
    decimal? VariancePercent,
    decimal? VirginRequirementGrams,
    decimal? RegrindRequirementGrams,
    DateTimeOffset EvaluatedAtUtc,
    DateTimeOffset CreatedAtUtc,
    uint Version);

public sealed record MaterialAssessmentDriverReadModel(
    string Code,
    string Label,
    string? Value,
    string State);

public sealed record MaterialAssessmentEvidenceReadModel(
    string Field,
    string? Value,
    string? Source,
    DateTimeOffset? RecordedAtUtc,
    bool IsMissing);

public sealed record MaterialRequirementOverviewReadModel(
    Guid MaterialId,
    string MaterialCode,
    MaterialRequirementAssessmentReadModel? CurrentRequirementAssessment,
    MaterialRequirementAssessmentReadModel? CurrentVarianceAssessment,
    MaterialRequirementAssessmentReadModel? CurrentRegrindAssessment,
    MaterialRequirementAssessmentReadModel? CurrentAvailabilityAssessment,
    IReadOnlyList<MaterialRequirementProfileSummaryReadModel> Profiles,
    IReadOnlyList<MaterialConsumptionObservationReadModel> RecentObservations,
    string TheoreticalRequirementDisclaimer,
    string ConsumptionObservationDisclaimer);

public sealed record CreateMaterialRequirementProfileCommand(
    Guid MaterialId,
    string RequirementType,
    string? ProductionOrderReference,
    string? ProductReference,
    string? MoldReference,
    decimal? PlannedProductionQuantity,
    decimal? GoodPartTargetQuantity,
    decimal? PartWeightGrams,
    decimal? RunnerWeightGramsPerShot,
    int? EffectiveCavityCount,
    decimal? SetupAllowanceGrams,
    decimal? ProcessLossAllowancePercent,
    decimal? PlannedVirginPercent,
    decimal? PlannedRegrindPercent,
    string SourceType,
    string? SourceReference,
    DateTimeOffset? DataRecordedAtUtc,
    string? IdempotencyKey);

public sealed record UpdateMaterialRequirementProfileCommand(
    Guid ProfileId,
    uint ExpectedVersion,
    string RequirementType,
    string? ProductionOrderReference,
    string? ProductReference,
    string? MoldReference,
    decimal? PlannedProductionQuantity,
    decimal? GoodPartTargetQuantity,
    decimal? PartWeightGrams,
    decimal? RunnerWeightGramsPerShot,
    int? EffectiveCavityCount,
    decimal? SetupAllowanceGrams,
    decimal? ProcessLossAllowancePercent,
    decimal? PlannedVirginPercent,
    decimal? PlannedRegrindPercent,
    string SourceType,
    string? SourceReference,
    DateTimeOffset? DataRecordedAtUtc);

public sealed record CreateMaterialConsumptionObservationCommand(
    Guid MaterialId,
    Guid? MaterialLotId,
    Guid? RequirementProfileId,
    string ObservationType,
    decimal Quantity,
    string UnitOfMeasure,
    decimal? VirginQuantity,
    decimal? RegrindQuantity,
    decimal? GoodPartQuantity,
    decimal? ScrapQuantity,
    string? ProductionOrderReference,
    string? ProductReference,
    string? MoldReference,
    DateTimeOffset ObservedAtUtc,
    string SourceType,
    string? SourceReference,
    DateTimeOffset? DataRecordedAtUtc,
    string? Notes,
    string IdempotencyKey);

public sealed record EvaluateMaterialRequirementCommand(
    Guid MaterialId,
    Guid RequirementProfileId,
    string? IdempotencyKey);

public sealed record EvaluateMaterialVarianceCommand(
    Guid MaterialId,
    Guid RequirementProfileId,
    string? IdempotencyKey);

public sealed record EvaluateMaterialRegrindPolicyCommand(
    Guid MaterialId,
    Guid RequirementProfileId,
    string? IdempotencyKey);

public sealed record EvaluateMaterialAvailabilityRiskCommand(
    Guid MaterialId,
    Guid? RequirementProfileId,
    string? IdempotencyKey);

public sealed record MaterialRequirementCalculationResult(
    string Status,
    decimal? TheoreticalRequirementGrams,
    decimal? VirginRequirementGrams,
    decimal? RegrindRequirementGrams,
    decimal? NetPartMaterialGrams,
    int? RequiredShots,
    decimal? RunnerMaterialGrams,
    decimal? ProcessLossGrams,
    decimal? SetupAllowanceGrams,
    IReadOnlyList<string> MissingInputs,
    IReadOnlyList<MaterialAssessmentEvidenceReadModel> Evidence,
    string RuleSetVersion,
    string Disclaimer);

public sealed record MaterialRegrindPolicyAssessmentResult(
    string Status,
    string ReasonCode,
    string ReasonLabel,
    decimal? PlannedRegrindPercent,
    decimal? PermittedRegrindPercent,
    string State,
    IReadOnlyList<MaterialAssessmentDriverReadModel> Drivers,
    IReadOnlyList<MaterialAssessmentEvidenceReadModel> Evidence);

public sealed record MaterialVarianceAssessmentResult(
    string Status,
    decimal? TheoreticalRequirementGrams,
    decimal? ObservedConsumptionGrams,
    decimal? VarianceGrams,
    decimal? VariancePercent,
    decimal TolerancePercent,
    IReadOnlyList<MaterialAssessmentDriverReadModel> Drivers,
    IReadOnlyList<MaterialAssessmentEvidenceReadModel> Evidence);

public sealed record MaterialAvailabilityRiskAssessmentResult(
    string Status,
    decimal? RequiredQuantityGrams,
    string? RequiredQuantityUnit,
    decimal? ErpAvailableQuantity,
    decimal? ErpReservedQuantity,
    string? ErpUnit,
    decimal? AvailableAfterReservationQuantity,
    decimal? ShortageQuantity,
    DateTimeOffset? ErpSourceUpdatedAtUtc,
    string? SourceFreshness,
    decimal? ObservedLotQuantityGrams,
    IReadOnlyList<MaterialAssessmentDriverReadModel> Drivers,
    IReadOnlyList<MaterialAssessmentEvidenceReadModel> Evidence);

public sealed record MaterialAvailabilityRiskWorkspaceReadModel(
    string RuleSetVersion,
    string AutomationDisclaimer,
    int ShortageCount,
    int SufficientCount,
    int InsufficientDataCount,
    int SourceConflictCount,
    int RequiresReviewCount,
    int TaskLinkedCount,
    int DataReviewCount,
    DateTimeOffset? LatestEvaluatedAtUtc,
    bool CanRead,
    bool CanManage,
    bool CanCreateTask,
    IReadOnlyList<MaterialAvailabilityRiskRowReadModel> Assessments);

public sealed record MaterialAvailabilityRiskRowReadModel(
    Guid Id,
    Guid MaterialId,
    string MaterialCode,
    string MaterialName,
    Guid? RequirementProfileId,
    string? ProductionOrderReference,
    string Status,
    decimal? RequiredQuantityGrams,
    decimal? ErpAvailableQuantity,
    decimal? ErpReservedQuantity,
    string? ErpUnit,
    decimal? AvailableAfterReservationQuantity,
    decimal? ShortageQuantity,
    string? SourceFreshness,
    DateTimeOffset EvaluatedAtUtc,
    uint Version,
    string FollowUpStatus,
    Guid? LinkedAiTaskId,
    bool CanCreateTaskDraft,
    string TaskDraftPurpose);

public sealed record MaterialAvailabilityRiskDetailReadModel(
    Guid Id,
    Guid FacilityId,
    Guid MaterialId,
    string MaterialCode,
    string MaterialName,
    Guid? RequirementProfileId,
    string? ProductionOrderReference,
    string? ProductReference,
    string? MoldReference,
    string Status,
    string RuleSetVersion,
    string InputFingerprint,
    decimal? RequiredQuantityGrams,
    decimal? ErpAvailableQuantity,
    decimal? ErpReservedQuantity,
    string? ErpUnit,
    decimal? AvailableAfterReservationQuantity,
    decimal? ShortageQuantity,
    DateTimeOffset? ErpSourceUpdatedAtUtc,
    string? SourceFreshness,
    IReadOnlyList<MaterialAssessmentDriverReadModel> Drivers,
    IReadOnlyList<MaterialAssessmentEvidenceReadModel> Evidence,
    DateTimeOffset EvaluatedAtUtc,
    DateTimeOffset CreatedAtUtc,
    uint Version);

public sealed record MaterialAvailabilityRiskListFilters(
    Guid? MaterialId,
    string? Status,
    int Page,
    int PageSize);

public sealed record MaterialAvailabilityRiskAssessmentSummaryReadModel(
    Guid Id,
    string Status,
    DateTimeOffset EvaluatedAtUtc,
    string RuleSetVersion);
