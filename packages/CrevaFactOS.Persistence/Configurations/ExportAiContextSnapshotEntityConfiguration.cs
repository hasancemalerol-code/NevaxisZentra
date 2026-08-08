using CrevaFactOS.Persistence.Export;
using CrevaFactOS.Persistence.Facilities;
using CrevaFactOS.Persistence.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CrevaFactOS.Persistence.Configurations;

internal sealed class ExportAiContextSnapshotEntityConfiguration
    : IEntityTypeConfiguration<ExportAiContextSnapshot>
{
    public void Configure(EntityTypeBuilder<ExportAiContextSnapshot> entity)
    {
        entity.ToTable("export_ai_context_snapshots");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasColumnName("id");
        entity.Property(e => e.TenantId).HasColumnName("tenant_id");
        entity.Property(e => e.FacilityId).HasColumnName("facility_id");
        entity.Property(e => e.ExportCaseId).HasColumnName("export_case_id");
        entity.Property(e => e.CountryResearchCaseId).HasColumnName("country_research_case_id");
        entity.Property(e => e.SnapshotVersion).HasColumnName("snapshot_version");
        entity.Property(e => e.ContextHash).HasColumnName("context_hash").HasMaxLength(128);
        entity.Property(e => e.ContextJson).HasColumnName("context_json").HasColumnType("jsonb");
        entity.Property(e => e.CreatedAtUtc).HasColumnName("created_at_utc");
        entity.Property(e => e.CreatedBySubjectId).HasColumnName("created_by_subject_id").HasMaxLength(200);

        entity.HasIndex(e => new { e.TenantId, e.FacilityId, e.ExportCaseId, e.SnapshotVersion })
            .HasDatabaseName("ix_export_ai_context_snapshots_tenant_facility_case_version");
        entity.HasIndex(e => new { e.TenantId, e.FacilityId, e.CountryResearchCaseId, e.SnapshotVersion })
            .HasDatabaseName("ix_export_ai_context_snapshots_tenant_facility_research_version");
        entity.HasIndex(e => e.ContextHash)
            .HasDatabaseName("ix_export_ai_context_snapshots_context_hash");

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

        entity.ToTable(table => table.HasCheckConstraint(
            "ck_export_ai_context_snapshots_scope",
            "export_case_id IS NOT NULL OR country_research_case_id IS NOT NULL"));
        entity.ToTable(table => table.HasCheckConstraint(
            "ck_export_ai_context_snapshots_snapshot_version",
            "snapshot_version >= 1"));
        entity.ToTable(table => table.HasCheckConstraint(
            "ck_export_ai_context_snapshots_context_hash",
            "context_hash <> ''"));
        entity.ToTable(table => table.HasCheckConstraint(
            "ck_export_ai_context_snapshots_context_json",
            "jsonb_typeof(context_json) = 'object'"));
    }
}
