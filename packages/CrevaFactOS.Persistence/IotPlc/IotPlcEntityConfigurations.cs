using CrevaFactOS.Persistence.Facilities;
using CrevaFactOS.Persistence.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CrevaFactOS.Persistence.IotPlc;

internal sealed class IotPlcEndpointEntityConfiguration : IEntityTypeConfiguration<IotPlcEndpoint>
{
    public void Configure(EntityTypeBuilder<IotPlcEndpoint> builder)
    {
        builder.ToTable("iot_plc_endpoints");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.TenantId).HasColumnName("tenant_id");
        builder.Property(e => e.FacilityId).HasColumnName("facility_id");
        builder.Property(e => e.EndpointCode).HasColumnName("endpoint_code").HasMaxLength(100);
        builder.Property(e => e.DisplayName).HasColumnName("display_name").HasMaxLength(200);
        builder.Property(e => e.ProtocolType).HasColumnName("protocol_type").HasMaxLength(50);
        builder.Property(e => e.ConnectionDirection).HasColumnName("connection_direction").HasMaxLength(50);
        builder.Property(e => e.EndpointStatus).HasColumnName("endpoint_status").HasMaxLength(20);
        builder.Property(e => e.ConfigurationProvenance).HasColumnName("configuration_provenance").HasMaxLength(500);
        builder.Property(e => e.ExternalSecretReference).HasColumnName("external_secret_reference").HasMaxLength(200);
        builder.Property(e => e.ConnectionProfileJson).HasColumnName("connection_profile_json");
        builder.Property(e => e.LastObservedConnectionState).HasColumnName("last_observed_connection_state").HasMaxLength(20);
        builder.Property(e => e.LastObservedAtUtc).HasColumnName("last_observed_at_utc");
        builder.Property(e => e.Version).HasColumnName("version").IsConcurrencyToken();
        builder.Property(e => e.CreatedAtUtc).HasColumnName("created_at_utc");
        builder.Property(e => e.UpdatedAtUtc).HasColumnName("updated_at_utc");
        builder.Property(e => e.CreatedBy).HasColumnName("created_by").HasMaxLength(200);
        builder.Property(e => e.UpdatedBy).HasColumnName("updated_by").HasMaxLength(200);

