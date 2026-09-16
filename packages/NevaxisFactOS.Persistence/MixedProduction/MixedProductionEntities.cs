namespace NevaxisFactOS.Persistence.MixedProduction;

public sealed class ManufacturingArea : IMixedProductionTenantFacilityEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}

public sealed class ProductionLine : IMixedProductionTenantFacilityEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid AreaId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? CurrentOrderReference { get; set; }
    public string? CurrentProductCode { get; set; }
    public string? CurrentShiftCode { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}

public sealed class ProductionStation : IMixedProductionTenantFacilityEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid LineId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string StationKind { get; set; } = MixedProductionStationKinds.Process;
    public int SequenceOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}

public sealed class ManufacturingResource : IMixedProductionTenantFacilityEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid StationId { get; set; }
    public string ResourceType { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? MachineRegistryCode { get; set; }
    public int? BufferCapacity { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}

public sealed class ProductRouting : IMixedProductionTenantFacilityEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string Revision { get; set; } = "A";
    public bool IsActive { get; set; } = true;
    public DateTimeOffset EffectiveFromUtc { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}

public sealed class RoutingOperation : IMixedProductionTenantFacilityEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid RoutingId { get; set; }
    public Guid StationId { get; set; }
    public int SequenceOrder { get; set; }
    public string OperationCode { get; set; } = string.Empty;
    public string OperationName { get; set; } = string.Empty;
    public string? WorkInstructionId { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
}

public sealed class BufferResourceState : IMixedProductionTenantFacilityEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid ResourceId { get; set; }
    public int Capacity { get; set; }
    public int CurrentQuantity { get; set; }
    public bool IsBlocked { get; set; }
    public bool IsStarved { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}

public sealed class CanonicalProductionEvent : IMixedProductionTenantFacilityEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid? AreaId { get; set; }
    public Guid? LineId { get; set; }
    public Guid? StationId { get; set; }
    public Guid? ResourceId { get; set; }
    public string? ResourceType { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string? OperationCode { get; set; }
    public string? OrderReference { get; set; }
    public string? ProductCode { get; set; }
    public string? MaterialCode { get; set; }
    public string? ToolCode { get; set; }
    public string? OperatorCode { get; set; }
    public string? ShiftCode { get; set; }
    public decimal? Quantity { get; set; }
    public string? Unit { get; set; }
    public string? ReasonCode { get; set; }
    public string? CorrelationKey { get; set; }
    public string? Source { get; set; }
    public string? SourceEventId { get; set; }
    public string? EvidenceJson { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
    public DateTimeOffset OccurredAtUtc { get; set; }
    public DateTimeOffset ReceivedAtUtc { get; set; }
}

public sealed class OperatorStationSession : IMixedProductionTenantFacilityEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid StationId { get; set; }
    public Guid ResourceId { get; set; }
    public string OperatorCode { get; set; } = string.Empty;
    public string OperationCode { get; set; } = string.Empty;
    public string? OrderReference { get; set; }
    public string? ShiftCode { get; set; }
    public string Status { get; set; } = "waiting";
    public string? Outcome { get; set; }
    public string? ReasonCode { get; set; }
    public Guid? WorkInstructionId { get; set; }
    public DateTimeOffset? StartedAtUtc { get; set; }
    public DateTimeOffset? CompletedAtUtc { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}

public sealed class DigitalWorkInstruction : IMixedProductionTenantFacilityEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public string OperationCode { get; set; } = string.Empty;
    public string? StationCode { get; set; }
    public string? ProductCode { get; set; }
    public string Revision { get; set; } = "A";
    public string Title { get; set; } = string.Empty;
    public string StepsJson { get; set; } = "[]";
    public string? QualityCheckpointsJson { get; set; }
    public string? SafetyNotes { get; set; }
    public DateTimeOffset EffectiveFromUtc { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}

public sealed class ConnectorResourceMapping : IMixedProductionTenantFacilityEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid? ConnectorId { get; set; }
    public string ConnectorCode { get; set; } = string.Empty;
    public string MappingKind { get; set; } = string.Empty;
    public Guid? StationId { get; set; }
    public Guid? ResourceId { get; set; }
    public string? OperationCode { get; set; }
    public string ExternalReference { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}
