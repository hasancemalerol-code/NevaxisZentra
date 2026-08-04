using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using CrevaFactOS.Persistence.Tenancy;

namespace CrevaFactOS.Persistence.Tests;

public sealed class PersistenceInfrastructureTests
{
    private const string Password = "not-for-logs";

    [Fact]
    public void ConnectionStringContainsExpectedSettings()
    {
        var connectionString = PostgreSqlConnectionFactory.BuildConnectionString(CreateOptions());
        var parsed = new NpgsqlConnectionStringBuilder(connectionString);

        Assert.Equal("db.example.test", parsed.Host);
        Assert.Equal(5544, parsed.Port);
        Assert.Equal("foundation", parsed.Database);
        Assert.Equal("foundation-user", parsed.Username);
        Assert.Equal(Password, parsed.Password);
        Assert.Equal(12, parsed.Timeout);
        Assert.Equal(34, parsed.CommandTimeout);
        Assert.False(parsed.Pooling);
    }

    [Fact]
    public void DiagnosticsAndRedactionDoNotExposePasswordOrCompleteConnectionString()
    {
        var options = CreateOptions();
        var connectionString = PostgreSqlConnectionFactory.BuildConnectionString(options);
        var diagnostics = PostgreSqlConnectionFactory.GetSafeEndpointDescription(options);
        var redacted = PostgreSqlConnectionFactory.RedactConnectionString(connectionString);

        Assert.DoesNotContain(Password, diagnostics, StringComparison.Ordinal);
        Assert.DoesNotContain(options.Username, diagnostics, StringComparison.Ordinal);
        Assert.DoesNotContain(Password, redacted, StringComparison.Ordinal);
        Assert.NotEqual(connectionString, redacted);
    }

    [Fact]
    public void DbContextContainsOnlyTenantIsolationInfrastructureMappings()
    {
        using var context = CreateContext();

        var mappedTypes = context.Model.GetEntityTypes()
            .Select(entityType => entityType.ClrType)
            .OrderBy(type => type.Name, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal([typeof(Tenant), typeof(TenantIsolationProbe)], mappedTypes);
    }

    [Fact]
    public void MigrationAssemblyContainsOnlyFoundationMigrations()
    {
        using var context = CreateContext();
        var migrations = context.Database.GetMigrations().ToArray();

        Assert.Equal(2, migrations.Length);
        Assert.EndsWith("_InitialInfrastructureBaseline", migrations[0], StringComparison.Ordinal);
        Assert.EndsWith("_AddTenantIsolationFoundation", migrations[1], StringComparison.Ordinal);
        Assert.Equal(
            CrevaFactOSDbContext.MigrationsAssemblyName,
            context.GetService<IMigrationsAssembly>().Assembly.GetName().Name);
    }

    [Fact]
    public void TenantIdentifierCannotBeMutated()
    {
        var tenantA = Guid.NewGuid();
        var accessor = new TenantContextAccessor();
        accessor.Set(new TenantContext(tenantA, "tenant-a"));
        using var context = CreateContext(accessor);
        var probe = new TenantIsolationProbe
        {
            Id = Guid.NewGuid(),
            TenantId = tenantA,
            ProbeValue = "probe",
        };
        context.Attach(probe);
        probe.TenantId = Guid.NewGuid();
        context.Entry(probe).Property(item => item.TenantId).IsModified = true;

        var exception = Assert.Throws<InvalidOperationException>(() => context.PrepareTenantScopedChanges());

        Assert.Contains("cannot be changed", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void CrossTenantAttachmentIsRejected()
    {
        var accessor = new TenantContextAccessor();
        accessor.Set(new TenantContext(Guid.NewGuid(), "tenant-a"));
        using var context = CreateContext(accessor);
        context.Add(new TenantIsolationProbe
        {
            Id = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            ProbeValue = "probe",
        });

        var exception = Assert.Throws<InvalidOperationException>(() => context.PrepareTenantScopedChanges());

        Assert.Contains("another tenant", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void MissingTenantContextRejectsTenantScopedSave()
    {
        using var context = CreateContext(new TenantContextAccessor());
        context.Add(new TenantIsolationProbe
        {
            Id = Guid.NewGuid(),
            ProbeValue = "probe",
        });

        Assert.Throws<InvalidOperationException>(() => context.PrepareTenantScopedChanges());
    }

    [Fact]
    public void AddedRecordReceivesCurrentTenantIdentifier()
    {
        var tenantId = Guid.NewGuid();
        var accessor = new TenantContextAccessor();
        accessor.Set(new TenantContext(tenantId, "tenant-a"));
        using var context = CreateContext(accessor);
        var probe = new TenantIsolationProbe
        {
            Id = Guid.NewGuid(),
            ProbeValue = "probe",
        };
        context.Add(probe);

        var preparedTenantId = context.PrepareTenantScopedChanges();

        Assert.Equal(tenantId, preparedTenantId);
        Assert.Equal(tenantId, probe.TenantId);
    }

    private static CrevaFactOSDbContext CreateContext(ITenantContextAccessor? tenantContextAccessor = null)
    {
        var options = new DbContextOptionsBuilder<CrevaFactOSDbContext>()
            .UseNpgsql(
                PostgreSqlConnectionFactory.BuildConnectionString(CreateOptions()),
                npgsql => npgsql.MigrationsAssembly(CrevaFactOSDbContext.MigrationsAssemblyName))
            .Options;
        return new CrevaFactOSDbContext(options, tenantContextAccessor);
    }

    private static PersistenceOptions CreateOptions() => new()
    {
        Enabled = true,
        Host = "db.example.test",
        Port = 5544,
        Database = "foundation",
        Username = "foundation-user",
        Password = Password,
        SslMode = "Disable",
        ConnectionTimeoutSeconds = 12,
        CommandTimeoutSeconds = 34,
        Pooling = false,
    };
}
