using CrevaFactOS.Persistence.Facilities;
using CrevaFactOS.Persistence.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CrevaFactOS.Persistence.Configurations;

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
