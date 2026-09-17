namespace NevaxisFactOS.Persistence.ModelGateway;

public interface IModelGatewayTenantFacilityEntity
{
    Guid Id { get; set; }
    Guid TenantId { get; set; }
    Guid FacilityId { get; set; }
}

public sealed class ModelGatewayAuditRecord : IModelGatewayTenantFacilityEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid? WorkerId { get; set; }
    public Guid? DigitalWorkCaseId { get; set; }
    public Guid? ContextSnapshotId { get; set; }
    public string RequestClassification { get; set; } = string.Empty;
    public string IntelligencePath { get; set; } = string.Empty;
    public string ProviderId { get; set; } = string.Empty;
    public string ModelName { get; set; } = string.Empty;
    public string ModelVersion { get; set; } = string.Empty;
    public string RoutingReason { get; set; } = string.Empty;
    public bool UsedFallback { get; set; }
    public string ResponseClassification { get; set; } = string.Empty;
    public string ValidationStatus { get; set; } = string.Empty;
    public string StructuredProposalJson { get; set; } = "{}";
    public string ValidationResultJson { get; set; } = "{}";
    public long LatencyMs { get; set; }
    public decimal EstimatedCost { get; set; }
    public int InputTokens { get; set; }
    public int OutputTokens { get; set; }
    public string EngineVersion { get; set; } = ModelGatewayEngineVersions.Router;
    public DateTimeOffset CreatedAtUtc { get; set; }
}

public sealed class ModelGatewayCostUsageRecord : IModelGatewayTenantFacilityEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid? WorkerId { get; set; }
    public Guid? DigitalWorkCaseId { get; set; }
    public string ProviderId { get; set; } = string.Empty;
    public string ModelName { get; set; } = string.Empty;
    public string RequestClassification { get; set; } = string.Empty;
    public int RequestCount { get; set; }
    public int InputTokens { get; set; }
    public int OutputTokens { get; set; }
    public decimal EstimatedCost { get; set; }
    public string BudgetWindow { get; set; } = "daily";
    public DateTimeOffset WindowStartUtc { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
}

public sealed class ModelGatewayBudgetPolicyRecord : IModelGatewayTenantFacilityEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid? WorkerId { get; set; }
    public decimal DailyBudget { get; set; }
    public decimal MonthlyBudget { get; set; }
    public decimal CaseBudget { get; set; }
    public string RestrictedRequestClassesJson { get; set; } = "[]";
    public bool IsActive { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}
