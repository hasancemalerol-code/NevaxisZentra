using NevaxisFactOS.Persistence.Facilities;
using NevaxisFactOS.Persistence.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace NevaxisFactOS.Persistence.Configurations;

internal sealed class ProcurementDeliveryRiskAssessmentEntityConfiguration
    : IEntityTypeConfiguration<Procurement.ProcurementDeliveryRiskAssessment>
{
    public void Configure(EntityTypeBuilder<Procurement.ProcurementDeliveryRiskAssessment> entity)
    {
        entity.ToTable("procurement_delivery_risk_assessments");
        entity.HasKey(a => a.Id);
        entity.Property(a => a.Id).HasColumnName("id");
        entity.Property(a => a.TenantId).HasColumnName("tenant_id");
        entity.Property(a => a.FacilityId).HasColumnName("facility_id");
        entity.Property(a => a.PurchaseOrderId).HasColumnName("purchase_order_id");
        entity.Property(a => a.PurchaseOrderLineId).HasColumnName("purchase_order_line_id");
        entity.Property(a => a.PurchaseOrderNumber).HasColumnName("purchase_order_number").HasMaxLength(100).IsRequired();
        entity.Property(a => a.ExternalSupplierId).HasColumnName("external_supplier_id").HasMaxLength(100);
        entity.Property(a => a.ExternalSupplierCode).HasColumnName("external_supplier_code").HasMaxLength(100);
        entity.Property(a => a.SupplierName).HasColumnName("supplier_name").HasMaxLength(200);
        entity.Property(a => a.OrderStatus).HasColumnName("order_status").HasMaxLength(30).IsRequired();
        entity.Property(a => a.LineNumber).HasColumnName("line_number");
        entity.Property(a => a.ExternalProductId).HasColumnName("external_product_id").HasMaxLength(100);
        entity.Property(a => a.ExternalProductCode).HasColumnName("external_product_code").HasMaxLength(100);
        entity.Property(a => a.LineDescription).HasColumnName("line_description").HasMaxLength(500);
        entity.Property(a => a.CommitmentDateUtc).HasColumnName("commitment_date_utc");
        entity.Property(a => a.OrderedQuantity).HasColumnName("ordered_quantity").HasPrecision(18, 4);
        entity.Property(a => a.ReceivedQuantity).HasColumnName("received_quantity").HasPrecision(18, 4);
        entity.Property(a => a.OpenQuantity).HasColumnName("open_quantity").HasPrecision(18, 4);
        entity.Property(a => a.Unit).HasColumnName("unit").HasMaxLength(20);
        entity.Property(a => a.Result).HasColumnName("result").HasMaxLength(30).IsRequired();
        entity.Property(a => a.RuleSetVersion).HasColumnName("rule_set_version").HasMaxLength(40).IsRequired();
        entity.Property(a => a.InputFingerprint).HasColumnName("input_fingerprint").HasMaxLength(64).IsRequired();
        entity.Property(a => a.IdempotencyKey).HasColumnName("idempotency_key").HasMaxLength(128);
        entity.Property(a => a.StructuredDrivers).HasColumnName("structured_drivers").HasColumnType("jsonb").IsRequired();
        entity.Property(a => a.StructuredEvidence).HasColumnName("structured_evidence").HasColumnType("jsonb").IsRequired();
        entity.Property(a => a.SourceUpdatedAtUtc).HasColumnName("source_updated_at_utc");
        entity.Property(a => a.SourceFreshness).HasColumnName("source_freshness").HasMaxLength(20);
        entity.Property(a => a.EvaluatedAtUtc).HasColumnName("evaluated_at_utc");
        entity.Property(a => a.IsCurrent).HasColumnName("is_current");
        entity.Property(a => a.Version).HasColumnName("version").IsConcurrencyToken();
        entity.Property(a => a.CreatedAtUtc).HasColumnName("created_at_utc");
        entity.Property(a => a.CreatedBySubjectId).HasColumnName("created_by_subject_id").HasMaxLength(200).IsRequired();
        entity.Property(a => a.LinkedAiTaskId).HasColumnName("linked_ai_task_id");
        entity.Property(a => a.FollowUpStatus).HasColumnName("follow_up_status").HasMaxLength(30);
        entity.Property(a => a.FollowUpUpdatedBy).HasColumnName("follow_up_updated_by").HasMaxLength(200);

        entity.HasIndex(a => new { a.TenantId, a.FacilityId, a.Result, a.EvaluatedAtUtc });
        entity.HasIndex(a => new { a.TenantId, a.FacilityId, a.PurchaseOrderId, a.EvaluatedAtUtc });
        entity.HasIndex(a => new { a.TenantId, a.FacilityId, a.ExternalSupplierCode, a.EvaluatedAtUtc });
        entity.HasIndex(a => new { a.TenantId, a.PurchaseOrderLineId, a.IdempotencyKey })
            .IsUnique()
            .HasFilter("idempotency_key IS NOT NULL");
        entity.HasIndex(a => new { a.TenantId, a.PurchaseOrderLineId, a.InputFingerprint })
            .IsUnique();
        entity.HasIndex(a => new { a.TenantId, a.FacilityId, a.PurchaseOrderLineId })
            .IsUnique()
            .HasFilter("is_current = TRUE");

        entity.HasOne<Tenant>().WithMany().HasForeignKey(a => a.TenantId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Facility>().WithMany()
            .HasForeignKey(a => new { a.TenantId, a.FacilityId })
            .HasPrincipalKey(f => new { f.TenantId, f.Id })
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class ProcurementDeliveryRiskFollowUpAuditEventEntityConfiguration
    : IEntityTypeConfiguration<Procurement.ProcurementDeliveryRiskFollowUpAuditEvent>
{
    public void Configure(EntityTypeBuilder<Procurement.ProcurementDeliveryRiskFollowUpAuditEvent> entity)
    {
        entity.ToTable("procurement_delivery_risk_follow_up_audit_events");
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
        entity.HasOne(e => e.Assessment).WithMany()
            .HasForeignKey(e => e.AssessmentId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Tenant>().WithMany().HasForeignKey(e => e.TenantId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Facility>().WithMany()
            .HasForeignKey(e => new { e.TenantId, e.FacilityId })
            .HasPrincipalKey(f => new { f.TenantId, f.Id })
            .OnDelete(DeleteBehavior.Restrict);
    }
}
