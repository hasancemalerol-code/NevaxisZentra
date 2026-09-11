using CrevaFactOS.Persistence.Facilities;
using CrevaFactOS.Persistence.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CrevaFactOS.Persistence.DigitalWorkforce;

internal sealed class DigitalWorkerEntityConfiguration : IEntityTypeConfiguration<DigitalWorker>
{
    public void Configure(EntityTypeBuilder<DigitalWorker> entity)
    {
        entity.ToTable("digital_workers");
        entity.HasKey(w => w.Id);
        entity.Property(w => w.Id).HasColumnName("id");
        entity.Property(w => w.TenantId).HasColumnName("tenant_id");
        entity.Property(w => w.FacilityId).HasColumnName("facility_id");
        entity.Property(w => w.Code).HasColumnName("code").HasMaxLength(30).IsRequired();
        entity.Property(w => w.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
        entity.Property(w => w.Description).HasColumnName("description").HasMaxLength(1000);
        entity.Property(w => w.Mission).HasColumnName("mission").HasMaxLength(500);
        entity.Property(w => w.Status).HasColumnName("status").HasMaxLength(30).IsRequired();
        entity.Property(w => w.AutonomyMode).HasColumnName("autonomy_mode").HasMaxLength(20).IsRequired();
        entity.Property(w => w.Enabled).HasColumnName("enabled");
        entity.Property(w => w.IsPaused).HasColumnName("is_paused");
        entity.Property(w => w.IsKilled).HasColumnName("is_killed");
        entity.Property(w => w.IsHumanControlled).HasColumnName("is_human_controlled");
        entity.Property(w => w.GlobalPauseActive).HasColumnName("global_pause_active");
        entity.Property(w => w.SourceClassification).HasColumnName("source_classification").HasMaxLength(20).IsRequired();
        entity.Property(w => w.DefaultPolicyProfileJson).HasColumnName("default_policy_profile_json");
        entity.Property(w => w.Version).HasColumnName("version");
        entity.Property(w => w.CreatedAtUtc).HasColumnName("created_at_utc");
        entity.Property(w => w.UpdatedAtUtc).HasColumnName("updated_at_utc");
        entity.Property(w => w.ConcurrencyToken).HasColumnName("concurrency_token").IsConcurrencyToken();
        entity.HasOne<Tenant>().WithMany().HasForeignKey(w => w.TenantId).OnDelete(DeleteBehavior.Restrict);
        entity.HasIndex(w => new { w.TenantId, w.Code }).IsUnique().HasDatabaseName("ux_digital_workers_tenant_code");
    }
}

internal sealed class DigitalWorkerPolicyEntityConfiguration : IEntityTypeConfiguration<DigitalWorkerPolicy>
{
    public void Configure(EntityTypeBuilder<DigitalWorkerPolicy> entity)
    {
        entity.ToTable("digital_worker_policies");
        entity.HasKey(p => p.Id);
        entity.Property(p => p.Id).HasColumnName("id");
        entity.Property(p => p.TenantId).HasColumnName("tenant_id");
        entity.Property(p => p.WorkerId).HasColumnName("worker_id");
        entity.Property(p => p.AllowedToolsJson).HasColumnName("allowed_tools_json");
        entity.Property(p => p.AllowedConnectorsJson).HasColumnName("allowed_connectors_json");
        entity.Property(p => p.DataScopeJson).HasColumnName("data_scope_json");
        entity.Property(p => p.TenantScopeJson).HasColumnName("tenant_scope_json");
        entity.Property(p => p.FacilityScopeJson).HasColumnName("facility_scope_json");
        entity.Property(p => p.LineScopeJson).HasColumnName("line_scope_json");
        entity.Property(p => p.MachineScopeJson).HasColumnName("machine_scope_json");
        entity.Property(p => p.AllowedActionsJson).HasColumnName("allowed_actions_json");
        entity.Property(p => p.ProhibitedActionsJson).HasColumnName("prohibited_actions_json");
        entity.Property(p => p.ApprovalRequiredForJson).HasColumnName("approval_required_for_json");
        entity.Property(p => p.MaxTransactionValue).HasColumnName("max_transaction_value");
        entity.Property(p => p.MaxRiskLevel).HasColumnName("max_risk_level");
        entity.Property(p => p.ExternalCommunicationPolicy).HasColumnName("external_communication_policy").HasMaxLength(30);
        entity.Property(p => p.WorkingHoursJson).HasColumnName("working_hours_json");
        entity.Property(p => p.RetryLimit).HasColumnName("retry_limit");
        entity.Property(p => p.EscalationPolicyJson).HasColumnName("escalation_policy_json");
        entity.Property(p => p.AiBudgetDaily).HasColumnName("ai_budget_daily");
        entity.Property(p => p.AiBudgetMonthly).HasColumnName("ai_budget_monthly");
        entity.Property(p => p.AiBudgetWarningThreshold).HasColumnName("ai_budget_warning_threshold");
        entity.Property(p => p.KillSwitch).HasColumnName("kill_switch");
        entity.Property(p => p.UpdatedAtUtc).HasColumnName("updated_at_utc");
        entity.Property(p => p.ConcurrencyToken).HasColumnName("concurrency_token").IsConcurrencyToken();
        entity.HasOne(p => p.Worker).WithOne(w => w.Policy).HasForeignKey<DigitalWorkerPolicy>(p => p.WorkerId);
        entity.HasIndex(p => p.WorkerId).IsUnique().HasDatabaseName("ux_digital_worker_policies_worker");
    }
}

internal sealed class DigitalWorkCaseEntityConfiguration : IEntityTypeConfiguration<DigitalWorkCase>
{
    public void Configure(EntityTypeBuilder<DigitalWorkCase> entity)
    {
        entity.ToTable("digital_work_cases");
        entity.HasKey(c => c.Id);
        entity.Property(c => c.Id).HasColumnName("id");
        entity.Property(c => c.TenantId).HasColumnName("tenant_id");
        entity.Property(c => c.FacilityId).HasColumnName("facility_id");
        entity.Property(c => c.Title).HasColumnName("title").HasMaxLength(300).IsRequired();
        entity.Property(c => c.Description).HasColumnName("description").HasMaxLength(2000);
        entity.Property(c => c.Source).HasColumnName("source").HasMaxLength(100);
        entity.Property(c => c.SourceClassification).HasColumnName("source_classification").HasMaxLength(20);
        entity.Property(c => c.Severity).HasColumnName("severity").HasMaxLength(20);
        entity.Property(c => c.Status).HasColumnName("status").HasMaxLength(30);
        entity.Property(c => c.CurrentStage).HasColumnName("current_stage").HasMaxLength(30);
        entity.Property(c => c.PrimaryWorkerId).HasColumnName("primary_worker_id");
        entity.Property(c => c.AutonomyModeAtStart).HasColumnName("autonomy_mode_at_start").HasMaxLength(20);
        entity.Property(c => c.RequiresApproval).HasColumnName("requires_approval");
        entity.Property(c => c.IsHumanControlled).HasColumnName("is_human_controlled");
        entity.Property(c => c.LinkedAiTaskId).HasColumnName("linked_ai_task_id");
        entity.Property(c => c.StartedAtUtc).HasColumnName("started_at_utc");
        entity.Property(c => c.TargetAtUtc).HasColumnName("target_at_utc");
        entity.Property(c => c.ClosedAtUtc).HasColumnName("closed_at_utc");
        entity.Property(c => c.VerifiedAtUtc).HasColumnName("verified_at_utc");
        entity.Property(c => c.Outcome).HasColumnName("outcome").HasMaxLength(500);
        entity.Property(c => c.BusinessImpact).HasColumnName("business_impact").HasMaxLength(500);
        entity.Property(c => c.EstimatedTimeSavedMinutes).HasColumnName("estimated_time_saved_minutes");
        entity.Property(c => c.AiCost).HasColumnName("ai_cost");
        entity.Property(c => c.VerificationState).HasColumnName("verification_state").HasMaxLength(20);
        entity.Property(c => c.ClosureContractKey).HasColumnName("closure_contract_key").HasMaxLength(100);
        entity.Property(c => c.MetadataJson).HasColumnName("metadata_json");
        entity.Property(c => c.OrchestrationRootCaseId).HasColumnName("orchestration_root_case_id");
        entity.Property(c => c.ParentCaseId).HasColumnName("parent_case_id");
        entity.Property(c => c.CorrelationKey).HasColumnName("correlation_key").HasMaxLength(200);
        entity.Property(c => c.PrimaryDomain).HasColumnName("primary_domain").HasMaxLength(30);
        entity.Property(c => c.ParticipatingDomainsJson).HasColumnName("participating_domains_json");
        entity.Property(c => c.CurrentBlockerCode).HasColumnName("current_blocker_code").HasMaxLength(40);
        entity.Property(c => c.RecoveryPlanJson).HasColumnName("recovery_plan_json");
        entity.Property(c => c.ImpactGraphJson).HasColumnName("impact_graph_json");
        entity.Property(c => c.DependencyGraphJson).HasColumnName("dependency_graph_json");
        entity.Property(c => c.OutcomeVerificationJson).HasColumnName("outcome_verification_json");
        entity.Property(c => c.AdministrativelyClosed).HasColumnName("administratively_closed");
        entity.Property(c => c.ReopenCount).HasColumnName("reopen_count");
        entity.Property(c => c.BlockerSinceUtc).HasColumnName("blocker_since_utc");
        entity.Property(c => c.CreatedAtUtc).HasColumnName("created_at_utc");
        entity.HasIndex(c => new { c.TenantId, c.CorrelationKey }).HasDatabaseName("ix_digital_work_cases_tenant_correlation");
        entity.Property(c => c.UpdatedAtUtc).HasColumnName("updated_at_utc");
        entity.Property(c => c.ConcurrencyToken).HasColumnName("concurrency_token").IsConcurrencyToken();
        entity.HasOne(c => c.PrimaryWorker).WithMany().HasForeignKey(c => c.PrimaryWorkerId);
        entity.HasIndex(c => new { c.TenantId, c.Status }).HasDatabaseName("ix_digital_work_cases_tenant_status");
        entity.HasIndex(c => new { c.TenantId, c.PrimaryWorkerId }).HasDatabaseName("ix_digital_work_cases_tenant_worker");
    }
}

internal sealed class DigitalWorkCaseParticipantEntityConfiguration : IEntityTypeConfiguration<DigitalWorkCaseParticipant>
{
    public void Configure(EntityTypeBuilder<DigitalWorkCaseParticipant> entity)
    {
        entity.ToTable("digital_work_case_participants");
        entity.HasKey(p => p.Id);
        entity.Property(p => p.Id).HasColumnName("id");
        entity.Property(p => p.TenantId).HasColumnName("tenant_id");
        entity.Property(p => p.CaseId).HasColumnName("case_id");
        entity.Property(p => p.WorkerId).HasColumnName("worker_id");
        entity.Property(p => p.Role).HasColumnName("role").HasMaxLength(20);
        entity.Property(p => p.JoinedAtUtc).HasColumnName("joined_at_utc");
        entity.Property(p => p.LeftAtUtc).HasColumnName("left_at_utc");
        entity.Property(p => p.HandoffReason).HasColumnName("handoff_reason").HasMaxLength(500);
        entity.HasOne(p => p.Case).WithMany(c => c.Participants).HasForeignKey(p => p.CaseId);
        entity.HasOne(p => p.Worker).WithMany().HasForeignKey(p => p.WorkerId);
    }
}

internal sealed class DigitalWorkActivityEntityConfiguration : IEntityTypeConfiguration<DigitalWorkActivity>
{
    public void Configure(EntityTypeBuilder<DigitalWorkActivity> entity)
    {
        entity.ToTable("digital_work_activities");
        entity.HasKey(a => a.Id);
        entity.Property(a => a.Id).HasColumnName("id");
        entity.Property(a => a.TenantId).HasColumnName("tenant_id");
        entity.Property(a => a.CaseId).HasColumnName("case_id");
        entity.Property(a => a.WorkerId).HasColumnName("worker_id");
        entity.Property(a => a.ActionType).HasColumnName("action_type").HasMaxLength(50);
        entity.Property(a => a.Description).HasColumnName("description").HasMaxLength(1000);
        entity.Property(a => a.Reason).HasColumnName("reason").HasMaxLength(500);
        entity.Property(a => a.Result).HasColumnName("result").HasMaxLength(500);
        entity.Property(a => a.EvidenceReference).HasColumnName("evidence_reference").HasMaxLength(500);
        entity.Property(a => a.SourceClassification).HasColumnName("source_classification").HasMaxLength(20);
        entity.Property(a => a.CorrelationId).HasColumnName("correlation_id").HasMaxLength(100);
        entity.Property(a => a.CreatedAtUtc).HasColumnName("created_at_utc");
        entity.HasOne(a => a.Case).WithMany(c => c.Activities).HasForeignKey(a => a.CaseId);
        entity.HasIndex(a => new { a.TenantId, a.WorkerId, a.CreatedAtUtc }).HasDatabaseName("ix_digital_work_activities_worker");
    }
}

internal sealed class DigitalWorkVerificationEntityConfiguration : IEntityTypeConfiguration<DigitalWorkVerification>
{
    public void Configure(EntityTypeBuilder<DigitalWorkVerification> entity)
    {
        entity.ToTable("digital_work_verifications");
        entity.HasKey(v => v.Id);
        entity.Property(v => v.Id).HasColumnName("id");
        entity.Property(v => v.TenantId).HasColumnName("tenant_id");
        entity.Property(v => v.CaseId).HasColumnName("case_id");
        entity.Property(v => v.VerificationType).HasColumnName("verification_type").HasMaxLength(100);
        entity.Property(v => v.Source).HasColumnName("source").HasMaxLength(100);
        entity.Property(v => v.EvidenceReference).HasColumnName("evidence_reference").HasMaxLength(500);
        entity.Property(v => v.ExpectedState).HasColumnName("expected_state").HasMaxLength(500);
        entity.Property(v => v.ObservedState).HasColumnName("observed_state").HasMaxLength(500);
        entity.Property(v => v.VerifiedBy).HasColumnName("verified_by").HasMaxLength(200);
        entity.Property(v => v.VerifiedAtUtc).HasColumnName("verified_at_utc");
        entity.Property(v => v.Confidence).HasColumnName("confidence");
        entity.Property(v => v.Result).HasColumnName("result").HasMaxLength(20);
        entity.Property(v => v.SourceClassification).HasColumnName("source_classification").HasMaxLength(20);
        entity.HasOne(v => v.Case).WithMany(c => c.Verifications).HasForeignKey(v => v.CaseId);
    }
}

internal sealed class DigitalWorkApprovalEntityConfiguration : IEntityTypeConfiguration<DigitalWorkApproval>
{
    public void Configure(EntityTypeBuilder<DigitalWorkApproval> entity)
    {
        entity.ToTable("digital_work_approvals");
        entity.HasKey(a => a.Id);
        entity.Property(a => a.Id).HasColumnName("id");
        entity.Property(a => a.TenantId).HasColumnName("tenant_id");
        entity.Property(a => a.CaseId).HasColumnName("case_id");
        entity.Property(a => a.WorkerId).HasColumnName("worker_id");
        entity.Property(a => a.Action).HasColumnName("action").HasMaxLength(100);
        entity.Property(a => a.BusinessReason).HasColumnName("business_reason").HasMaxLength(500);
        entity.Property(a => a.Impact).HasColumnName("impact").HasMaxLength(500);
        entity.Property(a => a.RiskLevel).HasColumnName("risk_level");
        entity.Property(a => a.Amount).HasColumnName("amount");
        entity.Property(a => a.EvidenceJson).HasColumnName("evidence_json");
        entity.Property(a => a.Status).HasColumnName("status").HasMaxLength(20);
        entity.Property(a => a.Decision).HasColumnName("decision").HasMaxLength(20);
        entity.Property(a => a.DecidedBySubjectId).HasColumnName("decided_by_subject_id").HasMaxLength(200);
        entity.Property(a => a.DecidedAtUtc).HasColumnName("decided_at_utc");
        entity.Property(a => a.CreatedAtUtc).HasColumnName("created_at_utc");
        entity.Property(a => a.ExpiresAtUtc).HasColumnName("expires_at_utc");
        entity.Property(a => a.ConcurrencyToken).HasColumnName("concurrency_token").IsConcurrencyToken();
        entity.HasOne(a => a.Case).WithMany(c => c.Approvals).HasForeignKey(a => a.CaseId);
        entity.HasIndex(a => new { a.TenantId, a.Status }).HasDatabaseName("ix_digital_work_approvals_tenant_status");
    }
}

internal sealed class DigitalWorkHandoffEntityConfiguration : IEntityTypeConfiguration<DigitalWorkHandoff>
{
    public void Configure(EntityTypeBuilder<DigitalWorkHandoff> entity)
    {
        entity.ToTable("digital_work_handoffs");
        entity.HasKey(h => h.Id);
        entity.Property(h => h.Id).HasColumnName("id");
        entity.Property(h => h.TenantId).HasColumnName("tenant_id");
        entity.Property(h => h.CaseId).HasColumnName("case_id");
        entity.Property(h => h.FromWorkerId).HasColumnName("from_worker_id");
        entity.Property(h => h.ToWorkerId).HasColumnName("to_worker_id");
        entity.Property(h => h.Reason).HasColumnName("reason").HasMaxLength(500);
        entity.Property(h => h.Dependency).HasColumnName("dependency").HasMaxLength(500);
        entity.Property(h => h.Status).HasColumnName("status").HasMaxLength(20);
        entity.Property(h => h.CreatedAtUtc).HasColumnName("created_at_utc");
        entity.Property(h => h.CompletedAtUtc).HasColumnName("completed_at_utc");
        entity.HasOne(h => h.Case).WithMany(c => c.Handoffs).HasForeignKey(h => h.CaseId);
    }
}

internal sealed class DigitalWorkerUsageEntityConfiguration : IEntityTypeConfiguration<DigitalWorkerUsage>
{
    public void Configure(EntityTypeBuilder<DigitalWorkerUsage> entity)
    {
        entity.ToTable("digital_worker_usages");
        entity.HasKey(u => u.Id);
        entity.Property(u => u.Id).HasColumnName("id");
        entity.Property(u => u.TenantId).HasColumnName("tenant_id");
        entity.Property(u => u.WorkerId).HasColumnName("worker_id");
        entity.Property(u => u.CaseId).HasColumnName("case_id");
        entity.Property(u => u.ModelTier).HasColumnName("model_tier").HasMaxLength(20);
        entity.Property(u => u.ModelName).HasColumnName("model_name").HasMaxLength(100);
        entity.Property(u => u.InputTokens).HasColumnName("input_tokens");
        entity.Property(u => u.OutputTokens).HasColumnName("output_tokens");
        entity.Property(u => u.EstimatedCost).HasColumnName("estimated_cost");
        entity.Property(u => u.CreatedAtUtc).HasColumnName("created_at_utc");
        entity.HasIndex(u => new { u.TenantId, u.WorkerId, u.CreatedAtUtc }).HasDatabaseName("ix_digital_worker_usages_worker");
    }
}

internal sealed class DigitalWorkerBudgetEntityConfiguration : IEntityTypeConfiguration<DigitalWorkerBudget>
{
    public void Configure(EntityTypeBuilder<DigitalWorkerBudget> entity)
    {
        entity.ToTable("digital_worker_budgets");
        entity.HasKey(b => b.Id);
        entity.Property(b => b.Id).HasColumnName("id");
        entity.Property(b => b.TenantId).HasColumnName("tenant_id");
        entity.Property(b => b.WorkerId).HasColumnName("worker_id");
        entity.Property(b => b.DailyLimit).HasColumnName("daily_limit");
        entity.Property(b => b.MonthlyLimit).HasColumnName("monthly_limit");
        entity.Property(b => b.WarningThreshold).HasColumnName("warning_threshold");
        entity.Property(b => b.DailyConsumed).HasColumnName("daily_consumed");
        entity.Property(b => b.MonthlyConsumed).HasColumnName("monthly_consumed");
        entity.Property(b => b.State).HasColumnName("state").HasMaxLength(20);
        entity.Property(b => b.UpdatedAtUtc).HasColumnName("updated_at_utc");
        entity.HasOne(b => b.Worker).WithOne(w => w.Budget).HasForeignKey<DigitalWorkerBudget>(b => b.WorkerId);
        entity.HasIndex(b => b.WorkerId).IsUnique().HasDatabaseName("ux_digital_worker_budgets_worker");
    }
}

internal sealed class DigitalWorkforceRuntimeStateEntityConfiguration : IEntityTypeConfiguration<DigitalWorkforceRuntimeState>
{
    public void Configure(EntityTypeBuilder<DigitalWorkforceRuntimeState> entity)
    {
        entity.ToTable("digital_workforce_runtime_states");
        entity.HasKey(s => s.Id);
        entity.Property(s => s.Id).HasColumnName("id");
        entity.Property(s => s.TenantId).HasColumnName("tenant_id");
        entity.Property(s => s.GlobalPauseActive).HasColumnName("global_pause_active");
        entity.Property(s => s.GlobalKillActive).HasColumnName("global_kill_active");
        entity.Property(s => s.UpdatedAtUtc).HasColumnName("updated_at_utc");
        entity.Property(s => s.ConcurrencyToken).HasColumnName("concurrency_token").IsConcurrencyToken();
        entity.HasIndex(s => s.TenantId).IsUnique().HasDatabaseName("ux_digital_workforce_runtime_states_tenant");
    }
}
