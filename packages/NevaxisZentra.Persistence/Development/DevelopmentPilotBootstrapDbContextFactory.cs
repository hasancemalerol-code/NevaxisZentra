using NevaxisZentra.Configuration;
using NevaxisZentra.Persistence.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace NevaxisZentra.Persistence.Development;

internal static class DevelopmentPilotBootstrapDbContextFactory
{
#if ZENTRA_PRODUCTION_BUILD
    // The development pilot bootstrap is unreachable in customer builds; it carries no default credential.
    private const string FallbackMigrationPassword = "";
#else
    private const string FallbackMigrationPassword = "local_dev_only_change_me";
#endif

    internal sealed record BootstrapDbContext(
        NevaxisZentraDbContext DbContext,
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
            Password = GetMigrationValue("PASSWORD", FallbackMigrationPassword),
            SslMode = GetMigrationValue("SSL_MODE", runtimeOptions.SslMode),
            ConnectionTimeoutSeconds = runtimeOptions.ConnectionTimeoutSeconds,
            CommandTimeoutSeconds = runtimeOptions.CommandTimeoutSeconds,
            Pooling = false,
        };

        var builder = new DbContextOptionsBuilder<NevaxisZentraDbContext>();
        builder.UseNpgsql(
            PostgreSqlConnectionFactory.BuildConnectionString(bootstrapOptions),
            npgsqlOptions =>
            {
                npgsqlOptions.MigrationsAssembly(NevaxisZentraDbContext.MigrationsAssemblyName);
                npgsqlOptions.MigrationsHistoryTable(NevaxisZentraDbContext.MigrationHistoryTableName);
                npgsqlOptions.CommandTimeout(bootstrapOptions.CommandTimeoutSeconds);
            });

        return new BootstrapDbContext(
            new NevaxisZentraDbContext(builder.Options, tenantContextAccessor),
            tenantContextAccessor);
    }

    private static string GetMigrationValue(string suffix, string fallback) =>
        LegacyEnvironmentVariables.GetSectioned("MIGRATIONS", suffix) ?? fallback;

    private static int GetMigrationInt32(string suffix, int fallback)
    {
        var value = LegacyEnvironmentVariables.GetSectioned("MIGRATIONS", suffix);
        return string.IsNullOrWhiteSpace(value) ? fallback : int.Parse(value, provider: null);
    }
}
