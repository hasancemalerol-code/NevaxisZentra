using CrevaFactOS.Persistence.Export;
using CrevaFactOS.Persistence.Facilities;
using CrevaFactOS.Persistence.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CrevaFactOS.Persistence.Configurations;

internal sealed class ExportDocumentEntityConfiguration : IEntityTypeConfiguration<ExportDocument>
{
    private static readonly Dictionary<ExportDocumentStatus, string> DocumentStatusValues =
        new()
        {
            [ExportDocumentStatus.Draft] = "draft",
            [ExportDocumentStatus.Received] = "received",
            [ExportDocumentStatus.Verified] = "verified",
            [ExportDocumentStatus.Rejected] = "rejected",
        };

    public void Configure(EntityTypeBuilder<ExportDocument> entity)
    {
        entity.ToTable("export_documents");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasColumnName("id");
        entity.Property(e => e.TenantId).HasColumnName("tenant_id");
        entity.Property(e => e.FacilityId).HasColumnName("facility_id");
        entity.Property(e => e.ExportCaseId).HasColumnName("export_case_id");
        entity.Property(e => e.DocumentType).HasColumnName("document_type").HasMaxLength(60);
        entity.Property(e => e.DocumentStatus)
            .HasColumnName("document_status")
            .HasConversion(
                value => DocumentStatusValues[value],
                value => DocumentStatusValues.First(p => p.Value == value).Key)
            .HasMaxLength(20);
        entity.Property(e => e.DisplayName).HasColumnName("display_name").HasMaxLength(300);
        entity.Property(e => e.FileReference).HasColumnName("file_reference").HasMaxLength(500);
        entity.Property(e => e.DocumentNumber).HasColumnName("document_number").HasMaxLength(100);
        entity.Property(e => e.IssuedAtUtc).HasColumnName("issued_at_utc");
        entity.Property(e => e.ExpiresAtUtc).HasColumnName("expires_at_utc");
        entity.Property(e => e.Source).HasColumnName("source").HasMaxLength(100);
        entity.Property(e => e.RejectionReason).HasColumnName("rejection_reason").HasMaxLength(1000);
        entity.Property(e => e.VerifiedAtUtc).HasColumnName("verified_at_utc");
        entity.Property(e => e.VerifiedBySubjectId).HasColumnName("verified_by_subject_id").HasMaxLength(200);
        entity.Property(e => e.CreatedAtUtc).HasColumnName("created_at_utc");
        entity.Property(e => e.CreatedBySubjectId).HasColumnName("created_by_subject_id").HasMaxLength(200);
        entity.Property(e => e.UpdatedAtUtc).HasColumnName("updated_at_utc");
        entity.Property(e => e.LastModifiedBySubjectId).HasColumnName("last_modified_by_subject_id").HasMaxLength(200);
        entity.Property(e => e.Version).HasColumnName("version").IsConcurrencyToken();

        entity.HasIndex(e => new { e.ExportCaseId, e.DocumentType })
            .HasDatabaseName("ix_export_documents_case_type");
        entity.HasIndex(e => new { e.TenantId, e.FacilityId, e.ExportCaseId })
            .HasDatabaseName("ix_export_documents_tenant_facility_case");

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

        entity.ToTable(table => table.HasCheckConstraint(
            "ck_export_documents_status",
            "document_status IN ('draft', 'received', 'verified', 'rejected')"));
        entity.ToTable(table => table.HasCheckConstraint(
            "ck_export_documents_type",
            "document_type <> ''"));
        entity.ToTable(table => table.HasCheckConstraint(
            "ck_export_documents_display_name",
            "display_name <> ''"));
        entity.ToTable(table => table.HasCheckConstraint(
            "ck_export_documents_version",
            "version >= 1"));
    }
}
