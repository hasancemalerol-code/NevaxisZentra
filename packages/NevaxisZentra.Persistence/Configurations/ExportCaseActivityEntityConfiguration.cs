using NevaxisZentra.Persistence.Export;
using NevaxisZentra.Persistence.Facilities;
using NevaxisZentra.Persistence.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace NevaxisZentra.Persistence.Configurations;

internal sealed class ExportCaseActivityEntityConfiguration : IEntityTypeConfiguration<ExportCaseActivity>
{
    private static readonly Dictionary<ExportActivitySourceType, string> SourceTypeValues =
        new()
        {
            [ExportActivitySourceType.System] = "system",
            [ExportActivitySourceType.User] = "user",
            [ExportActivitySourceType.Integration] = "integration",
            [ExportActivitySourceType.Ai] = "ai",
        };

    public void Configure(EntityTypeBuilder<ExportCaseActivity> entity)
    {
        entity.ToTable("export_case_activities");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasColumnName("id");
        entity.Property(e => e.TenantId).HasColumnName("tenant_id");
        entity.Property(e => e.FacilityId).HasColumnName("facility_id");
        entity.Property(e => e.ExportCaseId).HasColumnName("export_case_id");
        entity.Property(e => e.EventType).HasColumnName("event_type").HasMaxLength(80);
        entity.Property(e => e.Title).HasColumnName("title").HasMaxLength(300);
        entity.Property(e => e.Summary).HasColumnName("summary").HasMaxLength(2000);
        entity.Property(e => e.OccurredAtUtc).HasColumnName("occurred_at_utc");
        entity.Property(e => e.SourceType)
            .HasColumnName("source_type")
            .HasConversion(
                value => SourceTypeValues[value],
                value => SourceTypeValues.First(p => p.Value == value).Key)
            .HasMaxLength(20);
        entity.Property(e => e.ActorSubjectId).HasColumnName("actor_subject_id").HasMaxLength(200);
        entity.Property(e => e.RelatedEntityType).HasColumnName("related_entity_type").HasMaxLength(80);
        entity.Property(e => e.RelatedEntityId).HasColumnName("related_entity_id");
        entity.Property(e => e.CorrelationId).HasColumnName("correlation_id");

        entity.HasIndex(e => new { e.ExportCaseId, e.OccurredAtUtc })
            .HasDatabaseName("ix_export_case_activities_case_occurred");
        entity.HasIndex(e => new { e.TenantId, e.FacilityId, e.ExportCaseId })
            .HasDatabaseName("ix_export_case_activities_tenant_facility_case");

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
            "ck_export_case_activities_source_type",
            "source_type IN ('system', 'user', 'integration', 'ai')"));
        entity.ToTable(table => table.HasCheckConstraint(
            "ck_export_case_activities_event_type",
            "event_type <> ''"));
        entity.ToTable(table => table.HasCheckConstraint(
            "ck_export_case_activities_title",
            "title <> ''"));
    }
}
