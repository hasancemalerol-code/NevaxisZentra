using NevaxisFactOS.Persistence.AiUsage;
using NevaxisFactOS.Persistence.Facilities;
using NevaxisFactOS.Persistence.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace NevaxisFactOS.Persistence.Configurations;

internal sealed class AiUsageRecordEntityConfiguration
    : IEntityTypeConfiguration<AiUsageRecord>
{
    public void Configure(EntityTypeBuilder<AiUsageRecord> entity)
    {
        entity.ToTable("ai_usage_records");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasColumnName("id");
        entity.Property(e => e.TenantId).HasColumnName("tenant_id");
        entity.Property(e => e.FacilityId).HasColumnName("facility_id");
        entity.Property(e => e.UserSubjectId).HasColumnName("user_subject_id").HasMaxLength(200);
        entity.Property(e => e.Module).HasColumnName("module").HasMaxLength(50);
        entity.Property(e => e.Agent).HasColumnName("agent").HasMaxLength(100);
        entity.Property(e => e.RunId).HasColumnName("run_id");
        entity.Property(e => e.Provider).HasColumnName("provider").HasMaxLength(100);
        entity.Property(e => e.Model).HasColumnName("model").HasMaxLength(150);
        entity.Property(e => e.InputUsage).HasColumnName("input_usage");
        entity.Property(e => e.OutputUsage).HasColumnName("output_usage");
        entity.Property(e => e.CreditsConsumed).HasColumnName("credits_consumed").HasPrecision(14, 4);
        entity.Property(e => e.EstimatedProviderCost).HasColumnName("estimated_provider_cost").HasPrecision(14, 6);
        entity.Property(e => e.CreatedAtUtc).HasColumnName("created_at_utc");

        entity.HasIndex(e => new { e.TenantId, e.FacilityId, e.CreatedAtUtc })
            .HasDatabaseName("ix_ai_usage_records_tenant_facility_created");
        entity.HasIndex(e => new { e.TenantId, e.FacilityId, e.RunId })
            .HasDatabaseName("ix_ai_usage_records_tenant_facility_run");
        entity.HasIndex(e => new { e.TenantId, e.Module, e.Agent })
            .HasDatabaseName("ix_ai_usage_records_tenant_module_agent");

        entity.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(e => e.TenantId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Facility>()
            .WithMany()
            .HasForeignKey(e => new { e.TenantId, e.FacilityId })
            .HasPrincipalKey(f => new { f.TenantId, f.Id })
            .OnDelete(DeleteBehavior.Restrict);

        entity.ToTable(table => table.HasCheckConstraint(
            "ck_ai_usage_records_usage",
            "input_usage >= 0 AND output_usage >= 0"));
        entity.ToTable(table => table.HasCheckConstraint(
            "ck_ai_usage_records_credits",
            "credits_consumed >= 0"));
        entity.ToTable(table => table.HasCheckConstraint(
            "ck_ai_usage_records_module",
            "module <> ''"));
        entity.ToTable(table => table.HasCheckConstraint(
            "ck_ai_usage_records_agent",
            "agent <> ''"));
    }
}