        builder.HasIndex(e => new { e.TenantId, e.FacilityId, e.EndpointCode })
            .IsUnique()
            .HasDatabaseName("ux_iot_plc_endpoints_tenant_facility_code");

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(e => e.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Facility>()
            .WithMany()
            .HasForeignKey(e => new { e.TenantId, e.FacilityId })
            .HasPrincipalKey(f => new { f.TenantId, f.Id })
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class IotPlcSignalEntityConfiguration : IEntityTypeConfiguration<IotPlcSignal>
{
    public void Configure(EntityTypeBuilder<IotPlcSignal> builder)
    {
        builder.ToTable("iot_plc_signals");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).HasColumnName("id");
        builder.Property(s => s.TenantId).HasColumnName("tenant_id");
        builder.Property(s => s.FacilityId).HasColumnName("facility_id");
        builder.Property(s => s.EndpointId).HasColumnName("endpoint_id");
        builder.Property(s => s.SignalCode).HasColumnName("signal_code").HasMaxLength(100);
        builder.Property(s => s.DisplayName).HasColumnName("display_name").HasMaxLength(200);
        builder.Property(s => s.SourceAddress).HasColumnName("source_address").HasMaxLength(500);
        builder.Property(s => s.DataType).HasColumnName("data_type").HasMaxLength(20);
        builder.Property(s => s.EngineeringUnit).HasColumnName("engineering_unit").HasMaxLength(50);
        builder.Property(s => s.SemanticType).HasColumnName("semantic_type").HasMaxLength(50);
        builder.Property(s => s.MappedAssetType).HasColumnName("mapped_asset_type").HasMaxLength(50);
        builder.Property(s => s.MappedCanonicalAssetId).HasColumnName("mapped_canonical_asset_id");
        builder.Property(s => s.IsEnabled).HasColumnName("is_enabled");
        builder.Property(s => s.SamplingExpectation).HasColumnName("sampling_expectation").HasMaxLength(100);
        builder.Property(s => s.SourceProvenance).HasColumnName("source_provenance").HasMaxLength(500);
        builder.Property(s => s.Version).HasColumnName("version").IsConcurrencyToken();
        builder.Property(s => s.CreatedAtUtc).HasColumnName("created_at_utc");
        builder.Property(s => s.UpdatedAtUtc).HasColumnName("updated_at_utc");
        builder.Property(s => s.CreatedBy).HasColumnName("created_by").HasMaxLength(200);
        builder.Property(s => s.UpdatedBy).HasColumnName("updated_by").HasMaxLength(200);

        builder.HasIndex(s => new { s.EndpointId, s.SignalCode })
            .IsUnique()
            .HasDatabaseName("ux_iot_plc_signals_endpoint_code");

        builder.HasOne(s => s.Endpoint)
            .WithMany(e => e.Signals)
            .HasForeignKey(s => s.EndpointId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(s => s.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Facility>()
            .WithMany()
            .HasForeignKey(s => new { s.TenantId, s.FacilityId })
            .HasPrincipalKey(f => new { f.TenantId, f.Id })
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class IotPlcSignalObservationEntityConfiguration : IEntityTypeConfiguration<IotPlcSignalObservation>
{
    public void Configure(EntityTypeBuilder<IotPlcSignalObservation> builder)
    {
        builder.ToTable("iot_plc_signal_observations");
        builder.HasKey(o => o.Id);
        builder.Property(o => o.Id).HasColumnName("id");
        builder.Property(o => o.TenantId).HasColumnName("tenant_id");
        builder.Property(o => o.FacilityId).HasColumnName("facility_id");
        builder.Property(o => o.EndpointId).HasColumnName("endpoint_id");
        builder.Property(o => o.SignalDefinitionId).HasColumnName("signal_definition_id");
        builder.Property(o => o.MappedAssetType).HasColumnName("mapped_asset_type").HasMaxLength(50);
        builder.Property(o => o.MappedCanonicalAssetId).HasColumnName("mapped_canonical_asset_id");
        builder.Property(o => o.ObservedAtUtc).HasColumnName("observed_at_utc");
        builder.Property(o => o.ReceivedAtUtc).HasColumnName("received_at_utc");
        builder.Property(o => o.BooleanValue).HasColumnName("boolean_value");
        builder.Property(o => o.IntegerValue).HasColumnName("integer_value");
        builder.Property(o => o.DecimalValue).HasColumnName("decimal_value").HasPrecision(18, 6);
        builder.Property(o => o.TextValue).HasColumnName("text_value").HasMaxLength(2000);
        builder.Property(o => o.TimestampValue).HasColumnName("timestamp_value");
        builder.Property(o => o.QualityState).HasColumnName("quality_state").HasMaxLength(30);
        builder.Property(o => o.EvidenceState).HasColumnName("evidence_state").HasMaxLength(30);
        builder.Property(o => o.SourceSequenceReference).HasColumnName("source_sequence_reference").HasMaxLength(200);
        builder.Property(o => o.IdempotencyKey).HasColumnName("idempotency_key").HasMaxLength(128);
        builder.Property(o => o.PayloadFingerprint).HasColumnName("payload_fingerprint").HasMaxLength(64);
        builder.Property(o => o.SourceProvenance).HasColumnName("source_provenance").HasMaxLength(500);
        builder.Property(o => o.CreatedAtUtc).HasColumnName("created_at_utc");
        builder.Property(o => o.CreatedBy).HasColumnName("created_by").HasMaxLength(200);

        builder.HasIndex(o => new { o.TenantId, o.IdempotencyKey })
            .IsUnique()
            .HasDatabaseName("ux_iot_plc_signal_observations_tenant_idempotency")
            .HasFilter("idempotency_key IS NOT NULL");

        builder.HasIndex(o => new { o.SignalDefinitionId, o.SourceSequenceReference })
            .IsUnique()
            .HasDatabaseName("ux_iot_plc_signal_observations_signal_source_ref")
            .HasFilter("source_sequence_reference IS NOT NULL");

        builder.HasIndex(o => new { o.SignalDefinitionId, o.ObservedAtUtc })
            .IsUnique()
            .HasDatabaseName("ux_iot_plc_signal_observations_signal_observed_at");

        builder.HasOne(o => o.Signal)
            .WithMany()
            .HasForeignKey(o => o.SignalDefinitionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(o => o.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Facility>()
            .WithMany()
            .HasForeignKey(o => new { o.TenantId, o.FacilityId })
            .HasPrincipalKey(f => new { f.TenantId, f.Id })
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class IotPlcConnectionHealthObservationEntityConfiguration : IEntityTypeConfiguration<IotPlcConnectionHealthObservation>
{
    public void Configure(EntityTypeBuilder<IotPlcConnectionHealthObservation> builder)
    {
        builder.ToTable("iot_plc_connection_health_observations");
        builder.HasKey(h => h.Id);
        builder.Property(h => h.Id).HasColumnName("id");
        builder.Property(h => h.TenantId).HasColumnName("tenant_id");
        builder.Property(h => h.FacilityId).HasColumnName("facility_id");
        builder.Property(h => h.EndpointId).HasColumnName("endpoint_id");
        builder.Property(h => h.ObservedAtUtc).HasColumnName("observed_at_utc");
        builder.Property(h => h.ReceivedAtUtc).HasColumnName("received_at_utc");
        builder.Property(h => h.HealthState).HasColumnName("health_state").HasMaxLength(20);
        builder.Property(h => h.Source).HasColumnName("source").HasMaxLength(100);
        builder.Property(h => h.EvidenceState).HasColumnName("evidence_state").HasMaxLength(30);
        builder.Property(h => h.ReasonCode).HasColumnName("reason_code").HasMaxLength(100);
        builder.Property(h => h.LatencyMs).HasColumnName("latency_ms");
        builder.Property(h => h.LastSuccessfulObservationAtUtc).HasColumnName("last_successful_observation_at_utc");
        builder.Property(h => h.IdempotencyKey).HasColumnName("idempotency_key").HasMaxLength(128);
        builder.Property(h => h.PayloadFingerprint).HasColumnName("payload_fingerprint").HasMaxLength(64);
        builder.Property(h => h.CreatedAtUtc).HasColumnName("created_at_utc");
        builder.Property(h => h.CreatedBy).HasColumnName("created_by").HasMaxLength(200);

        builder.HasIndex(h => new { h.TenantId, h.IdempotencyKey })
            .IsUnique()
            .HasDatabaseName("ux_iot_plc_connection_health_tenant_idempotency")
            .HasFilter("idempotency_key IS NOT NULL");

        builder.HasIndex(h => new { h.EndpointId, h.ObservedAtUtc })
            .HasDatabaseName("ix_iot_plc_connection_health_endpoint_observed");

        builder.HasOne(h => h.Endpoint)
            .WithMany()
            .HasForeignKey(h => h.EndpointId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(h => h.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Facility>()
            .WithMany()
            .HasForeignKey(h => new { h.TenantId, h.FacilityId })
            .HasPrincipalKey(f => new { f.TenantId, f.Id })
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class IotPlcAuditEventEntityConfiguration : IEntityTypeConfiguration<IotPlcAuditEvent>
{
    public void Configure(EntityTypeBuilder<IotPlcAuditEvent> builder)
    {
        builder.ToTable("iot_plc_audit_events");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.TenantId).HasColumnName("tenant_id");
        builder.Property(e => e.FacilityId).HasColumnName("facility_id");
        builder.Property(e => e.EventType).HasColumnName("event_type").HasMaxLength(100);
        builder.Property(e => e.EntityType).HasColumnName("entity_type").HasMaxLength(100);
        builder.Property(e => e.EntityId).HasColumnName("entity_id");
        builder.Property(e => e.PayloadJson).HasColumnName("payload_json");
        builder.Property(e => e.ActorSubjectId).HasColumnName("actor_subject_id").HasMaxLength(200);
        builder.Property(e => e.OccurredAtUtc).HasColumnName("occurred_at_utc");
        builder.Property(e => e.CorrelationId).HasColumnName("correlation_id");

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(e => e.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Facility>()
            .WithMany()
            .HasForeignKey(e => new { e.TenantId, e.FacilityId })
            .HasPrincipalKey(f => new { f.TenantId, f.Id })
            .OnDelete(DeleteBehavior.Restrict);
    }
}
