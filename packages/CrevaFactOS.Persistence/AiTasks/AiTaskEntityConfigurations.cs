using CrevaFactOS.Persistence.Facilities;
using CrevaFactOS.Persistence.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CrevaFactOS.Persistence.AiTasks;

internal sealed class AiTaskEntityConfiguration : IEntityTypeConfiguration<AiTask>
{
    public void Configure(EntityTypeBuilder<AiTask> entity)
    {
        entity.ToTable("ai_tasks");
        entity.HasKey(t => t.Id);
        entity.Property(t => t.Id).HasColumnName("id");
        entity.Property(t => t.TenantId).HasColumnName("tenant_id");
        entity.Property(t => t.FacilityId).HasColumnName("facility_id");
        entity.Property(t => t.TaskReference).HasColumnName("task_reference").HasMaxLength(100).IsRequired();
        entity.Property(t => t.Title).HasColumnName("title").HasMaxLength(300).IsRequired();
        entity.Property(t => t.Summary).HasColumnName("summary").HasMaxLength(2000).IsRequired();
        entity.Property(t => t.Category).HasColumnName("category").HasMaxLength(50).IsRequired();
        entity.Property(t => t.Priority).HasColumnName("priority").HasMaxLength(20).IsRequired();
        entity.Property(t => t.RiskLevel).HasColumnName("risk_level").HasMaxLength(20).IsRequired();
        entity.Property(t => t.Status).HasColumnName("status").HasMaxLength(30).IsRequired();
        entity.Property(t => t.DetectedIssue).HasColumnName("detected_issue").HasMaxLength(4000).IsRequired();
        entity.Property(t => t.Reasoning).HasColumnName("reasoning").HasMaxLength(8000).IsRequired();
        entity.Property(t => t.RecommendationJson).HasColumnName("recommendation_json").IsRequired();
        entity.Property(t => t.ConfidenceJson).HasColumnName("confidence_json").IsRequired();
        entity.Property(t => t.ImpactJson).HasColumnName("impact_json").IsRequired();
        entity.Property(t => t.SourcesJson).HasColumnName("sources_json").IsRequired();
        entity.Property(t => t.AssigneeJson).HasColumnName("assignee_json");
        entity.Property(t => t.AssignedMembershipId).HasColumnName("assigned_membership_id");
        entity.Property(t => t.AssignedTeamChannelId).HasColumnName("assigned_team_channel_id");
        entity.Property(t => t.AcknowledgedAtUtc).HasColumnName("acknowledged_at_utc");
        entity.Property(t => t.StartedAtUtc).HasColumnName("started_at_utc");
        entity.Property(t => t.ResolvedAtUtc).HasColumnName("resolved_at_utc");
        entity.Property(t => t.EscalationLevel).HasColumnName("escalation_level");
        entity.Property(t => t.EscalationState).HasColumnName("escalation_state").HasMaxLength(20).IsRequired();
        entity.Property(t => t.SlaPolicyId).HasColumnName("sla_policy_id");
        entity.Property(t => t.LastEscalatedAtUtc).HasColumnName("last_escalated_at_utc");
        entity.Property(t => t.AssignmentReason).HasColumnName("assignment_reason").HasMaxLength(500);
        entity.Property(t => t.AssignmentSource).HasColumnName("assignment_source").HasMaxLength(40);
        entity.Property(t => t.RoutingRuleId).HasColumnName("routing_rule_id");
        entity.Property(t => t.RoutingMetadataJson).HasColumnName("routing_metadata_json");
        entity.Property(t => t.ApprovalState).HasColumnName("approval_state").HasMaxLength(20).IsRequired();
        entity.Property(t => t.ApprovalRequestedAtUtc).HasColumnName("approval_requested_at_utc");
        entity.Property(t => t.ApprovalRequestedBy).HasColumnName("approval_requested_by").HasMaxLength(200);
        entity.Property(t => t.ApprovalDecidedAtUtc).HasColumnName("approval_decided_at_utc");
        entity.Property(t => t.ApprovalDecidedBy).HasColumnName("approval_decided_by").HasMaxLength(200);
        entity.Property(t => t.ApprovalDecisionNote).HasColumnName("approval_decision_note").HasMaxLength(2000);
        entity.Property(t => t.ExecutionJson).HasColumnName("execution_json");
        entity.Property(t => t.SiteId).HasColumnName("site_id").HasMaxLength(100);
        entity.Property(t => t.ProductionLineId).HasColumnName("production_line_id").HasMaxLength(100);
        entity.Property(t => t.MachineId).HasColumnName("machine_id").HasMaxLength(100);
        entity.Property(t => t.MoldId).HasColumnName("mold_id").HasMaxLength(100);
        entity.Property(t => t.ProductionOrderId).HasColumnName("production_order_id").HasMaxLength(100);
        entity.Property(t => t.SalesOrderId).HasColumnName("sales_order_id").HasMaxLength(100);
        entity.Property(t => t.MaterialId).HasColumnName("material_id").HasMaxLength(100);
        entity.Property(t => t.SupplierId).HasColumnName("supplier_id").HasMaxLength(100);
        entity.Property(t => t.CustomerId).HasColumnName("customer_id").HasMaxLength(100);
        entity.Property(t => t.SourceModule).HasColumnName("source_module").HasMaxLength(100);
        entity.Property(t => t.SourceEntityId).HasColumnName("source_entity_id").HasMaxLength(200);
        entity.Property(t => t.ParentTaskId).HasColumnName("parent_task_id");
        entity.Property(t => t.PlaybookRunId).HasColumnName("playbook_run_id");
        entity.Property(t => t.PlaybookRunStepId).HasColumnName("playbook_run_step_id");
        entity.Property(t => t.DependencyState).HasColumnName("dependency_state").HasMaxLength(20).IsRequired();
        entity.Property(t => t.DependencyBlockReason).HasColumnName("dependency_block_reason").HasMaxLength(500);
        entity.Property(t => t.RuleSetVersion).HasColumnName("rule_set_version").HasMaxLength(100).IsRequired();
        entity.Property(t => t.IdempotencyKey).HasColumnName("idempotency_key").HasMaxLength(128);
        entity.Property(t => t.IdempotencyPayloadFingerprint).HasColumnName("idempotency_payload_fingerprint").HasMaxLength(64);
        entity.Property(t => t.DueAtUtc).HasColumnName("due_at_utc");
        entity.Property(t => t.CompletedAtUtc).HasColumnName("completed_at_utc");
        entity.Property(t => t.ClosureState).HasColumnName("closure_state").HasMaxLength(40).IsRequired();
        entity.Property(t => t.ClosurePolicyId).HasColumnName("closure_policy_id");
        entity.Property(t => t.MinimumEvidenceRequired).HasColumnName("minimum_evidence_required");
        entity.Property(t => t.FourEyesRequired).HasColumnName("four_eyes_required");
        entity.Property(t => t.AutomationRuleId).HasColumnName("automation_rule_id");
        entity.Property(t => t.AutomationRuleVersion).HasColumnName("automation_rule_version");
        entity.Property(t => t.AutomationOverrideJson).HasColumnName("automation_override_json");
        entity.Property(t => t.AutomationMetadataJson).HasColumnName("automation_metadata_json");
        entity.Property(t => t.AutomationTagsJson).HasColumnName("automation_tags_json");
        entity.Property(t => t.ResolutionSummary).HasColumnName("resolution_summary").HasMaxLength(4000);
        entity.Property(t => t.ResolutionCode).HasColumnName("resolution_code").HasMaxLength(50);
        entity.Property(t => t.ClosureReason).HasColumnName("closure_reason").HasMaxLength(2000);
        entity.Property(t => t.ClosureApprovalRequired).HasColumnName("closure_approval_required");
        entity.Property(t => t.ClosureRequestedAtUtc).HasColumnName("closure_requested_at_utc");
        entity.Property(t => t.ClosureRequestedByMembershipId).HasColumnName("closure_requested_by_membership_id");
        entity.Property(t => t.ClosureApprovedAtUtc).HasColumnName("closure_approved_at_utc");
        entity.Property(t => t.ClosureApprovedByMembershipId).HasColumnName("closure_approved_by_membership_id");
        entity.Property(t => t.ClosureRejectedAtUtc).HasColumnName("closure_rejected_at_utc");
        entity.Property(t => t.ClosureRejectedByMembershipId).HasColumnName("closure_rejected_by_membership_id");
        entity.Property(t => t.ClosureRejectionReason).HasColumnName("closure_rejection_reason").HasMaxLength(2000);
        entity.Property(t => t.ReopenedAtUtc).HasColumnName("reopened_at_utc");
        entity.Property(t => t.ReopenedByMembershipId).HasColumnName("reopened_by_membership_id");
        entity.Property(t => t.ReopenReason).HasColumnName("reopen_reason").HasMaxLength(2000);
        entity.Property(t => t.FinalClosedAtUtc).HasColumnName("final_closed_at_utc");
        entity.Property(t => t.Version).HasColumnName("version").IsConcurrencyToken();
        entity.Property(t => t.CreatedAtUtc).HasColumnName("created_at_utc");
        entity.Property(t => t.UpdatedAtUtc).HasColumnName("updated_at_utc");
        entity.Property(t => t.CreatedBy).HasColumnName("created_by").HasMaxLength(200).IsRequired();
        entity.Property(t => t.UpdatedBy).HasColumnName("updated_by").HasMaxLength(200).IsRequired();
        entity.HasOne<Tenant>().WithMany().HasForeignKey(t => t.TenantId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Facility>().WithMany()
            .HasForeignKey(t => new { t.TenantId, t.FacilityId })
            .HasPrincipalKey(f => new { f.TenantId, f.Id })
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasIndex(t => new { t.TenantId, t.TaskReference })
            .HasDatabaseName("ux_ai_tasks_tenant_reference")
            .IsUnique();
        entity.HasIndex(t => new { t.TenantId, t.IdempotencyKey })
            .HasDatabaseName("ux_ai_tasks_tenant_idempotency")
            .IsUnique()
            .HasFilter("idempotency_key IS NOT NULL");
        entity.HasIndex(t => new { t.TenantId, t.FacilityId })
            .HasDatabaseName("ix_ai_tasks_tenant_facility");
        entity.HasIndex(t => new { t.TenantId, t.FacilityId, t.Status })
            .HasDatabaseName("ix_ai_tasks_tenant_facility_status");
        entity.HasIndex(t => new { t.TenantId, t.FacilityId, t.Category })
            .HasDatabaseName("ix_ai_tasks_tenant_facility_category");
        entity.HasIndex(t => new { t.TenantId, t.FacilityId, t.Priority })
            .HasDatabaseName("ix_ai_tasks_tenant_facility_priority");
        entity.HasIndex(t => new { t.TenantId, t.FacilityId, t.SourceModule, t.SourceEntityId })
            .HasDatabaseName("ix_ai_tasks_tenant_facility_source");
        entity.HasIndex(t => new { t.TenantId, t.FacilityId, t.CreatedAtUtc })
            .HasDatabaseName("ix_ai_tasks_tenant_facility_created");
        entity.HasIndex(t => new { t.TenantId, t.FacilityId, t.AssignedMembershipId })
            .HasDatabaseName("ix_ai_tasks_tenant_facility_assignee");
        entity.HasIndex(t => new { t.TenantId, t.FacilityId, t.AssignedTeamChannelId })
            .HasDatabaseName("ix_ai_tasks_tenant_facility_team");
        entity.HasIndex(t => new { t.TenantId, t.FacilityId, t.DueAtUtc })
            .HasDatabaseName("ix_ai_tasks_tenant_facility_due");
        entity.HasIndex(t => new { t.TenantId, t.FacilityId, t.EscalationLevel })
            .HasDatabaseName("ix_ai_tasks_tenant_facility_escalation");
        entity.HasIndex(t => new { t.TenantId, t.FacilityId, t.ClosureState })
            .HasDatabaseName("ix_ai_tasks_tenant_facility_closure_state");
    }
}

internal sealed class AiTaskAuditEventEntityConfiguration : IEntityTypeConfiguration<AiTaskAuditEvent>
{
    public void Configure(EntityTypeBuilder<AiTaskAuditEvent> entity)
    {
        entity.ToTable("ai_task_audit_events");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasColumnName("id");
        entity.Property(e => e.TenantId).HasColumnName("tenant_id");
        entity.Property(e => e.FacilityId).HasColumnName("facility_id");
        entity.Property(e => e.TaskId).HasColumnName("task_id");
        entity.Property(e => e.EventType).HasColumnName("event_type").HasMaxLength(50).IsRequired();
        entity.Property(e => e.OccurredAtUtc).HasColumnName("occurred_at_utc");
        entity.Property(e => e.ActorType).HasColumnName("actor_type").HasMaxLength(20).IsRequired();
        entity.Property(e => e.ActorId).HasColumnName("actor_id").HasMaxLength(200);
        entity.Property(e => e.ActorLabel).HasColumnName("actor_label").HasMaxLength(200).IsRequired();
        entity.Property(e => e.Note).HasColumnName("note").HasMaxLength(2000);
        entity.Property(e => e.PreviousStatus).HasColumnName("previous_status").HasMaxLength(30);
        entity.Property(e => e.NextStatus).HasColumnName("next_status").HasMaxLength(30);
        entity.Property(e => e.CorrelationId).HasColumnName("correlation_id");
        entity.Property(e => e.MetadataJson).HasColumnName("metadata_json");
        entity.HasOne<Tenant>().WithMany().HasForeignKey(e => e.TenantId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Facility>().WithMany()
            .HasForeignKey(e => new { e.TenantId, e.FacilityId })
            .HasPrincipalKey(f => new { f.TenantId, f.Id })
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(e => e.Task).WithMany(t => t.AuditEvents)
            .HasForeignKey(e => e.TaskId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasIndex(e => new { e.TenantId, e.FacilityId, e.TaskId, e.OccurredAtUtc })
            .HasDatabaseName("ix_ai_task_audit_events_tenant_facility_task");
    }
}
