using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace NevaxisZentra.Persistence.AiTasks;

internal sealed class AiTaskPlaybookEntityConfiguration : IEntityTypeConfiguration<AiTaskPlaybook>
{
    public void Configure(EntityTypeBuilder<AiTaskPlaybook> entity)
    {
        entity.ToTable("ai_task_playbooks");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasColumnName("id");
        entity.Property(e => e.TenantId).HasColumnName("tenant_id");
        entity.Property(e => e.FacilityId).HasColumnName("facility_id");
        entity.Property(e => e.PlaybookKey).HasColumnName("playbook_key").HasMaxLength(80).IsRequired();
        entity.Property(e => e.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
        entity.Property(e => e.Description).HasColumnName("description").HasMaxLength(2000);
        entity.Property(e => e.Category).HasColumnName("category").HasMaxLength(50).IsRequired();
        entity.Property(e => e.TriggerContext).HasColumnName("trigger_context").HasMaxLength(100);
        entity.Property(e => e.Enabled).HasColumnName("enabled");
        entity.Property(e => e.Version).HasColumnName("version");
        entity.Property(e => e.CurrentRevisionId).HasColumnName("current_revision_id");
        entity.Property(e => e.EffectiveFromUtc).HasColumnName("effective_from_utc");
        entity.Property(e => e.EffectiveUntilUtc).HasColumnName("effective_until_utc");
        entity.Property(e => e.CreatedByMembershipId).HasColumnName("created_by_membership_id");
        entity.Property(e => e.UpdatedByMembershipId).HasColumnName("updated_by_membership_id");
        entity.Property(e => e.CreatedBy).HasColumnName("created_by").HasMaxLength(120).IsRequired();
        entity.Property(e => e.UpdatedBy).HasColumnName("updated_by").HasMaxLength(120).IsRequired();
        entity.Property(e => e.CreatedAtUtc).HasColumnName("created_at_utc");
        entity.Property(e => e.UpdatedAtUtc).HasColumnName("updated_at_utc");
        entity.HasIndex(e => new { e.TenantId, e.FacilityId, e.PlaybookKey })
            .IsUnique()
            .HasDatabaseName("ux_ai_task_playbooks_tenant_facility_key");
    }
}

internal sealed class AiTaskPlaybookRevisionEntityConfiguration : IEntityTypeConfiguration<AiTaskPlaybookRevision>
{
    public void Configure(EntityTypeBuilder<AiTaskPlaybookRevision> entity)
    {
        entity.ToTable("ai_task_playbook_revisions");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasColumnName("id");
        entity.Property(e => e.TenantId).HasColumnName("tenant_id");
        entity.Property(e => e.PlaybookId).HasColumnName("playbook_id");
        entity.Property(e => e.Version).HasColumnName("version");
        entity.Property(e => e.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
        entity.Property(e => e.Description).HasColumnName("description").HasMaxLength(2000);
        entity.Property(e => e.Category).HasColumnName("category").HasMaxLength(50).IsRequired();
        entity.Property(e => e.TriggerContext).HasColumnName("trigger_context").HasMaxLength(100);
        entity.Property(e => e.DefinitionSnapshotJson).HasColumnName("definition_snapshot_json").HasColumnType("text").IsRequired();
        entity.Property(e => e.ChangedBy).HasColumnName("changed_by").HasMaxLength(120).IsRequired();
        entity.Property(e => e.ChangedAtUtc).HasColumnName("changed_at_utc");
        entity.Property(e => e.ChangeReason).HasColumnName("change_reason").HasMaxLength(500).IsRequired();
        entity.HasIndex(e => new { e.TenantId, e.PlaybookId, e.Version })
            .IsUnique()
            .HasDatabaseName("ux_ai_task_playbook_revisions_playbook_version");
        entity.HasOne(e => e.Playbook)
            .WithMany(p => p.Revisions)
            .HasForeignKey(e => e.PlaybookId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class AiTaskPlaybookStepEntityConfiguration : IEntityTypeConfiguration<AiTaskPlaybookStep>
{
    public void Configure(EntityTypeBuilder<AiTaskPlaybookStep> entity)
    {
        entity.ToTable("ai_task_playbook_steps");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasColumnName("id");
        entity.Property(e => e.TenantId).HasColumnName("tenant_id");
        entity.Property(e => e.PlaybookRevisionId).HasColumnName("playbook_revision_id");
        entity.Property(e => e.StepKey).HasColumnName("step_key").HasMaxLength(80).IsRequired();
        entity.Property(e => e.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
        entity.Property(e => e.Description).HasColumnName("description").HasMaxLength(2000);
        entity.Property(e => e.SortOrder).HasColumnName("sort_order");
        entity.Property(e => e.StepType).HasColumnName("step_type").HasMaxLength(40).IsRequired();
        entity.Property(e => e.Required).HasColumnName("required");
        entity.Property(e => e.DefaultPriority).HasColumnName("default_priority").HasMaxLength(20).IsRequired();
        entity.Property(e => e.AssignmentPolicyJson).HasColumnName("assignment_policy_json");
        entity.Property(e => e.SlaPolicyJson).HasColumnName("sla_policy_json");
        entity.Property(e => e.EvidencePolicyJson).HasColumnName("evidence_policy_json");
        entity.Property(e => e.ClosurePolicyJson).HasColumnName("closure_policy_json");
        entity.Property(e => e.ConditionJson).HasColumnName("condition_json");
        entity.Property(e => e.TargetDurationMinutes).HasColumnName("target_duration_minutes");
        entity.HasIndex(e => new { e.TenantId, e.PlaybookRevisionId, e.StepKey })
            .IsUnique()
            .HasDatabaseName("ux_ai_task_playbook_steps_revision_key");
        entity.HasOne(e => e.Revision)
            .WithMany(r => r.Steps)
            .HasForeignKey(e => e.PlaybookRevisionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class AiTaskPlaybookStepDependencyEntityConfiguration : IEntityTypeConfiguration<AiTaskPlaybookStepDependency>
{
    public void Configure(EntityTypeBuilder<AiTaskPlaybookStepDependency> entity)
    {
        entity.ToTable("ai_task_playbook_step_dependencies");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasColumnName("id");
        entity.Property(e => e.TenantId).HasColumnName("tenant_id");
        entity.Property(e => e.PlaybookRevisionId).HasColumnName("playbook_revision_id");
        entity.Property(e => e.StepId).HasColumnName("step_id");
        entity.Property(e => e.DependsOnStepId).HasColumnName("depends_on_step_id");
        entity.Property(e => e.DependencyType).HasColumnName("dependency_type").HasMaxLength(40).IsRequired();
        entity.HasIndex(e => new { e.TenantId, e.PlaybookRevisionId, e.StepId, e.DependsOnStepId })
            .IsUnique()
            .HasDatabaseName("ux_ai_task_playbook_step_dependencies_revision_edge");
        entity.HasOne(e => e.Revision)
            .WithMany(r => r.Dependencies)
            .HasForeignKey(e => e.PlaybookRevisionId)
            .OnDelete(DeleteBehavior.Cascade);
        entity.HasOne(e => e.Step)
            .WithMany()
            .HasForeignKey(e => e.StepId)
            .OnDelete(DeleteBehavior.Cascade);
        entity.HasOne(e => e.DependsOnStep)
            .WithMany()
            .HasForeignKey(e => e.DependsOnStepId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class AiTaskPlaybookRunEntityConfiguration : IEntityTypeConfiguration<AiTaskPlaybookRun>
{
    public void Configure(EntityTypeBuilder<AiTaskPlaybookRun> entity)
    {
        entity.ToTable("ai_task_playbook_runs");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasColumnName("id");
        entity.Property(e => e.TenantId).HasColumnName("tenant_id");
        entity.Property(e => e.FacilityId).HasColumnName("facility_id");
        entity.Property(e => e.PlaybookId).HasColumnName("playbook_id");
        entity.Property(e => e.PlaybookVersion).HasColumnName("playbook_version");
        entity.Property(e => e.PlaybookRevisionId).HasColumnName("playbook_revision_id");
        entity.Property(e => e.TriggerSource).HasColumnName("trigger_source").HasMaxLength(40).IsRequired();
        entity.Property(e => e.SourceTaskId).HasColumnName("source_task_id");
        entity.Property(e => e.SourceEntityJson).HasColumnName("source_entity_json");
        entity.Property(e => e.Status).HasColumnName("status").HasMaxLength(30).IsRequired();
        entity.Property(e => e.IdempotencyKey).HasColumnName("idempotency_key").HasMaxLength(200).IsRequired();
        entity.Property(e => e.CorrelationId).HasColumnName("correlation_id");
        entity.Property(e => e.ParentTaskId).HasColumnName("parent_task_id");
        entity.Property(e => e.RequiredStepsCompleted).HasColumnName("required_steps_completed");
        entity.Property(e => e.RequiredStepsTotal).HasColumnName("required_steps_total");
        entity.Property(e => e.OptionalStepsCompleted).HasColumnName("optional_steps_completed");
        entity.Property(e => e.OptionalStepsTotal).HasColumnName("optional_steps_total");
        entity.Property(e => e.BlockedStepsCount).HasColumnName("blocked_steps_count");
        entity.Property(e => e.ApprovalsPending).HasColumnName("approvals_pending");
        entity.Property(e => e.AtRisk).HasColumnName("at_risk");
        entity.Property(e => e.CriticalPathJson).HasColumnName("critical_path_json");
        entity.Property(e => e.BlockReason).HasColumnName("block_reason").HasMaxLength(2000);
        entity.Property(e => e.StartedBy).HasColumnName("started_by").HasMaxLength(120).IsRequired();
        entity.Property(e => e.StartedAtUtc).HasColumnName("started_at_utc");
        entity.Property(e => e.CompletedAtUtc).HasColumnName("completed_at_utc");
        entity.Property(e => e.CancelledAtUtc).HasColumnName("cancelled_at_utc");
        entity.Property(e => e.CancelledBy).HasColumnName("cancelled_by").HasMaxLength(120);
        entity.Property(e => e.CancellationReason).HasColumnName("cancellation_reason").HasMaxLength(2000);
        entity.HasIndex(e => new { e.TenantId, e.IdempotencyKey })
            .IsUnique()
            .HasDatabaseName("ux_ai_task_playbook_runs_tenant_idempotency");
        entity.HasOne(e => e.Playbook)
            .WithMany(p => p.Runs)
            .HasForeignKey(e => e.PlaybookId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class AiTaskPlaybookRunStepEntityConfiguration : IEntityTypeConfiguration<AiTaskPlaybookRunStep>
{
    public void Configure(EntityTypeBuilder<AiTaskPlaybookRunStep> entity)
    {
        entity.ToTable("ai_task_playbook_run_steps");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasColumnName("id");
        entity.Property(e => e.TenantId).HasColumnName("tenant_id");
        entity.Property(e => e.FacilityId).HasColumnName("facility_id");
        entity.Property(e => e.RunId).HasColumnName("run_id");
        entity.Property(e => e.TemplateStepId).HasColumnName("template_step_id");
        entity.Property(e => e.StepKey).HasColumnName("step_key").HasMaxLength(80).IsRequired();
        entity.Property(e => e.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
        entity.Property(e => e.Required).HasColumnName("required");
        entity.Property(e => e.StepType).HasColumnName("step_type").HasMaxLength(40).IsRequired();
        entity.Property(e => e.Status).HasColumnName("status").HasMaxLength(30).IsRequired();
        entity.Property(e => e.LinkedTaskId).HasColumnName("linked_task_id");
        entity.Property(e => e.DependencyState).HasColumnName("dependency_state").HasMaxLength(20).IsRequired();
        entity.Property(e => e.BlockReason).HasColumnName("block_reason").HasMaxLength(2000);
        entity.Property(e => e.BlockedBy).HasColumnName("blocked_by").HasMaxLength(120);
        entity.Property(e => e.BlockedAtUtc).HasColumnName("blocked_at_utc");
        entity.Property(e => e.ReadyAtUtc).HasColumnName("ready_at_utc");
        entity.Property(e => e.StartedAtUtc).HasColumnName("started_at_utc");
        entity.Property(e => e.CompletedAtUtc).HasColumnName("completed_at_utc");
        entity.Property(e => e.SortOrder).HasColumnName("sort_order");
        entity.HasIndex(e => new { e.TenantId, e.RunId, e.StepKey })
            .IsUnique()
            .HasDatabaseName("ux_ai_task_playbook_run_steps_run_key");
        entity.HasOne(e => e.Run)
            .WithMany(r => r.Steps)
            .HasForeignKey(e => e.RunId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
