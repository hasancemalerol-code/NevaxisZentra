namespace NevaxisZentra.Persistence.FactoryOrchestration;

public sealed class FactoryCaseDependency
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid RootCaseId { get; set; }
    public string FromNodeKey { get; set; } = string.Empty;
    public string ToNodeKey { get; set; } = string.Empty;
    public string FromNodeType { get; set; } = FactoryDependencyNodeTypes.DomainIssue;
    public string ToNodeType { get; set; } = FactoryDependencyNodeTypes.DomainIssue;
    public string EdgeType { get; set; } = FactoryDependencyEdgeTypes.DependsOn;
    public string? LinkedCaseId { get; set; }
    public string? LinkedWorkerId { get; set; }
    public string? LinkedTaskId { get; set; }
    public string MetadataJson { get; set; } = "{}";
    public DateTimeOffset CreatedAtUtc { get; set; }
}

public sealed class FactoryEventCorrelation
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public string CorrelationKey { get; set; } = string.Empty;
    public Guid RootCaseId { get; set; }
    public string SourceType { get; set; } = string.Empty;
    public string SourceId { get; set; } = string.Empty;
    public string EventCategory { get; set; } = string.Empty;
    public DateTimeOffset FirstSeenAtUtc { get; set; }
    public DateTimeOffset LastSeenAtUtc { get; set; }
}
