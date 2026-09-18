namespace NevaxisZentra.Persistence.FactoryLearning;

public sealed class FactoryLoss
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public string Category { get; set; } = FactoryLossCategories.Other;
    public string Title { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string? LineId { get; set; }
    public string? MachineId { get; set; }
    public string? OrderId { get; set; }
    public string? MoldId { get; set; }
    public string? MaterialId { get; set; }
    public string? ShiftId { get; set; }
    public decimal DurationMinutes { get; set; }
    public decimal QuantityImpact { get; set; }
    public decimal? CostEstimate { get; set; }
    public string SourceModule { get; set; } = string.Empty;
    public string SourceReference { get; set; } = string.Empty;
    public string EvidenceJson { get; set; } = "[]";
    public string DataClassification { get; set; } = FactoryLearningDataClassifications.Derived;
    public decimal ConfidenceScore { get; set; }
    public string CorrelationKey { get; set; } = string.Empty;
    public int RecurrenceCount { get; set; } = 1;
    public Guid? FactoryCaseId { get; set; }
    public Guid? FactoryDecisionId { get; set; }
    public string LossTreePathJson { get; set; } = "[]";
    public DateTimeOffset FirstSeenAtUtc { get; set; }
    public DateTimeOffset LastSeenAtUtc { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public long ConcurrencyToken { get; set; }
}

public sealed class FactoryLossPattern
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public string PatternType { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string PatternKey { get; set; } = string.Empty;
    public string ContextJson { get; set; } = "{}";
    public string EvidenceJson { get; set; } = "[]";
    public int OccurrenceCount { get; set; }
    public decimal ConfidenceScore { get; set; }
    public string DataClassification { get; set; } = FactoryLearningDataClassifications.Derived;
    public DateTimeOffset FirstObservedAtUtc { get; set; }
    public DateTimeOffset LastObservedAtUtc { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public long ConcurrencyToken { get; set; }
}

public sealed class ImprovementOpportunity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid? PatternId { get; set; }
    public Guid? LossId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string ProblemSummary { get; set; } = string.Empty;
    public string HypothesisJson { get; set; } = "{}";
    public string ExpectedMechanism { get; set; } = string.Empty;
    public string ExpectedImpactJson { get; set; } = "{}";
    public string AffectedScopeJson { get; set; } = "{}";
    public string RiskJson { get; set; } = "[]";
    public string VerificationCriteriaJson { get; set; } = "[]";
    public string RollbackCriteriaJson { get; set; } = "[]";
    public decimal PriorityScore { get; set; }
    public decimal RecoverablePotentialScore { get; set; }
    public string ImpactClassification { get; set; } = FactoryLearningDataClassifications.Estimated;
    public string Status { get; set; } = ImprovementBacklogStatuses.New;
    public string OwnerSubjectId { get; set; } = string.Empty;
    public string Domain { get; set; } = string.Empty;
    public Guid? AssignedWorkerId { get; set; }
    public decimal ConfidenceScore { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public long ConcurrencyToken { get; set; }
}

public sealed class FactoryExperiment
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid OpportunityId { get; set; }
    public string ExperimentType { get; set; } = FactoryExperimentTypes.Process;
    public string Title { get; set; } = string.Empty;
    public string Hypothesis { get; set; } = string.Empty;
    public string ScopeJson { get; set; } = "{}";
    public string BaselineJson { get; set; } = "{}";
    public string TargetJson { get; set; } = "{}";
    public string GuardrailsJson { get; set; } = "[]";
    public string StopConditionsJson { get; set; } = "[]";
    public string Status { get; set; } = FactoryExperimentStatuses.Draft;
    public string Outcome { get; set; } = string.Empty;
    public string OwnerSubjectId { get; set; } = string.Empty;
    public string ApprovalState { get; set; } = "none";
    public string? ApprovedBy { get; set; }
    public DateTimeOffset? StartedAtUtc { get; set; }
    public DateTimeOffset? StoppedAtUtc { get; set; }
    public DateTimeOffset? CompletedAtUtc { get; set; }
    public DateTimeOffset? VerifiedAtUtc { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public long ConcurrencyToken { get; set; }
}

public sealed class FactoryExperimentMeasurement
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid ExperimentId { get; set; }
    public string Phase { get; set; } = "baseline";
    public string MetricKey { get; set; } = string.Empty;
    public decimal MetricValue { get; set; }
    public string Unit { get; set; } = string.Empty;
    public string DataClassification { get; set; } = FactoryLearningDataClassifications.Measured;
    public string EvidenceJson { get; set; } = "[]";
    public DateTimeOffset RecordedAtUtc { get; set; }
}

public sealed class VerifiedImprovement
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid ExperimentId { get; set; }
    public Guid OpportunityId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string OutcomeSummary { get; set; } = string.Empty;
    public string BeforeJson { get; set; } = "{}";
    public string AfterJson { get; set; } = "{}";
    public string DeltaJson { get; set; } = "{}";
    public decimal EffectivenessScore { get; set; }
    public string ImpactClassification { get; set; } = FactoryLearningDataClassifications.Measured;
    public string VerificationEvidenceJson { get; set; } = "[]";
    public string StandardizationState { get; set; } = "none";
    public DateTimeOffset VerifiedAtUtc { get; set; }
    public DateTimeOffset? StandardizedAtUtc { get; set; }
    public DateTimeOffset? SustainabilityCheckedAtUtc { get; set; }
    public string SustainabilityStatus { get; set; } = "pending";
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public long ConcurrencyToken { get; set; }
}

public sealed class FactoryLearningMemory
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid? VerifiedImprovementId { get; set; }
    public Guid? DecisionMemoryId { get; set; }
    public string ProblemPattern { get; set; } = string.Empty;
    public string ContextJson { get; set; } = "{}";
    public string RootCauseSummary { get; set; } = string.Empty;
    public string RootCauseConfidence { get; set; } = RootCauseConfidenceLevels.Unproven;
    public string InterventionSummary { get; set; } = string.Empty;
    public string ExperimentSummary { get; set; } = string.Empty;
    public string ResultSummary { get; set; } = string.Empty;
    public decimal EffectivenessScore { get; set; }
    public string MemoryQuality { get; set; } = LearningMemoryQuality.Partial;
    public string ScopeJson { get; set; } = "{}";
    public string EvidenceJson { get; set; } = "[]";
    public string RevalidationCriteriaJson { get; set; } = "[]";
    public DateTimeOffset RecordedAtUtc { get; set; }
}

public sealed class FactoryRootCauseAnalysis
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid? PatternId { get; set; }
    public Guid? LossId { get; set; }
    public string Method { get; set; } = "5-why";
    public string Summary { get; set; } = string.Empty;
    public string WhyChainJson { get; set; } = "[]";
    public string ContributingFactorsJson { get; set; } = "[]";
    public string CandidateCause { get; set; } = string.Empty;
    public string ConfidenceLevel { get; set; } = RootCauseConfidenceLevels.Unproven;
    public bool IsConfirmedCause { get; set; }
    public string EvidenceJson { get; set; } = "[]";
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public long ConcurrencyToken { get; set; }
}
