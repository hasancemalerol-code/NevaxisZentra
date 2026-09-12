using NevaxisFactOS.Configuration;
using NevaxisFactOS.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Design;

namespace NevaxisFactOS.Persistence.Migrations;

public sealed class NevaxisFactOSDesignTimeDbContextFactory
    : IDesignTimeDbContextFactory<NevaxisFactOSDbContext>
{
    public NevaxisFactOSDbContext CreateDbContext(string[] args)
    {
        var options = new PersistenceOptions
        {
            Enabled = true,
            Host = GetEnvironmentValue("HOST", "127.0.0.1"),
            Port = GetEnvironmentInt32("PORT", 5432),
            Database = GetEnvironmentValue("DATABASE", "crevafactos_local"),
            Username = GetEnvironmentValue("USERNAME", "crevafactos"),
            Password = GetEnvironmentValue("PASSWORD", "local_dev_only_change_me"),
            SslMode = GetEnvironmentValue("SSL_MODE", "Disable"),
            ConnectionTimeoutSeconds = GetEnvironmentInt32("CONNECTION_TIMEOUT_SECONDS", 15),
            CommandTimeoutSeconds = GetEnvironmentInt32("COMMAND_TIMEOUT_SECONDS", 30),
            Pooling = false,
        };

        var builder = new DbContextOptionsBuilder<NevaxisFactOSDbContext>();
        builder.UseNpgsql(
            PostgreSqlConnectionFactory.BuildConnectionString(options),
            npgsqlOptions =>
            {
                npgsqlOptions.MigrationsAssembly(NevaxisFactOSDbContext.MigrationsAssemblyName);
                npgsqlOptions.MigrationsHistoryTable(NevaxisFactOSDbContext.MigrationHistoryTableName);
                npgsqlOptions.CommandTimeout(options.CommandTimeoutSeconds);
            });

        // SQL-authored migrations (e.g. EXPORT-1L) may intentionally defer full
        // model-snapshot regeneration while still shipping complete DDL/RLS.
        builder.ConfigureWarnings(warnings =>
            warnings.Ignore(RelationalEventId.PendingModelChangesWarning));

        return new NevaxisFactOSDbContext(builder.Options);
    }

    private static string GetEnvironmentValue(string suffix, string fallback) =>
        LegacyEnvironmentVariables.GetSectioned("MIGRATIONS", suffix) ?? fallback;

    private static int GetEnvironmentInt32(string suffix, int fallback)
    {
        var value = LegacyEnvironmentVariables.GetSectioned("MIGRATIONS", suffix);
        return string.IsNullOrWhiteSpace(value) ? fallback : int.Parse(value, provider: null);
    }

}
