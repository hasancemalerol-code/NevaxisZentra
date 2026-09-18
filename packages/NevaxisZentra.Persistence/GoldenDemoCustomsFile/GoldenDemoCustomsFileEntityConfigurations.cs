using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace NevaxisZentra.Persistence.GoldenDemoCustomsFile;

internal sealed class GoldenDemoCustomsFileOutcomeEntityConfiguration : IEntityTypeConfiguration<GoldenDemoCustomsFileOutcome>
{
    public void Configure(EntityTypeBuilder<GoldenDemoCustomsFileOutcome> entity)
    {
        entity.ToTable("golden_demo_customs_file_outcomes");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasColumnName("id");
        entity.Property(e => e.TenantId).HasColumnName("tenant_id");
        entity.Property(e => e.FacilityId).HasColumnName("facility_id");
        entity.Property(e => e.WorkerCode).HasColumnName("worker_code").HasMaxLength(60);
        entity.Property(e => e.AssignedOutcomeText).HasColumnName("assigned_outcome_text");
        entity.Property(e => e.Status).HasColumnName("status").HasMaxLength(40);
        entity.Property(e => e.OrderNumber).HasColumnName("order_number").HasMaxLength(60);
        entity.Property(e => e.CustomerName).HasColumnName("customer_name").HasMaxLength(200);
        entity.Property(e => e.DestinationCountry).HasColumnName("destination_country").HasMaxLength(100);
        entity.Property(e => e.DeliveryAddress).HasColumnName("delivery_address");
        entity.Property(e => e.Currency).HasColumnName("currency").HasMaxLength(10);
        entity.Property(e => e.Incoterm).HasColumnName("incoterm").HasMaxLength(20);
        entity.Property(e => e.ProductLinesJson).HasColumnName("product_lines_json");
        entity.Property(e => e.PackageCount).HasColumnName("package_count");
        entity.Property(e => e.OrderGrossWeightKg).HasColumnName("order_gross_weight_kg");
        entity.Property(e => e.ShipmentReference).HasColumnName("shipment_reference").HasMaxLength(60);
        entity.Property(e => e.ShipmentNetWeightKg).HasColumnName("shipment_net_weight_kg");
        entity.Property(e => e.ShipmentGrossWeightKg).HasColumnName("shipment_gross_weight_kg");
        entity.Property(e => e.TradeRequirementsJson).HasColumnName("trade_requirements_json");
        entity.Property(e => e.SellerName).HasColumnName("seller_name").HasMaxLength(200);
        entity.Property(e => e.BuyerName).HasColumnName("buyer_name").HasMaxLength(200);
        entity.Property(e => e.ApprovedBySubjectId).HasColumnName("approved_by_subject_id").HasMaxLength(200);
        entity.Property(e => e.ApprovedAtUtc).HasColumnName("approved_at_utc");
        entity.Property(e => e.CurrentPackageId).HasColumnName("current_package_id");
        entity.Property(e => e.CreatedAtUtc).HasColumnName("created_at_utc");
        entity.Property(e => e.UpdatedAtUtc).HasColumnName("updated_at_utc");
        entity.Property(e => e.Version).HasColumnName("version").IsConcurrencyToken();
        entity.HasIndex(e => new { e.TenantId, e.FacilityId });
    }
}

internal sealed class GoldenDemoCustomsFileDocumentEntityConfiguration : IEntityTypeConfiguration<GoldenDemoCustomsFileDocument>
{
    public void Configure(EntityTypeBuilder<GoldenDemoCustomsFileDocument> entity)
    {
        entity.ToTable("golden_demo_customs_file_documents");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasColumnName("id");
        entity.Property(e => e.TenantId).HasColumnName("tenant_id");
        entity.Property(e => e.FacilityId).HasColumnName("facility_id");
        entity.Property(e => e.OutcomeId).HasColumnName("outcome_id");
        entity.Property(e => e.DocumentType).HasColumnName("document_type").HasMaxLength(60);
        entity.Property(e => e.VersionNumber).HasColumnName("version_number");
        entity.Property(e => e.IsCurrent).HasColumnName("is_current");
        entity.Property(e => e.Status).HasColumnName("status").HasMaxLength(30);
        entity.Property(e => e.ContentJson).HasColumnName("content_json");
        entity.Property(e => e.CreatedByWorkerCode).HasColumnName("created_by_worker_code").HasMaxLength(60);
        entity.Property(e => e.CreatedAtUtc).HasColumnName("created_at_utc");
        entity.HasIndex(e => new { e.OutcomeId, e.DocumentType, e.VersionNumber }).IsUnique();
    }
}

internal sealed class GoldenDemoCustomsFileBlockerEntityConfiguration : IEntityTypeConfiguration<GoldenDemoCustomsFileBlocker>
{
    public void Configure(EntityTypeBuilder<GoldenDemoCustomsFileBlocker> entity)
    {
        entity.ToTable("golden_demo_customs_file_blockers");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasColumnName("id");
        entity.Property(e => e.TenantId).HasColumnName("tenant_id");
        entity.Property(e => e.FacilityId).HasColumnName("facility_id");
        entity.Property(e => e.OutcomeId).HasColumnName("outcome_id");
        entity.Property(e => e.BlockerCode).HasColumnName("blocker_code").HasMaxLength(80);
        entity.Property(e => e.Status).HasColumnName("status").HasMaxLength(30);
        entity.Property(e => e.Reason).HasColumnName("reason");
        entity.Property(e => e.EvidenceJson).HasColumnName("evidence_json");
        entity.Property(e => e.RelatedDocumentId).HasColumnName("related_document_id");
        entity.Property(e => e.DetectedAtUtc).HasColumnName("detected_at_utc");
        entity.Property(e => e.ResolvedAtUtc).HasColumnName("resolved_at_utc");
    }
}

