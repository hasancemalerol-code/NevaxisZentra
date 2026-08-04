using Microsoft.EntityFrameworkCore;
using CrevaFactOS.Persistence.Tenancy;
using CrevaFactOS.Persistence.Identity;

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
