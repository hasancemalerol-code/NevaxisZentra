using CrevaFactOS.Persistence;
using CrevaFactOS.DatabaseTool;
using Npgsql;
using System.Net.Sockets;

var operation = args.FirstOrDefault()?.ToLowerInvariant() ?? "verify";
var options = operation == "state"
    ? DatabaseConfiguration.LoadMigrations()
    : DatabaseConfiguration.LoadRuntime();

if (args.Contains("--configuration-only", StringComparer.OrdinalIgnoreCase))
{
    _ = PostgreSqlConnectionFactory.BuildConnectionString(options);
    Console.WriteLine($"Persistence configuration is valid ({PostgreSqlConnectionFactory.GetSafeEndpointDescription(options)}).");
    return 0;
}

try
{
    if (operation == "tenant-isolation-verify")
    {
        await TenantIsolationVerifier.VerifyAsync();
        return 0;
    }

    if (operation == "authentication-membership-verify")
    {
        await AuthenticationMembershipVerifier.VerifyAsync();
        return 0;
    }

    if (operation == "authentication-membership-bootstrap")
    {
        await AuthenticationMembershipVerifier.BootstrapAsync();
        return 0;
    }

    if (operation == "authentication-membership-cleanup")
    {
        await AuthenticationMembershipVerifier.CleanupFixturesAsync();
        return 0;
    }

    if (operation == "permission-authorization-verify")
    {
        await PermissionAuthorizationVerifier.VerifyAsync();
        return 0;
    }

    if (operation == "permission-authorization-bootstrap")
    {
        await PermissionAuthorizationVerifier.BootstrapAsync();
        return 0;
    }

    if (operation == "permission-authorization-cleanup")
    {
        await PermissionAuthorizationVerifier.CleanupFixturesAsync();
        return 0;
    }

    await using var connection = new NpgsqlConnection(
        PostgreSqlConnectionFactory.BuildConnectionString(options));
    await connection.OpenAsync();

    if (operation == "verify")
    {
        await using var command = new NpgsqlCommand("SELECT 1", connection);
        var result = await command.ExecuteScalarAsync();
        if (!Equals(result, 1))
        {
            Console.Error.WriteLine("PostgreSQL query failed: SELECT 1 returned an unexpected result.");
            return 5;
        }

        Console.WriteLine($"PostgreSQL connectivity verified ({PostgreSqlConnectionFactory.GetSafeEndpointDescription(options)}).");
        return 0;
    }

    if (operation == "state")
    {
        const string sql = """
            SELECT table_schema, table_name
            FROM information_schema.tables
            WHERE table_schema NOT IN ('pg_catalog', 'information_schema')
            ORDER BY table_schema, table_name;
            """;
        await using var command = new NpgsqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync();
        var count = 0;
        while (await reader.ReadAsync())
        {
            Console.WriteLine($"{reader.GetString(0)}.{reader.GetString(1)}");
            count++;
        }

        Console.WriteLine($"User-visible table count: {count}.");

        await reader.CloseAsync();
        const string migrationCountSql = """
            SELECT CASE
                WHEN to_regclass('public."__EFMigrationsHistory"') IS NULL THEN -1
                ELSE (SELECT COUNT(*) FROM "__EFMigrationsHistory")
            END;
            """;
        await using var migrationCountCommand = new NpgsqlCommand(migrationCountSql, connection);
        var migrationCount = Convert.ToInt64(
            await migrationCountCommand.ExecuteScalarAsync(),
            System.Globalization.CultureInfo.InvariantCulture);
        Console.WriteLine(migrationCount < 0
            ? "EF migration history table is absent."
            : $"Applied EF migration count: {migrationCount}.");
        return 0;
    }

    Console.Error.WriteLine("Unknown database-tool operation.");
    return 2;
}
catch (PostgresException exception) when (exception.SqlState == PostgresErrorCodes.InvalidPassword)
{
    Console.Error.WriteLine("PostgreSQL authentication failed. Check the configured username and password.");
    return 3;
}
catch (PostgresException exception) when (exception.SqlState == PostgresErrorCodes.InvalidCatalogName)
{
    Console.Error.WriteLine("PostgreSQL database is missing. Check the configured database name.");
    return 4;
}
catch (PostgresException exception)
{
    Console.Error.WriteLine($"PostgreSQL query failed with SQLSTATE {exception.SqlState}. No credentials were logged.");
    return 5;
}
catch (NpgsqlException exception) when (exception.InnerException is SocketException or TimeoutException)
{
    Console.Error.WriteLine("PostgreSQL network connection failed. Check the host, port, and container health.");
    return 6;
}
catch (Exception exception)
{
    Console.Error.WriteLine($"PostgreSQL query failed ({exception.GetType().Name}). No credentials were logged.");
    return 5;
}
