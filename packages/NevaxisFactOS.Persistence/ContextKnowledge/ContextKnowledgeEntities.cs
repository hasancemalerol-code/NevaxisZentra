namespace NevaxisFactOS.Persistence.ContextKnowledge;

public interface IContextKnowledgeTenantFacilityEntity
{
    Guid Id { get; set; }
    Guid TenantId { get; set; }
    Guid FacilityId { get; set; }
}

public sealed class CanonicalOperationalStateRecord : IContextKnowledgeTenantFacilityEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string ResolutionStatus { get; set; } = ContextKnowledgeResolutionStatuses.Unknown;
    public string? ResolvedStateValue { get; set; }
    public decimal Confidence { get; set; }
    public string SourceClaimsJson { get; set; } = "[]";
    public string ConflictReasonsJson { get; set; } = "[]";
    public DateTimeOffset EvaluatedAtUtc { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}

public sealed class FactoryKnowledgeRecord : IContextKnowledgeTenantFacilityEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid? SourceCaseId { get; set; }
    public string KnowledgeKey { get; set; } = string.Empty;
    public string ProblemSymptom { get; set; } = string.Empty;
    public string RelevantConditionsJson { get; set; } = "[]";
    public string RootCause { get; set; } = string.Empty;
    public string DecisionSummary { get; set; } = string.Empty;
    public string ActionsJson { get; set; } = "[]";
    public string EvidenceRefsJson { get; set; } = "[]";
    public string BeforeStateJson { get; set; } = "{}";
    public string AfterStateJson { get; set; } = "{}";
    public string VerifiedOutcome { get; set; } = string.Empty;
    public decimal Confidence { get; set; }
    public string ApplicabilityConstraintsJson { get; set; } = "{}";
    public string PromotionStatus { get; set; } = ContextKnowledgePromotionStatuses.Pending;
    public DateTimeOffset? ObservationWindowStartUtc { get; set; }
    public DateTimeOffset? ObservationWindowEndUtc { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset? PromotedAtUtc { get; set; }
}

public sealed class ContextSnapshotRecord : IContextKnowledgeTenantFacilityEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid? DigitalWorkCaseId { get; set; }
    public string SnapshotCode { get; set; } = string.Empty;
    public string ScopeJson { get; set; } = "{}";
    public string BudgetJson { get; set; } = "{}";
    public string MaterializedContextJson { get; set; } = "{}";
    public string ProvenanceJson { get; set; } = "[]";
    public string MetricsJson { get; set; } = "{}";
    public string EngineVersion { get; set; } = ContextKnowledgeEngineVersions.Snapshot;
    public string CreatedBySubjectId { get; set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; set; }
}

public sealed class DataLifecyclePolicyRecord : IContextKnowledgeTenantFacilityEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public string DataType { get; set; } = string.Empty;
    public int HotRetentionHours { get; set; }
    public int WarmRetentionDays { get; set; }
    public int ColdArchiveDays { get; set; }
    public bool IsActive { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}

public sealed class TelemetryAggregateRecord : IContextKnowledgeTenantFacilityEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public string ResourceId { get; set; } = string.Empty;
    public string SignalType { get; set; } = string.Empty;
    public string Granularity { get; set; } = ContextKnowledgeAggregationGranularities.Minute;
    public DateTimeOffset WindowStartUtc { get; set; }
    public DateTimeOffset WindowEndUtc { get; set; }
    public decimal AggregateValue { get; set; }
    public int SampleCount { get; set; }
    public bool PreserveRawWindow { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
}
