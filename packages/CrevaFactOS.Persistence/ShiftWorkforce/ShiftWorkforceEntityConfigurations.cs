using CrevaFactOS.Persistence.Facilities;
using CrevaFactOS.Persistence.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CrevaFactOS.Persistence.ShiftWorkforce;

internal sealed class ShiftWorkforcePersonEntityConfiguration : IEntityTypeConfiguration<ShiftWorkforcePerson>
{
    public void Configure(EntityTypeBuilder<ShiftWorkforcePerson> entity)
    {
        entity.ToTable("shift_workforce_persons");
        entity.HasKey(p => p.Id);
        entity.Property(p => p.Id).HasColumnName("id");
        entity.Property(p => p.TenantId).HasColumnName("tenant_id");
        entity.Property(p => p.FacilityId).HasColumnName("facility_id");
        entity.Property(p => p.SourceSystem).HasColumnName("source_system").HasMaxLength(50).IsRequired();
        entity.Property(p => p.ExternalEmployeeRef).HasColumnName("external_employee_ref").HasMaxLength(200).IsRequired();
        entity.Property(p => p.EmployeeCode).HasColumnName("employee_code").HasMaxLength(100).IsRequired();
        entity.Property(p => p.EmployeeCodeNormalized).HasColumnName("employee_code_normalized").HasMaxLength(100).IsRequired();
        entity.Property(p => p.DisplayName).HasColumnName("display_name").HasMaxLength(200).IsRequired();
        entity.Property(p => p.Department).HasColumnName("department").HasMaxLength(200);
        entity.Property(p => p.JobTitle).HasColumnName("job_title").HasMaxLength(200);
        entity.Property(p => p.IsActive).HasColumnName("is_active");
        entity.Property(p => p.AvailabilityState).HasColumnName("availability_state").HasMaxLength(30).IsRequired();
        entity.Property(p => p.SourceShiftReference).HasColumnName("source_shift_reference").HasMaxLength(200);
        entity.Property(p => p.SourceCalendarReference).HasColumnName("source_calendar_reference").HasMaxLength(200);
        entity.Property(p => p.SourceUpdatedAtUtc).HasColumnName("source_updated_at_utc");
        entity.Property(p => p.SynchronizedAtUtc).HasColumnName("synchronized_at_utc");
        entity.Property(p => p.SourceFingerprint).HasColumnName("source_fingerprint").HasMaxLength(64);
        entity.Property(p => p.FreshnessState).HasColumnName("freshness_state").HasMaxLength(20).IsRequired();
        entity.Property(p => p.SourceLimitationsJson).HasColumnName("source_limitations_json").IsRequired();
        entity.Property(p => p.CreatedAtUtc).HasColumnName("created_at_utc");
        entity.Property(p => p.UpdatedAtUtc).HasColumnName("updated_at_utc");
        entity.Property(p => p.Version).HasColumnName("version").IsConcurrencyToken();
        entity.HasOne<Tenant>().WithMany().HasForeignKey(p => p.TenantId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Facility>().WithMany()
            .HasForeignKey(p => new { p.TenantId, p.FacilityId })
            .HasPrincipalKey(f => new { f.TenantId, f.Id })
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasIndex(p => new { p.TenantId, p.SourceSystem, p.ExternalEmployeeRef })
            .HasDatabaseName("ix_shift_workforce_persons_tenant_source_external_ref")
            .IsUnique();
        entity.HasIndex(p => new { p.TenantId, p.SourceSystem, p.EmployeeCodeNormalized })
            .HasDatabaseName("ix_shift_workforce_persons_tenant_source_employee_code")
            .IsUnique();
        entity.HasIndex(p => new { p.TenantId, p.FacilityId, p.IsActive })
            .HasDatabaseName("ix_shift_workforce_persons_tenant_facility_active");
        entity.HasIndex(p => new { p.TenantId, p.FacilityId, p.AvailabilityState })
            .HasDatabaseName("ix_shift_workforce_persons_tenant_facility_availability");
        entity.HasIndex(p => new { p.TenantId, p.FacilityId, p.FreshnessState })
            .HasDatabaseName("ix_shift_workforce_persons_tenant_facility_freshness");
    }
}

internal sealed class ShiftWorkforceCompetencyDefinitionEntityConfiguration : IEntityTypeConfiguration<ShiftWorkforceCompetencyDefinition>
{
    public void Configure(EntityTypeBuilder<ShiftWorkforceCompetencyDefinition> entity)
    {
        entity.ToTable("shift_workforce_competency_definitions");
        entity.HasKey(d => d.Id);
        entity.Property(d => d.Id).HasColumnName("id");
        entity.Property(d => d.TenantId).HasColumnName("tenant_id");
        entity.Property(d => d.Code).HasColumnName("code").HasMaxLength(100).IsRequired();
        entity.Property(d => d.DisplayLabelTr).HasColumnName("display_label_tr").HasMaxLength(200).IsRequired();
        entity.Property(d => d.DescriptionTr).HasColumnName("description_tr").HasMaxLength(500);
        entity.Property(d => d.IsActive).HasColumnName("is_active");
        entity.Property(d => d.SortOrder).HasColumnName("sort_order");
        entity.HasOne<Tenant>().WithMany().HasForeignKey(d => d.TenantId).OnDelete(DeleteBehavior.Restrict);
        entity.HasIndex(d => new { d.TenantId, d.Code })
            .HasDatabaseName("ix_shift_workforce_competency_definitions_tenant_code")
            .IsUnique();
    }
}

internal sealed class ShiftWorkforceCompetencyAssignmentEntityConfiguration : IEntityTypeConfiguration<ShiftWorkforceCompetencyAssignment>
{
    public void Configure(EntityTypeBuilder<ShiftWorkforceCompetencyAssignment> entity)
    {
        entity.ToTable("shift_workforce_competency_assignments");
        entity.HasKey(a => a.Id);
        entity.Property(a => a.Id).HasColumnName("id");
        entity.Property(a => a.TenantId).HasColumnName("tenant_id");
        entity.Property(a => a.FacilityId).HasColumnName("facility_id");
        entity.Property(a => a.WorkforcePersonId).HasColumnName("workforce_person_id");
        entity.Property(a => a.CompetencyDefinitionId).HasColumnName("competency_definition_id");
        entity.Property(a => a.MachineFamilyScope).HasColumnName("machine_family_scope").HasMaxLength(200);
        entity.Property(a => a.ProcessScope).HasColumnName("process_scope").HasMaxLength(200);
        entity.Property(a => a.MaterialScope).HasColumnName("material_scope").HasMaxLength(200);
        entity.Property(a => a.EquipmentScope).HasColumnName("equipment_scope").HasMaxLength(200);
        entity.Property(a => a.CompetencyLevel).HasColumnName("competency_level").HasMaxLength(30).IsRequired();
        entity.Property(a => a.VerificationStatus).HasColumnName("verification_status").HasMaxLength(30).IsRequired();
        entity.Property(a => a.ValidFromUtc).HasColumnName("valid_from_utc");
        entity.Property(a => a.ValidUntilUtc).HasColumnName("valid_until_utc");
        entity.Property(a => a.VerifiedBySubjectId).HasColumnName("verified_by_subject_id").HasMaxLength(200);
        entity.Property(a => a.VerifiedAtUtc).HasColumnName("verified_at_utc");
        entity.Property(a => a.OperationalNote).HasColumnName("operational_note").HasMaxLength(1000);
        entity.Property(a => a.Version).HasColumnName("version").IsConcurrencyToken();
        entity.Property(a => a.CreateIdempotencyKey).HasColumnName("create_idempotency_key").HasMaxLength(128);
        entity.Property(a => a.CreateIdempotencyPayloadFingerprint).HasColumnName("create_idempotency_payload_fingerprint").HasMaxLength(64);
        entity.Property(a => a.CreatedAtUtc).HasColumnName("created_at_utc");
        entity.Property(a => a.UpdatedAtUtc).HasColumnName("updated_at_utc");
        entity.Property(a => a.CreatedBy).HasColumnName("created_by").HasMaxLength(200).IsRequired();
        entity.Property(a => a.UpdatedBy).HasColumnName("updated_by").HasMaxLength(200).IsRequired();
        entity.HasOne(a => a.WorkforcePerson)
            .WithMany(p => p.CompetencyAssignments)
            .HasForeignKey(a => a.WorkforcePersonId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(a => a.CompetencyDefinition)
            .WithMany(d => d.Assignments)
            .HasForeignKey(a => a.CompetencyDefinitionId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Tenant>().WithMany().HasForeignKey(a => a.TenantId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Facility>().WithMany()
            .HasForeignKey(a => new { a.TenantId, a.FacilityId })
            .HasPrincipalKey(f => new { f.TenantId, f.Id })
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasIndex(a => new { a.TenantId, a.WorkforcePersonId, a.CreateIdempotencyKey })
            .HasDatabaseName("ix_shift_workforce_competency_assignments_tenant_person_idempotency")
            .IsUnique()
            .HasFilter("create_idempotency_key IS NOT NULL");
        entity.HasIndex(a => new { a.TenantId, a.FacilityId, a.WorkforcePersonId })
            .HasDatabaseName("ix_shift_workforce_competency_assignments_tenant_facility_person");
    }
}

internal sealed class ShiftWorkforceAuditEventEntityConfiguration : IEntityTypeConfiguration<ShiftWorkforceAuditEvent>
{
    public void Configure(EntityTypeBuilder<ShiftWorkforceAuditEvent> entity)
    {
        entity.ToTable("shift_workforce_audit_events");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.Id).HasColumnName("id");
        entity.Property(e => e.TenantId).HasColumnName("tenant_id");
        entity.Property(e => e.FacilityId).HasColumnName("facility_id");
        entity.Property(e => e.EventType).HasColumnName("event_type").HasMaxLength(100).IsRequired();
        entity.Property(e => e.EntityType).HasColumnName("entity_type").HasMaxLength(100).IsRequired();
        entity.Property(e => e.EntityId).HasColumnName("entity_id");
        entity.Property(e => e.PayloadJson).HasColumnName("payload_json").IsRequired();
        entity.Property(e => e.ActorSubjectId).HasColumnName("actor_subject_id").HasMaxLength(200).IsRequired();
        entity.Property(e => e.OccurredAtUtc).HasColumnName("occurred_at_utc");
        entity.Property(e => e.CorrelationId).HasColumnName("correlation_id");
        entity.HasOne<Tenant>().WithMany().HasForeignKey(e => e.TenantId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<Facility>().WithMany()
            .HasForeignKey(e => new { e.TenantId, e.FacilityId })
            .HasPrincipalKey(f => new { f.TenantId, f.Id })
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasIndex(e => new { e.TenantId, e.FacilityId, e.OccurredAtUtc })
            .HasDatabaseName("ix_shift_workforce_audit_events_tenant_facility_occurred");
        entity.HasIndex(e => new { e.TenantId, e.EntityType, e.EntityId })
            .HasDatabaseName("ix_shift_workforce_audit_events_tenant_entity");
    }
}
