using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace NevaxisFactOS.Persistence.MixedProduction;

internal static class MixedProductionEntityConfigurationExtensions
{
    public static void ConfigureTenantFacility<T>(EntityTypeBuilder<T> builder, string tableName)
        where T : class
    {
        builder.ToTable(tableName);
        builder.HasKey("Id");
        builder.Property<Guid>("Id").HasColumnName("id");
        builder.Property<Guid>("TenantId").HasColumnName("tenant_id");
        builder.Property<Guid>("FacilityId").HasColumnName("facility_id");
    }
}

public sealed class ManufacturingAreaConfiguration : IEntityTypeConfiguration<ManufacturingArea>
{
    public void Configure(EntityTypeBuilder<ManufacturingArea> builder)
    {
        builder.ToTable("mixed_production_areas");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Code).HasMaxLength(64).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.HasIndex(x => new { x.TenantId, x.FacilityId, x.Code }).IsUnique();
    }
}

public sealed class ProductionLineConfiguration : IEntityTypeConfiguration<ProductionLine>
{
    public void Configure(EntityTypeBuilder<ProductionLine> builder)
    {
        builder.ToTable("mixed_production_lines");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Code).HasMaxLength(64).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.CurrentOrderReference).HasMaxLength(64);
        builder.Property(x => x.CurrentProductCode).HasMaxLength(64);
        builder.Property(x => x.CurrentShiftCode).HasMaxLength(32);
        builder.HasIndex(x => new { x.TenantId, x.FacilityId, x.Code }).IsUnique();
    }
}

public sealed class ProductionStationConfiguration : IEntityTypeConfiguration<ProductionStation>
{
    public void Configure(EntityTypeBuilder<ProductionStation> builder)
    {
        builder.ToTable("mixed_production_stations");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Code).HasMaxLength(64).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.StationKind).HasMaxLength(32).IsRequired();
        builder.HasIndex(x => new { x.TenantId, x.FacilityId, x.LineId, x.Code }).IsUnique();
    }
}

public sealed class ManufacturingResourceConfiguration : IEntityTypeConfiguration<ManufacturingResource>
{
    public void Configure(EntityTypeBuilder<ManufacturingResource> builder)
    {
        builder.ToTable("mixed_production_resources");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ResourceType).HasMaxLength(32).IsRequired();
        builder.Property(x => x.Code).HasMaxLength(64).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.MachineRegistryCode).HasMaxLength(64);
        builder.HasIndex(x => new { x.TenantId, x.FacilityId, x.StationId, x.Code }).IsUnique();
    }
}

public sealed class ProductRoutingConfiguration : IEntityTypeConfiguration<ProductRouting>
{
    public void Configure(EntityTypeBuilder<ProductRouting> builder)
    {
        builder.ToTable("mixed_production_product_routings");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ProductCode).HasMaxLength(64).IsRequired();
        builder.Property(x => x.ProductName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Revision).HasMaxLength(16).IsRequired();
        builder.HasIndex(x => new { x.TenantId, x.FacilityId, x.ProductCode, x.Revision }).IsUnique();
    }
}

public sealed class RoutingOperationConfiguration : IEntityTypeConfiguration<RoutingOperation>
{
    public void Configure(EntityTypeBuilder<RoutingOperation> builder)
    {
        builder.ToTable("mixed_production_routing_operations");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.OperationCode).HasMaxLength(64).IsRequired();
        builder.Property(x => x.OperationName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.WorkInstructionId).HasMaxLength(64);
        builder.HasIndex(x => new { x.TenantId, x.FacilityId, x.RoutingId, x.SequenceOrder }).IsUnique();
    }
}

public sealed class BufferResourceStateConfiguration : IEntityTypeConfiguration<BufferResourceState>
{
    public void Configure(EntityTypeBuilder<BufferResourceState> builder)
    {
        builder.ToTable("mixed_production_buffer_states");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.TenantId, x.FacilityId, x.ResourceId }).IsUnique();
    }
}

public sealed class CanonicalProductionEventConfiguration : IEntityTypeConfiguration<CanonicalProductionEvent>
{
    public void Configure(EntityTypeBuilder<CanonicalProductionEvent> builder)
    {
        builder.ToTable("mixed_production_events");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.EventType).HasMaxLength(64).IsRequired();
        builder.Property(x => x.ResourceType).HasMaxLength(32);
        builder.Property(x => x.OperationCode).HasMaxLength(64);
        builder.Property(x => x.OrderReference).HasMaxLength(64);
        builder.Property(x => x.ProductCode).HasMaxLength(64);
        builder.Property(x => x.MaterialCode).HasMaxLength(64);
        builder.Property(x => x.ToolCode).HasMaxLength(64);
        builder.Property(x => x.OperatorCode).HasMaxLength(64);
        builder.Property(x => x.ShiftCode).HasMaxLength(32);
        builder.Property(x => x.Unit).HasMaxLength(16);
        builder.Property(x => x.ReasonCode).HasMaxLength(64);
        builder.Property(x => x.CorrelationKey).HasMaxLength(256);
        builder.Property(x => x.Source).HasMaxLength(64);
        builder.Property(x => x.SourceEventId).HasMaxLength(128);
        builder.Property(x => x.IdempotencyKey).HasMaxLength(256).IsRequired();
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
        builder.Property(x => x.OperatorCode).HasMaxLength(64).IsRequired();
        builder.Property(x => x.OperationCode).HasMaxLength(64).IsRequired();
        builder.Property(x => x.OrderReference).HasMaxLength(64);
        builder.Property(x => x.ShiftCode).HasMaxLength(32);
        builder.Property(x => x.Status).HasMaxLength(32).IsRequired();
        builder.Property(x => x.Outcome).HasMaxLength(32);
        builder.Property(x => x.ReasonCode).HasMaxLength(64);
        builder.HasIndex(x => new { x.TenantId, x.FacilityId, x.StationId, x.Status });
    }
}

public sealed class DigitalWorkInstructionConfiguration : IEntityTypeConfiguration<DigitalWorkInstruction>
{
    public void Configure(EntityTypeBuilder<DigitalWorkInstruction> builder)
    {
        builder.ToTable("mixed_production_work_instructions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.OperationCode).HasMaxLength(64).IsRequired();
        builder.Property(x => x.StationCode).HasMaxLength(64);
        builder.Property(x => x.ProductCode).HasMaxLength(64);
        builder.Property(x => x.Revision).HasMaxLength(16).IsRequired();
        builder.Property(x => x.Title).HasMaxLength(256).IsRequired();
        builder.HasIndex(x => new { x.TenantId, x.FacilityId, x.OperationCode, x.Revision });
    }
}

public sealed class ConnectorResourceMappingConfiguration : IEntityTypeConfiguration<ConnectorResourceMapping>
{
    public void Configure(EntityTypeBuilder<ConnectorResourceMapping> builder)
    {
        builder.ToTable("mixed_production_connector_mappings");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ConnectorCode).HasMaxLength(64).IsRequired();
        builder.Property(x => x.MappingKind).HasMaxLength(32).IsRequired();
        builder.Property(x => x.OperationCode).HasMaxLength(64);
        builder.Property(x => x.ExternalReference).HasMaxLength(256).IsRequired();
        builder.HasIndex(x => new { x.TenantId, x.FacilityId, x.ConnectorCode, x.ExternalReference }).IsUnique();
    }
}
