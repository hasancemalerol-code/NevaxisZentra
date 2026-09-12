using NevaxisFactOS.Persistence.Authorization;
using NevaxisFactOS.Persistence.Identity;
using NevaxisFactOS.Persistence.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace NevaxisFactOS.Persistence.AdminAccess;

internal sealed class TenantMembershipPermissionOverrideEntityConfiguration
    : IEntityTypeConfiguration<TenantMembershipPermissionOverride>
{
    public void Configure(EntityTypeBuilder<TenantMembershipPermissionOverride> entity)
    {
        entity.ToTable("tenant_membership_permission_overrides");
        entity.HasKey(item => item.Id);
        entity.Property(item => item.Id).HasColumnName("id");
        entity.Property(item => item.TenantId).HasColumnName("tenant_id");
        entity.Property(item => item.TenantMembershipId).HasColumnName("tenant_membership_id");
        entity.Property(item => item.PermissionDefinitionId).HasColumnName("permission_definition_id");
        entity.Property(item => item.Effect).HasColumnName("effect").HasMaxLength(20);
        entity.Property(item => item.RecordScope).HasColumnName("record_scope").HasMaxLength(20);
        entity.Property(item => item.EffectiveFromUtc).HasColumnName("effective_from_utc");
        entity.Property(item => item.EffectiveUntilUtc).HasColumnName("effective_until_utc");
        entity.Property(item => item.Status).HasColumnName("status").HasMaxLength(20);
        entity.Property(item => item.CreatedAtUtc).HasColumnName("created_at_utc");
        entity.Property(item => item.UpdatedAtUtc).HasColumnName("updated_at_utc");
        entity.Property(item => item.Version).HasColumnName("version").IsConcurrencyToken();
        entity.HasIndex(item => new { item.TenantId, item.TenantMembershipId, item.PermissionDefinitionId })
            .IsUnique()
            .HasDatabaseName("ux_tenant_membership_permission_overrides_grant");
        entity.HasIndex(item => new { item.TenantId, item.TenantMembershipId, item.Effect, item.Status })
            .HasDatabaseName("ix_tenant_membership_permission_overrides_lookup");
        entity.HasOne<Tenant>().WithMany().HasForeignKey(item => item.TenantId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<TenantMembership>().WithMany()
            .HasForeignKey(item => new { item.TenantId, item.TenantMembershipId })
            .HasPrincipalKey(membership => new { membership.TenantId, membership.Id })
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<PermissionDefinition>().WithMany()
            .HasForeignKey(item => item.PermissionDefinitionId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.ToTable(table =>
        {
            table.HasCheckConstraint("ck_tenant_membership_permission_overrides_effect", "effect IN ('allow', 'deny')");
            table.HasCheckConstraint("ck_tenant_membership_permission_overrides_status", "status IN ('Active', 'Inactive', 'Retired')");
            table.HasCheckConstraint(
                "ck_tenant_membership_permission_overrides_scope",
                "record_scope IS NULL OR record_scope IN ('own', 'assigned', 'facility', 'tenant')");
        });
    }
}

internal sealed class AuthorizationAuditEventEntityConfiguration
    : IEntityTypeConfiguration<AuthorizationAuditEvent>
{
    public void Configure(EntityTypeBuilder<AuthorizationAuditEvent> entity)
    {
        entity.ToTable("authorization_audit_events");
        entity.HasKey(item => item.Id);
        entity.Property(item => item.Id).HasColumnName("id");
        entity.Property(item => item.TenantId).HasColumnName("tenant_id");
        entity.Property(item => item.EventType).HasColumnName("event_type").HasMaxLength(150);
        entity.Property(item => item.EntityType).HasColumnName("entity_type").HasMaxLength(80);
        entity.Property(item => item.EntityReference).HasColumnName("entity_reference").HasMaxLength(120);
        entity.Property(item => item.ActorReference).HasColumnName("actor_reference").HasMaxLength(120);
        entity.Property(item => item.BeforeJson).HasColumnName("before_json");
        entity.Property(item => item.AfterJson).HasColumnName("after_json");
        entity.Property(item => item.CorrelationId).HasColumnName("correlation_id");
        entity.Property(item => item.CreatedAtUtc).HasColumnName("created_at_utc");
        entity.HasIndex(item => new { item.TenantId, item.CreatedAtUtc })
            .HasDatabaseName("ix_authorization_audit_events_tenant_created");
        entity.HasIndex(item => new { item.TenantId, item.EntityType, item.EntityReference })
            .HasDatabaseName("ix_authorization_audit_events_entity");
        entity.HasOne<Tenant>().WithMany().HasForeignKey(item => item.TenantId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class AdminAccessIdempotencyRecordEntityConfiguration
    : IEntityTypeConfiguration<AdminAccessIdempotencyRecord>
{
    public void Configure(EntityTypeBuilder<AdminAccessIdempotencyRecord> entity)
    {
        entity.ToTable("admin_access_idempotency_records");
        entity.HasKey(item => item.Id);
        entity.Property(item => item.Id).HasColumnName("id");
        entity.Property(item => item.TenantId).HasColumnName("tenant_id");
        entity.Property(item => item.IdempotencyKey).HasColumnName("idempotency_key").HasMaxLength(120);
        entity.Property(item => item.RequestHash).HasColumnName("request_hash").HasMaxLength(64);
        entity.Property(item => item.Operation).HasColumnName("operation").HasMaxLength(80);
        entity.Property(item => item.CreatedAtUtc).HasColumnName("created_at_utc");
        entity.HasIndex(item => new { item.TenantId, item.IdempotencyKey })
            .IsUnique()
            .HasDatabaseName("ux_admin_access_idempotency_key");
        entity.HasOne<Tenant>().WithMany().HasForeignKey(item => item.TenantId).OnDelete(DeleteBehavior.Restrict);
    }
}
