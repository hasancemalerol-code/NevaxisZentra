using NevaxisFactOS.Persistence.Facilities;
using NevaxisFactOS.Persistence.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace NevaxisFactOS.Persistence.Configurations;

internal sealed class MaintenancePredictiveRiskAssessmentEntityConfiguration
    : IEntityTypeConfiguration<Maintenance.MaintenancePredictiveRiskAssessment>
{
    public void Configure(EntityTypeBuilder<Maintenance.MaintenancePredictiveRiskAssessment> entity)
    {
        entity.ToTable("maintenance_predictive_risk_assessments");
        entity.HasKey(a => a.Id);
        entity.Property(a => a.Id).HasColumnName("id");
        entity.Property(a => a.TenantId).HasColumnName("tenant_id");
        entity.Property(a => a.FacilityId).HasColumnName("facility_id");
        entity.Property(a => a.AssetReference).HasColumnName("asset_reference").HasMaxLength(200);
        entity.Property(a => a.MachineId).HasColumnName("machine_id");
        entity.Property(a => a.RiskBand).HasColumnName("risk_band").HasMaxLength(30);
        entity.Property(a => a.ReviewPriority).HasColumnName("review_priority").HasMaxLength(20);
        entity.Property(a => a.RuleSetVersion).HasColumnName("rule_set_version").HasMaxLength(40);
        entity.Property(a => a.ConditionEvaluationIdentity).HasColumnName("condition_evaluation_identity").HasMaxLength(128);
        entity.Property(a => a.DataFreshness).HasColumnName("data_freshness").HasMaxLength(20);
        entity.Property(a => a.DataSufficiency).HasColumnName("data_sufficiency").HasMaxLength(20);
        entity.Property(a => a.ActiveAnomalyCount).HasColumnName("active_anomaly_count");
        entity.Property(a => a.MaintenanceHistorySummary).HasColumnName("maintenance_history_summary").HasMaxLength(2000);
        entity.Property(a => a.Explanation).HasColumnName("explanation").HasMaxLength(4000);
        entity.Property(a => a.CreationSource).HasColumnName("creation_source").HasMaxLength(50);
        entity.Property(a => a.AssessedAtUtc).HasColumnName("assessed_at_utc");
        entity.Property(a => a.IsCurrent).HasColumnName("is_current");
        entity.Property(a => a.LinkedAiTaskId).HasColumnName("linked_ai_task_id");
        entity.Property(a => a.FollowUpStatus).HasColumnName("follow_up_status").HasMaxLength(30);
        entity.Property(a => a.CreatedAtUtc).HasColumnName("created_at_utc");
        entity.Property(a => a.UpdatedAtUtc).HasColumnName("updated_at_utc");
        entity.Property(a => a.UpdatedBy).HasColumnName("updated_by").HasMaxLength(200);

        entity.HasIndex(a => new { a.TenantId, a.FacilityId, a.AssetReference, a.ConditionEvaluationIdentity, a.RuleSetVersion }).IsUnique();
        entity.HasIndex(a => new { a.TenantId, a.FacilityId, a.AssetReference, a.IsCurrent });
        entity.HasMany(a => a.Drivers).WithOne(d => d.Assessment!).HasForeignKey(d => d.AssessmentId).OnDelete(DeleteBehavior.Cascade);
        entity.HasMany(a => a.EvidenceItems).WithOne(e => e.Assessment!).HasForeignKey(e => e.AssessmentId).OnDelete(DeleteBehavior.Cascade);
        entity.HasOne<Tenant>().WithMany().HasForeignKey(a => a.TenantId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Facility>().WithMany()
            .HasForeignKey(a => new { a.TenantId, a.FacilityId })
            .HasPrincipalKey(f => new { f.TenantId, f.Id })
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class MaintenancePredictiveRiskDriverEntityConfiguration
    : IEntityTypeConfiguration<Maintenance.MaintenancePredictiveRiskDriver>
{
    public void Configure(EntityTypeBuilder<Maintenance.MaintenancePredictiveRiskDriver> entity)
    {
        entity.ToTable("maintenance_predictive_risk_drivers");
        entity.HasKey(d => d.Id);
        entity.Property(d => d.Id).HasColumnName("id");
        entity.Property(d => d.TenantId).HasColumnName("tenant_id");
        entity.Property(d => d.FacilityId).HasColumnName("facility_id");
        entity.Property(d => d.AssessmentId).HasColumnName("assessment_id");
        entity.Property(d => d.DriverCode).HasColumnName("driver_code").HasMaxLength(80);
        entity.Property(d => d.Summary).HasColumnName("summary").HasMaxLength(2000);
        entity.Property(d => d.SortOrder).HasColumnName("sort_order");
        entity.Property(d => d.CreatedAtUtc).HasColumnName("created_at_utc");
        entity.HasIndex(d => new { d.TenantId, d.FacilityId, d.AssessmentId, d.SortOrder });
        entity.HasOne<Tenant>().WithMany().HasForeignKey(d => d.TenantId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Facility>().WithMany()
            .HasForeignKey(d => new { d.TenantId, d.FacilityId })
            .HasPrincipalKey(f => new { f.TenantId, f.Id })
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class MaintenancePredictiveRiskEvidenceEntityConfiguration
    : IEntityTypeConfiguration<Maintenance.MaintenancePredictiveRiskEvidence>
{
    public void Configure(EntityTypeBuilder<Maintenance.MaintenancePredictiveRiskEvidence> entity)
    {
        entity.ToTable("maintenance_predictive_risk_evidence");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasColumnName("id");
        entity.Property(e => e.TenantId).HasColumnName("tenant_id");
        entity.Property(e => e.FacilityId).HasColumnName("facility_id");
        entity.Property(e => e.AssessmentId).HasColumnName("assessment_id");
        entity.Property(e => e.EvidenceType).HasColumnName("evidence_type").HasMaxLength(50);
        entity.Property(e => e.EvidenceReference).HasColumnName("evidence_reference").HasMaxLength(200);
        entity.Property(e => e.Summary).HasColumnName("summary").HasMaxLength(2000);
        entity.Property(e => e.SortOrder).HasColumnName("sort_order");
        entity.Property(e => e.CreatedAtUtc).HasColumnName("created_at_utc");
        entity.HasIndex(e => new { e.TenantId, e.FacilityId, e.AssessmentId, e.SortOrder });
        entity.HasOne<Tenant>().WithMany().HasForeignKey(e => e.TenantId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Facility>().WithMany()
            .HasForeignKey(e => new { e.TenantId, e.FacilityId })
            .HasPrincipalKey(f => new { f.TenantId, f.Id })
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class MaintenancePredictiveRecommendationEntityConfiguration
    : IEntityTypeConfiguration<Maintenance.MaintenancePredictiveRecommendation>
{
    public void Configure(EntityTypeBuilder<Maintenance.MaintenancePredictiveRecommendation> entity)
    {
        entity.ToTable("maintenance_predictive_recommendations");
        entity.HasKey(r => r.Id);
        entity.Property(r => r.Id).HasColumnName("id");
        entity.Property(r => r.TenantId).HasColumnName("tenant_id");
        entity.Property(r => r.FacilityId).HasColumnName("facility_id");
        entity.Property(r => r.AssetReference).HasColumnName("asset_reference").HasMaxLength(200);
        entity.Property(r => r.AssessmentId).HasColumnName("assessment_id");
        entity.Property(r => r.Category).HasColumnName("category").HasMaxLength(60);
        entity.Property(r => r.ReviewPriority).HasColumnName("review_priority").HasMaxLength(20);
        entity.Property(r => r.Explanation).HasColumnName("explanation").HasMaxLength(4000);
        entity.Property(r => r.RuleSetVersion).HasColumnName("rule_set_version").HasMaxLength(40);
        entity.Property(r => r.Status).HasColumnName("status").HasMaxLength(20);
        entity.Property(r => r.GeneratedAtUtc).HasColumnName("generated_at_utc");
        entity.Property(r => r.CreatedAtUtc).HasColumnName("created_at_utc");
        entity.HasIndex(r => new { r.TenantId, r.FacilityId, r.AssessmentId }).IsUnique();
        entity.HasIndex(r => new { r.TenantId, r.FacilityId, r.AssetReference, r.Status });
        entity.HasOne(r => r.Assessment).WithMany().HasForeignKey(r => r.AssessmentId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Tenant>().WithMany().HasForeignKey(r => r.TenantId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Facility>().WithMany()
            .HasForeignKey(r => new { r.TenantId, r.FacilityId })
            .HasPrincipalKey(f => new { f.TenantId, f.Id })
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class MaintenancePredictiveRiskAuditEventEntityConfiguration
    : IEntityTypeConfiguration<Maintenance.MaintenancePredictiveRiskAuditEvent>
{
    public void Configure(EntityTypeBuilder<Maintenance.MaintenancePredictiveRiskAuditEvent> entity)
    {
        entity.ToTable("maintenance_predictive_risk_audit_events");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasColumnName("id");
        entity.Property(e => e.TenantId).HasColumnName("tenant_id");
        entity.Property(e => e.FacilityId).HasColumnName("facility_id");
        entity.Property(e => e.AssessmentId).HasColumnName("assessment_id");
        entity.Property(e => e.EventType).HasColumnName("event_type").HasMaxLength(50);
        entity.Property(e => e.PayloadJson).HasColumnName("payload_json").HasColumnType("jsonb");
        entity.Property(e => e.Actor).HasColumnName("actor").HasMaxLength(200);
        entity.Property(e => e.OccurredAtUtc).HasColumnName("occurred_at_utc");
        entity.HasIndex(e => new { e.TenantId, e.FacilityId, e.AssessmentId, e.OccurredAtUtc });
        entity.HasOne(e => e.Assessment).WithMany().HasForeignKey(e => e.AssessmentId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Tenant>().WithMany().HasForeignKey(e => e.TenantId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Facility>().WithMany()
            .HasForeignKey(e => new { e.TenantId, e.FacilityId })
            .HasPrincipalKey(f => new { f.TenantId, f.Id })
            .OnDelete(DeleteBehavior.Restrict);
    }
}
