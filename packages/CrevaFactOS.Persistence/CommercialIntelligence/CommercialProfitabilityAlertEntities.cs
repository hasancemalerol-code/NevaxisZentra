namespace CrevaFactOS.Persistence.CommercialIntelligence;

public sealed class CommercialProfitabilityAlert
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public string AlertReference { get; set; } = string.Empty;
    public string AlertCategory { get; set; } = string.Empty;
    public string Severity { get; set; } = CommercialProfitabilityAlertSeverities.Medium;
    public string LifecycleStatus { get; set; } = CommercialProfitabilityAlertLifecycleStatuses.Active;
    public Guid SourceComparisonId { get; set; }
    public string SourceComparisonType { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string EvidenceSummary { get; set; } = string.Empty;
    public string EvidenceStatus { get; set; } = string.Empty;
    public string ComparisonOutcome { get; set; } = string.Empty;
    public string? LeadingDriverCode { get; set; }
    public string SourceSnapshotJson { get; set; } = "{}";
    public string IdempotencyKey { get; set; } = string.Empty;
    public string IdempotencyPayloadFingerprint { get; set; } = string.Empty;
    public Guid? LinkedAiTaskId { get; set; }
    public string RuleSetVersion { get; set; } = CommercialProfitabilityAlertConstants.RuleSetVersion;
    public string SourceProvenance { get; set; } = string.Empty;
    public uint Version { get; set; } = 1;
    public DateTimeOffset MaterializedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public string UpdatedBy { get; set; } = string.Empty;

    public CommercialProfitabilityComparisonRun? SourceComparison { get; set; }
    public ICollection<CommercialProfitabilityAlertAuditEvent> AuditEvents { get; set; } = [];
}

public sealed class CommercialProfitabilityAlertAuditEvent
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid AlertId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string PayloadJson { get; set; } = "{}";
    public string ActorSubjectId { get; set; } = string.Empty;
    public DateTimeOffset OccurredAtUtc { get; set; }
    public Guid? CorrelationId { get; set; }

    public CommercialProfitabilityAlert? Alert { get; set; }
}
