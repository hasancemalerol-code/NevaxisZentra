using NevaxisFactOS.Persistence;
using NevaxisFactOS.DatabaseTool;
using NevaxisFactOS.Persistence.Development;
using Microsoft.Extensions.Options;
using Npgsql;
using System.Net.Sockets;

var operation = args.FirstOrDefault()?.ToLowerInvariant() ?? "verify";
var options = operation is "state" or "development-pilot-paid-access-ensure"
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

    if (operation == "development-pilot-paid-access-ensure")
    {
        var pilotOptions = new DevelopmentPilotOptions { Enabled = true };
        await DevelopmentPilotAccessEnsureService.EnsurePaidAccessAsync(
            Options.Create(options),
            Options.Create(pilotOptions));

        await using var verifyConnection = new NpgsqlConnection(
            PostgreSqlConnectionFactory.BuildConnectionString(options));
        await verifyConnection.OpenAsync();
        await using var verifyCommand = new NpgsqlCommand(
            """
            SELECT COUNT(*)
            FROM saas_tenant_subscriptions
            WHERE tenant_id = @tenant_id AND is_current;
            """,
            verifyConnection);
        verifyCommand.Parameters.AddWithValue(
            "tenant_id",
            DevelopmentPilotIds.TenantId);
        var count = Convert.ToInt64(await verifyCommand.ExecuteScalarAsync(), System.Globalization.CultureInfo.InvariantCulture);

        await using var tenantCommand = new NpgsqlCommand(
            """
            SELECT id::text, tenant_key
            FROM tenants
            WHERE tenant_key = 'dev-pilot-tenant' OR id = @tenant_id
            ORDER BY tenant_key;
            """,
            verifyConnection);
        tenantCommand.Parameters.AddWithValue("tenant_id", DevelopmentPilotIds.TenantId);
        await using var tenantReader = await tenantCommand.ExecuteReaderAsync();
        while (await tenantReader.ReadAsync())
        {
            Console.WriteLine(
                $"Tenant row: id={tenantReader.GetString(0)} key={tenantReader.GetString(1)}");
        }
        await tenantReader.CloseAsync();

        await using var subscriptionCommand = new NpgsqlCommand(
            """
            SELECT id::text, tenant_id::text, is_current, subscription_status
            FROM saas_tenant_subscriptions
            WHERE tenant_id = @tenant_id;
            """,
            verifyConnection);
        subscriptionCommand.Parameters.AddWithValue("tenant_id", DevelopmentPilotIds.TenantId);
        await using var subscriptionReader = await subscriptionCommand.ExecuteReaderAsync();
        while (await subscriptionReader.ReadAsync())
        {
            Console.WriteLine(
                $"Subscription row: id={subscriptionReader.GetString(0)} tenant={subscriptionReader.GetString(1)} current={subscriptionReader.GetBoolean(2)} status={subscriptionReader.GetString(3)}");
        }
        await subscriptionReader.CloseAsync();

        Console.WriteLine($"Development pilot paid access ensured (current subscriptions: {count}).");

        var runtimeOptions = DatabaseConfiguration.LoadRuntime();
        runtimeOptions.Host = options.Host;
        runtimeOptions.Port = options.Port;
        runtimeOptions.Database = options.Database;
        await using var runtimeConnection = new NpgsqlConnection(
            PostgreSqlConnectionFactory.BuildConnectionString(runtimeOptions));
        await runtimeConnection.OpenAsync();
        await using var runtimeTransaction = await runtimeConnection.BeginTransactionAsync();
        await using (var setTenant = new NpgsqlCommand(
                         "SELECT set_config('NevaxisFactOS.tenant_id', @tenant_id, false);",
                         runtimeConnection,
                         runtimeTransaction))
        {
            setTenant.Parameters.AddWithValue("tenant_id", DevelopmentPilotIds.TenantId.ToString("D"));
            await setTenant.ExecuteNonQueryAsync();
        }

        await using var runtimeCountCommand = new NpgsqlCommand(
            """
            SELECT current_user,
                   current_setting('NevaxisFactOS.tenant_id', true),
                   COUNT(*)
            FROM saas_tenant_subscriptions
            WHERE is_current;
            """,
            runtimeConnection,
            runtimeTransaction);
        await using var runtimeReader = await runtimeCountCommand.ExecuteReaderAsync();
        await runtimeReader.ReadAsync();
        Console.WriteLine(
            $"Runtime-role user={runtimeReader.GetString(0)} tenant setting={runtimeReader.GetString(1)} visible current subscriptions={runtimeReader.GetInt64(2)}.");
        await runtimeReader.CloseAsync();
        await runtimeTransaction.CommitAsync();
        return 0;
    }

    if (operation == "audit-trail-verify")
    {
        await AuditTrailVerifier.VerifyAsync();
        return 0;
    }

    if (operation == "audit-trail-bootstrap")
    {
        await AuditTrailVerifier.BootstrapAsync();
        return 0;
    }

    if (operation == "audit-trail-cleanup")
    {
        await AuditTrailVerifier.CleanupFixturesAsync();
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
    Console.Error.WriteLine($"PostgreSQL query failed ({exception.GetType().Name}): {exception.Message}");
    return 5;
}
