using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using CrevaFactOS.Persistence.Facilities;
using CrevaFactOS.Persistence.Tenancy;

namespace CrevaFactOS.Persistence.Configurations;

internal sealed class EdgeGatewayEntityConfiguration : IEntityTypeConfiguration<Edge.EdgeGateway>
{
    public void Configure(EntityTypeBuilder<Edge.EdgeGateway> entity)
    {
        entity.ToTable("edge_gateways");
        entity.HasKey(g => g.Id);
        entity.Property(g => g.Id).HasColumnName("id");
        entity.Property(g => g.TenantId).HasColumnName("tenant_id");
        entity.Property(g => g.FacilityId).HasColumnName("facility_id");
        entity.Property(g => g.GatewayCode).HasColumnName("gateway_code").HasMaxLength(50);
        entity.Property(g => g.Name).HasColumnName("name").HasMaxLength(200);
        entity.Property(g => g.Description).HasColumnName("description").HasMaxLength(500);
        entity.Property(g => g.VendorKey).HasColumnName("vendor_key").HasMaxLength(30);
        entity.Property(g => g.Status).HasColumnName("status").HasMaxLength(20);
        entity.Property(g => g.LastSeenAtUtc).HasColumnName("last_seen_at_utc");
        entity.Property(g => g.LastHeartbeatAtUtc).HasColumnName("last_heartbeat_at_utc");
        entity.Property(g => g.LastHealthMessage).HasColumnName("last_health_message").HasMaxLength(500);
        entity.Property(g => g.CredentialReference).HasColumnName("credential_reference").HasMaxLength(200);
        entity.Property(g => g.CreatedAtUtc).HasColumnName("created_at_utc");
        entity.Property(g => g.UpdatedAtUtc).HasColumnName("updated_at_utc");
        entity.Property(g => g.Version).HasColumnName("version").IsConcurrencyToken();

        entity.HasIndex(g => new { g.TenantId, g.FacilityId, g.GatewayCode }).IsUnique();
        entity.HasOne<Tenant>().WithMany().HasForeignKey(g => g.TenantId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Facility>().WithMany()
            .HasForeignKey(g => new { g.TenantId, g.FacilityId })
            .HasPrincipalKey(f => new { f.TenantId, f.Id })
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class MachineRegistryEntityConfiguration : IEntityTypeConfiguration<Edge.MachineRegistry>
{
    public void Configure(EntityTypeBuilder<Edge.MachineRegistry> entity)
    {
        entity.ToTable("machine_registry");
        entity.HasKey(m => m.Id);
        entity.Property(m => m.Id).HasColumnName("id");
        entity.Property(m => m.TenantId).HasColumnName("tenant_id");
        entity.Property(m => m.FacilityId).HasColumnName("facility_id");
        entity.Property(m => m.MachineCode).HasColumnName("machine_code").HasMaxLength(50);
        entity.Property(m => m.Name).HasColumnName("name").HasMaxLength(200);
        entity.Property(m => m.Description).HasColumnName("description").HasMaxLength(500);
        entity.Property(m => m.AssetClass).HasColumnName("asset_class").HasMaxLength(50);
        entity.Property(m => m.AssetReference).HasColumnName("asset_reference").HasMaxLength(200);
        entity.Property(m => m.Status).HasColumnName("status").HasMaxLength(20);
        entity.Property(m => m.IsReadyForProduction).HasColumnName("is_ready_for_production");
        entity.Property(m => m.LastSeenAtUtc).HasColumnName("last_seen_at_utc");
        entity.Property(m => m.LastEventAtUtc).HasColumnName("last_event_at_utc");
        entity.Property(m => m.LastKnownStatus).HasColumnName("last_known_status").HasMaxLength(20);
        entity.Property(m => m.LastKnownStatusAtUtc).HasColumnName("last_known_status_at_utc");
        entity.Property(m => m.CreatedAtUtc).HasColumnName("created_at_utc");
        entity.Property(m => m.UpdatedAtUtc).HasColumnName("updated_at_utc");
        entity.Property(m => m.Version).HasColumnName("version").IsConcurrencyToken();

        entity.HasIndex(m => new { m.TenantId, m.FacilityId, m.MachineCode }).IsUnique();
        entity.HasOne<Tenant>().WithMany().HasForeignKey(m => m.TenantId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Facility>().WithMany()
            .HasForeignKey(m => new { m.TenantId, m.FacilityId })
            .HasPrincipalKey(f => new { f.TenantId, f.Id })
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class MachineGatewayAssignmentEntityConfiguration : IEntityTypeConfiguration<Edge.MachineGatewayAssignment>
{
    public void Configure(EntityTypeBuilder<Edge.MachineGatewayAssignment> entity)
    {
        entity.ToTable("machine_gateway_assignments");
        entity.HasKey(a => a.Id);
        entity.Property(a => a.Id).HasColumnName("id");
        entity.Property(a => a.TenantId).HasColumnName("tenant_id");
        entity.Property(a => a.FacilityId).HasColumnName("facility_id");
        entity.Property(a => a.MachineId).HasColumnName("machine_id");
        entity.Property(a => a.GatewayId).HasColumnName("gateway_id");
        entity.Property(a => a.AdapterKey).HasColumnName("adapter_key").HasMaxLength(100);
        entity.Property(a => a.Status).HasColumnName("status").HasMaxLength(20);
        entity.Property(a => a.AssignedAtUtc).HasColumnName("assigned_at_utc");
        entity.Property(a => a.UpdatedAtUtc).HasColumnName("updated_at_utc");

        entity.HasIndex(a => new { a.TenantId, a.FacilityId, a.MachineId, a.GatewayId }).IsUnique();
        entity.HasOne(a => a.Machine).WithMany(m => m.Assignments)
            .HasForeignKey(a => a.MachineId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(a => a.Gateway).WithMany(g => g.Assignments)
            .HasForeignKey(a => a.GatewayId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Tenant>().WithMany().HasForeignKey(a => a.TenantId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Facility>().WithMany()
            .HasForeignKey(a => new { a.TenantId, a.FacilityId })
            .HasPrincipalKey(f => new { f.TenantId, f.Id })
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class MachineEventEntityConfiguration : IEntityTypeConfiguration<Edge.MachineEvent>
{
    public void Configure(EntityTypeBuilder<Edge.MachineEvent> entity)
    {
        entity.ToTable("machine_events");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasColumnName("id");
        entity.Property(e => e.TenantId).HasColumnName("tenant_id");
        entity.Property(e => e.FacilityId).HasColumnName("facility_id");
        entity.Property(e => e.MachineId).HasColumnName("machine_id");
        entity.Property(e => e.GatewayId).HasColumnName("gateway_id");
        entity.Property(e => e.EventKind).HasColumnName("event_kind").HasMaxLength(30);
        entity.Property(e => e.SourceTimestampUtc).HasColumnName("source_timestamp_utc");
        entity.Property(e => e.ReceivedAtUtc).HasColumnName("received_at_utc");
        entity.Property(e => e.SourceSequence).HasColumnName("source_sequence");
        entity.Property(e => e.IdempotencyKey).HasColumnName("idempotency_key").HasMaxLength(200);
        entity.Property(e => e.Payload).HasColumnName("payload").HasMaxLength(4096);
        entity.Property(e => e.IsDuplicate).HasColumnName("is_duplicate");
        entity.Property(e => e.DuplicateOfIdempotencyKey)
            .HasColumnName("duplicate_of_idempotency_key").HasMaxLength(200);

        entity.HasIndex(e => new { e.TenantId, e.FacilityId, e.IdempotencyKey }).IsUnique();
        entity.HasIndex(e => new { e.TenantId, e.FacilityId, e.MachineId, e.SourceSequence });
        entity.HasOne<Tenant>().WithMany().HasForeignKey(e => e.TenantId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Facility>().WithMany()
            .HasForeignKey(e => new { e.TenantId, e.FacilityId })
            .HasPrincipalKey(f => new { f.TenantId, f.Id })
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class MachineRuntimeEntityConfiguration : IEntityTypeConfiguration<Edge.MachineRuntime>
{
    public void Configure(EntityTypeBuilder<Edge.MachineRuntime> entity)
    {
        entity.ToTable("machine_runtime");
        entity.HasKey(r => r.Id);
        entity.Property(r => r.Id).HasColumnName("id");
        entity.Property(r => r.TenantId).HasColumnName("tenant_id");
        entity.Property(r => r.FacilityId).HasColumnName("facility_id");
        entity.Property(r => r.MachineId).HasColumnName("machine_id");
        entity.Property(r => r.Status).HasColumnName("status").HasMaxLength(20);
        entity.Property(r => r.IsReadyForProduction).HasColumnName("is_ready_for_production");
        entity.Property(r => r.LastSeenAtUtc).HasColumnName("last_seen_at_utc");
        entity.Property(r => r.LastEventId).HasColumnName("last_event_id");
        entity.Property(r => r.LastSourceSequence).HasColumnName("last_source_sequence");
        entity.Property(r => r.LastSequenceOutOfOrder).HasColumnName("last_sequence_out_of_order");
        entity.Property(r => r.UpdatedAtUtc).HasColumnName("updated_at_utc");

        entity.HasIndex(r => new { r.TenantId, r.FacilityId, r.MachineId }).IsUnique();
        entity.HasOne<Tenant>().WithMany().HasForeignKey(r => r.TenantId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Facility>().WithMany()
            .HasForeignKey(r => new { r.TenantId, r.FacilityId })
            .HasPrincipalKey(f => new { f.TenantId, f.Id })
            .OnDelete(DeleteBehavior.Restrict);
    }
}
