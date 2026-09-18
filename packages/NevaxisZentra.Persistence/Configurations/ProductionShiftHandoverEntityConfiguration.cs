using NevaxisZentra.Persistence.Production;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace NevaxisZentra.Persistence.Configurations;

public sealed class ProductionShiftHandoverEntityConfiguration
    : IEntityTypeConfiguration<ProductionShiftHandover>
{
    public void Configure(EntityTypeBuilder<ProductionShiftHandover> builder)
    {
        builder.ToTable("production_shift_handovers");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.TenantId).HasColumnName("tenant_id");
        builder.Property(e => e.FacilityId).HasColumnName("facility_id");
        builder.Property(e => e.ProductionOrderId).HasColumnName("production_order_id");
        builder.Property(e => e.FromShiftAssignmentId).HasColumnName("from_shift_assignment_id");
        builder.Property(e => e.ToShiftAssignmentId).HasColumnName("to_shift_assignment_id");
        builder.Property(e => e.Note).HasColumnName("note").HasMaxLength(2000);
        builder.Property(e => e.OpenIssuesSummary).HasColumnName("open_issues_summary").HasMaxLength(2000);
        builder.Property(e => e.CreatedAtUtc).HasColumnName("created_at_utc");
        builder.Property(e => e.CreatedBySubjectId).HasColumnName("created_by_subject_id").HasMaxLength(200);
        builder.Property(e => e.CorrelationId).HasColumnName("correlation_id");

        builder.HasIndex(e => new { e.ProductionOrderId, e.CreatedAtUtc })
            .HasDatabaseName("ix_production_shift_handovers_order_created");
    }
}
