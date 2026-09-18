using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace NevaxisZentra.Persistence.MixedProduction;

internal interface IMixedProductionTenantFacilityEntity
{
    Guid Id { get; set; }
    Guid TenantId { get; set; }
    Guid FacilityId { get; set; }
}

internal static class MixedProductionEntityConfigurationExtensions
{
    internal static void MapTenantFacilityColumns<T>(EntityTypeBuilder<T> builder)
        where T : class, IMixedProductionTenantFacilityEntity
    {
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.TenantId).HasColumnName("tenant_id");
        builder.Property(x => x.FacilityId).HasColumnName("facility_id");
    }
}

public sealed class ManufacturingAreaConfiguration : IEntityTypeConfiguration<ManufacturingArea>
{
    public void Configure(EntityTypeBuilder<ManufacturingArea> builder)
    {
        builder.ToTable("mixed_production_areas");
        builder.HasKey(x => x.Id);
        MixedProductionEntityConfigurationExtensions.MapTenantFacilityColumns(builder);
        builder.Property(x => x.Code).HasColumnName("code").HasMaxLength(64).IsRequired();
        builder.Property(x => x.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
        builder.Property(x => x.IsActive).HasColumnName("is_active");
        builder.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc");
        builder.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc");
        builder.HasIndex(x => new { x.TenantId, x.FacilityId, x.Code }).IsUnique();
    }
}

public sealed class ProductionLineConfiguration : IEntityTypeConfiguration<ProductionLine>
{
    public void Configure(EntityTypeBuilder<ProductionLine> builder)
    {
        builder.ToTable("mixed_production_lines");
        builder.HasKey(x => x.Id);
        MixedProductionEntityConfigurationExtensions.MapTenantFacilityColumns(builder);
        builder.Property(x => x.AreaId).HasColumnName("area_id");
        builder.Property(x => x.Code).HasColumnName("code").HasMaxLength(64).IsRequired();
        builder.Property(x => x.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
        builder.Property(x => x.CurrentOrderReference).HasColumnName("current_order_reference").HasMaxLength(64);
        builder.Property(x => x.CurrentProductCode).HasColumnName("current_product_code").HasMaxLength(64);
        builder.Property(x => x.CurrentShiftCode).HasColumnName("current_shift_code").HasMaxLength(32);
        builder.Property(x => x.IsActive).HasColumnName("is_active");
        builder.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc");
        builder.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc");
        builder.HasIndex(x => new { x.TenantId, x.FacilityId, x.Code }).IsUnique();
    }
}

public sealed class ProductionStationConfiguration : IEntityTypeConfiguration<ProductionStation>
{
    public void Configure(EntityTypeBuilder<ProductionStation> builder)
    {
        builder.ToTable("mixed_production_stations");
        builder.HasKey(x => x.Id);
        MixedProductionEntityConfigurationExtensions.MapTenantFacilityColumns(builder);
        builder.Property(x => x.LineId).HasColumnName("line_id");
        builder.Property(x => x.Code).HasColumnName("code").HasMaxLength(64).IsRequired();
        builder.Property(x => x.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
        builder.Property(x => x.StationKind).HasColumnName("station_kind").HasMaxLength(32).IsRequired();
        builder.Property(x => x.SequenceOrder).HasColumnName("sequence_order");
        builder.Property(x => x.IsActive).HasColumnName("is_active");
        builder.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc");
        builder.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc");
        builder.HasIndex(x => new { x.TenantId, x.FacilityId, x.LineId, x.Code }).IsUnique();
    }
}

public sealed class ManufacturingResourceConfiguration : IEntityTypeConfiguration<ManufacturingResource>
{
    public void Configure(EntityTypeBuilder<ManufacturingResource> builder)
    {
        builder.ToTable("mixed_production_resources");
        builder.HasKey(x => x.Id);
        MixedProductionEntityConfigurationExtensions.MapTenantFacilityColumns(builder);
        builder.Property(x => x.StationId).HasColumnName("station_id");
        builder.Property(x => x.ResourceType).HasColumnName("resource_type").HasMaxLength(32).IsRequired();
        builder.Property(x => x.Code).HasColumnName("code").HasMaxLength(64).IsRequired();
        builder.Property(x => x.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
        builder.Property(x => x.MachineRegistryCode).HasColumnName("machine_registry_code").HasMaxLength(64);
        builder.Property(x => x.BufferCapacity).HasColumnName("buffer_capacity");
        builder.Property(x => x.IsActive).HasColumnName("is_active");
        builder.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc");
        builder.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc");
        builder.HasIndex(x => new { x.TenantId, x.FacilityId, x.StationId, x.Code }).IsUnique();
    }
}

public sealed class ProductRoutingConfiguration : IEntityTypeConfiguration<ProductRouting>
{
    public void Configure(EntityTypeBuilder<ProductRouting> builder)
    {
        builder.ToTable("mixed_production_product_routings");
        builder.HasKey(x => x.Id);
        MixedProductionEntityConfigurationExtensions.MapTenantFacilityColumns(builder);
        builder.Property(x => x.ProductCode).HasColumnName("product_code").HasMaxLength(64).IsRequired();
        builder.Property(x => x.ProductName).HasColumnName("product_name").HasMaxLength(200).IsRequired();
        builder.Property(x => x.Revision).HasColumnName("revision").HasMaxLength(16).IsRequired();
        builder.Property(x => x.IsActive).HasColumnName("is_active");
        builder.Property(x => x.EffectiveFromUtc).HasColumnName("effective_from_utc");
        builder.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc");
        builder.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc");
        builder.HasIndex(x => new { x.TenantId, x.FacilityId, x.ProductCode, x.Revision }).IsUnique();
    }
}

public sealed class RoutingOperationConfiguration : IEntityTypeConfiguration<RoutingOperation>
{
    public void Configure(EntityTypeBuilder<RoutingOperation> builder)
    {
        builder.ToTable("mixed_production_routing_operations");
        builder.HasKey(x => x.Id);
        MixedProductionEntityConfigurationExtensions.MapTenantFacilityColumns(builder);
        builder.Property(x => x.RoutingId).HasColumnName("routing_id");
        builder.Property(x => x.StationId).HasColumnName("station_id");
        builder.Property(x => x.SequenceOrder).HasColumnName("sequence_order");
        builder.Property(x => x.OperationCode).HasColumnName("operation_code").HasMaxLength(64).IsRequired();
        builder.Property(x => x.OperationName).HasColumnName("operation_name").HasMaxLength(200).IsRequired();
        builder.Property(x => x.WorkInstructionId).HasColumnName("work_instruction_id").HasMaxLength(64);
        builder.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc");
        builder.HasIndex(x => new { x.TenantId, x.FacilityId, x.RoutingId, x.SequenceOrder }).IsUnique();
    }
}

public sealed class BufferResourceStateConfiguration : IEntityTypeConfiguration<BufferResourceState>
{
    public void Configure(EntityTypeBuilder<BufferResourceState> builder)
    {
        builder.ToTable("mixed_production_buffer_states");
        builder.HasKey(x => x.Id);
        MixedProductionEntityConfigurationExtensions.MapTenantFacilityColumns(builder);
        builder.Property(x => x.ResourceId).HasColumnName("resource_id");
        builder.Property(x => x.Capacity).HasColumnName("capacity");
        builder.Property(x => x.CurrentQuantity).HasColumnName("current_quantity");
        builder.Property(x => x.IsBlocked).HasColumnName("is_blocked");
        builder.Property(x => x.IsStarved).HasColumnName("is_starved");
        builder.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc");
        builder.HasIndex(x => new { x.TenantId, x.FacilityId, x.ResourceId }).IsUnique();
    }
}

public sealed class CanonicalProductionEventConfiguration : IEntityTypeConfiguration<CanonicalProductionEvent>
{
    public void Configure(EntityTypeBuilder<CanonicalProductionEvent> builder)
    {
        builder.ToTable("mixed_production_events");
        builder.HasKey(x => x.Id);
        MixedProductionEntityConfigurationExtensions.MapTenantFacilityColumns(builder);
        builder.Property(x => x.AreaId).HasColumnName("area_id");
        builder.Property(x => x.LineId).HasColumnName("line_id");
        builder.Property(x => x.StationId).HasColumnName("station_id");
        builder.Property(x => x.ResourceId).HasColumnName("resource_id");
        builder.Property(x => x.ResourceType).HasColumnName("resource_type").HasMaxLength(32);
        builder.Property(x => x.EventType).HasColumnName("event_type").HasMaxLength(64).IsRequired();
        builder.Property(x => x.OperationCode).HasColumnName("operation_code").HasMaxLength(64);
        builder.Property(x => x.OrderReference).HasColumnName("order_reference").HasMaxLength(64);
        builder.Property(x => x.ProductCode).HasColumnName("product_code").HasMaxLength(64);
        builder.Property(x => x.MaterialCode).HasColumnName("material_code").HasMaxLength(64);
        builder.Property(x => x.ToolCode).HasColumnName("tool_code").HasMaxLength(64);
        builder.Property(x => x.OperatorCode).HasColumnName("operator_code").HasMaxLength(64);
        builder.Property(x => x.ShiftCode).HasColumnName("shift_code").HasMaxLength(32);
        builder.Property(x => x.Quantity).HasColumnName("quantity");
        builder.Property(x => x.Unit).HasColumnName("unit").HasMaxLength(16);
        builder.Property(x => x.ReasonCode).HasColumnName("reason_code").HasMaxLength(64);
        builder.Property(x => x.CorrelationKey).HasColumnName("correlation_key").HasMaxLength(256);
        builder.Property(x => x.Source).HasColumnName("source").HasMaxLength(64);
        builder.Property(x => x.SourceEventId).HasColumnName("source_event_id").HasMaxLength(128);
        builder.Property(x => x.EvidenceJson).HasColumnName("evidence_json");
        builder.Property(x => x.IdempotencyKey).HasColumnName("idempotency_key").HasMaxLength(256).IsRequired();
        builder.Property(x => x.OccurredAtUtc).HasColumnName("occurred_at_utc");
        builder.Property(x => x.ReceivedAtUtc).HasColumnName("received_at_utc");
        builder.HasIndex(x => new { x.TenantId, x.FacilityId, x.IdempotencyKey }).IsUnique();
        builder.HasIndex(x => new { x.TenantId, x.FacilityId, x.LineId, x.OccurredAtUtc });
    }
}

public sealed class OperatorStationSessionConfiguration : IEntityTypeConfiguration<OperatorStationSession>
{
    public void Configure(EntityTypeBuilder<OperatorStationSession> builder)
    {
        builder.ToTable("mixed_production_operator_sessions");
        builder.HasKey(x => x.Id);
        MixedProductionEntityConfigurationExtensions.MapTenantFacilityColumns(builder);
        builder.Property(x => x.StationId).HasColumnName("station_id");
        builder.Property(x => x.ResourceId).HasColumnName("resource_id");
        builder.Property(x => x.OperatorCode).HasColumnName("operator_code").HasMaxLength(64).IsRequired();
        builder.Property(x => x.OperationCode).HasColumnName("operation_code").HasMaxLength(64).IsRequired();
        builder.Property(x => x.OrderReference).HasColumnName("order_reference").HasMaxLength(64);
        builder.Property(x => x.ShiftCode).HasColumnName("shift_code").HasMaxLength(32);
        builder.Property(x => x.Status).HasColumnName("status").HasMaxLength(32).IsRequired();
        builder.Property(x => x.Outcome).HasColumnName("outcome").HasMaxLength(32);
        builder.Property(x => x.ReasonCode).HasColumnName("reason_code").HasMaxLength(64);
        builder.Property(x => x.WorkInstructionId).HasColumnName("work_instruction_id");
        builder.Property(x => x.StartedAtUtc).HasColumnName("started_at_utc");
        builder.Property(x => x.CompletedAtUtc).HasColumnName("completed_at_utc");
        builder.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc");
        builder.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc");
        builder.HasIndex(x => new { x.TenantId, x.FacilityId, x.StationId, x.Status });
    }
}

public sealed class DigitalWorkInstructionConfiguration : IEntityTypeConfiguration<DigitalWorkInstruction>
{
    public void Configure(EntityTypeBuilder<DigitalWorkInstruction> builder)
    {
        builder.ToTable("mixed_production_work_instructions");
        builder.HasKey(x => x.Id);
        MixedProductionEntityConfigurationExtensions.MapTenantFacilityColumns(builder);
        builder.Property(x => x.OperationCode).HasColumnName("operation_code").HasMaxLength(64).IsRequired();
        builder.Property(x => x.StationCode).HasColumnName("station_code").HasMaxLength(64);
        builder.Property(x => x.ProductCode).HasColumnName("product_code").HasMaxLength(64);
        builder.Property(x => x.Revision).HasColumnName("revision").HasMaxLength(16).IsRequired();
        builder.Property(x => x.Title).HasColumnName("title").HasMaxLength(256).IsRequired();
        builder.Property(x => x.StepsJson).HasColumnName("steps_json");
        builder.Property(x => x.QualityCheckpointsJson).HasColumnName("quality_checkpoints_json");
        builder.Property(x => x.SafetyNotes).HasColumnName("safety_notes");
        builder.Property(x => x.EffectiveFromUtc).HasColumnName("effective_from_utc");
        builder.Property(x => x.IsActive).HasColumnName("is_active");
        builder.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc");
        builder.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc");
        builder.HasIndex(x => new { x.TenantId, x.FacilityId, x.OperationCode, x.Revision });
    }
}

public sealed class ConnectorResourceMappingConfiguration : IEntityTypeConfiguration<ConnectorResourceMapping>
{
    public void Configure(EntityTypeBuilder<ConnectorResourceMapping> builder)
    {
        builder.ToTable("mixed_production_connector_mappings");
        builder.HasKey(x => x.Id);
        MixedProductionEntityConfigurationExtensions.MapTenantFacilityColumns(builder);
        builder.Property(x => x.ConnectorId).HasColumnName("connector_id");
        builder.Property(x => x.ConnectorCode).HasColumnName("connector_code").HasMaxLength(64).IsRequired();
        builder.Property(x => x.MappingKind).HasColumnName("mapping_kind").HasMaxLength(32).IsRequired();
        builder.Property(x => x.StationId).HasColumnName("station_id");
        builder.Property(x => x.ResourceId).HasColumnName("resource_id");
        builder.Property(x => x.OperationCode).HasColumnName("operation_code").HasMaxLength(64);
        builder.Property(x => x.ExternalReference).HasColumnName("external_reference").HasMaxLength(256).IsRequired();
        builder.Property(x => x.IsActive).HasColumnName("is_active");
        builder.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc");
        builder.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc");
        builder.HasIndex(x => new { x.TenantId, x.FacilityId, x.ConnectorCode, x.ExternalReference }).IsUnique();
    }
}
