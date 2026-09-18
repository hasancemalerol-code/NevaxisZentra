using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace NevaxisZentra.Persistence.ModelGateway;

internal static class ModelGatewayEntityConfigurationExtensions
{
    internal static void MapTenantFacilityColumns<T>(EntityTypeBuilder<T> builder)
        where T : class, IModelGatewayTenantFacilityEntity
    {
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.TenantId).HasColumnName("tenant_id");
        builder.Property(x => x.FacilityId).HasColumnName("facility_id");
    }
}

public sealed class ModelGatewayAuditRecordConfiguration : IEntityTypeConfiguration<ModelGatewayAuditRecord>
{
    public void Configure(EntityTypeBuilder<ModelGatewayAuditRecord> builder)
    {
        builder.ToTable("model_gateway_audit_records");
        builder.HasKey(x => x.Id);
        ModelGatewayEntityConfigurationExtensions.MapTenantFacilityColumns(builder);
        builder.Property(x => x.WorkerId).HasColumnName("worker_id");
        builder.Property(x => x.DigitalWorkCaseId).HasColumnName("digital_work_case_id");
        builder.Property(x => x.ContextSnapshotId).HasColumnName("context_snapshot_id");
        builder.Property(x => x.RequestClassification).HasColumnName("request_classification").HasMaxLength(64).IsRequired();
        builder.Property(x => x.IntelligencePath).HasColumnName("intelligence_path").HasMaxLength(32).IsRequired();
        builder.Property(x => x.ProviderId).HasColumnName("provider_id").HasMaxLength(64).IsRequired();
        builder.Property(x => x.ModelName).HasColumnName("model_name").HasMaxLength(128).IsRequired();
        builder.Property(x => x.ModelVersion).HasColumnName("model_version").HasMaxLength(32).IsRequired();
        builder.Property(x => x.RoutingReason).HasColumnName("routing_reason").HasMaxLength(512).IsRequired();
        builder.Property(x => x.UsedFallback).HasColumnName("used_fallback");
        builder.Property(x => x.ResponseClassification).HasColumnName("response_classification").HasMaxLength(64).IsRequired();
        builder.Property(x => x.ValidationStatus).HasColumnName("validation_status").HasMaxLength(32).IsRequired();
        builder.Property(x => x.StructuredProposalJson).HasColumnName("structured_proposal_json");
        builder.Property(x => x.ValidationResultJson).HasColumnName("validation_result_json");
        builder.Property(x => x.LatencyMs).HasColumnName("latency_ms");
        builder.Property(x => x.EstimatedCost).HasColumnName("estimated_cost");
        builder.Property(x => x.InputTokens).HasColumnName("input_tokens");
        builder.Property(x => x.OutputTokens).HasColumnName("output_tokens");
        builder.Property(x => x.EngineVersion).HasColumnName("engine_version").HasMaxLength(64).IsRequired();
        builder.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc");
        builder.HasIndex(x => new { x.TenantId, x.FacilityId, x.DigitalWorkCaseId, x.CreatedAtUtc });
    }
}

public sealed class ModelGatewayCostUsageRecordConfiguration : IEntityTypeConfiguration<ModelGatewayCostUsageRecord>
{
    public void Configure(EntityTypeBuilder<ModelGatewayCostUsageRecord> builder)
    {
        builder.ToTable("model_gateway_cost_usage_records");
        builder.HasKey(x => x.Id);
        ModelGatewayEntityConfigurationExtensions.MapTenantFacilityColumns(builder);
        builder.Property(x => x.WorkerId).HasColumnName("worker_id");
        builder.Property(x => x.DigitalWorkCaseId).HasColumnName("digital_work_case_id");
        builder.Property(x => x.ProviderId).HasColumnName("provider_id").HasMaxLength(64).IsRequired();
        builder.Property(x => x.ModelName).HasColumnName("model_name").HasMaxLength(128).IsRequired();
        builder.Property(x => x.RequestClassification).HasColumnName("request_classification").HasMaxLength(64).IsRequired();
        builder.Property(x => x.RequestCount).HasColumnName("request_count");
        builder.Property(x => x.InputTokens).HasColumnName("input_tokens");
        builder.Property(x => x.OutputTokens).HasColumnName("output_tokens");
        builder.Property(x => x.EstimatedCost).HasColumnName("estimated_cost");
        builder.Property(x => x.BudgetWindow).HasColumnName("budget_window").HasMaxLength(16).IsRequired();
        builder.Property(x => x.WindowStartUtc).HasColumnName("window_start_utc");
        builder.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc");
        builder.HasIndex(x => new { x.TenantId, x.FacilityId, x.WorkerId, x.WindowStartUtc });
    }
}

public sealed class ModelGatewayBudgetPolicyRecordConfiguration : IEntityTypeConfiguration<ModelGatewayBudgetPolicyRecord>
{
    public void Configure(EntityTypeBuilder<ModelGatewayBudgetPolicyRecord> builder)
    {
        builder.ToTable("model_gateway_budget_policies");
        builder.HasKey(x => x.Id);
        ModelGatewayEntityConfigurationExtensions.MapTenantFacilityColumns(builder);
        builder.Property(x => x.WorkerId).HasColumnName("worker_id");
        builder.Property(x => x.DailyBudget).HasColumnName("daily_budget");
        builder.Property(x => x.MonthlyBudget).HasColumnName("monthly_budget");
        builder.Property(x => x.CaseBudget).HasColumnName("case_budget");
        builder.Property(x => x.RestrictedRequestClassesJson).HasColumnName("restricted_request_classes_json");
        builder.Property(x => x.IsActive).HasColumnName("is_active");
        builder.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc");
        builder.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc");
        builder.HasIndex(x => new { x.TenantId, x.FacilityId, x.WorkerId }).IsUnique();
    }
}
