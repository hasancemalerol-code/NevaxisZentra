using CrevaFactOS.Persistence.Export;
using CrevaFactOS.Persistence.Facilities;
using CrevaFactOS.Persistence.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CrevaFactOS.Persistence.Configurations;

internal sealed class ExportAiAnalysisRunEntityConfiguration
    : IEntityTypeConfiguration<ExportAiAnalysisRun>
{
    private static readonly Dictionary<ExportAiAnalysisRunType, string> RunTypeValues =
        new()
        {
            [ExportAiAnalysisRunType.CaseOverview] = "case-overview",
            [ExportAiAnalysisRunType.ComplianceExplanation] = "compliance-explanation",
            [ExportAiAnalysisRunType.ReadinessAnalysis] = "readiness-analysis",
            [ExportAiAnalysisRunType.MissingDataAnalysis] = "missing-data-analysis",
            [ExportAiAnalysisRunType.CountryResearchAssist] = "country-research-assist",
            [ExportAiAnalysisRunType.NextActions] = "next-actions",
            [ExportAiAnalysisRunType.DocumentGapAnalysis] = "document-gap-analysis",
            [ExportAiAnalysisRunType.CrossModuleRiskAnalysis] = "cross-module-risk-analysis",
        };

    private static readonly Dictionary<ExportAiAnalysisRunStatus, string> StatusValues =
        new()
        {
            [ExportAiAnalysisRunStatus.Queued] = "queued",
            [ExportAiAnalysisRunStatus.Running] = "running",
            [ExportAiAnalysisRunStatus.Completed] = "completed",
            [ExportAiAnalysisRunStatus.Failed] = "failed",
            [ExportAiAnalysisRunStatus.Cancelled] = "cancelled",
        };

    public void Configure(EntityTypeBuilder<ExportAiAnalysisRun> entity)
    {
        entity.ToTable("export_ai_analysis_runs");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasColumnName("id");
        entity.Property(e => e.TenantId).HasColumnName("tenant_id");
        entity.Property(e => e.FacilityId).HasColumnName("facility_id");
        entity.Property(e => e.ExportCaseId).HasColumnName("export_case_id");
        entity.Property(e => e.CountryResearchCaseId).HasColumnName("country_research_case_id");
        entity.Property(e => e.ContextSnapshotId).HasColumnName("context_snapshot_id");
        entity.Property(e => e.RunType)
            .HasColumnName("run_type")
            .HasConversion(
                value => RunTypeValues[value],
                value => RunTypeValues.First(p => p.Value == value).Key)
            .HasMaxLength(40);
        entity.Property(e => e.Status)
            .HasColumnName("status")
            .HasConversion(
                value => StatusValues[value],
                value => StatusValues.First(p => p.Value == value).Key)
            .HasMaxLength(20);
        entity.Property(e => e.Provider).HasColumnName("provider").HasMaxLength(100);
        entity.Property(e => e.Model).HasColumnName("model").HasMaxLength(150);
        entity.Property(e => e.PromptVersion).HasColumnName("prompt_version").HasMaxLength(100);
        entity.Property(e => e.StartedAtUtc).HasColumnName("started_at_utc");
        entity.Property(e => e.CompletedAtUtc).HasColumnName("completed_at_utc");
        entity.Property(e => e.RequestedBySubjectId).HasColumnName("requested_by_subject_id").HasMaxLength(200);
        entity.Property(e => e.InputUsageUnits).HasColumnName("input_usage_units");
        entity.Property(e => e.OutputUsageUnits).HasColumnName("output_usage_units");
        entity.Property(e => e.AiCreditsConsumed).HasColumnName("ai_credits_consumed").HasPrecision(14, 4);
        entity.Property(e => e.FailureCode).HasColumnName("failure_code").HasMaxLength(100);
        entity.Property(e => e.FailureMessage).HasColumnName("failure_message").HasMaxLength(2000);
        entity.Property(e => e.ReasoningSummary).HasColumnName("reasoning_summary").HasMaxLength(4000);
        entity.Property(e => e.Version).HasColumnName("version").IsConcurrencyToken();

        entity.HasIndex(e => new { e.TenantId, e.FacilityId, e.ExportCaseId, e.StartedAtUtc })
            .HasDatabaseName("ix_export_ai_analysis_runs_tenant_facility_case_started");
        entity.HasIndex(e => new { e.TenantId, e.FacilityId, e.CountryResearchCaseId, e.StartedAtUtc })
            .HasDatabaseName("ix_export_ai_analysis_runs_tenant_facility_research_started");
        entity.HasIndex(e => e.ContextSnapshotId)
            .HasDatabaseName("ix_export_ai_analysis_runs_context_snapshot");

        entity.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(e => e.TenantId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Facility>()
            .WithMany()
            .HasForeignKey(e => new { e.TenantId, e.FacilityId })
            .HasPrincipalKey(f => new { f.TenantId, f.Id })
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<ExportCase>()
            .WithMany()
            .HasForeignKey(e => e.ExportCaseId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<ExportCountryResearchCase>()
            .WithMany()
            .HasForeignKey(e => e.CountryResearchCaseId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<ExportAiContextSnapshot>()
            .WithMany()
            .HasForeignKey(e => e.ContextSnapshotId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.ToTable(table => table.HasCheckConstraint(
            "ck_export_ai_analysis_runs_scope",
            "export_case_id IS NOT NULL OR country_research_case_id IS NOT NULL"));
        entity.ToTable(table => table.HasCheckConstraint(
            "ck_export_ai_analysis_runs_run_type",
            "run_type IN ('case-overview', 'compliance-explanation', 'readiness-analysis', 'missing-data-analysis', 'country-research-assist', 'next-actions', 'document-gap-analysis', 'cross-module-risk-analysis')"));
        entity.ToTable(table => table.HasCheckConstraint(
            "ck_export_ai_analysis_runs_status",
            "status IN ('queued', 'running', 'completed', 'failed', 'cancelled')"));
        entity.ToTable(table => table.HasCheckConstraint(
            "ck_export_ai_analysis_runs_usage",
            "input_usage_units >= 0 AND output_usage_units >= 0"));
        entity.ToTable(table => table.HasCheckConstraint(
            "ck_export_ai_analysis_runs_credits",
            "ai_credits_consumed >= 0"));
        entity.ToTable(table => table.HasCheckConstraint(
            "ck_export_ai_analysis_runs_version",
            "version >= 1"));
    }
}
