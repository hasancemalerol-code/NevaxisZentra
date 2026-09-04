using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CrevaFactOS.Persistence.AiTasks;

internal sealed class AiTaskAiAnalysisEntityConfiguration : IEntityTypeConfiguration<AiTaskAiAnalysis>
{
    public void Configure(EntityTypeBuilder<AiTaskAiAnalysis> entity)
    {
        entity.ToTable("ai_task_ai_analyses");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasColumnName("id");
        entity.Property(e => e.TenantId).HasColumnName("tenant_id");
        entity.Property(e => e.FacilityId).HasColumnName("facility_id");
        entity.Property(e => e.TaskId).HasColumnName("task_id");
        entity.Property(e => e.AnalysisType).HasColumnName("analysis_type").HasMaxLength(40);
        entity.Property(e => e.ProviderName).HasColumnName("provider_name").HasMaxLength(80);
        entity.Property(e => e.ModelName).HasColumnName("model_name").HasMaxLength(80);
        entity.Property(e => e.PromptTemplateVersion).HasColumnName("prompt_template_version").HasMaxLength(80);
        entity.Property(e => e.ConfidenceBand).HasColumnName("confidence_band").HasMaxLength(30);
        entity.Property(e => e.ResultJson).HasColumnName("result_json").HasColumnType("text");
        entity.Property(e => e.IdempotencyKey).HasColumnName("idempotency_key").HasMaxLength(200);
        entity.Property(e => e.CorrelationId).HasColumnName("correlation_id");
        entity.Property(e => e.Superseded).HasColumnName("superseded");
        entity.Property(e => e.SupersededByAnalysisId).HasColumnName("superseded_by_analysis_id");
        entity.Property(e => e.CreatedAtUtc).HasColumnName("created_at_utc");
        entity.Property(e => e.CreatedBy).HasColumnName("created_by").HasMaxLength(120);

        entity.HasIndex(e => new { e.TenantId, e.TaskId, e.IdempotencyKey })
            .IsUnique()
            .HasDatabaseName("ux_ai_task_ai_analyses_idempotency");
        entity.HasIndex(e => new { e.TenantId, e.FacilityId, e.TaskId, e.CreatedAtUtc })
            .HasDatabaseName("ix_ai_task_ai_analyses_task_time");
    }
}

internal sealed class AiTaskAiRecommendationEntityConfiguration : IEntityTypeConfiguration<AiTaskAiRecommendation>
{
    public void Configure(EntityTypeBuilder<AiTaskAiRecommendation> entity)
    {
        entity.ToTable("ai_task_ai_recommendations");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasColumnName("id");
        entity.Property(e => e.TenantId).HasColumnName("tenant_id");
        entity.Property(e => e.FacilityId).HasColumnName("facility_id");
        entity.Property(e => e.TaskId).HasColumnName("task_id");
        entity.Property(e => e.AnalysisId).HasColumnName("analysis_id");
        entity.Property(e => e.ActionType).HasColumnName("action_type").HasMaxLength(60);
        entity.Property(e => e.Title).HasColumnName("title").HasMaxLength(200);
        entity.Property(e => e.Rationale).HasColumnName("rationale").HasMaxLength(2000);
        entity.Property(e => e.ConfidenceBand).HasColumnName("confidence_band").HasMaxLength(30);
        entity.Property(e => e.UrgencyBand).HasColumnName("urgency_band").HasMaxLength(30);
        entity.Property(e => e.RelatedEntityType).HasColumnName("related_entity_type").HasMaxLength(60);
        entity.Property(e => e.RelatedEntityId).HasColumnName("related_entity_id").HasMaxLength(120);
        entity.Property(e => e.PayloadJson).HasColumnName("payload_json").HasColumnType("text");
        entity.Property(e => e.Status).HasColumnName("status").HasMaxLength(30);
        entity.Property(e => e.FeedbackDecision).HasColumnName("feedback_decision").HasMaxLength(30);
        entity.Property(e => e.FeedbackReason).HasColumnName("feedback_reason").HasMaxLength(2000);
        entity.Property(e => e.FeedbackByMembershipId).HasColumnName("feedback_by_membership_id");
        entity.Property(e => e.FeedbackAtUtc).HasColumnName("feedback_at_utc");
        entity.Property(e => e.AppliedResultJson).HasColumnName("applied_result_json").HasColumnType("text");
        entity.Property(e => e.CreatedAtUtc).HasColumnName("created_at_utc");

        entity.HasIndex(e => new { e.TenantId, e.TaskId, e.Status })
            .HasDatabaseName("ix_ai_task_ai_recommendations_task_status");
    }
}

internal sealed class AiTaskRelationshipEntityConfiguration : IEntityTypeConfiguration<AiTaskRelationship>
{
    public void Configure(EntityTypeBuilder<AiTaskRelationship> entity)
    {
        entity.ToTable("ai_task_relationships");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasColumnName("id");
        entity.Property(e => e.TenantId).HasColumnName("tenant_id");
        entity.Property(e => e.FacilityId).HasColumnName("facility_id");
        entity.Property(e => e.SourceTaskId).HasColumnName("source_task_id");
        entity.Property(e => e.TargetTaskId).HasColumnName("target_task_id");
        entity.Property(e => e.RelationshipType).HasColumnName("relationship_type").HasMaxLength(40);
        entity.Property(e => e.Status).HasColumnName("status").HasMaxLength(30);
        entity.Property(e => e.ConfidenceBand).HasColumnName("confidence_band").HasMaxLength(30);
        entity.Property(e => e.Explanation).HasColumnName("explanation").HasMaxLength(2000);
        entity.Property(e => e.AnalysisId).HasColumnName("analysis_id");
        entity.Property(e => e.ConfirmedByMembershipId).HasColumnName("confirmed_by_membership_id");
        entity.Property(e => e.ConfirmedAtUtc).HasColumnName("confirmed_at_utc");
        entity.Property(e => e.CreatedAtUtc).HasColumnName("created_at_utc");
        entity.Property(e => e.CreatedBy).HasColumnName("created_by").HasMaxLength(120);

        entity.HasIndex(e => new { e.TenantId, e.SourceTaskId, e.TargetTaskId, e.RelationshipType })
            .IsUnique()
            .HasDatabaseName("ux_ai_task_relationships_pair_type");
    }
}
