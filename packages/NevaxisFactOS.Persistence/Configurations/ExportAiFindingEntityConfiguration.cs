using NevaxisFactOS.Persistence.Export;
using NevaxisFactOS.Persistence.Facilities;
using NevaxisFactOS.Persistence.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace NevaxisFactOS.Persistence.Configurations;

internal sealed class ExportAiFindingEntityConfiguration
    : IEntityTypeConfiguration<ExportAiFinding>
{
    private static readonly Dictionary<ExportAiFindingCategory, string> CategoryValues =
        new()
        {
            [ExportAiFindingCategory.Compliance] = "compliance",
            [ExportAiFindingCategory.Document] = "document",
            [ExportAiFindingCategory.Packing] = "packing",
            [ExportAiFindingCategory.Shipment] = "shipment",
            [ExportAiFindingCategory.Broker] = "broker",
            [ExportAiFindingCategory.CountryResearch] = "country-research",
            [ExportAiFindingCategory.Quality] = "quality",
            [ExportAiFindingCategory.Production] = "production",
            [ExportAiFindingCategory.Planning] = "planning",
            [ExportAiFindingCategory.Erp] = "erp",
            [ExportAiFindingCategory.Risk] = "risk",
            [ExportAiFindingCategory.DataQuality] = "data-quality",
            [ExportAiFindingCategory.Other] = "other",
        };

    private static readonly Dictionary<ExportAiFindingSeverity, string> SeverityValues =
        new()
        {
            [ExportAiFindingSeverity.Info] = "info",
            [ExportAiFindingSeverity.Warning] = "warning",
            [ExportAiFindingSeverity.Critical] = "critical",
        };

    private static readonly Dictionary<ExportAiSourceType, string> SourceTypeValues =
        new()
        {
            [ExportAiSourceType.VerifiedRule] = "verified-rule",
            [ExportAiSourceType.VerifiedKnowledge] = "verified-knowledge",
            [ExportAiSourceType.HistoricalObservation] = "historical-observation",
            [ExportAiSourceType.BrokerInput] = "broker-input",
            [ExportAiSourceType.OperationalData] = "operational-data",
            [ExportAiSourceType.AiInterpretation] = "ai-interpretation",
            [ExportAiSourceType.SystemDeterministic] = "system-deterministic",
        };

    public void Configure(EntityTypeBuilder<ExportAiFinding> entity)
    {
        entity.ToTable("export_ai_findings");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasColumnName("id");
        entity.Property(e => e.TenantId).HasColumnName("tenant_id");
        entity.Property(e => e.FacilityId).HasColumnName("facility_id");
        entity.Property(e => e.AnalysisRunId).HasColumnName("analysis_run_id");
        entity.Property(e => e.Category)
            .HasColumnName("category")
            .HasConversion(
                value => CategoryValues[value],
                value => CategoryValues.First(p => p.Value == value).Key)
            .HasMaxLength(30);
        entity.Property(e => e.Title).HasColumnName("title").HasMaxLength(300);
        entity.Property(e => e.Summary).HasColumnName("summary").HasMaxLength(4000);
        entity.Property(e => e.Severity)
            .HasColumnName("severity")
            .HasConversion(
                value => SeverityValues[value],
                value => SeverityValues.First(p => p.Value == value).Key)
            .HasMaxLength(10);
        entity.Property(e => e.Confidence).HasColumnName("confidence").HasPrecision(5, 4);
        entity.Property(e => e.SourceType)
            .HasColumnName("source_type")
            .HasConversion(
                value => SourceTypeValues[value],
                value => SourceTypeValues.First(p => p.Value == value).Key)
            .HasMaxLength(30);
        entity.Property(e => e.SourceReferencesJson).HasColumnName("source_references_json").HasColumnType("jsonb");
        entity.Property(e => e.RelatedRequirementId).HasColumnName("related_requirement_id");
        entity.Property(e => e.RelatedDocumentId).HasColumnName("related_document_id");
        entity.Property(e => e.RelatedShipmentId).HasColumnName("related_shipment_id");
        entity.Property(e => e.Actionable).HasColumnName("actionable");
        entity.Property(e => e.HumanReviewRequired).HasColumnName("human_review_required");
        entity.Property(e => e.CreatedAtUtc).HasColumnName("created_at_utc");

        entity.HasIndex(e => e.AnalysisRunId)
            .HasDatabaseName("ix_export_ai_findings_analysis_run");
        entity.HasIndex(e => new { e.TenantId, e.FacilityId, e.AnalysisRunId })
            .HasDatabaseName("ix_export_ai_findings_tenant_facility_run");

        entity.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(e => e.TenantId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Facility>()
            .WithMany()
            .HasForeignKey(e => new { e.TenantId, e.FacilityId })
            .HasPrincipalKey(f => new { f.TenantId, f.Id })
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<ExportAiAnalysisRun>()
            .WithMany()
            .HasForeignKey(e => e.AnalysisRunId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.ToTable(table => table.HasCheckConstraint(
            "ck_export_ai_findings_category",
            "category IN ('compliance', 'document', 'packing', 'shipment', 'broker', 'country-research', 'quality', 'production', 'planning', 'erp', 'risk', 'data-quality', 'other')"));
        entity.ToTable(table => table.HasCheckConstraint(
            "ck_export_ai_findings_severity",
            "severity IN ('info', 'warning', 'critical')"));
        entity.ToTable(table => table.HasCheckConstraint(
            "ck_export_ai_findings_source_type",
            "source_type IN ('verified-rule', 'verified-knowledge', 'historical-observation', 'broker-input', 'operational-data', 'ai-interpretation', 'system-deterministic')"));
        entity.ToTable(table => table.HasCheckConstraint(
            "ck_export_ai_findings_confidence",
            "confidence >= 0 AND confidence <= 1"));
        entity.ToTable(table => table.HasCheckConstraint(
            "ck_export_ai_findings_source_references",
            "jsonb_typeof(source_references_json) = 'array'"));
        entity.ToTable(table => table.HasCheckConstraint(
            "ck_export_ai_findings_title",
            "title <> ''"));
    }
}
