namespace NevaxisZentra.Persistence.FactoryNetworkOptimization;

public sealed class FactoryNetwork
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string PlanningHorizon { get; set; } = "operational";
    public string TimezonePolicyJson { get; set; } = "{}";
    public string Status { get; set; } = FactoryNetworkLifecycleStatuses.Active;
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public long ConcurrencyToken { get; set; }
}

public sealed class FactoryNetworkFacility
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid NetworkId { get; set; }
    public Guid FacilityId { get; set; }
    public string Role { get; set; } = "member";
    public int SortOrder { get; set; }
}

public sealed class FacilityCapabilityProfile
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public string TechnologiesJson { get; set; } = "[]";
    public string LinesJson { get; set; } = "[]";
    public string MachineFamiliesJson { get; set; } = "[]";
    public string MoldCompatibilityJson { get; set; } = "[]";
    public string MaterialsJson { get; set; } = "[]";
    public string QualityCertificationsJson { get; set; } = "[]";
    public string ShiftsJson { get; set; } = "[]";
    public string CapacityJson { get; set; } = "{}";
    public string RiskJson { get; set; } = "{}";
    public DateTimeOffset UpdatedAtUtc { get; set; }
}

public sealed class NetworkCapacitySnapshot
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid NetworkId { get; set; }
    public DateTimeOffset CapturedAtUtc { get; set; }
    public string FacilityCapacityJson { get; set; } = "[]";
    public string DemandJson { get; set; } = "[]";
    public string FreshnessJson { get; set; } = "{}";
}

public sealed class NetworkScenario
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid NetworkId { get; set; }
    public Guid? AllocationId { get; set; }
    public string ScenarioCode { get; set; } = "A";
    public string Title { get; set; } = string.Empty;
    public string ActionType { get; set; } = string.Empty;
    public Guid SourceFacilityId { get; set; }
    public Guid DestinationFacilityId { get; set; }
    public string OrderId { get; set; } = string.Empty;
    public decimal Score { get; set; }
    public string ComparisonJson { get; set; } = "{}";
    public string Status { get; set; } = "proposed";
    public DateTimeOffset CreatedAtUtc { get; set; }
}

public sealed class NetworkAllocation
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid NetworkId { get; set; }
    public Guid? FactoryDecisionId { get; set; }
    public Guid? SelectedScenarioId { get; set; }
    public string ActionType { get; set; } = string.Empty;
    public string Status { get; set; } = NetworkAllocationStatuses.Draft;
    public Guid SourceFacilityId { get; set; }
    public Guid DestinationFacilityId { get; set; }
    public string TriggerType { get; set; } = string.Empty;
    public string ReasonJson { get; set; } = "{}";
    public string ExpectedImpactJson { get; set; } = "{}";
    public string ActualImpactJson { get; set; } = "{}";
    public string InitiatedBySubjectId { get; set; } = string.Empty;
    public DateTimeOffset? PublishedAtUtc { get; set; }
    public DateTimeOffset? VerifiedAtUtc { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public long ConcurrencyToken { get; set; }
}

public sealed class NetworkAllocationItem
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid AllocationId { get; set; }
    public string OrderId { get; set; } = string.Empty;
    public Guid SourceFacilityId { get; set; }
    public Guid DestinationFacilityId { get; set; }
    public decimal TotalQuantity { get; set; }
    public decimal SourceQuantity { get; set; }
    public decimal DestinationQuantity { get; set; }
    public Guid? SourcePlanVersionId { get; set; }
    public Guid? DestinationPlanVersionId { get; set; }
    public string Status { get; set; } = "planned";
}

public sealed class FactoryNetworkPolicy
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid NetworkId { get; set; }
    public string AutonomyMode { get; set; } = NetworkAutonomyModes.Draft;
    public string PolicyEnvelopeJson { get; set; } = "{}";
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public long ConcurrencyToken { get; set; }
}

public sealed class NetworkActivityEvent
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid NetworkId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public DateTimeOffset OccurredAtUtc { get; set; }
}
