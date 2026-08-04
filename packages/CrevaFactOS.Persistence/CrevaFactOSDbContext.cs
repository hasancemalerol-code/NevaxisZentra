using Microsoft.EntityFrameworkCore;
using CrevaFactOS.Persistence.Tenancy;
using CrevaFactOS.Persistence.Identity;
using CrevaFactOS.Persistence.Authorization;
using CrevaFactOS.Persistence.Audit;

namespace CrevaFactOS.Persistence;

public sealed class CrevaFactOSDbContext : DbContext
{
    public const string MigrationsAssemblyName = "CrevaFactOS.Persistence.Migrations";
    public const string MigrationHistoryTableName = "__EFMigrationsHistory";

    private readonly ITenantContextAccessor? tenantContextAccessor;

    public CrevaFactOSDbContext(
        DbContextOptions<CrevaFactOSDbContext> options,
        ITenantContextAccessor? tenantContextAccessor = null)
        : base(options)
    {
        this.tenantContextAccessor = tenantContextAccessor;
    }

    public DbSet<Tenant> Tenants => Set<Tenant>();

    public DbSet<TenantIsolationProbe> TenantIsolationProbes => Set<TenantIsolationProbe>();

    public DbSet<IdentitySubject> IdentitySubjects => Set<IdentitySubject>();

    public DbSet<TenantMembership> TenantMemberships => Set<TenantMembership>();

    public DbSet<PermissionDefinition> PermissionDefinitions => Set<PermissionDefinition>();

    public DbSet<TenantRole> TenantRoles => Set<TenantRole>();

    public DbSet<TenantRolePermissionAssignment> TenantRolePermissions => Set<TenantRolePermissionAssignment>();

    public DbSet<TenantMembershipRole> TenantMembershipRoles => Set<TenantMembershipRole>();

    public DbSet<AuditRecord> AuditRecords => Set<AuditRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Tenant>(entity =>
        {
            entity.ToTable("tenants");
            entity.HasKey(tenant => tenant.Id);
            entity.Property(tenant => tenant.Id).HasColumnName("id");
            entity.Property(tenant => tenant.TenantKey)
                .HasColumnName("tenant_key")
                .HasMaxLength(100);
            entity.Property(tenant => tenant.DisplayName)
                .HasColumnName("display_name")
                .HasMaxLength(200);
            entity.Property(tenant => tenant.Status)
                .HasColumnName("status")
                .HasConversion<string>()
                .HasMaxLength(20);
            entity.Property(tenant => tenant.CreatedAtUtc).HasColumnName("created_at_utc");
            entity.HasIndex(tenant => tenant.TenantKey).IsUnique();
            entity.ToTable(table => table.HasCheckConstraint(
                "ck_tenants_status",
                "status IN ('Active', 'Inactive')"));
            entity.HasQueryFilter(tenant =>
                tenantContextAccessor != null
                && tenantContextAccessor.IsAvailable
                && tenant.Id == tenantContextAccessor.Current!.TenantId);
        });

