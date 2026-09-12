using NevaxisFactOS.Persistence.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace NevaxisFactOS.Persistence.Tests;

public sealed class TenantDatabaseSessionTests
{
    [Fact]
    public async Task ExecuteReadAsyncRequiresTenantContext()
    {
        using var context = CreateContext();
        var session = new TenantDatabaseSession(context, new UnavailableTenantContextAccessor());

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            session.ExecuteReadAsync(
                _ => Task.FromResult(true),
                CancellationToken.None));
    }

    [Fact]
    public async Task BeginTransactionAsyncRequiresTenantContext()
    {
        using var context = CreateContext();
        var session = new TenantDatabaseSession(context, new UnavailableTenantContextAccessor());

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            session.BeginTransactionAsync(CancellationToken.None));
    }

    private static NevaxisFactOSDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<NevaxisFactOSDbContext>()
            .UseNpgsql(
                PostgreSqlConnectionFactory.BuildConnectionString(CreateOptions()),
                npgsql => npgsql.MigrationsAssembly(NevaxisFactOSDbContext.MigrationsAssemblyName))
            .Options;
        return new NevaxisFactOSDbContext(options);
    }

    private static PersistenceOptions CreateOptions() => new()
    {
        Enabled = true,
        Host = "db.example.test",
        Port = 5544,
        Database = "foundation",
        Username = "foundation-user",
        Password = "not-for-logs",
        SslMode = "Disable",
        ConnectionTimeoutSeconds = 12,
        CommandTimeoutSeconds = 34,
        Pooling = false,
    };

    private sealed class UnavailableTenantContextAccessor : ITenantContextAccessor
    {
        public bool IsAvailable => false;

        public TenantContext? Current => null;

        public Guid RequireTenantId() =>
            throw new InvalidOperationException("Tenant context is unavailable.");
    }
}
