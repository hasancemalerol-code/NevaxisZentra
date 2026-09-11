using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CrevaFactOS.Persistence.ShiftOperations;

internal sealed class OperationalShiftInstanceEntityConfiguration : IEntityTypeConfiguration<OperationalShiftInstance>
{
    public void Configure(EntityTypeBuilder<OperationalShiftInstance> entity)
    {
        entity.ToTable("operational_shift_instances");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasColumnName("id");
        entity.Property(e => e.TenantId).HasColumnName("tenant_id");
        entity.Property(e => e.FacilityId).HasColumnName("facility_id");
        entity.Property(e => e.ProductionShiftId).HasColumnName("production_shift_id");
        entity.Property(e => e.ShiftCode).HasColumnName("shift_code").HasMaxLength(20);
        entity.Property(e => e.Name).HasColumnName("name").HasMaxLength(120);
        entity.Property(e => e.ShiftDate).HasColumnName("shift_date");
        entity.Property(e => e.PlannedStartUtc).HasColumnName("planned_start_utc");
        entity.Property(e => e.PlannedEndUtc).HasColumnName("planned_end_utc");
        entity.Property(e => e.ActualStartUtc).HasColumnName("actual_start_utc");
        entity.Property(e => e.ActualEndUtc).HasColumnName("actual_end_utc");
        entity.Property(e => e.Status).HasColumnName("status").HasMaxLength(30);
        entity.Property(e => e.HandoverState).HasColumnName("handover_state").HasMaxLength(30);
        entity.Property(e => e.SupervisorMembershipId).HasColumnName("supervisor_membership_id");
        entity.Property(e => e.SupervisorLabel).HasColumnName("supervisor_label").HasMaxLength(200);
        entity.Property(e => e.TeamLabel).HasColumnName("team_label").HasMaxLength(200);
        entity.Property(e => e.ProductionContextJson).HasColumnName("production_context_json");
        entity.Property(e => e.HandoverReadinessScore).HasColumnName("handover_readiness_score");
        entity.Property(e => e.AiSummaryJson).HasColumnName("ai_summary_json");
        entity.Property(e => e.FactoryHandoverId).HasColumnName("factory_handover_id");
        entity.Property(e => e.CreatedAtUtc).HasColumnName("created_at_utc");
        entity.Property(e => e.UpdatedAtUtc).HasColumnName("updated_at_utc");
        entity.Property(e => e.ConcurrencyToken).HasColumnName("concurrency_token").IsConcurrencyToken();

        entity.HasIndex(e => new { e.TenantId, e.FacilityId, e.Status })
            .HasDatabaseName("ix_operational_shift_instances_tenant_facility_status");
        entity.HasIndex(e => new { e.TenantId, e.FacilityId, e.ShiftDate })
            .HasDatabaseName("ix_operational_shift_instances_tenant_facility_date");

        entity.HasOne<CrevaFactOS.Persistence.Facilities.Facility>()
            .WithMany()
            .HasForeignKey(e => new { e.TenantId, e.FacilityId })
            .HasPrincipalKey(f => new { f.TenantId, f.Id })
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class ShiftOpenItemEntityConfiguration : IEntityTypeConfiguration<ShiftOpenItem>
{
    public void Configure(EntityTypeBuilder<ShiftOpenItem> entity)
    {
        entity.ToTable("shift_open_items");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasColumnName("id");
        entity.Property(e => e.TenantId).HasColumnName("tenant_id");
        entity.Property(e => e.FacilityId).HasColumnName("facility_id");
        entity.Property(e => e.ShiftInstanceId).HasColumnName("shift_instance_id");
        entity.Property(e => e.SourceType).HasColumnName("source_type").HasMaxLength(60);
        entity.Property(e => e.SourceId).HasColumnName("source_id").HasMaxLength(120);
        entity.Property(e => e.Category).HasColumnName("category").HasMaxLength(30);
        entity.Property(e => e.Title).HasColumnName("title").HasMaxLength(200);
        entity.Property(e => e.Description).HasColumnName("description").HasMaxLength(4000);
        entity.Property(e => e.Severity).HasColumnName("severity").HasMaxLength(20);
        entity.Property(e => e.Status).HasColumnName("status").HasMaxLength(30);
        entity.Property(e => e.OwnerType).HasColumnName("owner_type").HasMaxLength(30);
        entity.Property(e => e.OwnerId).HasColumnName("owner_id").HasMaxLength(120);
        entity.Property(e => e.PrimaryWorkerId).HasColumnName("primary_worker_id");
        entity.Property(e => e.LinkedAiTaskId).HasColumnName("linked_ai_task_id");
        entity.Property(e => e.DigitalWorkCaseId).HasColumnName("digital_work_case_id");
        entity.Property(e => e.DeadlineUtc).HasColumnName("deadline_utc");
        entity.Property(e => e.RequiresHandover).HasColumnName("requires_handover");
        entity.Property(e => e.HandoverPriority).HasColumnName("handover_priority").HasMaxLength(20);
        entity.Property(e => e.VerificationRequired).HasColumnName("verification_required");
        entity.Property(e => e.ProvenanceJson).HasColumnName("provenance_json");
        entity.Property(e => e.FreshnessJson).HasColumnName("freshness_json");
        entity.Property(e => e.CarriedFromShiftInstanceId).HasColumnName("carried_from_shift_instance_id");
        entity.Property(e => e.CarriedToShiftInstanceId).HasColumnName("carried_to_shift_instance_id");
        entity.Property(e => e.CarryoverCount).HasColumnName("carryover_count");
        entity.Property(e => e.CreatedAtUtc).HasColumnName("created_at_utc");
        entity.Property(e => e.ResolvedAtUtc).HasColumnName("resolved_at_utc");
        entity.Property(e => e.UpdatedAtUtc).HasColumnName("updated_at_utc");
        entity.Property(e => e.ConcurrencyToken).HasColumnName("concurrency_token").IsConcurrencyToken();

        entity.HasIndex(e => new { e.TenantId, e.ShiftInstanceId, e.Status })
            .HasDatabaseName("ix_shift_open_items_tenant_shift_status");
        entity.HasIndex(e => new { e.TenantId, e.ShiftInstanceId, e.SourceType, e.SourceId })
            .IsUnique()
            .HasDatabaseName("ux_shift_open_items_tenant_shift_source");

        entity.HasOne(e => e.ShiftInstance)
            .WithMany(s => s.OpenItems)
            .HasForeignKey(e => e.ShiftInstanceId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class ShiftHandoverAcceptanceEntityConfiguration : IEntityTypeConfiguration<ShiftHandoverAcceptance>
{
    public void Configure(EntityTypeBuilder<ShiftHandoverAcceptance> entity)
    {
        entity.ToTable("shift_handover_acceptances");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasColumnName("id");
        entity.Property(e => e.TenantId).HasColumnName("tenant_id");
        entity.Property(e => e.FacilityId).HasColumnName("facility_id");
        entity.Property(e => e.ShiftInstanceId).HasColumnName("shift_instance_id");
        entity.Property(e => e.FactoryHandoverId).HasColumnName("factory_handover_id");
        entity.Property(e => e.OutgoingMembershipId).HasColumnName("outgoing_membership_id");
        entity.Property(e => e.IncomingMembershipId).HasColumnName("incoming_membership_id");
        entity.Property(e => e.OutgoingSupervisorLabel).HasColumnName("outgoing_supervisor_label").HasMaxLength(200);
        entity.Property(e => e.IncomingSupervisorLabel).HasColumnName("incoming_supervisor_label").HasMaxLength(200);
        entity.Property(e => e.PreparedAtUtc).HasColumnName("prepared_at_utc");
        entity.Property(e => e.HandedOverAtUtc).HasColumnName("handed_over_at_utc");
        entity.Property(e => e.AcceptedAtUtc).HasColumnName("accepted_at_utc");
        entity.Property(e => e.OutgoingComments).HasColumnName("outgoing_comments").HasMaxLength(4000);
        entity.Property(e => e.IncomingAcknowledgement).HasColumnName("incoming_acknowledgement").HasMaxLength(4000);
        entity.Property(e => e.Verified).HasColumnName("verified");
        entity.Property(e => e.VerificationNotes).HasColumnName("verification_notes").HasMaxLength(2000);
        entity.Property(e => e.CreatedAtUtc).HasColumnName("created_at_utc");
        entity.Property(e => e.ConcurrencyToken).HasColumnName("concurrency_token").IsConcurrencyToken();

        entity.HasIndex(e => new { e.TenantId, e.ShiftInstanceId })
            .IsUnique()
            .HasDatabaseName("ux_shift_handover_acceptances_tenant_shift");
    }
}