        modelBuilder.Entity<TenantIsolationProbe>(entity =>
        {
            entity.ToTable("tenant_isolation_probes");
            entity.HasKey(probe => probe.Id);
            entity.Property(probe => probe.Id).HasColumnName("id");
            entity.Property(probe => probe.TenantId).HasColumnName("tenant_id");
            entity.Property(probe => probe.ProbeValue)
                .HasColumnName("probe_value")
                .HasMaxLength(200);
            entity.HasIndex(probe => probe.TenantId);
            entity.HasOne<Tenant>()
                .WithMany()
                .HasForeignKey(probe => probe.TenantId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasQueryFilter(probe =>
                tenantContextAccessor != null
                && tenantContextAccessor.IsAvailable
                && probe.TenantId == tenantContextAccessor.Current!.TenantId);
        });

        modelBuilder.Entity<IdentitySubject>(entity =>
        {
            entity.ToTable("identity_subjects");
            entity.HasKey(identity => identity.Id);
            entity.Property(identity => identity.Id).HasColumnName("id");
            entity.Property(identity => identity.ExternalIssuer)
                .HasColumnName("external_issuer")
                .HasMaxLength(200);
            entity.Property(identity => identity.ExternalSubject)
                .HasColumnName("external_subject")
                .HasMaxLength(200);
            entity.Property(identity => identity.Status)
                .HasColumnName("status")
                .HasConversion<string>()
                .HasMaxLength(20);
            entity.Property(identity => identity.CreatedAtUtc).HasColumnName("created_at_utc");
            entity.HasIndex(identity => new { identity.ExternalIssuer, identity.ExternalSubject })
                .IsUnique();
            entity.ToTable(table => table.HasCheckConstraint(
                "ck_identity_subjects_status",
                "status IN ('Active', 'Inactive')"));
        });

        modelBuilder.Entity<TenantMembership>(entity =>
        {
            entity.ToTable("tenant_memberships");
            entity.HasKey(membership => membership.Id);
            entity.Property(membership => membership.Id).HasColumnName("id");
            entity.Property(membership => membership.TenantId).HasColumnName("tenant_id");
            entity.Property(membership => membership.IdentitySubjectId).HasColumnName("identity_subject_id");
            entity.Property(membership => membership.Status)
                .HasColumnName("status")
                .HasConversion<string>()
                .HasMaxLength(20);
            entity.Property(membership => membership.CreatedAtUtc).HasColumnName("created_at_utc");
            entity.HasIndex(membership => new { membership.TenantId, membership.IdentitySubjectId })
                .IsUnique();
            entity.HasIndex(membership => new { membership.TenantId, membership.Id })
                .IsUnique();
            entity.HasIndex(membership => membership.IdentitySubjectId);
            entity.HasOne<Tenant>()
                .WithMany()
                .HasForeignKey(membership => membership.TenantId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<IdentitySubject>()
                .WithMany()
                .HasForeignKey(membership => membership.IdentitySubjectId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.ToTable(table => table.HasCheckConstraint(
                "ck_tenant_memberships_status",
                "status IN ('Active', 'Inactive')"));
            entity.HasQueryFilter(membership =>
                tenantContextAccessor != null
                && tenantContextAccessor.IsAvailable
                && membership.TenantId == tenantContextAccessor.Current!.TenantId);
        });

        modelBuilder.Entity<PermissionDefinition>(entity =>
        {
            entity.ToTable("permission_definitions");
            entity.HasKey(permission => permission.Id);
            entity.Property(permission => permission.Id).HasColumnName("id");
            entity.Property(permission => permission.Key).HasColumnName("key").HasMaxLength(150);
            entity.Property(permission => permission.Description).HasColumnName("description").HasMaxLength(300);
            entity.Property(permission => permission.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(20);
            entity.Property(permission => permission.CreatedAtUtc).HasColumnName("created_at_utc");
            entity.HasIndex(permission => permission.Key).IsUnique();
            entity.ToTable(table =>
            {
                table.HasCheckConstraint("ck_permission_definitions_status", "status IN ('Active', 'Inactive')");
                table.HasCheckConstraint(
                    "ck_permission_definitions_key",
                    "key ~ '^[a-z][a-z0-9]*(\\.[a-z][a-z0-9]*)+$'");
            });
        });

        modelBuilder.Entity<TenantRole>(entity =>
        {
            entity.ToTable("tenant_roles");
            entity.HasKey(role => role.Id);
            entity.Property(role => role.Id).HasColumnName("id");
            entity.Property(role => role.TenantId).HasColumnName("tenant_id");
            entity.Property(role => role.Key).HasColumnName("key").HasMaxLength(100);
            entity.Property(role => role.DisplayName).HasColumnName("display_name").HasMaxLength(200);
            entity.Property(role => role.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(20);
            entity.Property(role => role.CreatedAtUtc).HasColumnName("created_at_utc");
            entity.HasIndex(role => new { role.TenantId, role.Key }).IsUnique();
            entity.HasIndex(role => new { role.TenantId, role.Id }).IsUnique();
            entity.HasOne<Tenant>().WithMany().HasForeignKey(role => role.TenantId).OnDelete(DeleteBehavior.Cascade);
            entity.ToTable(table => table.HasCheckConstraint(
                "ck_tenant_roles_status",
                "status IN ('Active', 'Inactive')"));
            entity.HasQueryFilter(role => tenantContextAccessor != null
                && tenantContextAccessor.IsAvailable
                && role.TenantId == tenantContextAccessor.Current!.TenantId);
        });

        modelBuilder.Entity<TenantRolePermissionAssignment>(entity =>
        {
            entity.ToTable("tenant_role_permissions");
            entity.HasKey(item => new { item.TenantRoleId, item.PermissionDefinitionId });
            entity.Property(item => item.TenantId).HasColumnName("tenant_id");
            entity.Property(item => item.TenantRoleId).HasColumnName("tenant_role_id");
            entity.Property(item => item.PermissionDefinitionId).HasColumnName("permission_definition_id");
            entity.HasIndex(item => item.TenantId);
            entity.HasIndex(item => item.PermissionDefinitionId);
            entity.HasOne<TenantRole>().WithMany()
                .HasForeignKey(item => new { item.TenantId, item.TenantRoleId })
                .HasPrincipalKey(role => new { role.TenantId, role.Id })
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<PermissionDefinition>().WithMany()
                .HasForeignKey(item => item.PermissionDefinitionId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasQueryFilter(item => tenantContextAccessor != null
                && tenantContextAccessor.IsAvailable
                && item.TenantId == tenantContextAccessor.Current!.TenantId);
        });

        modelBuilder.Entity<TenantMembershipRole>(entity =>
        {
            entity.ToTable("tenant_membership_roles");
            entity.HasKey(item => new { item.TenantMembershipId, item.TenantRoleId });
            entity.Property(item => item.TenantId).HasColumnName("tenant_id");
            entity.Property(item => item.TenantMembershipId).HasColumnName("tenant_membership_id");
            entity.Property(item => item.TenantRoleId).HasColumnName("tenant_role_id");
            entity.Property(item => item.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(20);
            entity.Property(item => item.AssignedAtUtc).HasColumnName("assigned_at_utc");
            entity.HasIndex(item => item.TenantId);
            entity.HasIndex(item => item.TenantRoleId);
            entity.HasOne<TenantMembership>().WithMany()
                .HasForeignKey(item => new { item.TenantId, item.TenantMembershipId })
                .HasPrincipalKey(membership => new { membership.TenantId, membership.Id })
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<TenantRole>().WithMany()
                .HasForeignKey(item => new { item.TenantId, item.TenantRoleId })
                .HasPrincipalKey(role => new { role.TenantId, role.Id })
                .OnDelete(DeleteBehavior.Cascade);
            entity.ToTable(table => table.HasCheckConstraint(
                "ck_tenant_membership_roles_status",
                "status IN ('Active', 'Inactive')"));
            entity.HasQueryFilter(item => tenantContextAccessor != null
                && tenantContextAccessor.IsAvailable
                && item.TenantId == tenantContextAccessor.Current!.TenantId);
        });

        modelBuilder.Entity<AuditRecord>(entity =>
        {
            entity.ToTable("audit_records");
            entity.HasKey(record => record.Id);
            entity.Property(record => record.Id).HasColumnName("id");
            entity.Property(record => record.TenantId).HasColumnName("tenant_id");
            entity.Property(record => record.IdentitySubjectId).HasColumnName("identity_subject_id");
            entity.Property(record => record.TenantMembershipId).HasColumnName("tenant_membership_id");
            entity.Property(record => record.EventType).HasColumnName("event_type").HasMaxLength(150);
            entity.Property(record => record.Action).HasColumnName("action").HasMaxLength(100);
            entity.Property(record => record.ResourceType).HasColumnName("resource_type").HasMaxLength(100);
            entity.Property(record => record.ResourceId).HasColumnName("resource_id").HasMaxLength(200);
            entity.Property(record => record.Outcome).HasColumnName("outcome").HasConversion<string>().HasMaxLength(20);
            entity.Property(record => record.PermissionKey).HasColumnName("permission_key").HasMaxLength(150);
            entity.Property(record => record.CorrelationId).HasColumnName("correlation_id");
            entity.Property(record => record.CausationId).HasColumnName("causation_id");
            entity.Property(record => record.RequestId).HasColumnName("request_id").HasMaxLength(100);
            entity.Property(record => record.Source).HasColumnName("source").HasConversion<string>().HasMaxLength(20);
            entity.Property(record => record.OccurredAtUtc).HasColumnName("occurred_at_utc");
            entity.Property(record => record.RecordedAtUtc).HasColumnName("recorded_at_utc");
            entity.Property(record => record.MetadataJson).HasColumnName("metadata_json").HasColumnType("jsonb");
            entity.Property(record => record.FailureCode).HasColumnName("failure_code").HasMaxLength(100);
            entity.HasIndex(record => new { record.TenantId, record.OccurredAtUtc });
            entity.HasIndex(record => new { record.TenantId, record.EventType });
            entity.HasIndex(record => new { record.TenantId, record.IdentitySubjectId });
            entity.HasIndex(record => record.CorrelationId);
            entity.HasIndex(record => new { record.ResourceType, record.ResourceId });
            entity.HasOne<Tenant>().WithMany().HasForeignKey(record => record.TenantId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<IdentitySubject>().WithMany().HasForeignKey(record => record.IdentitySubjectId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<TenantMembership>().WithMany().HasForeignKey(record => record.TenantMembershipId).OnDelete(DeleteBehavior.Restrict);
            entity.ToTable(table =>
            {
                table.HasCheckConstraint("ck_audit_records_outcome", "outcome IN ('Success', 'Failure', 'Denied')");
                table.HasCheckConstraint("ck_audit_records_source", "source IN ('Api', 'Worker', 'EdgeAgent', 'System')");
                table.HasCheckConstraint("ck_audit_records_event_type", "event_type ~ '^[a-z][a-z0-9]*(\\.[a-z][a-z0-9]*)+$'");
                table.HasCheckConstraint("ck_audit_records_action", "action ~ '^[a-z][a-z0-9]*(\\.[a-z][a-z0-9]*)*$'");
                table.HasCheckConstraint(
                    "ck_audit_records_event_taxonomy",
                    "event_type IN ('authentication.succeeded', 'authentication.failed', 'tenant.access.granted', 'tenant.access.denied', 'authorization.granted', 'authorization.denied', 'audit.write.failed', 'infrastructure.configuration.rejected')");
                table.HasCheckConstraint(
                    "ck_audit_records_action_taxonomy",
                    "action IN ('authenticate', 'access.tenant', 'authorize', 'write.audit', 'validate.configuration')");
                table.HasCheckConstraint("ck_audit_records_resource_type", "resource_type ~ '^[a-z][a-z0-9]*(\\.[a-z][a-z0-9]*)*$'");
                table.HasCheckConstraint("ck_audit_records_correlation", "correlation_id <> '00000000-0000-0000-0000-000000000000'::uuid");
                table.HasCheckConstraint("ck_audit_records_metadata", "jsonb_typeof(metadata_json) = 'object' AND octet_length(metadata_json::text) <= 4096");
                table.HasCheckConstraint(
                    "ck_audit_records_actor_boundary",
                    "(tenant_id IS NOT NULL AND identity_subject_id IS NOT NULL AND tenant_membership_id IS NOT NULL) OR (tenant_id IS NULL AND identity_subject_id IS NULL AND tenant_membership_id IS NULL AND permission_key IS NULL AND source = 'System')");
            });
            entity.HasQueryFilter(record => tenantContextAccessor != null
                && tenantContextAccessor.IsAvailable
                && record.TenantId == tenantContextAccessor.Current!.TenantId);
        });
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        var tenantId = PrepareTenantScopedChanges();
        if (tenantId is null)
        {
            return base.SaveChanges(acceptAllChangesOnSuccess);
        }

        var ownsTransaction = Database.CurrentTransaction is null;
        using var transaction = ownsTransaction ? Database.BeginTransaction() : null;
        SetTenantSessionContext(tenantId.Value);
        var result = base.SaveChanges(acceptAllChangesOnSuccess);
        transaction?.Commit();
        return result;
    }

    public override async Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        var tenantId = PrepareTenantScopedChanges();
        if (tenantId is null)
        {
            return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }

        var ownsTransaction = Database.CurrentTransaction is null;
        await using var transaction = ownsTransaction
            ? await Database.BeginTransactionAsync(cancellationToken)
            : null;
        await SetTenantSessionContextAsync(tenantId.Value, cancellationToken);
        var result = await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        if (transaction is not null)
        {
            await transaction.CommitAsync(cancellationToken);
        }

        return result;
    }

    internal void SetTenantSessionContext(Guid tenantId)
    {
        Database.ExecuteSqlInterpolated(
            $"SELECT set_config('crevafactos.tenant_id', {tenantId.ToString()}, true)");
    }

    internal Task SetTenantSessionContextAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        return Database.ExecuteSqlInterpolatedAsync(
            $"SELECT set_config('crevafactos.tenant_id', {tenantId.ToString()}, true)",
            cancellationToken);
    }

    internal Guid? PrepareTenantScopedChanges()
    {
        var entries = ChangeTracker.Entries<ITenantScopedEntity>()
            .Where(entry => entry.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .ToArray();
        if (entries.Length == 0)
        {
            return null;
        }

        var tenantId = tenantContextAccessor?.RequireTenantId()
            ?? throw new InvalidOperationException("Tenant context is required to save tenant-scoped records.");

        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Added)
            {
                if (entry.Entity.TenantId != Guid.Empty && entry.Entity.TenantId != tenantId)
                {
                    throw new InvalidOperationException("A tenant-scoped record cannot be attached to another tenant.");
                }

                entry.Entity.TenantId = tenantId;
                continue;
            }

            var tenantProperty = entry.Property(nameof(ITenantScopedEntity.TenantId));
            if (tenantProperty.IsModified || entry.Entity.TenantId != tenantId)
            {
                throw new InvalidOperationException("The tenant identifier of an existing record cannot be changed.");
            }
        }

        return tenantId;
    }
}
