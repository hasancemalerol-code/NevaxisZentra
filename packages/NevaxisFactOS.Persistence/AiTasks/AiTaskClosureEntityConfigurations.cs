using NevaxisFactOS.Persistence.Facilities;
using NevaxisFactOS.Persistence.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace NevaxisFactOS.Persistence.AiTasks;

internal sealed class AiTaskEvidenceEntityConfiguration : IEntityTypeConfiguration<AiTaskEvidence>
{
    public void Configure(EntityTypeBuilder<AiTaskEvidence> entity)
    {
        entity.ToTable("ai_task_evidence");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasColumnName("id");
        entity.Property(e => e.TenantId).HasColumnName("tenant_id");
        entity.Property(e => e.FacilityId).HasColumnName("facility_id");
        entity.Property(e => e.TaskId).HasColumnName("task_id");
        entity.Property(e => e.EvidenceType).HasColumnName("evidence_type").HasMaxLength(50).IsRequired();
        entity.Property(e => e.Title).HasColumnName("title").HasMaxLength(300).IsRequired();
        entity.Property(e => e.Description).HasColumnName("description").HasMaxLength(4000);
        entity.Property(e => e.ReferenceMetadataJson).HasColumnName("reference_metadata_json");
        entity.Property(e => e.SourceModule).HasColumnName("source_module").HasMaxLength(100);
        entity.Property(e => e.SourceEntityId).HasColumnName("source_entity_id").HasMaxLength(200);
        entity.Property(e => e.SubmittedByMembershipId).HasColumnName("submitted_by_membership_id");
        entity.Property(e => e.SubmittedAtUtc).HasColumnName("submitted_at_utc");
        entity.Property(e => e.SubmittedByLabel).HasColumnName("submitted_by_label").HasMaxLength(200).IsRequired();
        entity.HasOne<Tenant>().WithMany().HasForeignKey(e => e.TenantId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Facility>().WithMany()
            .HasForeignKey(e => new { e.TenantId, e.FacilityId })
            .HasPrincipalKey(f => new { f.TenantId, f.Id })
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(e => e.Task).WithMany(t => t.EvidenceItems)
            .HasForeignKey(e => e.TaskId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasIndex(e => new { e.TenantId, e.FacilityId, e.TaskId, e.SubmittedAtUtc })
            .HasDatabaseName("ix_ai_task_evidence_tenant_facility_task");
    }
}

internal sealed class AiTaskClosurePolicyEntityConfiguration : IEntityTypeConfiguration<AiTaskClosurePolicy>
{
    public void Configure(EntityTypeBuilder<AiTaskClosurePolicy> entity)
    {
        entity.ToTable("ai_task_closure_policies");
        entity.HasKey(p => p.Id);
        entity.Property(p => p.Id).HasColumnName("id");
        entity.Property(p => p.TenantId).HasColumnName("tenant_id");
        entity.Property(p => p.FacilityId).HasColumnName("facility_id");
        entity.Property(p => p.PolicyKey).HasColumnName("policy_key").HasMaxLength(100).IsRequired();
        entity.Property(p => p.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
        entity.Property(p => p.Category).HasColumnName("category").HasMaxLength(50);
        entity.Property(p => p.Priority).HasColumnName("priority").HasMaxLength(20);
        entity.Property(p => p.SourceModule).HasColumnName("source_module").HasMaxLength(100);
        entity.Property(p => p.MinimumEvidenceCount).HasColumnName("minimum_evidence_count");
        entity.Property(p => p.RequiredEvidenceTypesJson).HasColumnName("required_evidence_types_json");
        entity.Property(p => p.ApprovalRequired).HasColumnName("approval_required");
        entity.Property(p => p.FourEyesRequired).HasColumnName("four_eyes_required");
        entity.Property(p => p.ResolutionSummaryRequired).HasColumnName("resolution_summary_required");
        entity.Property(p => p.Enabled).HasColumnName("enabled");
        entity.Property(p => p.CreatedAtUtc).HasColumnName("created_at_utc");
        entity.Property(p => p.UpdatedAtUtc).HasColumnName("updated_at_utc");
        entity.HasOne<Tenant>().WithMany().HasForeignKey(p => p.TenantId).OnDelete(DeleteBehavior.Restrict);
        entity.HasIndex(p => new { p.TenantId, p.PolicyKey })
            .HasDatabaseName("ux_ai_task_closure_policies_tenant_key")
            .IsUnique();
        entity.HasIndex(p => new { p.TenantId, p.FacilityId, p.Category, p.Priority })
            .HasDatabaseName("ix_ai_task_closure_policies_tenant_facility_match");
    }
}
