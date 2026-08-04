using CrevaFactOS.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CrevaFactOS.Persistence.Migrations;

public sealed class CrevaFactOSDesignTimeDbContextFactory
    : IDesignTimeDbContextFactory<CrevaFactOSDbContext>
{
    public CrevaFactOSDbContext CreateDbContext(string[] args)
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

        var builder = new DbContextOptionsBuilder<CrevaFactOSDbContext>();
        builder.UseNpgsql(
            PostgreSqlConnectionFactory.BuildConnectionString(options),
            npgsqlOptions =>
            {
                npgsqlOptions.MigrationsAssembly(CrevaFactOSDbContext.MigrationsAssemblyName);
                npgsqlOptions.MigrationsHistoryTable(CrevaFactOSDbContext.MigrationHistoryTableName);
                npgsqlOptions.CommandTimeout(options.CommandTimeoutSeconds);
            });

        return new CrevaFactOSDbContext(builder.Options);
    }

    private static string GetEnvironmentValue(string suffix, string fallback) =>
        Environment.GetEnvironmentVariable($"CREVAFACTOS__MIGRATIONS__{suffix}") ?? fallback;

    private static int GetEnvironmentInt32(string suffix, int fallback)
    {
        var value = Environment.GetEnvironmentVariable($"CREVAFACTOS__MIGRATIONS__{suffix}");
        return string.IsNullOrWhiteSpace(value) ? fallback : int.Parse(value, provider: null);
    }

}
