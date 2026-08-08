using CrevaFactOS.Persistence.Export;
using CrevaFactOS.Persistence.Facilities;
using CrevaFactOS.Persistence.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CrevaFactOS.Persistence.Configurations;

internal sealed class ExportAiRecommendationEntityConfiguration
    : IEntityTypeConfiguration<ExportAiRecommendation>
{
    private static readonly Dictionary<ExportAiRecommendationPriority, string> PriorityValues =
        new()
        {
            [ExportAiRecommendationPriority.Low] = "low",
            [ExportAiRecommendationPriority.Medium] = "medium",
            [ExportAiRecommendationPriority.High] = "high",
            [ExportAiRecommendationPriority.Critical] = "critical",
        };

    private static readonly Dictionary<ExportAiRecommendationStatus, string> StatusValues =
        new()
        {
            [ExportAiRecommendationStatus.Proposed] = "proposed",
            [ExportAiRecommendationStatus.Accepted] = "accepted",
            [ExportAiRecommendationStatus.Rejected] = "rejected",
            [ExportAiRecommendationStatus.Dismissed] = "dismissed",
            [ExportAiRecommendationStatus.Expired] = "expired",
        };

    private static readonly Dictionary<ExportAiProposedActionType, string> ProposedActionTypeValues =
        new()
        {
            [ExportAiProposedActionType.CreateTask] = "create-task",
            [ExportAiProposedActionType.OpenDocument] = "open-document",
            [ExportAiProposedActionType.OpenComplianceRequirement] = "open-compliance-requirement",
            [ExportAiProposedActionType.OpenBrokerCorrection] = "open-broker-correction",
            [ExportAiProposedActionType.OpenShipment] = "open-shipment",
            [ExportAiProposedActionType.OpenProductionOrder] = "open-production-order",
            [ExportAiProposedActionType.OpenPlanningRecord] = "open-planning-record",
            [ExportAiProposedActionType.RequestHumanReview] = "request-human-review",
            [ExportAiProposedActionType.DraftBrokerQuestion] = "draft-broker-question",
        };

    private static readonly Dictionary<ExportAiReviewDecision, string> ReviewDecisionValues =
        new()
        {
            [ExportAiReviewDecision.Accepted] = "accepted",
            [ExportAiReviewDecision.Rejected] = "rejected",
            [ExportAiReviewDecision.Dismissed] = "dismissed",
        };

    public void Configure(EntityTypeBuilder<ExportAiRecommendation> entity)
    {
        entity.ToTable("export_ai_recommendations");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasColumnName("id");
        entity.Property(e => e.TenantId).HasColumnName("tenant_id");
        entity.Property(e => e.FacilityId).HasColumnName("facility_id");
        entity.Property(e => e.AnalysisRunId).HasColumnName("analysis_run_id");
        entity.Property(e => e.Title).HasColumnName("title").HasMaxLength(300);
        entity.Property(e => e.Description).HasColumnName("description").HasMaxLength(4000);
        entity.Property(e => e.Priority)
            .HasColumnName("priority")
            .HasConversion(
                value => PriorityValues[value],
                value => PriorityValues.First(p => p.Value == value).Key)
            .HasMaxLength(10);
        entity.Property(e => e.Confidence).HasColumnName("confidence").HasPrecision(5, 4);
        entity.Property(e => e.ReasoningSummary).HasColumnName("reasoning_summary").HasMaxLength(4000);
        entity.Property(e => e.ProposedActionType)
            .HasColumnName("proposed_action_type")
            .HasConversion(
                value => value.HasValue ? ProposedActionTypeValues[value.Value] : null,
                value => value == null ? null : ProposedActionTypeValues.First(p => p.Value == value).Key)
            .HasMaxLength(40);
        entity.Property(e => e.ProposedActionPayloadJson).HasColumnName("proposed_action_payload_json").HasColumnType("jsonb");
        entity.Property(e => e.Status)
            .HasColumnName("status")
            .HasConversion(
                value => StatusValues[value],
                value => StatusValues.First(p => p.Value == value).Key)
            .HasMaxLength(20);
        entity.Property(e => e.CreatedAtUtc).HasColumnName("created_at_utc");
        entity.Property(e => e.ReviewedAtUtc).HasColumnName("reviewed_at_utc");
        entity.Property(e => e.ReviewedBySubjectId).HasColumnName("reviewed_by_subject_id").HasMaxLength(200);
        entity.Property(e => e.ReviewDecision)
            .HasColumnName("review_decision")
            .HasConversion(
                value => value.HasValue ? ReviewDecisionValues[value.Value] : null,
                value => value == null ? null : ReviewDecisionValues.First(p => p.Value == value).Key)
            .HasMaxLength(20);
        entity.Property(e => e.ReviewNote).HasColumnName("review_note").HasMaxLength(2000);
        entity.Property(e => e.Version).HasColumnName("version").IsConcurrencyToken();

        entity.HasIndex(e => e.AnalysisRunId)
            .HasDatabaseName("ix_export_ai_recommendations_analysis_run");
        entity.HasIndex(e => new { e.TenantId, e.FacilityId, e.AnalysisRunId, e.Status })
            .HasDatabaseName("ix_export_ai_recommendations_tenant_facility_run_status");

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
            "ck_export_ai_recommendations_priority",
            "priority IN ('low', 'medium', 'high', 'critical')"));
        entity.ToTable(table => table.HasCheckConstraint(
            "ck_export_ai_recommendations_status",
            "status IN ('proposed', 'accepted', 'rejected', 'dismissed', 'expired')"));
        entity.ToTable(table => table.HasCheckConstraint(
            "ck_export_ai_recommendations_proposed_action_type",
            "proposed_action_type IS NULL OR proposed_action_type IN ('create-task', 'open-document', 'open-compliance-requirement', 'open-broker-correction', 'open-shipment', 'open-production-order', 'open-planning-record', 'request-human-review', 'draft-broker-question')"));
        entity.ToTable(table => table.HasCheckConstraint(
            "ck_export_ai_recommendations_review_decision",
            "review_decision IS NULL OR review_decision IN ('accepted', 'rejected', 'dismissed')"));
        entity.ToTable(table => table.HasCheckConstraint(
            "ck_export_ai_recommendations_confidence",
            "confidence >= 0 AND confidence <= 1"));
        entity.ToTable(table => table.HasCheckConstraint(
            "ck_export_ai_recommendations_version",
            "version >= 1"));
        entity.ToTable(table => table.HasCheckConstraint(
            "ck_export_ai_recommendations_title",
            "title <> ''"));
    }
}
