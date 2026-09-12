namespace NevaxisFactOS.Persistence.ProductionManager;

public sealed class FactoryStateSnapshot
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public DateTimeOffset CapturedAtUtc { get; set; }
    public string ProductionStateJson { get; set; } = "{}";
    public string CapacityStateJson { get; set; } = "{}";
    public string MachineStateJson { get; set; } = "{}";
    public string MaterialStateJson { get; set; } = "{}";
    public string QualityStateJson { get; set; } = "{}";
    public string MaintenanceStateJson { get; set; } = "{}";
    public string MoldStateJson { get; set; } = "{}";
    public string SupplierStateJson { get; set; } = "{}";
    public string ShiftStateJson { get; set; } = "{}";
    public string ActiveCasesJson { get; set; } = "[]";
    public string ConstraintsJson { get; set; } = "{}";
    public string FreshnessJson { get; set; } = "{}";
}

public sealed class FactoryPrediction
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid? StateSnapshotId { get; set; }
    public string PredictionType { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string Severity { get; set; } = "medium";
    public string ConfidenceBand { get; set; } = ProductionManagerConfidenceBands.Medium;
    public decimal ConfidenceScore { get; set; }
    public string Horizon { get; set; } = string.Empty;
    public string EvidenceJson { get; set; } = "[]";
    public string DataClassification { get; set; } = ProductionManagerDataClassifications.Predicted;
    public string CorrelationKey { get; set; } = string.Empty;
    public DateTimeOffset PredictedAtUtc { get; set; }
    public DateTimeOffset? ExpiresAtUtc { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class FactoryDecision
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid? FactoryCaseId { get; set; }
    public Guid StateSnapshotId { get; set; }
    public string DecisionType { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string ProblemSummary { get; set; } = string.Empty;
    public string Status { get; set; } = ProductionManagerDecisionStatuses.Proposed;
    public string AutonomyMode { get; set; } = ProductionManagerAutonomyModes.Draft;
    public string ConfidenceBand { get; set; } = ProductionManagerConfidenceBands.Medium;
    public decimal ConfidenceScore { get; set; }
    public Guid? SelectedOptionId { get; set; }
    public string? SelectedBy { get; set; }
    public string ExpectedOutcomeJson { get; set; } = "{}";
    public string VerificationCriteriaJson { get; set; } = "[]";
    public string ApprovalState { get; set; } = "none";
    public Guid? AiDecisionLedgerId { get; set; }
    public string? ExecutionIdempotencyKey { get; set; }
    public DateTimeOffset? ExecutedAtUtc { get; set; }
    public DateTimeOffset? VerifiedAtUtc { get; set; }
    public string ActualOutcomeJson { get; set; } = "{}";
    public string DecisionResult { get; set; } = string.Empty;
    public decimal? EffectivenessScore { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public long ConcurrencyToken { get; set; }

    public FactoryStateSnapshot? StateSnapshot { get; set; }
    public ICollection<FactoryDecisionOption> Options { get; set; } = new List<FactoryDecisionOption>();
}

public sealed class FactoryDecisionOption
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid DecisionId { get; set; }
    public string OptionCode { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ExpectedOutcomeJson { get; set; } = "{}";
    public string RisksJson { get; set; } = "[]";
    public decimal Score { get; set; }
    public bool RequiresApproval { get; set; }
    public bool IsRejected { get; set; }
    public string? RejectionReason { get; set; }
    public int Rank { get; set; }
    public string DataClassification { get; set; } = ProductionManagerDataClassifications.Estimated;
    public DateTimeOffset CreatedAtUtc { get; set; }

    public FactoryDecision? Decision { get; set; }
}

public sealed class FactorySimulation
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid? DecisionId { get; set; }
    public Guid? StateSnapshotId { get; set; }
    public string ScenarioType { get; set; } = string.Empty;
    public string InputJson { get; set; } = "{}";
    public string OutputJson { get; set; } = "{}";
    public string ComparisonJson { get; set; } = "{}";
    public string DataClassification { get; set; } = ProductionManagerDataClassifications.Simulated;
    public string CreatedBy { get; set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; set; }
}

public sealed class FactoryDecisionOutcome
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid DecisionId { get; set; }
    public string ExpectedJson { get; set; } = "{}";
    public string ActualJson { get; set; } = "{}";
    public string VarianceJson { get; set; } = "{}";
    public decimal EffectivenessScore { get; set; }
    public string Result { get; set; } = string.Empty;
    public string DataClassification { get; set; } = ProductionManagerDataClassifications.Verified;
    public DateTimeOffset RecordedAtUtc { get; set; }
}

public sealed class FactoryDecisionMemory
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid DecisionId { get; set; }
    public string DecisionType { get; set; } = string.Empty;
    public string ContextJson { get; set; } = "{}";
    public string SelectedOptionCode { get; set; } = string.Empty;
    public string OutcomeSummary { get; set; } = string.Empty;
    public decimal EffectivenessScore { get; set; }
    public string ConditionsJson { get; set; } = "{}";
    public DateTimeOffset RecordedAtUtc { get; set; }
}

public sealed class ProductionManagerPolicy
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public string AutonomyMode { get; set; } = ProductionManagerAutonomyModes.Draft;
    public string AllowedDecisionTypesJson { get; set; } = "[]";
    public decimal MaxScheduleChangeHours { get; set; } = 8;
    public decimal MaxFinancialImpact { get; set; } = 0;
    public decimal ApprovalThresholdScore { get; set; } = 0.7m;
    public decimal LowConfidenceThreshold { get; set; } = 0.45m;
    public int StaleDataMaxMinutes { get; set; } = 60;
    public string ProhibitedDecisionsJson { get; set; } = "[]";
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public long ConcurrencyToken { get; set; }
}
