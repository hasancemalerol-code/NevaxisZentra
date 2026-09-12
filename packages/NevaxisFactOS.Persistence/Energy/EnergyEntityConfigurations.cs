using NevaxisFactOS.Persistence.Facilities;
using NevaxisFactOS.Persistence.Edge;
using NevaxisFactOS.Persistence.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace NevaxisFactOS.Persistence.Energy;

internal sealed class EnergyMeasurementPointEntityConfiguration : IEntityTypeConfiguration<EnergyMeasurementPoint>
{
    public void Configure(EntityTypeBuilder<EnergyMeasurementPoint> entity)
    {
        entity.ToTable("energy_measurement_points");
        entity.HasKey(p => p.Id);
        entity.Property(p => p.Id).HasColumnName("id");
        entity.Property(p => p.TenantId).HasColumnName("tenant_id");
        entity.Property(p => p.FacilityId).HasColumnName("facility_id");
        entity.Property(p => p.PointCode).HasColumnName("point_code").HasMaxLength(100).IsRequired();
        entity.Property(p => p.DisplayName).HasColumnName("display_name").HasMaxLength(200).IsRequired();
        entity.Property(p => p.MeasurementScope).HasColumnName("measurement_scope").HasMaxLength(50).IsRequired();
        entity.Property(p => p.MachineAssetId).HasColumnName("machine_asset_id");
        entity.Property(p => p.EdgeAssetReference).HasColumnName("edge_asset_reference").HasMaxLength(200);
        entity.Property(p => p.EquipmentCategory).HasColumnName("equipment_category").HasMaxLength(50);
        entity.Property(p => p.MeasurementUnit).HasColumnName("measurement_unit").HasMaxLength(20).IsRequired();
        entity.Property(p => p.Status).HasColumnName("status").HasMaxLength(20).IsRequired();
        entity.Property(p => p.SourceProvenance).HasColumnName("source_provenance").HasMaxLength(500).IsRequired();
        entity.Property(p => p.Version).HasColumnName("version").IsConcurrencyToken();
        entity.Property(p => p.CreatedAtUtc).HasColumnName("created_at_utc");
        entity.Property(p => p.UpdatedAtUtc).HasColumnName("updated_at_utc");
        entity.Property(p => p.CreatedBy).HasColumnName("created_by").HasMaxLength(200).IsRequired();
        entity.Property(p => p.UpdatedBy).HasColumnName("updated_by").HasMaxLength(200).IsRequired();
        entity.HasOne<Tenant>().WithMany().HasForeignKey(p => p.TenantId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Facility>().WithMany()
            .HasForeignKey(p => new { p.TenantId, p.FacilityId })
            .HasPrincipalKey(f => new { f.TenantId, f.Id })
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<MachineRegistry>().WithMany()
            .HasForeignKey(p => p.MachineAssetId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasIndex(p => new { p.TenantId, p.FacilityId, p.PointCode })
            .HasDatabaseName("ux_energy_measurement_points_tenant_facility_code")
            .IsUnique();
        entity.HasIndex(p => new { p.TenantId, p.FacilityId, p.Status })
            .HasDatabaseName("ix_energy_measurement_points_tenant_facility_status");
    }
}

internal sealed class EnergyReadingEntityConfiguration : IEntityTypeConfiguration<EnergyReading>
{
    public void Configure(EntityTypeBuilder<EnergyReading> entity)
    {
        entity.ToTable("energy_readings");
        entity.HasKey(r => r.Id);
        entity.Property(r => r.Id).HasColumnName("id");
        entity.Property(r => r.TenantId).HasColumnName("tenant_id");
        entity.Property(r => r.FacilityId).HasColumnName("facility_id");
        entity.Property(r => r.MeasurementPointId).HasColumnName("measurement_point_id");
        entity.Property(r => r.OccurredAtUtc).HasColumnName("occurred_at_utc");
        entity.Property(r => r.ReceivedAtUtc).HasColumnName("received_at_utc");
        entity.Property(r => r.CumulativeEnergyKwh).HasColumnName("cumulative_energy_kwh").HasPrecision(18, 4);
        entity.Property(r => r.IntervalEnergyKwh).HasColumnName("interval_energy_kwh").HasPrecision(18, 4);
        entity.Property(r => r.ActivePowerKw).HasColumnName("active_power_kw").HasPrecision(18, 4);
        entity.Property(r => r.MaximumDemandKw).HasColumnName("maximum_demand_kw").HasPrecision(18, 4);
        entity.Property(r => r.SourceType).HasColumnName("source_type").HasMaxLength(50).IsRequired();
        entity.Property(r => r.SourceReference).HasColumnName("source_reference").HasMaxLength(200);
        entity.Property(r => r.FreshnessState).HasColumnName("freshness_state").HasMaxLength(20).IsRequired();
        entity.Property(r => r.EvidenceState).HasColumnName("evidence_state").HasMaxLength(30).IsRequired();
        entity.Property(r => r.IdempotencyKey).HasColumnName("idempotency_key").HasMaxLength(128);
        entity.Property(r => r.PayloadFingerprint).HasColumnName("payload_fingerprint").HasMaxLength(64);
        entity.Property(r => r.CreatedAtUtc).HasColumnName("created_at_utc");
        entity.Property(r => r.CreatedBy).HasColumnName("created_by").HasMaxLength(200).IsRequired();
        entity.HasOne<Tenant>().WithMany().HasForeignKey(r => r.TenantId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Facility>().WithMany()
            .HasForeignKey(r => new { r.TenantId, r.FacilityId })
            .HasPrincipalKey(f => new { f.TenantId, f.Id })
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(r => r.MeasurementPoint).WithMany()
            .HasForeignKey(r => r.MeasurementPointId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasIndex(r => new { r.TenantId, r.IdempotencyKey })
            .HasDatabaseName("ux_energy_readings_tenant_idempotency")
            .IsUnique()
            .HasFilter("idempotency_key IS NOT NULL");
        entity.HasIndex(r => new { r.TenantId, r.FacilityId, r.MeasurementPointId, r.OccurredAtUtc })
            .HasDatabaseName("ix_energy_readings_tenant_facility_point_occurred");
    }
}

internal sealed class EnergyConsumptionWindowEntityConfiguration : IEntityTypeConfiguration<EnergyConsumptionWindow>
{
    public void Configure(EntityTypeBuilder<EnergyConsumptionWindow> entity)
    {
        entity.ToTable("energy_consumption_windows");
        entity.HasKey(w => w.Id);
        entity.Property(w => w.Id).HasColumnName("id");
        entity.Property(w => w.TenantId).HasColumnName("tenant_id");
        entity.Property(w => w.FacilityId).HasColumnName("facility_id");
        entity.Property(w => w.MeasurementPointId).HasColumnName("measurement_point_id");
        entity.Property(w => w.WindowStartUtc).HasColumnName("window_start_utc");
        entity.Property(w => w.WindowEndUtc).HasColumnName("window_end_utc");
        entity.Property(w => w.ConsumedKwh).HasColumnName("consumed_kwh").HasPrecision(18, 4);
        entity.Property(w => w.AveragePowerKw).HasColumnName("average_power_kw").HasPrecision(18, 4);
        entity.Property(w => w.MaximumDemandKw).HasColumnName("maximum_demand_kw").HasPrecision(18, 4);
        entity.Property(w => w.CalculationMethod).HasColumnName("calculation_method").HasMaxLength(50).IsRequired();
        entity.Property(w => w.EvidenceStatus).HasColumnName("evidence_status").HasMaxLength(30).IsRequired();
        entity.Property(w => w.ReadingCount).HasColumnName("reading_count");
        entity.Property(w => w.FreshnessState).HasColumnName("freshness_state").HasMaxLength(20).IsRequired();
        entity.Property(w => w.SourceReferencesJson).HasColumnName("source_references_json").IsRequired();
        entity.Property(w => w.RuleSetVersion).HasColumnName("rule_set_version").HasMaxLength(100).IsRequired();
        entity.Property(w => w.CostAmount).HasColumnName("cost_amount").HasPrecision(18, 4);
        entity.Property(w => w.CostCurrencyCode).HasColumnName("cost_currency_code").HasMaxLength(10);
        entity.Property(w => w.TariffSnapshotId).HasColumnName("tariff_snapshot_id");
        entity.Property(w => w.MissingDataExplanation).HasColumnName("missing_data_explanation").HasMaxLength(500);
        entity.Property(w => w.EvaluateIdempotencyKey).HasColumnName("evaluate_idempotency_key").HasMaxLength(128);
        entity.Property(w => w.EvaluateIdempotencyPayloadFingerprint).HasColumnName("evaluate_idempotency_payload_fingerprint").HasMaxLength(64);
        entity.Property(w => w.CalculatedAtUtc).HasColumnName("calculated_at_utc");
        entity.Property(w => w.CreatedAtUtc).HasColumnName("created_at_utc");
        entity.Property(w => w.CreatedBy).HasColumnName("created_by").HasMaxLength(200).IsRequired();
        entity.HasOne<Tenant>().WithMany().HasForeignKey(w => w.TenantId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Facility>().WithMany()
            .HasForeignKey(w => new { w.TenantId, w.FacilityId })
            .HasPrincipalKey(f => new { f.TenantId, f.Id })
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<EnergyMeasurementPoint>().WithMany()
            .HasForeignKey(w => w.MeasurementPointId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<EnergyTariffSnapshot>().WithMany()
            .HasForeignKey(w => w.TariffSnapshotId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasIndex(w => new { w.TenantId, w.EvaluateIdempotencyKey })
            .HasDatabaseName("ux_energy_consumption_windows_tenant_evaluate_idempotency")
            .IsUnique()
            .HasFilter("evaluate_idempotency_key IS NOT NULL");
        entity.HasIndex(w => new { w.TenantId, w.FacilityId, w.MeasurementPointId, w.WindowStartUtc, w.WindowEndUtc, w.RuleSetVersion })
            .HasDatabaseName("ux_energy_consumption_windows_point_window_ruleset")
            .IsUnique();
        entity.HasIndex(w => new { w.TenantId, w.FacilityId, w.CalculatedAtUtc })
            .HasDatabaseName("ix_energy_consumption_windows_tenant_facility_calculated");
    }
}

internal sealed class EnergyAllocationEntityConfiguration : IEntityTypeConfiguration<EnergyAllocation>
{
    public void Configure(EntityTypeBuilder<EnergyAllocation> entity)
    {
        entity.ToTable("energy_allocations");
        entity.HasKey(a => a.Id);
        entity.Property(a => a.Id).HasColumnName("id");
        entity.Property(a => a.TenantId).HasColumnName("tenant_id");
        entity.Property(a => a.FacilityId).HasColumnName("facility_id");
        entity.Property(a => a.ConsumptionWindowId).HasColumnName("consumption_window_id");
        entity.Property(a => a.TargetType).HasColumnName("target_type").HasMaxLength(50).IsRequired();
        entity.Property(a => a.TargetReference).HasColumnName("target_reference").HasMaxLength(200);
        entity.Property(a => a.AllocatedKwh).HasColumnName("allocated_kwh").HasPrecision(18, 4);
        entity.Property(a => a.AllocationMethod).HasColumnName("allocation_method").HasMaxLength(50).IsRequired();
        entity.Property(a => a.EvidenceSource).HasColumnName("evidence_source").HasMaxLength(100).IsRequired();
        entity.Property(a => a.ConfidenceStatus).HasColumnName("confidence_status").HasMaxLength(30).IsRequired();
        entity.Property(a => a.RuleSetVersion).HasColumnName("rule_set_version").HasMaxLength(100).IsRequired();
        entity.Property(a => a.CalculatedAtUtc).HasColumnName("calculated_at_utc");
        entity.Property(a => a.CreatedAtUtc).HasColumnName("created_at_utc");
        entity.HasOne<Tenant>().WithMany().HasForeignKey(a => a.TenantId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Facility>().WithMany()
            .HasForeignKey(a => new { a.TenantId, a.FacilityId })
            .HasPrincipalKey(f => new { f.TenantId, f.Id })
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(a => a.ConsumptionWindow).WithMany(w => w.Allocations)
            .HasForeignKey(a => a.ConsumptionWindowId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasIndex(a => new { a.TenantId, a.ConsumptionWindowId, a.TargetType, a.TargetReference })
            .HasDatabaseName("ix_energy_allocations_window_target");
    }
}

internal sealed class EnergyTariffSnapshotEntityConfiguration : IEntityTypeConfiguration<EnergyTariffSnapshot>
{
    public void Configure(EntityTypeBuilder<EnergyTariffSnapshot> entity)
    {
        entity.ToTable("energy_tariff_snapshots");
        entity.HasKey(t => t.Id);
        entity.Property(t => t.Id).HasColumnName("id");
        entity.Property(t => t.TenantId).HasColumnName("tenant_id");
        entity.Property(t => t.FacilityId).HasColumnName("facility_id");
        entity.Property(t => t.CurrencyCode).HasColumnName("currency_code").HasMaxLength(10).IsRequired();
        entity.Property(t => t.ValidFromUtc).HasColumnName("valid_from_utc");
        entity.Property(t => t.ValidToUtc).HasColumnName("valid_to_utc");
        entity.Property(t => t.EnergyUnitPrice).HasColumnName("energy_unit_price").HasPrecision(18, 6);
        entity.Property(t => t.DemandUnitPrice).HasColumnName("demand_unit_price").HasPrecision(18, 6);
        entity.Property(t => t.Provenance).HasColumnName("provenance").HasMaxLength(500).IsRequired();
        entity.Property(t => t.Version).HasColumnName("version").IsConcurrencyToken();
        entity.Property(t => t.CreatedAtUtc).HasColumnName("created_at_utc");
        entity.Property(t => t.UpdatedAtUtc).HasColumnName("updated_at_utc");
        entity.HasOne<Tenant>().WithMany().HasForeignKey(t => t.TenantId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Facility>().WithMany()
            .HasForeignKey(t => new { t.TenantId, t.FacilityId })
            .HasPrincipalKey(f => new { f.TenantId, f.Id })
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasIndex(t => new { t.TenantId, t.FacilityId, t.ValidFromUtc, t.ValidToUtc })
            .HasDatabaseName("ix_energy_tariff_snapshots_tenant_facility_validity");
    }
}

internal sealed class EnergyAuditEventEntityConfiguration : IEntityTypeConfiguration<EnergyAuditEvent>
{
    public void Configure(EntityTypeBuilder<EnergyAuditEvent> entity)
    {
        entity.ToTable("energy_audit_events");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasColumnName("id");
        entity.Property(e => e.TenantId).HasColumnName("tenant_id");
        entity.Property(e => e.FacilityId).HasColumnName("facility_id");
        entity.Property(e => e.EventType).HasColumnName("event_type").HasMaxLength(100).IsRequired();
        entity.Property(e => e.EntityType).HasColumnName("entity_type").HasMaxLength(100).IsRequired();
        entity.Property(e => e.EntityId).HasColumnName("entity_id");
        entity.Property(e => e.PayloadJson).HasColumnName("payload_json").IsRequired();
        entity.Property(e => e.ActorSubjectId).HasColumnName("actor_subject_id").HasMaxLength(200).IsRequired();
        entity.Property(e => e.OccurredAtUtc).HasColumnName("occurred_at_utc");
        entity.Property(e => e.CorrelationId).HasColumnName("correlation_id");
        entity.HasOne<Tenant>().WithMany().HasForeignKey(e => e.TenantId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Facility>().WithMany()
            .HasForeignKey(e => new { e.TenantId, e.FacilityId })
            .HasPrincipalKey(f => new { f.TenantId, f.Id })
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasIndex(e => new { e.TenantId, e.FacilityId, e.OccurredAtUtc })
            .HasDatabaseName("ix_energy_audit_events_tenant_facility_occurred");
    }
}
