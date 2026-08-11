using CrevaFactOS.Persistence.Facilities;
using CrevaFactOS.Persistence.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CrevaFactOS.Persistence.Configurations;

internal sealed class MaintenanceAiActionDraftEntityConfiguration
    : IEntityTypeConfiguration<Maintenance.MaintenanceAiActionDraft>
{
    public void Configure(EntityTypeBuilder<Maintenance.MaintenanceAiActionDraft> entity)
    {
        entity.ToTable("maintenance_ai_action_drafts");
        entity.HasKey(d => d.Id);
        entity.Property(d => d.Id).HasColumnName("id");
        entity.Property(d => d.TenantId).HasColumnName("tenant_id");
        entity.Property(d => d.FacilityId).HasColumnName("facility_id");
        entity.Property(d => d.AssetReference).HasColumnName("asset_reference").HasMaxLength(200);
        entity.Property(d => d.AssetName).HasColumnName("asset_name").HasMaxLength(200);
        entity.Property(d => d.ConditionEvaluationReference).HasColumnName("condition_evaluation_reference").HasMaxLength(128);
        entity.Property(d => d.RiskAssessmentId).HasColumnName("risk_assessment_id");
        entity.Property(d => d.InputFingerprint).HasColumnName("input_fingerprint").HasMaxLength(128);
        entity.Property(d => d.RuleSetVersion).HasColumnName("rule_set_version").HasMaxLength(40);
        entity.Property(d => d.ProviderIdentifier).HasColumnName("provider_identifier").HasMaxLength(80);
        entity.Property(d => d.ModelIdentifier).HasColumnName("model_identifier").HasMaxLength(80);
        entity.Property(d => d.PromptVersion).HasColumnName("prompt_version").HasMaxLength(40);
        entity.Property(d => d.InputSnapshotJson).HasColumnName("input_snapshot_json");
        entity.Property(d => d.RawProviderResponseJson).HasColumnName("raw_provider_response_json");
        entity.Property(d => d.ValidatedOutputJson).HasColumnName("validated_output_json");
        entity.Property(d => d.AiOriginalOutputJson).HasColumnName("ai_original_output_json");
        entity.Property(d => d.QualitativeConfidence).HasColumnName("qualitative_confidence").HasMaxLength(20);
        entity.Property(d => d.Status).HasColumnName("status").HasMaxLength(30);
        entity.Property(d => d.Version).HasColumnName("version");
        entity.Property(d => d.SuggestedTitle).HasColumnName("suggested_title").HasMaxLength(300);
        entity.Property(d => d.SuggestedDescription).HasColumnName("suggested_description").HasMaxLength(4000);
        entity.Property(d => d.SuggestedPriority).HasColumnName("suggested_priority").HasMaxLength(20);
        entity.Property(d => d.SuggestedRequestType).HasColumnName("suggested_request_type").HasMaxLength(30);
        entity.Property(d => d.HumanEditedTitle).HasColumnName("human_edited_title").HasMaxLength(300);
        entity.Property(d => d.HumanEditedDescription).HasColumnName("human_edited_description").HasMaxLength(4000);
        entity.Property(d => d.HumanEditedPriority).HasColumnName("human_edited_priority").HasMaxLength(20);
        entity.Property(d => d.HumanEditedRequestType).HasColumnName("human_edited_request_type").HasMaxLength(30);
        entity.Property(d => d.HumanEditorSubjectId).HasColumnName("human_editor_subject_id").HasMaxLength(200);
        entity.Property(d => d.HumanEditedAtUtc).HasColumnName("human_edited_at_utc");
        entity.Property(d => d.GeneratedBySubjectId).HasColumnName("generated_by_subject_id").HasMaxLength(200);
        entity.Property(d => d.GeneratedAtUtc).HasColumnName("generated_at_utc");
        entity.Property(d => d.LastReviewedAtUtc).HasColumnName("last_reviewed_at_utc");
        entity.Property(d => d.ApprovedBySubjectId).HasColumnName("approved_by_subject_id").HasMaxLength(200);
        entity.Property(d => d.ApprovedAtUtc).HasColumnName("approved_at_utc");
        entity.Property(d => d.RejectedBySubjectId).HasColumnName("rejected_by_subject_id").HasMaxLength(200);
        entity.Property(d => d.RejectedAtUtc).HasColumnName("rejected_at_utc");
        entity.Property(d => d.RejectionReason).HasColumnName("rejection_reason").HasMaxLength(2000);
        entity.Property(d => d.CreatedMaintenanceRequestId).HasColumnName("created_maintenance_request_id");
        entity.Property(d => d.ExecutionResultAtUtc).HasColumnName("execution_result_at_utc");
        entity.Property(d => d.FailureCode).HasColumnName("failure_code").HasMaxLength(80);
        entity.Property(d => d.FailureMessage).HasColumnName("failure_message").HasMaxLength(2000);
        entity.Property(d => d.CreatedAtUtc).HasColumnName("created_at_utc");
        entity.Property(d => d.UpdatedAtUtc).HasColumnName("updated_at_utc");

        entity.HasIndex(d => new { d.TenantId, d.FacilityId, d.AssetReference, d.GeneratedAtUtc })
            .HasDatabaseName("ix_maad_tenant_facility_asset_generated");
        entity.HasIndex(d => new { d.TenantId, d.FacilityId, d.InputFingerprint })
            .HasDatabaseName("ix_maad_tenant_facility_fingerprint");

        entity.HasOne<Tenant>().WithMany().HasForeignKey(d => d.TenantId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Facility>().WithMany()
            .HasForeignKey(d => new { d.TenantId, d.FacilityId })
            .HasPrincipalKey(f => new { f.TenantId, f.Id })
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class MaintenanceAiDraftAuditEventEntityConfiguration
    : IEntityTypeConfiguration<Maintenance.MaintenanceAiDraftAuditEvent>
{
    public void Configure(EntityTypeBuilder<Maintenance.MaintenanceAiDraftAuditEvent> entity)
    {
        entity.ToTable("maintenance_ai_draft_audit_events");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasColumnName("id");
        entity.Property(e => e.TenantId).HasColumnName("tenant_id");
        entity.Property(e => e.FacilityId).HasColumnName("facility_id");
        entity.Property(e => e.DraftId).HasColumnName("draft_id");
        entity.Property(e => e.Action).HasColumnName("action").HasMaxLength(120);
        entity.Property(e => e.ActorSubjectId).HasColumnName("actor_subject_id").HasMaxLength(200);
        entity.Property(e => e.DraftVersion).HasColumnName("draft_version");
        entity.Property(e => e.CorrelationId).HasColumnName("correlation_id");
        entity.Property(e => e.ResultCode).HasColumnName("result_code").HasMaxLength(40);
        entity.Property(e => e.DetailsJson).HasColumnName("details_json");
        entity.Property(e => e.OccurredAtUtc).HasColumnName("occurred_at_utc");

        entity.HasIndex(e => new { e.TenantId, e.FacilityId, e.DraftId, e.OccurredAtUtc })
            .HasDatabaseName("ix_made_tenant_facility_draft_occurred");

        entity.HasOne<Maintenance.MaintenanceAiActionDraft>().WithMany()
            .HasForeignKey(e => e.DraftId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