internal sealed class GoldenDemoCustomsFileCorrectiveWorkEntityConfiguration
    : IEntityTypeConfiguration<GoldenDemoCustomsFileCorrectiveWork>
{
    public void Configure(EntityTypeBuilder<GoldenDemoCustomsFileCorrectiveWork> entity)
    {
        entity.ToTable("golden_demo_customs_file_corrective_works");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasColumnName("id");
        entity.Property(e => e.TenantId).HasColumnName("tenant_id");
        entity.Property(e => e.FacilityId).HasColumnName("facility_id");
        entity.Property(e => e.OutcomeId).HasColumnName("outcome_id");
        entity.Property(e => e.BlockerId).HasColumnName("blocker_id");
        entity.Property(e => e.Title).HasColumnName("title").HasMaxLength(200);
        entity.Property(e => e.Status).HasColumnName("status").HasMaxLength(30);
        entity.Property(e => e.ResponsibleRole).HasColumnName("responsible_role").HasMaxLength(100);
        entity.Property(e => e.CompletionEvidenceJson).HasColumnName("completion_evidence_json");
        entity.Property(e => e.RelatedDocumentId).HasColumnName("related_document_id");
        entity.Property(e => e.CreatedAtUtc).HasColumnName("created_at_utc");
        entity.Property(e => e.CompletedAtUtc).HasColumnName("completed_at_utc");
    }
}

internal sealed class GoldenDemoCustomsFilePackageEntityConfiguration : IEntityTypeConfiguration<GoldenDemoCustomsFilePackage>
{
    public void Configure(EntityTypeBuilder<GoldenDemoCustomsFilePackage> entity)
    {
        entity.ToTable("golden_demo_customs_file_packages");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasColumnName("id");
        entity.Property(e => e.TenantId).HasColumnName("tenant_id");
        entity.Property(e => e.FacilityId).HasColumnName("facility_id");
        entity.Property(e => e.OutcomeId).HasColumnName("outcome_id");
        entity.Property(e => e.PackageVersion).HasColumnName("package_version");
        entity.Property(e => e.ManifestJson).HasColumnName("manifest_json");
        entity.Property(e => e.DocumentReferenceIdsJson).HasColumnName("document_reference_ids_json");
        entity.Property(e => e.AssembledAtUtc).HasColumnName("assembled_at_utc");
        entity.HasIndex(e => new { e.OutcomeId, e.PackageVersion }).IsUnique();
    }
}

internal sealed class GoldenDemoCustomsFileVerificationRunEntityConfiguration
    : IEntityTypeConfiguration<GoldenDemoCustomsFileVerificationRun>
{
    public void Configure(EntityTypeBuilder<GoldenDemoCustomsFileVerificationRun> entity)
    {
        entity.ToTable("golden_demo_customs_file_verification_runs");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasColumnName("id");
        entity.Property(e => e.TenantId).HasColumnName("tenant_id");
        entity.Property(e => e.FacilityId).HasColumnName("facility_id");
        entity.Property(e => e.OutcomeId).HasColumnName("outcome_id");
        entity.Property(e => e.Result).HasColumnName("result").HasMaxLength(40);
        entity.Property(e => e.ChecksJson).HasColumnName("checks_json");
        entity.Property(e => e.Summary).HasColumnName("summary");
        entity.Property(e => e.TriggeredBySubjectId).HasColumnName("triggered_by_subject_id").HasMaxLength(200);
        entity.Property(e => e.CreatedAtUtc).HasColumnName("created_at_utc");
    }
}

internal sealed class GoldenDemoCustomsFileActivityEntityConfiguration : IEntityTypeConfiguration<GoldenDemoCustomsFileActivity>
{
    public void Configure(EntityTypeBuilder<GoldenDemoCustomsFileActivity> entity)
    {
        entity.ToTable("golden_demo_customs_file_activities");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasColumnName("id");
        entity.Property(e => e.TenantId).HasColumnName("tenant_id");
        entity.Property(e => e.FacilityId).HasColumnName("facility_id");
        entity.Property(e => e.OutcomeId).HasColumnName("outcome_id");
        entity.Property(e => e.StageCode).HasColumnName("stage_code").HasMaxLength(60);
        entity.Property(e => e.Actor).HasColumnName("actor").HasMaxLength(200);
        entity.Property(e => e.Action).HasColumnName("action");
        entity.Property(e => e.Status).HasColumnName("status").HasMaxLength(40);
        entity.Property(e => e.EvidenceReference).HasColumnName("evidence_reference").HasMaxLength(200);
        entity.Property(e => e.OccurredAtUtc).HasColumnName("occurred_at_utc");
        entity.HasIndex(e => new { e.OutcomeId, e.OccurredAtUtc });
    }
}
