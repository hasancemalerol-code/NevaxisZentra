using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace NevaxisFactOS.Persistence.FactoryOrchestration;

internal sealed class FactoryCaseDependencyEntityConfiguration : IEntityTypeConfiguration<FactoryCaseDependency>
{
    public void Configure(EntityTypeBuilder<FactoryCaseDependency> entity)
    {
        entity.ToTable("factory_case_dependencies");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasColumnName("id");
        entity.Property(e => e.TenantId).HasColumnName("tenant_id");
        entity.Property(e => e.FacilityId).HasColumnName("facility_id");
        entity.Property(e => e.RootCaseId).HasColumnName("root_case_id");
        entity.Property(e => e.FromNodeKey).HasColumnName("from_node_key").HasMaxLength(120);
        entity.Property(e => e.ToNodeKey).HasColumnName("to_node_key").HasMaxLength(120);
        entity.Property(e => e.FromNodeType).HasColumnName("from_node_type").HasMaxLength(30);
        entity.Property(e => e.ToNodeType).HasColumnName("to_node_type").HasMaxLength(30);
        entity.Property(e => e.EdgeType).HasColumnName("edge_type").HasMaxLength(30);
        entity.Property(e => e.LinkedCaseId).HasColumnName("linked_case_id").HasMaxLength(120);
        entity.Property(e => e.LinkedWorkerId).HasColumnName("linked_worker_id").HasMaxLength(120);
        entity.Property(e => e.LinkedTaskId).HasColumnName("linked_task_id").HasMaxLength(120);
        entity.Property(e => e.MetadataJson).HasColumnName("metadata_json");
        entity.Property(e => e.CreatedAtUtc).HasColumnName("created_at_utc");
        entity.HasIndex(e => new { e.TenantId, e.RootCaseId })
            .HasDatabaseName("ix_factory_case_dependencies_tenant_root");
    }
}

internal sealed class FactoryEventCorrelationEntityConfiguration : IEntityTypeConfiguration<FactoryEventCorrelation>
{
    public void Configure(EntityTypeBuilder<FactoryEventCorrelation> entity)
    {
        entity.ToTable("factory_event_correlations");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasColumnName("id");
        entity.Property(e => e.TenantId).HasColumnName("tenant_id");
        entity.Property(e => e.FacilityId).HasColumnName("facility_id");
        entity.Property(e => e.CorrelationKey).HasColumnName("correlation_key").HasMaxLength(200);
        entity.Property(e => e.RootCaseId).HasColumnName("root_case_id");
        entity.Property(e => e.SourceType).HasColumnName("source_type").HasMaxLength(60);
        entity.Property(e => e.SourceId).HasColumnName("source_id").HasMaxLength(120);
        entity.Property(e => e.EventCategory).HasColumnName("event_category").HasMaxLength(60);
        entity.Property(e => e.FirstSeenAtUtc).HasColumnName("first_seen_at_utc");
        entity.Property(e => e.LastSeenAtUtc).HasColumnName("last_seen_at_utc");
        entity.HasIndex(e => new { e.TenantId, e.CorrelationKey })
            .IsUnique()
            .HasDatabaseName("ux_factory_event_correlations_tenant_key");
        entity.HasIndex(e => new { e.TenantId, e.FacilityId, e.SourceType, e.SourceId })
            .HasDatabaseName("ix_factory_event_correlations_tenant_source");
    }
}
