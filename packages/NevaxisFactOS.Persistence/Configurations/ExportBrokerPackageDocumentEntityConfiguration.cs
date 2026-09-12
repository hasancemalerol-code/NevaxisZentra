using NevaxisFactOS.Persistence.Export;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace NevaxisFactOS.Persistence.Configurations;

internal sealed class ExportBrokerPackageDocumentEntityConfiguration
    : IEntityTypeConfiguration<ExportBrokerPackageDocument>
{
    public void Configure(EntityTypeBuilder<ExportBrokerPackageDocument> entity)
    {
        entity.ToTable("export_broker_package_documents");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasColumnName("id");
        entity.Property(e => e.PackageVersionId).HasColumnName("package_version_id");
        entity.Property(e => e.DocumentId).HasColumnName("document_id");
        entity.Property(e => e.DocumentType).HasColumnName("document_type").HasMaxLength(60);
        entity.Property(e => e.DocumentStatusSnapshot).HasColumnName("document_status_snapshot").HasMaxLength(20);
        entity.Property(e => e.DisplayNameSnapshot).HasColumnName("display_name_snapshot").HasMaxLength(300);
        entity.Property(e => e.Included).HasColumnName("included");

        entity.HasIndex(e => e.PackageVersionId)
            .HasDatabaseName("ix_export_broker_package_documents_version");
        entity.HasIndex(e => new { e.PackageVersionId, e.DocumentId })
            .IsUnique()
            .HasDatabaseName("uix_export_broker_package_documents_version_document");

        entity.HasOne<ExportBrokerPackageVersion>()
            .WithMany()
            .HasForeignKey(e => e.PackageVersionId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.ToTable(table => table.HasCheckConstraint(
            "ck_export_broker_package_documents_type",
            "document_type <> ''"));
    }
}
