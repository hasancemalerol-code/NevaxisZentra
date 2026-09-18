using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace NevaxisZentra.Persistence.ShiftHandover;

internal sealed class ShiftHandoverEntityConfiguration : IEntityTypeConfiguration<FactoryShiftHandover>
{
    public void Configure(EntityTypeBuilder<FactoryShiftHandover> entity)
    {
        entity.ToTable("factory_shift_handovers");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasColumnName("id");
        entity.Property(e => e.TenantId).HasColumnName("tenant_id");
        entity.Property(e => e.FacilityId).HasColumnName("facility_id");
        entity.Property(e => e.HandoverReference).HasColumnName("handover_reference").HasMaxLength(40);
        entity.Property(e => e.ShiftDate).HasColumnName("shift_date");
        entity.Property(e => e.ShiftPeriod).HasColumnName("shift_period").HasMaxLength(20);
        entity.Property(e => e.Department).HasColumnName("department").HasMaxLength(120);
        entity.Property(e => e.Summary).HasColumnName("summary").HasMaxLength(4000);
        entity.Property(e => e.OutgoingMembershipId).HasColumnName("outgoing_membership_id");
        entity.Property(e => e.IncomingMembershipId).HasColumnName("incoming_membership_id");
        entity.Property(e => e.Status).HasColumnName("status").HasMaxLength(30);
        entity.Property(e => e.CreatedAtUtc).HasColumnName("created_at_utc");
        entity.Property(e => e.UpdatedAtUtc).HasColumnName("updated_at_utc");
        entity.Property(e => e.HandedOverAtUtc).HasColumnName("handed_over_at_utc");
        entity.Property(e => e.AcknowledgedAtUtc).HasColumnName("acknowledged_at_utc");
        entity.Property(e => e.CompletedAtUtc).HasColumnName("completed_at_utc");
        entity.Property(e => e.Version).HasColumnName("version").IsConcurrencyToken();

        entity.HasIndex(e => new { e.TenantId, e.FacilityId, e.Status })
            .HasDatabaseName("ix_shift_handovers_tenant_facility_status");
        entity.HasIndex(e => new { e.TenantId, e.FacilityId, e.ShiftDate })
            .HasDatabaseName("ix_shift_handovers_tenant_facility_shift_date");
        entity.HasIndex(e => new { e.TenantId, e.HandoverReference })
            .IsUnique()
            .HasDatabaseName("ux_shift_handovers_tenant_reference");

        entity.HasOne<NevaxisZentra.Persistence.Facilities.Facility>()
            .WithMany()
            .HasForeignKey(e => new { e.TenantId, e.FacilityId })
            .HasPrincipalKey(f => new { f.TenantId, f.Id })
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class ShiftHandoverItemEntityConfiguration : IEntityTypeConfiguration<ShiftHandoverItem>
{
    public void Configure(EntityTypeBuilder<ShiftHandoverItem> entity)
    {
        entity.ToTable("factory_shift_handover_items");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasColumnName("id");
        entity.Property(e => e.TenantId).HasColumnName("tenant_id");
        entity.Property(e => e.FacilityId).HasColumnName("facility_id");
        entity.Property(e => e.HandoverId).HasColumnName("handover_id");
        entity.Property(e => e.SortOrder).HasColumnName("sort_order");
        entity.Property(e => e.Category).HasColumnName("category").HasMaxLength(30);
        entity.Property(e => e.Title).HasColumnName("title").HasMaxLength(200);
        entity.Property(e => e.Description).HasColumnName("description").HasMaxLength(4000);
        entity.Property(e => e.Severity).HasColumnName("severity").HasMaxLength(20);
        entity.Property(e => e.SourceModule).HasColumnName("source_module").HasMaxLength(60);
        entity.Property(e => e.SourceEntityId).HasColumnName("source_entity_id").HasMaxLength(120);
        entity.Property(e => e.Status).HasColumnName("status").HasMaxLength(20);
        entity.Property(e => e.RequiresFollowUp).HasColumnName("requires_follow_up");
        entity.Property(e => e.LinkedAiTaskId).HasColumnName("linked_ai_task_id");
        entity.Property(e => e.EvidenceMetadataJson).HasColumnName("evidence_metadata_json").HasMaxLength(2000);
        entity.Property(e => e.CreatedAtUtc).HasColumnName("created_at_utc");
        entity.Property(e => e.UpdatedAtUtc).HasColumnName("updated_at_utc");
        entity.Property(e => e.Version).HasColumnName("version").IsConcurrencyToken();

        entity.HasIndex(e => new { e.TenantId, e.HandoverId, e.SortOrder })
            .HasDatabaseName("ix_shift_handover_items_tenant_handover_sort");

        entity.HasOne(e => e.Handover)
            .WithMany(h => h.Items)
            .HasForeignKey(e => e.HandoverId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class ShiftHandoverEventEntityConfiguration : IEntityTypeConfiguration<ShiftHandoverEvent>
{
    public void Configure(EntityTypeBuilder<ShiftHandoverEvent> entity)
    {
        entity.ToTable("factory_shift_handover_events");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasColumnName("id");
        entity.Property(e => e.TenantId).HasColumnName("tenant_id");
        entity.Property(e => e.FacilityId).HasColumnName("facility_id");
        entity.Property(e => e.HandoverId).HasColumnName("handover_id");
        entity.Property(e => e.EventType).HasColumnName("event_type").HasMaxLength(60);
        entity.Property(e => e.ActorMembershipId).HasColumnName("actor_membership_id").HasMaxLength(200);
        entity.Property(e => e.ActorLabel).HasColumnName("actor_label").HasMaxLength(200);
        entity.Property(e => e.PayloadJson).HasColumnName("payload_json");
        entity.Property(e => e.OccurredAtUtc).HasColumnName("occurred_at_utc");

        entity.HasIndex(e => new { e.TenantId, e.FacilityId, e.HandoverId, e.OccurredAtUtc })
            .HasDatabaseName("ix_shift_handover_events_tenant_handover_occurred");

        entity.HasOne(e => e.Handover)
            .WithMany(h => h.Events)
            .HasForeignKey(e => e.HandoverId)
            .OnDelete(DeleteBehavior.Cascade);

        entity.HasOne<NevaxisZentra.Persistence.Facilities.Facility>()
            .WithMany()
            .HasForeignKey(e => new { e.TenantId, e.FacilityId })
            .HasPrincipalKey(f => new { f.TenantId, f.Id })
            .OnDelete(DeleteBehavior.Restrict);
    }
}
