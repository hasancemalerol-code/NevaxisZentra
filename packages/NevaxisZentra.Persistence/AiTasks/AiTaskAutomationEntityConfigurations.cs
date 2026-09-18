using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace NevaxisZentra.Persistence.AiTasks;

internal sealed class AiTaskAutomationRuleEntityConfiguration : IEntityTypeConfiguration<AiTaskAutomationRule>
{
    public void Configure(EntityTypeBuilder<AiTaskAutomationRule> entity)
    {
        entity.ToTable("ai_task_automation_rules");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasColumnName("id");
        entity.Property(e => e.TenantId).HasColumnName("tenant_id");
        entity.Property(e => e.FacilityId).HasColumnName("facility_id");
        entity.Property(e => e.RuleKey).HasColumnName("rule_key").HasMaxLength(80).IsRequired();
        entity.Property(e => e.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
        entity.Property(e => e.Description).HasColumnName("description").HasMaxLength(2000);
        entity.Property(e => e.Enabled).HasColumnName("enabled");
        entity.Property(e => e.SortOrder).HasColumnName("sort_order");
        entity.Property(e => e.TriggerType).HasColumnName("trigger_type").HasMaxLength(40).IsRequired();
        entity.Property(e => e.ConditionsJson).HasColumnName("conditions_json").HasColumnType("text").IsRequired();
        entity.Property(e => e.ActionsJson).HasColumnName("actions_json").HasColumnType("text").IsRequired();
        entity.Property(e => e.EffectiveFromUtc).HasColumnName("effective_from_utc");
        entity.Property(e => e.EffectiveUntilUtc).HasColumnName("effective_until_utc");
        entity.Property(e => e.Version).HasColumnName("version");
        entity.Property(e => e.CurrentRevisionId).HasColumnName("current_revision_id");
        entity.Property(e => e.LastMatchedAtUtc).HasColumnName("last_matched_at_utc");
        entity.Property(e => e.CreatedBy).HasColumnName("created_by").HasMaxLength(120).IsRequired();
        entity.Property(e => e.UpdatedBy).HasColumnName("updated_by").HasMaxLength(120).IsRequired();
        entity.Property(e => e.CreatedAtUtc).HasColumnName("created_at_utc");
        entity.Property(e => e.UpdatedAtUtc).HasColumnName("updated_at_utc");
        entity.HasIndex(e => new { e.TenantId, e.FacilityId, e.SortOrder })
            .HasDatabaseName("ix_ai_task_automation_rules_tenant_facility_sort");
        entity.HasIndex(e => new { e.TenantId, e.FacilityId, e.RuleKey })
            .IsUnique()
            .HasDatabaseName("ux_ai_task_automation_rules_tenant_facility_key");
    }
}

internal sealed class AiTaskAutomationRuleRevisionEntityConfiguration : IEntityTypeConfiguration<AiTaskAutomationRuleRevision>
{
    public void Configure(EntityTypeBuilder<AiTaskAutomationRuleRevision> entity)
    {
        entity.ToTable("ai_task_automation_rule_revisions");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasColumnName("id");
        entity.Property(e => e.TenantId).HasColumnName("tenant_id");
        entity.Property(e => e.RuleId).HasColumnName("rule_id");
        entity.Property(e => e.Version).HasColumnName("version");
        entity.Property(e => e.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
        entity.Property(e => e.Description).HasColumnName("description").HasMaxLength(2000);
        entity.Property(e => e.Enabled).HasColumnName("enabled");
        entity.Property(e => e.SortOrder).HasColumnName("sort_order");
        entity.Property(e => e.TriggerType).HasColumnName("trigger_type").HasMaxLength(40).IsRequired();
        entity.Property(e => e.ConditionsJson).HasColumnName("conditions_json").HasColumnType("text").IsRequired();
        entity.Property(e => e.ActionsJson).HasColumnName("actions_json").HasColumnType("text").IsRequired();
        entity.Property(e => e.EffectiveFromUtc).HasColumnName("effective_from_utc");
        entity.Property(e => e.EffectiveUntilUtc).HasColumnName("effective_until_utc");
        entity.Property(e => e.ChangedBy).HasColumnName("changed_by").HasMaxLength(120).IsRequired();
        entity.Property(e => e.ChangedAtUtc).HasColumnName("changed_at_utc");
        entity.Property(e => e.ChangeReason).HasColumnName("change_reason").HasMaxLength(500).IsRequired();
        entity.HasIndex(e => new { e.TenantId, e.RuleId, e.Version })
            .IsUnique()
            .HasDatabaseName("ux_ai_task_automation_rule_revisions_rule_version");
        entity.HasOne(e => e.Rule)
            .WithMany(r => r.Revisions)
            .HasForeignKey(e => e.RuleId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class AiTaskAutomationExecutionEntityConfiguration : IEntityTypeConfiguration<AiTaskAutomationExecution>
{
    public void Configure(EntityTypeBuilder<AiTaskAutomationExecution> entity)
    {
        entity.ToTable("ai_task_automation_executions");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasColumnName("id");
        entity.Property(e => e.TenantId).HasColumnName("tenant_id");
        entity.Property(e => e.FacilityId).HasColumnName("facility_id");
        entity.Property(e => e.TaskId).HasColumnName("task_id");
        entity.Property(e => e.RuleId).HasColumnName("rule_id");
        entity.Property(e => e.RuleVersion).HasColumnName("rule_version");
        entity.Property(e => e.TriggerType).HasColumnName("trigger_type").HasMaxLength(40).IsRequired();
        entity.Property(e => e.IdempotencyKey).HasColumnName("idempotency_key").HasMaxLength(200).IsRequired();
        entity.Property(e => e.MatchedConditionsJson).HasColumnName("matched_conditions_json").HasColumnType("text").IsRequired();
        entity.Property(e => e.ActionsAppliedJson).HasColumnName("actions_applied_json").HasColumnType("text").IsRequired();
        entity.Property(e => e.ConflictsJson).HasColumnName("conflicts_json").HasColumnType("text").IsRequired();
        entity.Property(e => e.OutcomeJson).HasColumnName("outcome_json").HasColumnType("text").IsRequired();
        entity.Property(e => e.Simulated).HasColumnName("simulated");
        entity.Property(e => e.CorrelationId).HasColumnName("correlation_id");
        entity.Property(e => e.ExecutedAtUtc).HasColumnName("executed_at_utc");
        entity.Property(e => e.ActorType).HasColumnName("actor_type").HasMaxLength(20).IsRequired();
        entity.Property(e => e.ActorId).HasColumnName("actor_id").HasMaxLength(120).IsRequired();
        entity.HasIndex(e => new { e.TenantId, e.FacilityId, e.ExecutedAtUtc })
            .HasDatabaseName("ix_ai_task_automation_executions_tenant_facility_time");
        entity.HasIndex(e => new { e.TenantId, e.TaskId, e.IdempotencyKey })
            .IsUnique()
            .HasDatabaseName("ux_ai_task_automation_executions_idempotency");
        entity.HasOne(e => e.Rule)
            .WithMany(r => r.Executions)
            .HasForeignKey(e => e.RuleId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
