using NevaxisFactOS.Persistence.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace NevaxisFactOS.Persistence.Development;

internal static class DevelopmentPilotBootstrapDbContextFactory
{
    internal sealed record BootstrapDbContext(
        NevaxisFactOSDbContext DbContext,
        TenantContextAccessor TenantContextAccessor);

    internal static BootstrapDbContext Create(PersistenceOptions runtimeOptions)
    {
        var tenantContextAccessor = new TenantContextAccessor();

        // Use the runtime host/port/database to ensure same PostgreSQL instance,
        // but use migration credentials (broader permissions needed for seeding).
        var bootstrapOptions = new PersistenceOptions
        {
            Enabled = true,
            Host = GetMigrationValue("HOST", runtimeOptions.Host),
            Port = GetMigrationInt32("PORT", runtimeOptions.Port),
            Database = GetMigrationValue("DATABASE", runtimeOptions.Database),
            Username = GetMigrationValue("USERNAME", "crevafactos"),
            Password = GetMigrationValue("PASSWORD", "local_dev_only_change_me"),
            SslMode = GetMigrationValue("SSL_MODE", runtimeOptions.SslMode),
            ConnectionTimeoutSeconds = runtimeOptions.ConnectionTimeoutSeconds,
            CommandTimeoutSeconds = runtimeOptions.CommandTimeoutSeconds,
            Pooling = false,
        };

        var builder = new DbContextOptionsBuilder<NevaxisFactOSDbContext>();
        builder.UseNpgsql(
            PostgreSqlConnectionFactory.BuildConnectionString(bootstrapOptions),
            npgsqlOptions =>
            {
                npgsqlOptions.MigrationsAssembly(NevaxisFactOSDbContext.MigrationsAssemblyName);
                npgsqlOptions.MigrationsHistoryTable(NevaxisFactOSDbContext.MigrationHistoryTableName);
                npgsqlOptions.CommandTimeout(bootstrapOptions.CommandTimeoutSeconds);
            });

        return new BootstrapDbContext(
            new NevaxisFactOSDbContext(builder.Options, tenantContextAccessor),
            tenantContextAccessor);
    }

    private static string GetMigrationValue(string suffix, string fallback) =>
        Environment.GetEnvironmentVariable($"NEVAXIS_FACTOS__MIGRATIONS__{suffix}") ?? fallback;

    private static int GetMigrationInt32(string suffix, int fallback)
    {
        var value = Environment.GetEnvironmentVariable($"NEVAXIS_FACTOS__MIGRATIONS__{suffix}");
        return string.IsNullOrWhiteSpace(value) ? fallback : int.Parse(value, provider: null);
    }
}
