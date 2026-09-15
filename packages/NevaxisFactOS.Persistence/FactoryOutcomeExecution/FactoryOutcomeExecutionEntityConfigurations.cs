using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace NevaxisFactOS.Persistence.FactoryOutcomeExecution;

internal sealed class FactoryOutcomeExecutionActionEntityConfiguration : IEntityTypeConfiguration<FactoryOutcomeExecutionAction>
{
    public void Configure(EntityTypeBuilder<FactoryOutcomeExecutionAction> entity)
    {
        entity.ToTable("factory_outcome_execution_actions");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasColumnName("id");
        entity.Property(e => e.TenantId).HasColumnName("tenant_id");
        entity.Property(e => e.FacilityId).HasColumnName("facility_id");
        entity.Property(e => e.RootCaseId).HasColumnName("root_case_id");
        entity.Property(e => e.ChildCaseId).HasColumnName("child_case_id");
        entity.Property(e => e.ActionKey).HasColumnName("action_key").HasMaxLength(120);
        entity.Property(e => e.WorkerCode).HasColumnName("worker_code").HasMaxLength(60);
        entity.Property(e => e.Title).HasColumnName("title").HasMaxLength(240);
        entity.Property(e => e.ExecutionOrder).HasColumnName("execution_order");
        entity.Property(e => e.Status).HasColumnName("status").HasMaxLength(40);
        entity.Property(e => e.IdempotencyKey).HasColumnName("idempotency_key").HasMaxLength(200);
        entity.Property(e => e.DependsOnActionKeysJson).HasColumnName("depends_on_action_keys_json");
        entity.Property(e => e.LinkedAiTaskId).HasColumnName("linked_ai_task_id");
        entity.Property(e => e.AttemptCount).HasColumnName("attempt_count");
        entity.Property(e => e.MaxAttempts).HasColumnName("max_attempts");
        entity.Property(e => e.LastError).HasColumnName("last_error");
        entity.Property(e => e.NextRetryAtUtc).HasColumnName("next_retry_at_utc");
        entity.Property(e => e.StartedAtUtc).HasColumnName("started_at_utc");
        entity.Property(e => e.CompletedAtUtc).HasColumnName("completed_at_utc");
        entity.Property(e => e.CreatedAtUtc).HasColumnName("created_at_utc");
        entity.Property(e => e.UpdatedAtUtc).HasColumnName("updated_at_utc");
        entity.Property(e => e.ConcurrencyToken).HasColumnName("concurrency_token");
        entity.HasIndex(e => new { e.TenantId, e.RootCaseId, e.ActionKey })
            .IsUnique()
            .HasDatabaseName("ux_factory_outcome_execution_actions_root_action");
        entity.HasIndex(e => new { e.TenantId, e.IdempotencyKey })
            .IsUnique()
            .HasDatabaseName("ux_factory_outcome_execution_actions_idempotency");
    }
}

internal sealed class FactoryOutcomeEventReceiptEntityConfiguration : IEntityTypeConfiguration<FactoryOutcomeEventReceipt>
{
    public void Configure(EntityTypeBuilder<FactoryOutcomeEventReceipt> entity)
    {
        entity.ToTable("factory_outcome_event_receipts");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasColumnName("id");
        entity.Property(e => e.TenantId).HasColumnName("tenant_id");
        entity.Property(e => e.FacilityId).HasColumnName("facility_id");
        entity.Property(e => e.SourceType).HasColumnName("source_type").HasMaxLength(60);
        entity.Property(e => e.SourceId).HasColumnName("source_id").HasMaxLength(120);
        entity.Property(e => e.RootCaseId).HasColumnName("root_case_id");
        entity.Property(e => e.CreatedNewCase).HasColumnName("created_new_case");
        entity.Property(e => e.ProcessedAtUtc).HasColumnName("processed_at_utc");
        entity.HasIndex(e => new { e.TenantId, e.FacilityId, e.SourceType, e.SourceId })
            .IsUnique()
            .HasDatabaseName("ux_factory_outcome_event_receipts_source");
    }
}

internal sealed class FactoryOutcomeLearningRecordEntityConfiguration : IEntityTypeConfiguration<FactoryOutcomeLearningRecord>
{
    public void Configure(EntityTypeBuilder<FactoryOutcomeLearningRecord> entity)
    {
        entity.ToTable("factory_outcome_learning_records");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasColumnName("id");
        entity.Property(e => e.TenantId).HasColumnName("tenant_id");
        entity.Property(e => e.FacilityId).HasColumnName("facility_id");
        entity.Property(e => e.SourceCaseId).HasColumnName("source_case_id");
        entity.Property(e => e.ProblemPattern).HasColumnName("problem_pattern").HasMaxLength(240);
        entity.Property(e => e.CausePattern).HasColumnName("cause_pattern").HasMaxLength(240);
        entity.Property(e => e.SelectedStrategyKey).HasColumnName("selected_strategy_key").HasMaxLength(80);
        entity.Property(e => e.AlternativeStrategyKey).HasColumnName("alternative_strategy_key").HasMaxLength(80);
        entity.Property(e => e.ActionsJson).HasColumnName("actions_json");
        entity.Property(e => e.ActualRecoveredQuantity).HasColumnName("actual_recovered_quantity");
        entity.Property(e => e.TimeToRecoveryMinutes).HasColumnName("time_to_recovery_minutes");
        entity.Property(e => e.VerificationResult).HasColumnName("verification_result").HasMaxLength(40);
        entity.Property(e => e.FailureSummary).HasColumnName("failure_summary");
        entity.Property(e => e.VerifiedAtUtc).HasColumnName("verified_at_utc");
        entity.Property(e => e.CreatedAtUtc).HasColumnName("created_at_utc");
        entity.HasIndex(e => new { e.TenantId, e.FacilityId, e.ProblemPattern })
            .HasDatabaseName("ix_factory_outcome_learning_records_problem");
    }
}
