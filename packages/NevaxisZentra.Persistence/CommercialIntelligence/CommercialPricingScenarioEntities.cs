namespace NevaxisZentra.Persistence.CommercialIntelligence;

public sealed class CommercialPricingScenario
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public string ScenarioReference { get; set; } = string.Empty;
    public string LifecycleStatus { get; set; } = CommercialPricingScenarioLifecycleStatuses.Draft;
    public string? GuardrailResult { get; set; }
    public Guid ProfileId { get; set; }
    public string ProfileVersion { get; set; } = string.Empty;
    public Guid? MaterialId { get; set; }
    public string? CustomerReference { get; set; }
    public Guid SalesEvidenceObservationId { get; set; }
    public decimal? Quantity { get; set; }
    public decimal BaselineUnitPrice { get; set; }
    public decimal ProposedUnitPrice { get; set; }
    public decimal? ProposedDiscountRatio { get; set; }
    public DateTimeOffset? ValidityWindowStartUtc { get; set; }
    public DateTimeOffset? ValidityWindowEndUtc { get; set; }
    public string CommercialRationale { get; set; } = string.Empty;
    public Guid? LatestEvaluationSnapshotId { get; set; }
    public Guid? LinkedAiTaskId { get; set; }
    public string RuleSetVersion { get; set; } = CommercialPricingScenarioConstants.RuleSetVersion;
    public string SourceProvenance { get; set; } = string.Empty;
    public uint Version { get; set; } = 1;
    public int EvaluationCount { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public string UpdatedBy { get; set; } = string.Empty;

    public CommercialProfitabilityProfile? Profile { get; set; }
    public CommercialPricingScenarioEvaluationSnapshot? LatestEvaluationSnapshot { get; set; }
    public ICollection<CommercialPricingScenarioEvaluationSnapshot> EvaluationSnapshots { get; set; } = [];
    public ICollection<CommercialPricingScenarioAuditEvent> AuditEvents { get; set; } = [];
}

public sealed class CommercialPricingScenarioEvaluationSnapshot
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid ScenarioId { get; set; }
    public uint ScenarioVersionAtEvaluation { get; set; }
    public int EvaluationSequence { get; set; }
    public string GuardrailResult { get; set; } = string.Empty;
    public string AssessmentOutcome { get; set; } = string.Empty;
    public string EvidenceStatus { get; set; } = string.Empty;
    public string CostCoverageStatus { get; set; } = string.Empty;
    public string? LeadingDriverCode { get; set; }
    public string? InsufficientDataReason { get; set; }
    public decimal BaselineUnitPrice { get; set; }
    public decimal ProposedUnitPrice { get; set; }
    public decimal TotalIncludedUnitCost { get; set; }
    public decimal ExpectedUnitContribution { get; set; }
    public decimal? ExpectedContributionMarginRatio { get; set; }
    public decimal? BaselineContributionMarginRatio { get; set; }
    public decimal? ContributionMarginDelta { get; set; }
    public decimal ProfileMinimumContributionMarginRatio { get; set; }
    public Guid ProfileId { get; set; }
    public string ProfileVersion { get; set; } = string.Empty;
    public string InputSnapshotJson { get; set; } = "{}";
    public string OutputSnapshotJson { get; set; } = "{}";
    public string RuleSetVersion { get; set; } = CommercialPricingScenarioConstants.RuleSetVersion;
    public DateTimeOffset EvaluatedAtUtc { get; set; }
    public string EvaluatedBy { get; set; } = string.Empty;

    public CommercialPricingScenario? Scenario { get; set; }
}

public sealed class CommercialPricingScenarioAuditEvent
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid ScenarioId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string PayloadJson { get; set; } = "{}";
    public string ActorSubjectId { get; set; } = string.Empty;
    public DateTimeOffset OccurredAtUtc { get; set; }
    public Guid? CorrelationId { get; set; }

    public CommercialPricingScenario? Scenario { get; set; }
}
