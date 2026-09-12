using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NevaxisFactOS.Persistence.Facilities;
using NevaxisFactOS.Persistence.Tenancy;

namespace NevaxisFactOS.Persistence.Configurations;

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

// ============================================================
// EDGE-2 entity configurations
// ============================================================

internal sealed class MachineTelemetrySampleEntityConfiguration : IEntityTypeConfiguration<Edge.MachineTelemetrySample>
{
    public void Configure(EntityTypeBuilder<Edge.MachineTelemetrySample> entity)
    {
        entity.ToTable("machine_telemetry");
        entity.HasKey(t => t.Id);
        entity.Property(t => t.Id).HasColumnName("id");
        entity.Property(t => t.TenantId).HasColumnName("tenant_id");
        entity.Property(t => t.FacilityId).HasColumnName("facility_id");
        entity.Property(t => t.MachineId).HasColumnName("machine_id");
        entity.Property(t => t.GatewayId).HasColumnName("gateway_id");
        entity.Property(t => t.MetricKey).HasColumnName("metric_key").HasMaxLength(80);
        entity.Property(t => t.NumericValue).HasColumnName("numeric_value");
        entity.Property(t => t.TextValue).HasColumnName("text_value").HasMaxLength(500);
        entity.Property(t => t.BooleanValue).HasColumnName("boolean_value");
        entity.Property(t => t.Unit).HasColumnName("unit").HasMaxLength(30);
        entity.Property(t => t.Quality).HasColumnName("quality").HasMaxLength(20);
        entity.Property(t => t.Transport).HasColumnName("transport").HasMaxLength(30);
        entity.Property(t => t.SourceTimestampUtc).HasColumnName("source_timestamp_utc");
        entity.Property(t => t.ReceivedAtUtc).HasColumnName("received_at_utc");
        entity.Property(t => t.SourceSequence).HasColumnName("source_sequence");
        entity.Property(t => t.IdempotencyKey).HasColumnName("idempotency_key").HasMaxLength(200);
        entity.Property(t => t.IsDuplicate).HasColumnName("is_duplicate");
        entity.Property(t => t.CreatedAtUtc).HasColumnName("created_at_utc");

        entity.HasIndex(t => new { t.TenantId, t.FacilityId, t.IdempotencyKey }).IsUnique();
        entity.HasIndex(t => new { t.TenantId, t.FacilityId, t.MachineId, t.MetricKey, t.SourceTimestampUtc });
        entity.HasOne<Tenant>().WithMany().HasForeignKey(t => t.TenantId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Facility>().WithMany()
            .HasForeignKey(t => new { t.TenantId, t.FacilityId })
            .HasPrincipalKey(f => new { f.TenantId, f.Id })
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class MachineLatestTelemetryEntityConfiguration : IEntityTypeConfiguration<Edge.MachineLatestTelemetry>
{
    public void Configure(EntityTypeBuilder<Edge.MachineLatestTelemetry> entity)
    {
        entity.ToTable("machine_latest_telemetry");
        entity.HasKey(t => t.Id);
        entity.Property(t => t.Id).HasColumnName("id");
        entity.Property(t => t.TenantId).HasColumnName("tenant_id");
        entity.Property(t => t.FacilityId).HasColumnName("facility_id");
        entity.Property(t => t.MachineId).HasColumnName("machine_id");
        entity.Property(t => t.MetricKey).HasColumnName("metric_key").HasMaxLength(80);
        entity.Property(t => t.NumericValue).HasColumnName("numeric_value");
        entity.Property(t => t.TextValue).HasColumnName("text_value").HasMaxLength(500);
        entity.Property(t => t.BooleanValue).HasColumnName("boolean_value");
        entity.Property(t => t.Unit).HasColumnName("unit").HasMaxLength(30);
        entity.Property(t => t.Quality).HasColumnName("quality").HasMaxLength(20);
        entity.Property(t => t.SourceTimestampUtc).HasColumnName("source_timestamp_utc");
        entity.Property(t => t.ReceivedAtUtc).HasColumnName("received_at_utc");
        entity.Property(t => t.SourceSequence).HasColumnName("source_sequence");
        entity.Property(t => t.UpdatedAtUtc).HasColumnName("updated_at_utc");

        entity.HasIndex(t => new { t.TenantId, t.FacilityId, t.MachineId, t.MetricKey }).IsUnique();
        entity.HasOne<Tenant>().WithMany().HasForeignKey(t => t.TenantId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Facility>().WithMany()
            .HasForeignKey(t => new { t.TenantId, t.FacilityId })
            .HasPrincipalKey(f => new { f.TenantId, f.Id })
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class MachineObservedCycleEntityConfiguration : IEntityTypeConfiguration<Edge.MachineObservedCycle>
{
    public void Configure(EntityTypeBuilder<Edge.MachineObservedCycle> entity)
    {
        entity.ToTable("machine_observed_cycles");
        entity.HasKey(c => c.Id);
        entity.Property(c => c.Id).HasColumnName("id");
        entity.Property(c => c.TenantId).HasColumnName("tenant_id");
        entity.Property(c => c.FacilityId).HasColumnName("facility_id");
        entity.Property(c => c.MachineId).HasColumnName("machine_id");
        entity.Property(c => c.GatewayId).HasColumnName("gateway_id");
        entity.Property(c => c.CycleIdentity).HasColumnName("cycle_identity").HasMaxLength(200);
        entity.Property(c => c.CycleDurationMs).HasColumnName("cycle_duration_ms");
        entity.Property(c => c.ObservedCounter).HasColumnName("observed_counter");
        entity.Property(c => c.Quality).HasColumnName("quality").HasMaxLength(20);
        entity.Property(c => c.SourceTimestampUtc).HasColumnName("source_timestamp_utc");
        entity.Property(c => c.ReceivedAtUtc).HasColumnName("received_at_utc");
        entity.Property(c => c.SourceSequence).HasColumnName("source_sequence");
        entity.Property(c => c.IdempotencyKey).HasColumnName("idempotency_key").HasMaxLength(200);
        entity.Property(c => c.IsDuplicate).HasColumnName("is_duplicate");
        entity.Property(c => c.IsOutOfOrder).HasColumnName("is_out_of_order");
        entity.Property(c => c.CreatedAtUtc).HasColumnName("created_at_utc");

        entity.HasIndex(c => new { c.TenantId, c.FacilityId, c.IdempotencyKey }).IsUnique();
        entity.HasIndex(c => new { c.TenantId, c.FacilityId, c.MachineId, c.SourceTimestampUtc });
        entity.HasOne<Tenant>().WithMany().HasForeignKey(c => c.TenantId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Facility>().WithMany()
            .HasForeignKey(c => new { c.TenantId, c.FacilityId })
            .HasPrincipalKey(f => new { f.TenantId, f.Id })
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class MachineCycleProjectionEntityConfiguration : IEntityTypeConfiguration<Edge.MachineCycleProjection>
{
    public void Configure(EntityTypeBuilder<Edge.MachineCycleProjection> entity)
    {
        entity.ToTable("machine_cycle_projection");
        entity.HasKey(c => c.Id);
        entity.Property(c => c.Id).HasColumnName("id");
        entity.Property(c => c.TenantId).HasColumnName("tenant_id");
        entity.Property(c => c.FacilityId).HasColumnName("facility_id");
        entity.Property(c => c.MachineId).HasColumnName("machine_id");
        entity.Property(c => c.ObservedCycleCount).HasColumnName("observed_cycle_count");
        entity.Property(c => c.LatestObservedCounter).HasColumnName("latest_observed_counter");
        entity.Property(c => c.LastCycleAtUtc).HasColumnName("last_cycle_at_utc");
        entity.Property(c => c.LastCycleDurationMs).HasColumnName("last_cycle_duration_ms");
        entity.Property(c => c.AverageRecentCycleDurationMs).HasColumnName("average_recent_cycle_duration_ms");
        entity.Property(c => c.LastSourceSequence).HasColumnName("last_source_sequence");
        entity.Property(c => c.UpdatedAtUtc).HasColumnName("updated_at_utc");

        entity.HasIndex(c => new { c.TenantId, c.FacilityId, c.MachineId }).IsUnique();
        entity.HasOne<Tenant>().WithMany().HasForeignKey(c => c.TenantId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Facility>().WithMany()
            .HasForeignKey(c => new { c.TenantId, c.FacilityId })
            .HasPrincipalKey(f => new { f.TenantId, f.Id })
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class MachineCounterObservationEntityConfiguration : IEntityTypeConfiguration<Edge.MachineCounterObservation>
{
    public void Configure(EntityTypeBuilder<Edge.MachineCounterObservation> entity)
    {
        entity.ToTable("machine_counter_observations");
        entity.HasKey(c => c.Id);
        entity.Property(c => c.Id).HasColumnName("id");
        entity.Property(c => c.TenantId).HasColumnName("tenant_id");
        entity.Property(c => c.FacilityId).HasColumnName("facility_id");
        entity.Property(c => c.MachineId).HasColumnName("machine_id");
        entity.Property(c => c.GatewayId).HasColumnName("gateway_id");
        entity.Property(c => c.CounterKey).HasColumnName("counter_key").HasMaxLength(80);
        entity.Property(c => c.CounterValue).HasColumnName("counter_value");
        entity.Property(c => c.Quality).HasColumnName("quality").HasMaxLength(20);
        entity.Property(c => c.CounterResetSuspected).HasColumnName("counter_reset_suspected");
        entity.Property(c => c.SourceTimestampUtc).HasColumnName("source_timestamp_utc");
        entity.Property(c => c.ReceivedAtUtc).HasColumnName("received_at_utc");
        entity.Property(c => c.SourceSequence).HasColumnName("source_sequence");
        entity.Property(c => c.IdempotencyKey).HasColumnName("idempotency_key").HasMaxLength(200);
        entity.Property(c => c.CreatedAtUtc).HasColumnName("created_at_utc");

        entity.HasIndex(c => new { c.TenantId, c.FacilityId, c.IdempotencyKey }).IsUnique();
        entity.HasIndex(c => new { c.TenantId, c.FacilityId, c.MachineId, c.CounterKey, c.SourceTimestampUtc });
        entity.HasOne<Tenant>().WithMany().HasForeignKey(c => c.TenantId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Facility>().WithMany()
            .HasForeignKey(c => new { c.TenantId, c.FacilityId })
            .HasPrincipalKey(f => new { f.TenantId, f.Id })
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class MachineStateTransitionEntityConfiguration : IEntityTypeConfiguration<Edge.MachineStateTransition>
{
    public void Configure(EntityTypeBuilder<Edge.MachineStateTransition> entity)
    {
        entity.ToTable("machine_state_transitions");
        entity.HasKey(t => t.Id);
        entity.Property(t => t.Id).HasColumnName("id");
        entity.Property(t => t.TenantId).HasColumnName("tenant_id");
        entity.Property(t => t.FacilityId).HasColumnName("facility_id");
        entity.Property(t => t.MachineId).HasColumnName("machine_id");
        entity.Property(t => t.FromState).HasColumnName("from_state").HasMaxLength(20);
        entity.Property(t => t.ToState).HasColumnName("to_state").HasMaxLength(20);
        entity.Property(t => t.Reason).HasColumnName("reason").HasMaxLength(200);
        entity.Property(t => t.SourceTimestampUtc).HasColumnName("source_timestamp_utc");
        entity.Property(t => t.ReceivedAtUtc).HasColumnName("received_at_utc");
        entity.Property(t => t.SourceSequence).HasColumnName("source_sequence");
        entity.Property(t => t.IsOutOfOrder).HasColumnName("is_out_of_order");
        entity.Property(t => t.CreatedAtUtc).HasColumnName("created_at_utc");

        entity.HasIndex(t => new { t.TenantId, t.FacilityId, t.MachineId, t.SourceTimestampUtc });
        entity.HasOne<Tenant>().WithMany().HasForeignKey(t => t.TenantId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Facility>().WithMany()
            .HasForeignKey(t => new { t.TenantId, t.FacilityId })
            .HasPrincipalKey(f => new { f.TenantId, f.Id })
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class MachineSignalProjectionEntityConfiguration : IEntityTypeConfiguration<Edge.MachineSignalProjection>
{
    public void Configure(EntityTypeBuilder<Edge.MachineSignalProjection> entity)
    {
        entity.ToTable("machine_signal_projection");
        entity.HasKey(s => s.Id);
        entity.Property(s => s.Id).HasColumnName("id");
        entity.Property(s => s.TenantId).HasColumnName("tenant_id");
        entity.Property(s => s.FacilityId).HasColumnName("facility_id");
        entity.Property(s => s.MachineId).HasColumnName("machine_id");
        entity.Property(s => s.Connectivity).HasColumnName("connectivity").HasMaxLength(20);
        entity.Property(s => s.ObservedState).HasColumnName("observed_state").HasMaxLength(20);
        entity.Property(s => s.LastCycleAtUtc).HasColumnName("last_cycle_at_utc");
        entity.Property(s => s.LastCycleDurationMs).HasColumnName("last_cycle_duration_ms");
        entity.Property(s => s.ObservedCycleCount).HasColumnName("observed_cycle_count");
        entity.Property(s => s.LatestCounter).HasColumnName("latest_counter");
        entity.Property(s => s.Quality).HasColumnName("quality").HasMaxLength(20);
        entity.Property(s => s.LastTelemetryAtUtc).HasColumnName("last_telemetry_at_utc");
        entity.Property(s => s.IsInObservedDowntime).HasColumnName("is_in_observed_downtime");
        entity.Property(s => s.CurrentDowntimeStartedAtUtc).HasColumnName("current_downtime_started_at_utc");
        entity.Property(s => s.ObservedDowntimeDurationMs).HasColumnName("observed_downtime_duration_ms");
        entity.Property(s => s.ActiveAlarmCount).HasColumnName("active_alarm_count");
        entity.Property(s => s.HighestAlarmSeverity).HasColumnName("highest_alarm_severity").HasMaxLength(20);
        entity.Property(s => s.IsInObservedSetup).HasColumnName("is_in_observed_setup");
        entity.Property(s => s.ObservedSetupStartedAtUtc).HasColumnName("observed_setup_started_at_utc");
        entity.Property(s => s.ObservedScrapCount).HasColumnName("observed_scrap_count");
        entity.Property(s => s.ObservedScrapQuantity).HasColumnName("observed_scrap_quantity");
        entity.Property(s => s.LastObservedScrapAtUtc).HasColumnName("last_observed_scrap_at_utc");
        entity.Property(s => s.LastOperationalSourceSequence).HasColumnName("last_operational_source_sequence");
        entity.Property(s => s.UpdatedAtUtc).HasColumnName("updated_at_utc");

        entity.HasIndex(s => new { s.TenantId, s.FacilityId, s.MachineId }).IsUnique();
        entity.HasOne<Tenant>().WithMany().HasForeignKey(s => s.TenantId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Facility>().WithMany()
            .HasForeignKey(s => new { s.TenantId, s.FacilityId })
            .HasPrincipalKey(f => new { f.TenantId, f.Id })
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class MachineDowntimeEpisodeEntityConfiguration : IEntityTypeConfiguration<Edge.MachineDowntimeEpisode>
{
    public void Configure(EntityTypeBuilder<Edge.MachineDowntimeEpisode> entity)
    {
        entity.ToTable("machine_downtime_episodes");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasColumnName("id");
        entity.Property(e => e.TenantId).HasColumnName("tenant_id");
        entity.Property(e => e.FacilityId).HasColumnName("facility_id");
        entity.Property(e => e.MachineId).HasColumnName("machine_id");
        entity.Property(e => e.GatewayId).HasColumnName("gateway_id");
        entity.Property(e => e.ExternalEpisodeId).HasColumnName("external_episode_id").HasMaxLength(200);
        entity.Property(e => e.StartedAtSourceUtc).HasColumnName("started_at_source_utc");
        entity.Property(e => e.EndedAtSourceUtc).HasColumnName("ended_at_source_utc");
        entity.Property(e => e.StartedAtReceivedUtc).HasColumnName("started_at_received_utc");
        entity.Property(e => e.EndedAtReceivedUtc).HasColumnName("ended_at_received_utc");
        entity.Property(e => e.DurationMs).HasColumnName("duration_ms");
        entity.Property(e => e.ReasonCode).HasColumnName("reason_code").HasMaxLength(80);
        entity.Property(e => e.ReasonCategory).HasColumnName("reason_category").HasMaxLength(40);
        entity.Property(e => e.ReasonText).HasColumnName("reason_text").HasMaxLength(500);
        entity.Property(e => e.IsMicroStop).HasColumnName("is_micro_stop");
        entity.Property(e => e.Quality).HasColumnName("quality").HasMaxLength(20);
        entity.Property(e => e.Transport).HasColumnName("transport").HasMaxLength(30);
        entity.Property(e => e.StartSourceSequence).HasColumnName("start_source_sequence");
        entity.Property(e => e.EndSourceSequence).HasColumnName("end_source_sequence");
        entity.Property(e => e.StartIdempotencyKey).HasColumnName("start_idempotency_key").HasMaxLength(200);
        entity.Property(e => e.EndIdempotencyKey).HasColumnName("end_idempotency_key").HasMaxLength(200);
        entity.Property(e => e.IsClosed).HasColumnName("is_closed");
        entity.Property(e => e.IsOutOfOrder).HasColumnName("is_out_of_order");
        entity.Property(e => e.CreatedAtUtc).HasColumnName("created_at_utc");
        entity.Property(e => e.UpdatedAtUtc).HasColumnName("updated_at_utc");
        entity.HasIndex(e => new { e.TenantId, e.FacilityId, e.StartIdempotencyKey }).IsUnique();
        entity.HasIndex(e => new { e.TenantId, e.FacilityId, e.MachineId, e.StartedAtSourceUtc });
        entity.HasOne<Tenant>().WithMany().HasForeignKey(e => e.TenantId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Facility>().WithMany()
            .HasForeignKey(e => new { e.TenantId, e.FacilityId })
            .HasPrincipalKey(f => new { f.TenantId, f.Id })
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class MachineDowntimeObservationEntityConfiguration : IEntityTypeConfiguration<Edge.MachineDowntimeObservation>
{
    public void Configure(EntityTypeBuilder<Edge.MachineDowntimeObservation> entity)
    {
        entity.ToTable("machine_downtime_observations");
        entity.HasKey(o => o.Id);
        entity.Property(o => o.Id).HasColumnName("id");
        entity.Property(o => o.TenantId).HasColumnName("tenant_id");
        entity.Property(o => o.FacilityId).HasColumnName("facility_id");
        entity.Property(o => o.MachineId).HasColumnName("machine_id");
        entity.Property(o => o.GatewayId).HasColumnName("gateway_id");
        entity.Property(o => o.EpisodeId).HasColumnName("episode_id");
        entity.Property(o => o.EventKind).HasColumnName("event_kind").HasMaxLength(60);
        entity.Property(o => o.ExternalEpisodeId).HasColumnName("external_episode_id").HasMaxLength(200);
        entity.Property(o => o.ReasonCode).HasColumnName("reason_code").HasMaxLength(80);
        entity.Property(o => o.ReasonCategory).HasColumnName("reason_category").HasMaxLength(40);
        entity.Property(o => o.ReasonText).HasColumnName("reason_text").HasMaxLength(500);
        entity.Property(o => o.Quality).HasColumnName("quality").HasMaxLength(20);
        entity.Property(o => o.Transport).HasColumnName("transport").HasMaxLength(30);
        entity.Property(o => o.SourceTimestampUtc).HasColumnName("source_timestamp_utc");
        entity.Property(o => o.ReceivedAtUtc).HasColumnName("received_at_utc");
        entity.Property(o => o.SourceSequence).HasColumnName("source_sequence");
        entity.Property(o => o.IdempotencyKey).HasColumnName("idempotency_key").HasMaxLength(200);
        entity.Property(o => o.IsDuplicate).HasColumnName("is_duplicate");
        entity.Property(o => o.IsOutOfOrder).HasColumnName("is_out_of_order");
        entity.Property(o => o.IsUnmatchedEnd).HasColumnName("is_unmatched_end");
        entity.Property(o => o.CreatedAtUtc).HasColumnName("created_at_utc");
        entity.HasIndex(o => new { o.TenantId, o.FacilityId, o.IdempotencyKey }).IsUnique();
        entity.HasIndex(o => new { o.TenantId, o.FacilityId, o.MachineId, o.SourceTimestampUtc });
        entity.HasOne<Tenant>().WithMany().HasForeignKey(o => o.TenantId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Facility>().WithMany()
            .HasForeignKey(o => new { o.TenantId, o.FacilityId })
            .HasPrincipalKey(f => new { f.TenantId, f.Id })
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class MachineAlarmObservationEntityConfiguration : IEntityTypeConfiguration<Edge.MachineAlarmObservation>
{
    public void Configure(EntityTypeBuilder<Edge.MachineAlarmObservation> entity)
    {
        entity.ToTable("machine_alarm_observations");
        entity.HasKey(o => o.Id);
        entity.Property(o => o.Id).HasColumnName("id");
        entity.Property(o => o.TenantId).HasColumnName("tenant_id");
        entity.Property(o => o.FacilityId).HasColumnName("facility_id");
        entity.Property(o => o.MachineId).HasColumnName("machine_id");
        entity.Property(o => o.GatewayId).HasColumnName("gateway_id");
        entity.Property(o => o.AlarmIdentity).HasColumnName("alarm_identity").HasMaxLength(120);
        entity.Property(o => o.ExternalAlarmCode).HasColumnName("external_alarm_code").HasMaxLength(80);
        entity.Property(o => o.AlarmText).HasColumnName("alarm_text").HasMaxLength(500);
        entity.Property(o => o.Severity).HasColumnName("severity").HasMaxLength(20);
        entity.Property(o => o.EventKind).HasColumnName("event_kind").HasMaxLength(60);
        entity.Property(o => o.SourceTimestampUtc).HasColumnName("source_timestamp_utc");
        entity.Property(o => o.ReceivedAtUtc).HasColumnName("received_at_utc");
        entity.Property(o => o.SourceSequence).HasColumnName("source_sequence");
        entity.Property(o => o.Quality).HasColumnName("quality").HasMaxLength(20);
        entity.Property(o => o.Transport).HasColumnName("transport").HasMaxLength(30);
        entity.Property(o => o.IdempotencyKey).HasColumnName("idempotency_key").HasMaxLength(200);
        entity.Property(o => o.IsDuplicate).HasColumnName("is_duplicate");
        entity.Property(o => o.IsOutOfOrder).HasColumnName("is_out_of_order");
        entity.Property(o => o.IsUnmatchedClear).HasColumnName("is_unmatched_clear");
        entity.Property(o => o.CreatedAtUtc).HasColumnName("created_at_utc");
        entity.HasIndex(o => new { o.TenantId, o.FacilityId, o.IdempotencyKey }).IsUnique();
        entity.HasIndex(o => new { o.TenantId, o.FacilityId, o.MachineId, o.SourceTimestampUtc });
        entity.HasOne<Tenant>().WithMany().HasForeignKey(o => o.TenantId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Facility>().WithMany()
            .HasForeignKey(o => new { o.TenantId, o.FacilityId })
            .HasPrincipalKey(f => new { f.TenantId, f.Id })
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class MachineActiveAlarmEntityConfiguration : IEntityTypeConfiguration<Edge.MachineActiveAlarm>
{
    public void Configure(EntityTypeBuilder<Edge.MachineActiveAlarm> entity)
    {
        entity.ToTable("machine_active_alarms");
        entity.HasKey(a => a.Id);
        entity.Property(a => a.Id).HasColumnName("id");
        entity.Property(a => a.TenantId).HasColumnName("tenant_id");
        entity.Property(a => a.FacilityId).HasColumnName("facility_id");
        entity.Property(a => a.MachineId).HasColumnName("machine_id");
        entity.Property(a => a.AlarmIdentity).HasColumnName("alarm_identity").HasMaxLength(120);
        entity.Property(a => a.ExternalAlarmCode).HasColumnName("external_alarm_code").HasMaxLength(80);
        entity.Property(a => a.AlarmText).HasColumnName("alarm_text").HasMaxLength(500);
        entity.Property(a => a.Severity).HasColumnName("severity").HasMaxLength(20);
        entity.Property(a => a.RaisedAtSourceUtc).HasColumnName("raised_at_source_utc");
        entity.Property(a => a.RaisedAtReceivedUtc).HasColumnName("raised_at_received_utc");
        entity.Property(a => a.SourceSequence).HasColumnName("source_sequence");
        entity.Property(a => a.Quality).HasColumnName("quality").HasMaxLength(20);
        entity.Property(a => a.RaisedObservationId).HasColumnName("raised_observation_id");
        entity.HasIndex(a => new { a.TenantId, a.FacilityId, a.MachineId, a.AlarmIdentity }).IsUnique();
        entity.HasOne<Tenant>().WithMany().HasForeignKey(a => a.TenantId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Facility>().WithMany()
            .HasForeignKey(a => new { a.TenantId, a.FacilityId })
            .HasPrincipalKey(f => new { f.TenantId, f.Id })
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class MachineAlarmProjectionEntityConfiguration : IEntityTypeConfiguration<Edge.MachineAlarmProjection>
{
    public void Configure(EntityTypeBuilder<Edge.MachineAlarmProjection> entity)
    {
        entity.ToTable("machine_alarm_projection");
        entity.HasKey(p => p.Id);
        entity.Property(p => p.Id).HasColumnName("id");
        entity.Property(p => p.TenantId).HasColumnName("tenant_id");
        entity.Property(p => p.FacilityId).HasColumnName("facility_id");
        entity.Property(p => p.MachineId).HasColumnName("machine_id");
        entity.Property(p => p.ActiveAlarmCount).HasColumnName("active_alarm_count");
        entity.Property(p => p.HighestSeverity).HasColumnName("highest_severity").HasMaxLength(20);
        entity.Property(p => p.LatestAlarmCode).HasColumnName("latest_alarm_code").HasMaxLength(80);
        entity.Property(p => p.LatestAlarmText).HasColumnName("latest_alarm_text").HasMaxLength(500);
        entity.Property(p => p.LatestRaisedAtUtc).HasColumnName("latest_raised_at_utc");
        entity.Property(p => p.LastSourceSequence).HasColumnName("last_source_sequence");
        entity.Property(p => p.UpdatedAtUtc).HasColumnName("updated_at_utc");
        entity.HasIndex(p => new { p.TenantId, p.FacilityId, p.MachineId }).IsUnique();
        entity.HasOne<Tenant>().WithMany().HasForeignKey(p => p.TenantId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Facility>().WithMany()
            .HasForeignKey(p => new { p.TenantId, p.FacilityId })
            .HasPrincipalKey(f => new { f.TenantId, f.Id })
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class MachineSetupEpisodeEntityConfiguration : IEntityTypeConfiguration<Edge.MachineSetupEpisode>
{
    public void Configure(EntityTypeBuilder<Edge.MachineSetupEpisode> entity)
    {
        entity.ToTable("machine_setup_episodes");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasColumnName("id");
        entity.Property(e => e.TenantId).HasColumnName("tenant_id");
        entity.Property(e => e.FacilityId).HasColumnName("facility_id");
        entity.Property(e => e.MachineId).HasColumnName("machine_id");
        entity.Property(e => e.GatewayId).HasColumnName("gateway_id");
        entity.Property(e => e.ExternalEpisodeId).HasColumnName("external_episode_id").HasMaxLength(200);
        entity.Property(e => e.SetupType).HasColumnName("setup_type").HasMaxLength(80);
        entity.Property(e => e.FromReference).HasColumnName("from_reference").HasMaxLength(200);
        entity.Property(e => e.ToReference).HasColumnName("to_reference").HasMaxLength(200);
        entity.Property(e => e.StartedAtSourceUtc).HasColumnName("started_at_source_utc");
        entity.Property(e => e.CompletedAtSourceUtc).HasColumnName("completed_at_source_utc");
        entity.Property(e => e.StartedAtReceivedUtc).HasColumnName("started_at_received_utc");
        entity.Property(e => e.CompletedAtReceivedUtc).HasColumnName("completed_at_received_utc");
        entity.Property(e => e.DurationMs).HasColumnName("duration_ms");
        entity.Property(e => e.Quality).HasColumnName("quality").HasMaxLength(20);
        entity.Property(e => e.Transport).HasColumnName("transport").HasMaxLength(30);
        entity.Property(e => e.StartSourceSequence).HasColumnName("start_source_sequence");
        entity.Property(e => e.CompleteSourceSequence).HasColumnName("complete_source_sequence");
        entity.Property(e => e.StartIdempotencyKey).HasColumnName("start_idempotency_key").HasMaxLength(200);
        entity.Property(e => e.CompleteIdempotencyKey).HasColumnName("complete_idempotency_key").HasMaxLength(200);
        entity.Property(e => e.IsClosed).HasColumnName("is_closed");
        entity.Property(e => e.IsOutOfOrder).HasColumnName("is_out_of_order");
        entity.Property(e => e.CreatedAtUtc).HasColumnName("created_at_utc");
        entity.Property(e => e.UpdatedAtUtc).HasColumnName("updated_at_utc");
        entity.HasIndex(e => new { e.TenantId, e.FacilityId, e.StartIdempotencyKey }).IsUnique();
        entity.HasIndex(e => new { e.TenantId, e.FacilityId, e.MachineId, e.StartedAtSourceUtc });
        entity.HasOne<Tenant>().WithMany().HasForeignKey(e => e.TenantId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Facility>().WithMany()
            .HasForeignKey(e => new { e.TenantId, e.FacilityId })
            .HasPrincipalKey(f => new { f.TenantId, f.Id })
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class MachineSetupObservationEntityConfiguration : IEntityTypeConfiguration<Edge.MachineSetupObservation>
{
    public void Configure(EntityTypeBuilder<Edge.MachineSetupObservation> entity)
    {
        entity.ToTable("machine_setup_observations");
        entity.HasKey(o => o.Id);
        entity.Property(o => o.Id).HasColumnName("id");
        entity.Property(o => o.TenantId).HasColumnName("tenant_id");
        entity.Property(o => o.FacilityId).HasColumnName("facility_id");
        entity.Property(o => o.MachineId).HasColumnName("machine_id");
        entity.Property(o => o.GatewayId).HasColumnName("gateway_id");
        entity.Property(o => o.EpisodeId).HasColumnName("episode_id");
        entity.Property(o => o.EventKind).HasColumnName("event_kind").HasMaxLength(60);
        entity.Property(o => o.ExternalEpisodeId).HasColumnName("external_episode_id").HasMaxLength(200);
        entity.Property(o => o.SetupType).HasColumnName("setup_type").HasMaxLength(80);
        entity.Property(o => o.FromReference).HasColumnName("from_reference").HasMaxLength(200);
        entity.Property(o => o.ToReference).HasColumnName("to_reference").HasMaxLength(200);
        entity.Property(o => o.Quality).HasColumnName("quality").HasMaxLength(20);
        entity.Property(o => o.Transport).HasColumnName("transport").HasMaxLength(30);
        entity.Property(o => o.SourceTimestampUtc).HasColumnName("source_timestamp_utc");
        entity.Property(o => o.ReceivedAtUtc).HasColumnName("received_at_utc");
        entity.Property(o => o.SourceSequence).HasColumnName("source_sequence");
        entity.Property(o => o.IdempotencyKey).HasColumnName("idempotency_key").HasMaxLength(200);
        entity.Property(o => o.IsDuplicate).HasColumnName("is_duplicate");
        entity.Property(o => o.IsOutOfOrder).HasColumnName("is_out_of_order");
        entity.Property(o => o.IsUnmatchedComplete).HasColumnName("is_unmatched_complete");
        entity.Property(o => o.CreatedAtUtc).HasColumnName("created_at_utc");
        entity.HasIndex(o => new { o.TenantId, o.FacilityId, o.IdempotencyKey }).IsUnique();
        entity.HasIndex(o => new { o.TenantId, o.FacilityId, o.MachineId, o.SourceTimestampUtc });
        entity.HasOne<Tenant>().WithMany().HasForeignKey(o => o.TenantId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Facility>().WithMany()
            .HasForeignKey(o => new { o.TenantId, o.FacilityId })
            .HasPrincipalKey(f => new { f.TenantId, f.Id })
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class MachineScrapObservationEntityConfiguration : IEntityTypeConfiguration<Edge.MachineScrapObservation>
{
    public void Configure(EntityTypeBuilder<Edge.MachineScrapObservation> entity)
    {
        entity.ToTable("machine_scrap_observations");
        entity.HasKey(o => o.Id);
        entity.Property(o => o.Id).HasColumnName("id");
        entity.Property(o => o.TenantId).HasColumnName("tenant_id");
        entity.Property(o => o.FacilityId).HasColumnName("facility_id");
        entity.Property(o => o.MachineId).HasColumnName("machine_id");
        entity.Property(o => o.GatewayId).HasColumnName("gateway_id");
        entity.Property(o => o.ExternalObservationId).HasColumnName("external_observation_id").HasMaxLength(200);
        entity.Property(o => o.Quantity).HasColumnName("quantity");
        entity.Property(o => o.Unit).HasColumnName("unit").HasMaxLength(30);
        entity.Property(o => o.ReasonCode).HasColumnName("reason_code").HasMaxLength(80);
        entity.Property(o => o.ReasonCategory).HasColumnName("reason_category").HasMaxLength(40);
        entity.Property(o => o.ReasonText).HasColumnName("reason_text").HasMaxLength(500);
        entity.Property(o => o.Quality).HasColumnName("quality").HasMaxLength(20);
        entity.Property(o => o.Transport).HasColumnName("transport").HasMaxLength(30);
        entity.Property(o => o.SourceTimestampUtc).HasColumnName("source_timestamp_utc");
        entity.Property(o => o.ReceivedAtUtc).HasColumnName("received_at_utc");
        entity.Property(o => o.SourceSequence).HasColumnName("source_sequence");
        entity.Property(o => o.IdempotencyKey).HasColumnName("idempotency_key").HasMaxLength(200);
        entity.Property(o => o.IsDuplicate).HasColumnName("is_duplicate");
        entity.Property(o => o.IsOutOfOrder).HasColumnName("is_out_of_order");
        entity.Property(o => o.CreatedAtUtc).HasColumnName("created_at_utc");
        entity.HasIndex(o => new { o.TenantId, o.FacilityId, o.IdempotencyKey }).IsUnique();
        entity.HasIndex(o => new { o.TenantId, o.FacilityId, o.MachineId, o.SourceTimestampUtc });
        entity.HasOne<Tenant>().WithMany().HasForeignKey(o => o.TenantId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Facility>().WithMany()
            .HasForeignKey(o => new { o.TenantId, o.FacilityId })
            .HasPrincipalKey(f => new { f.TenantId, f.Id })
            .OnDelete(DeleteBehavior.Restrict);
    }
}
