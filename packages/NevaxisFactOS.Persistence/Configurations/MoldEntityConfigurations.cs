using NevaxisFactOS.Persistence.Facilities;
using NevaxisFactOS.Persistence.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace NevaxisFactOS.Persistence.Configurations;

internal sealed class MoldRegistryEntryEntityConfiguration : IEntityTypeConfiguration<Mold.MoldRegistryEntry>
{
    public void Configure(EntityTypeBuilder<Mold.MoldRegistryEntry> entity)
    {
        entity.ToTable("molds");
        entity.HasKey(m => m.Id);
        entity.Property(m => m.Id).HasColumnName("id");
        entity.Property(m => m.TenantId).HasColumnName("tenant_id");
        entity.Property(m => m.FacilityId).HasColumnName("facility_id");
        entity.Property(m => m.Code).HasColumnName("code").HasMaxLength(100);
        entity.Property(m => m.CodeNormalized).HasColumnName("code_normalized").HasMaxLength(100);
        entity.Property(m => m.Name).HasColumnName("name").HasMaxLength(200);
        entity.Property(m => m.Description).HasColumnName("description").HasMaxLength(1000);
        entity.Property(m => m.SerialNumber).HasColumnName("serial_number").HasMaxLength(100);
        entity.Property(m => m.Manufacturer).HasColumnName("manufacturer").HasMaxLength(200);
        entity.Property(m => m.ManufactureYear).HasColumnName("manufacture_year");
        entity.Property(m => m.ManufactureDate).HasColumnName("manufacture_date");
        entity.Property(m => m.CommissioningDate).HasColumnName("commissioning_date");
        entity.Property(m => m.SourceType).HasColumnName("source_type").HasMaxLength(20)
            .HasConversion(v => Mold.MoldValueConversions.ToStorage(v), v => Mold.MoldValueConversions.SourceFromStorage(v));
        entity.Property(m => m.ExternalSourceReference).HasColumnName("external_source_reference").HasMaxLength(200);
        entity.Property(m => m.OwnershipType).HasColumnName("ownership_type").HasMaxLength(30)
            .HasConversion(v => Mold.MoldValueConversions.ToStorage(v), v => Mold.MoldValueConversions.OwnershipFromStorage(v));
        entity.Property(m => m.OwnerReference).HasColumnName("owner_reference").HasMaxLength(200);
        entity.Property(m => m.LocationType).HasColumnName("location_type").HasMaxLength(30)
            .HasConversion(v => Mold.MoldValueConversions.ToStorage(v), v => Mold.MoldValueConversions.LocationFromStorage(v));
        entity.Property(m => m.LocationLabel).HasColumnName("location_label").HasMaxLength(200);
        entity.Property(m => m.LocationChangedAtUtc).HasColumnName("location_changed_at_utc");
        entity.Property(m => m.LocationChangedBySubjectId).HasColumnName("location_changed_by_subject_id").HasMaxLength(200);
        entity.Property(m => m.LifecycleState).HasColumnName("lifecycle_state").HasMaxLength(20)
            .HasConversion(v => Mold.MoldValueConversions.ToStorage(v), v => Mold.MoldValueConversions.LifecycleFromStorage(v));
        entity.Property(m => m.MoldType).HasColumnName("mold_type").HasMaxLength(100);
        entity.Property(m => m.CavityCount).HasColumnName("cavity_count");
        entity.Property(m => m.NominalShotLife).HasColumnName("nominal_shot_life");
        entity.Property(m => m.CurrentShotCount).HasColumnName("current_shot_count");
        entity.Property(m => m.CounterRecordedAtUtc).HasColumnName("counter_recorded_at_utc");
        entity.Property(m => m.CounterSource).HasColumnName("counter_source").HasMaxLength(20)
            .HasConversion(
                v => v.HasValue ? Mold.MoldValueConversions.ToStorage(v.Value) : null,
                v => v == null ? null : Mold.MoldValueConversions.CounterSourceFromStorage(v));
        entity.Property(m => m.CounterSourceReference).HasColumnName("counter_source_reference").HasMaxLength(200);
        entity.Property(m => m.CounterRecordedBySubjectId).HasColumnName("counter_recorded_by_subject_id").HasMaxLength(200);
        entity.Property(m => m.CounterNote).HasColumnName("counter_note").HasMaxLength(1000);
        entity.Property(m => m.PrimaryProductReference).HasColumnName("primary_product_reference").HasMaxLength(200);
        entity.Property(m => m.MaterialFamilyReference).HasColumnName("material_family_reference").HasMaxLength(200);
        entity.Property(m => m.RunnerType).HasColumnName("runner_type").HasMaxLength(100);
        entity.Property(m => m.HotRunnerPresent).HasColumnName("hot_runner_present");
        entity.Property(m => m.MoldWeightKg).HasColumnName("mold_weight_kg").HasPrecision(18, 3);
        entity.Property(m => m.DimensionLengthMm).HasColumnName("dimension_length_mm").HasPrecision(18, 3);
        entity.Property(m => m.DimensionWidthMm).HasColumnName("dimension_width_mm").HasPrecision(18, 3);
        entity.Property(m => m.DimensionHeightMm).HasColumnName("dimension_height_mm").HasPrecision(18, 3);
        entity.Property(m => m.Notes).HasColumnName("notes").HasMaxLength(2000);
        entity.Property(m => m.CreateIdempotencyKey).HasColumnName("create_idempotency_key").HasMaxLength(128);
        entity.Property(m => m.CreateIdempotencyPayloadFingerprint).HasColumnName("create_idempotency_payload_fingerprint").HasMaxLength(64);
        entity.Property(m => m.CreatedAtUtc).HasColumnName("created_at_utc");
        entity.Property(m => m.UpdatedAtUtc).HasColumnName("updated_at_utc");
        entity.Property(m => m.CreatedBySubjectId).HasColumnName("created_by_subject_id").HasMaxLength(200);
        entity.Property(m => m.UpdatedBySubjectId).HasColumnName("updated_by_subject_id").HasMaxLength(200);
        entity.Property(m => m.Version).HasColumnName("version").IsConcurrencyToken();
        entity.Property(m => m.LinkedAiTaskId).HasColumnName("linked_ai_task_id");
        entity.Property(m => m.FollowUpStatus).HasColumnName("follow_up_status").HasMaxLength(30);
        entity.Property(m => m.FollowUpUpdatedBy).HasColumnName("follow_up_updated_by").HasMaxLength(200);

        entity.HasIndex(m => new { m.TenantId, m.CodeNormalized }).IsUnique();
        entity.HasIndex(m => new { m.TenantId, m.CreateIdempotencyKey }).IsUnique()
            .HasFilter("create_idempotency_key IS NOT NULL");
        entity.HasIndex(m => new { m.TenantId, m.FacilityId, m.LifecycleState });
        entity.HasOne<Tenant>().WithMany().HasForeignKey(m => m.TenantId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Facility>().WithMany()
            .HasForeignKey(m => new { m.TenantId, m.FacilityId })
            .HasPrincipalKey(f => new { f.TenantId, f.Id })
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class MoldLifecycleEventEntityConfiguration : IEntityTypeConfiguration<Mold.MoldLifecycleEvent>
{
    public void Configure(EntityTypeBuilder<Mold.MoldLifecycleEvent> entity)
    {
        entity.ToTable("mold_lifecycle_events");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasColumnName("id");
        entity.Property(e => e.TenantId).HasColumnName("tenant_id");
        entity.Property(e => e.FacilityId).HasColumnName("facility_id");
        entity.Property(e => e.MoldId).HasColumnName("mold_id");
        entity.Property(e => e.PreviousState).HasColumnName("previous_state").HasMaxLength(20)
            .HasConversion(v => Mold.MoldValueConversions.ToStorage(v), v => Mold.MoldValueConversions.LifecycleFromStorage(v));
        entity.Property(e => e.NewState).HasColumnName("new_state").HasMaxLength(20)
            .HasConversion(v => Mold.MoldValueConversions.ToStorage(v), v => Mold.MoldValueConversions.LifecycleFromStorage(v));
        entity.Property(e => e.Reason).HasColumnName("reason").HasMaxLength(1000);
        entity.Property(e => e.ActorSubjectId).HasColumnName("actor_subject_id").HasMaxLength(200);
        entity.Property(e => e.OccurredAtUtc).HasColumnName("occurred_at_utc");
        entity.Property(e => e.IdempotencyKey).HasColumnName("idempotency_key").HasMaxLength(128);
        entity.Property(e => e.SourceReference).HasColumnName("source_reference").HasMaxLength(200);
        entity.Property(e => e.Version).HasColumnName("version");

        entity.HasIndex(e => new { e.TenantId, e.MoldId, e.IdempotencyKey }).IsUnique();
        entity.HasIndex(e => new { e.TenantId, e.FacilityId, e.MoldId, e.OccurredAtUtc });
        entity.HasOne<Mold.MoldRegistryEntry>().WithMany()
            .HasForeignKey(e => e.MoldId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class MoldCounterSnapshotEntityConfiguration : IEntityTypeConfiguration<Mold.MoldCounterSnapshot>
{
    public void Configure(EntityTypeBuilder<Mold.MoldCounterSnapshot> entity)
    {
        entity.ToTable("mold_counter_snapshots");
        entity.HasKey(s => s.Id);
        entity.Property(s => s.Id).HasColumnName("id");
        entity.Property(s => s.TenantId).HasColumnName("tenant_id");
        entity.Property(s => s.FacilityId).HasColumnName("facility_id");
        entity.Property(s => s.MoldId).HasColumnName("mold_id");
        entity.Property(s => s.ShotCount).HasColumnName("shot_count");
        entity.Property(s => s.RecordedAtUtc).HasColumnName("recorded_at_utc");
        entity.Property(s => s.SourceType).HasColumnName("source_type").HasMaxLength(20)
            .HasConversion(v => Mold.MoldValueConversions.ToStorage(v), v => Mold.MoldValueConversions.CounterSourceFromStorage(v));
        entity.Property(s => s.SourceReference).HasColumnName("source_reference").HasMaxLength(200);
        entity.Property(s => s.RecordedBySubjectId).HasColumnName("recorded_by_subject_id").HasMaxLength(200);
        entity.Property(s => s.Note).HasColumnName("note").HasMaxLength(1000);
        entity.Property(s => s.IdempotencyKey).HasColumnName("idempotency_key").HasMaxLength(128);
        entity.Property(s => s.Version).HasColumnName("version");

        entity.HasIndex(s => new { s.TenantId, s.MoldId, s.IdempotencyKey }).IsUnique();
        entity.HasIndex(s => new { s.TenantId, s.FacilityId, s.MoldId, s.RecordedAtUtc });
        entity.HasOne<Mold.MoldRegistryEntry>().WithMany()
            .HasForeignKey(s => s.MoldId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class MoldFollowUpAuditEventEntityConfiguration : IEntityTypeConfiguration<Mold.MoldFollowUpAuditEvent>
{
    public void Configure(EntityTypeBuilder<Mold.MoldFollowUpAuditEvent> entity)
    {
        entity.ToTable("mold_follow_up_audit_events");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasColumnName("id");
        entity.Property(e => e.TenantId).HasColumnName("tenant_id");
        entity.Property(e => e.FacilityId).HasColumnName("facility_id");
        entity.Property(e => e.MoldId).HasColumnName("mold_id");
        entity.Property(e => e.EventType).HasColumnName("event_type").HasMaxLength(50);
        entity.Property(e => e.PayloadJson).HasColumnName("payload_json").HasColumnType("jsonb");
        entity.Property(e => e.Actor).HasColumnName("actor").HasMaxLength(200);
        entity.Property(e => e.OccurredAtUtc).HasColumnName("occurred_at_utc");

        entity.HasIndex(e => e.MoldId);
        entity.HasIndex(e => new { e.TenantId, e.FacilityId, e.MoldId, e.OccurredAtUtc });
        entity.HasOne<Tenant>().WithMany().HasForeignKey(e => e.TenantId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Facility>().WithMany()
            .HasForeignKey(e => new { e.TenantId, e.FacilityId })
            .HasPrincipalKey(f => new { f.TenantId, f.Id })
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(e => e.Mold).WithMany()
            .HasForeignKey(e => e.MoldId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class MoldMaintenanceRecordEntityConfiguration : IEntityTypeConfiguration<Mold.MoldMaintenanceRecord>
{
    public void Configure(EntityTypeBuilder<Mold.MoldMaintenanceRecord> entity)
    {
        entity.ToTable("mold_maintenance_records");
        entity.HasKey(r => r.Id);
        entity.Property(r => r.Id).HasColumnName("id");
        entity.Property(r => r.TenantId).HasColumnName("tenant_id");
        entity.Property(r => r.FacilityId).HasColumnName("facility_id");
        entity.Property(r => r.MoldId).HasColumnName("mold_id");
        entity.Property(r => r.MaintenanceType).HasColumnName("maintenance_type").HasMaxLength(30)
            .HasConversion(v => Mold.MoldHistoryValueConversions.ToStorage(v), v => Mold.MoldHistoryValueConversions.MaintenanceTypeFromStorage(v));
        entity.Property(r => r.Status).HasColumnName("status").HasMaxLength(20)
            .HasConversion(v => Mold.MoldHistoryValueConversions.ToStorage(v), v => Mold.MoldHistoryValueConversions.MaintenanceStatusFromStorage(v));
        entity.Property(r => r.OpenedAtUtc).HasColumnName("opened_at_utc");
        entity.Property(r => r.CompletedAtUtc).HasColumnName("completed_at_utc");
        entity.Property(r => r.Summary).HasColumnName("summary").HasMaxLength(1000);
        entity.Property(r => r.Findings).HasColumnName("findings").HasMaxLength(2000);
        entity.Property(r => r.ActionsTaken).HasColumnName("actions_taken").HasMaxLength(2000);
        entity.Property(r => r.PerformedByDisplayName).HasColumnName("performed_by_display_name").HasMaxLength(200);
        entity.Property(r => r.ExternalMaintenanceRequestReference).HasColumnName("external_maintenance_request_reference").HasMaxLength(200);
        entity.Property(r => r.ExternalWorkOrderReference).HasColumnName("external_work_order_reference").HasMaxLength(200);
        entity.Property(r => r.SourceType).HasColumnName("source_type").HasMaxLength(20)
            .HasConversion(v => Mold.MoldValueConversions.ToStorage(v), v => Mold.MoldValueConversions.SourceFromStorage(v));
        entity.Property(r => r.SourceReference).HasColumnName("source_reference").HasMaxLength(200);
        entity.Property(r => r.IdempotencyKey).HasColumnName("idempotency_key").HasMaxLength(128);
        entity.Property(r => r.IdempotencyPayloadFingerprint).HasColumnName("idempotency_payload_fingerprint").HasMaxLength(64);
        entity.Property(r => r.CreatedAtUtc).HasColumnName("created_at_utc");
        entity.Property(r => r.UpdatedAtUtc).HasColumnName("updated_at_utc");
        entity.Property(r => r.CreatedBySubjectId).HasColumnName("created_by_subject_id").HasMaxLength(200);
        entity.Property(r => r.UpdatedBySubjectId).HasColumnName("updated_by_subject_id").HasMaxLength(200);
        entity.Property(r => r.Version).HasColumnName("version").IsConcurrencyToken();

        entity.HasIndex(r => new { r.TenantId, r.MoldId, r.IdempotencyKey }).IsUnique();
        entity.HasIndex(r => new { r.TenantId, r.FacilityId, r.MoldId, r.OpenedAtUtc });
        entity.HasOne<Mold.MoldRegistryEntry>().WithMany().HasForeignKey(r => r.MoldId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Tenant>().WithMany().HasForeignKey(r => r.TenantId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Facility>().WithMany()
            .HasForeignKey(r => new { r.TenantId, r.FacilityId })
            .HasPrincipalKey(f => new { f.TenantId, f.Id })
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class MoldRevisionRecordEntityConfiguration : IEntityTypeConfiguration<Mold.MoldRevisionRecord>
{
    public void Configure(EntityTypeBuilder<Mold.MoldRevisionRecord> entity)
    {
        entity.ToTable("mold_revision_records");
        entity.HasKey(r => r.Id);
        entity.Property(r => r.Id).HasColumnName("id");
        entity.Property(r => r.TenantId).HasColumnName("tenant_id");
        entity.Property(r => r.FacilityId).HasColumnName("facility_id");
        entity.Property(r => r.MoldId).HasColumnName("mold_id");
        entity.Property(r => r.RevisionNumber).HasColumnName("revision_number").HasMaxLength(50);
        entity.Property(r => r.RevisionNumberNormalized).HasColumnName("revision_number_normalized").HasMaxLength(50);
        entity.Property(r => r.RevisionType).HasColumnName("revision_type").HasMaxLength(30)
            .HasConversion(v => Mold.MoldHistoryValueConversions.ToStorage(v), v => Mold.MoldHistoryValueConversions.RevisionTypeFromStorage(v));
        entity.Property(r => r.RevisionDate).HasColumnName("revision_date");
        entity.Property(r => r.Summary).HasColumnName("summary").HasMaxLength(1000);
        entity.Property(r => r.Reason).HasColumnName("reason").HasMaxLength(1000);
        entity.Property(r => r.TechnicalChange).HasColumnName("technical_change").HasMaxLength(2000);
        entity.Property(r => r.PreviousCavityCount).HasColumnName("previous_cavity_count");
        entity.Property(r => r.NewCavityCount).HasColumnName("new_cavity_count");
        entity.Property(r => r.PerformedByDisplayName).HasColumnName("performed_by_display_name").HasMaxLength(200);
        entity.Property(r => r.ExternalReference).HasColumnName("external_reference").HasMaxLength(200);
        entity.Property(r => r.SourceType).HasColumnName("source_type").HasMaxLength(20)
            .HasConversion(v => Mold.MoldValueConversions.ToStorage(v), v => Mold.MoldValueConversions.SourceFromStorage(v));
        entity.Property(r => r.SourceReference).HasColumnName("source_reference").HasMaxLength(200);
        entity.Property(r => r.IdempotencyKey).HasColumnName("idempotency_key").HasMaxLength(128);
        entity.Property(r => r.IdempotencyPayloadFingerprint).HasColumnName("idempotency_payload_fingerprint").HasMaxLength(64);
        entity.Property(r => r.CreatedAtUtc).HasColumnName("created_at_utc");
        entity.Property(r => r.CreatedBySubjectId).HasColumnName("created_by_subject_id").HasMaxLength(200);

        entity.HasIndex(r => new { r.TenantId, r.MoldId, r.IdempotencyKey }).IsUnique();
        entity.HasIndex(r => new { r.TenantId, r.MoldId, r.RevisionNumberNormalized }).IsUnique();
        entity.HasIndex(r => new { r.TenantId, r.FacilityId, r.MoldId, r.RevisionDate });
        entity.HasOne<Mold.MoldRegistryEntry>().WithMany().HasForeignKey(r => r.MoldId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Tenant>().WithMany().HasForeignKey(r => r.TenantId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Facility>().WithMany()
            .HasForeignKey(r => new { r.TenantId, r.FacilityId })
            .HasPrincipalKey(f => new { f.TenantId, f.Id })
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class MoldTrialRecordEntityConfiguration : IEntityTypeConfiguration<Mold.MoldTrialRecord>
{
    public void Configure(EntityTypeBuilder<Mold.MoldTrialRecord> entity)
    {
        entity.ToTable("mold_trial_records");
        entity.HasKey(r => r.Id);
        entity.Property(r => r.Id).HasColumnName("id");
        entity.Property(r => r.TenantId).HasColumnName("tenant_id");
        entity.Property(r => r.FacilityId).HasColumnName("facility_id");
        entity.Property(r => r.MoldId).HasColumnName("mold_id");
        entity.Property(r => r.TrialNumber).HasColumnName("trial_number").HasMaxLength(50);
        entity.Property(r => r.TrialNumberNormalized).HasColumnName("trial_number_normalized").HasMaxLength(50);
        entity.Property(r => r.TrialDate).HasColumnName("trial_date");
        entity.Property(r => r.Result).HasColumnName("result").HasMaxLength(30)
            .HasConversion(v => Mold.MoldHistoryValueConversions.ToStorage(v), v => Mold.MoldHistoryValueConversions.TrialResultFromStorage(v));
        entity.Property(r => r.MachineReference).HasColumnName("machine_reference").HasMaxLength(200);
        entity.Property(r => r.ProductionOrderReference).HasColumnName("production_order_reference").HasMaxLength(200);
        entity.Property(r => r.ProductReference).HasColumnName("product_reference").HasMaxLength(200);
        entity.Property(r => r.MaterialReference).HasColumnName("material_reference").HasMaxLength(200);
        entity.Property(r => r.CycleTimeSeconds).HasColumnName("cycle_time_seconds").HasPrecision(18, 3);
        entity.Property(r => r.ShotCount).HasColumnName("shot_count");
        entity.Property(r => r.GoodPartCount).HasColumnName("good_part_count");
        entity.Property(r => r.ScrapPartCount).HasColumnName("scrap_part_count");
        entity.Property(r => r.Findings).HasColumnName("findings").HasMaxLength(2000);
        entity.Property(r => r.Decision).HasColumnName("decision").HasMaxLength(1000);
        entity.Property(r => r.PerformedByDisplayName).HasColumnName("performed_by_display_name").HasMaxLength(200);
        entity.Property(r => r.SourceType).HasColumnName("source_type").HasMaxLength(20)
            .HasConversion(v => Mold.MoldValueConversions.ToStorage(v), v => Mold.MoldValueConversions.SourceFromStorage(v));
        entity.Property(r => r.SourceReference).HasColumnName("source_reference").HasMaxLength(200);
        entity.Property(r => r.IdempotencyKey).HasColumnName("idempotency_key").HasMaxLength(128);
        entity.Property(r => r.IdempotencyPayloadFingerprint).HasColumnName("idempotency_payload_fingerprint").HasMaxLength(64);
        entity.Property(r => r.CreatedAtUtc).HasColumnName("created_at_utc");
        entity.Property(r => r.CreatedBySubjectId).HasColumnName("created_by_subject_id").HasMaxLength(200);

        entity.HasIndex(r => new { r.TenantId, r.MoldId, r.IdempotencyKey }).IsUnique();
        entity.HasIndex(r => new { r.TenantId, r.MoldId, r.TrialNumberNormalized }).IsUnique();
        entity.HasIndex(r => new { r.TenantId, r.FacilityId, r.MoldId, r.TrialDate });
        entity.HasOne<Mold.MoldRegistryEntry>().WithMany().HasForeignKey(r => r.MoldId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Tenant>().WithMany().HasForeignKey(r => r.TenantId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Facility>().WithMany()
            .HasForeignKey(r => new { r.TenantId, r.FacilityId })
            .HasPrincipalKey(f => new { f.TenantId, f.Id })
            .OnDelete(DeleteBehavior.Restrict);
    }
}
