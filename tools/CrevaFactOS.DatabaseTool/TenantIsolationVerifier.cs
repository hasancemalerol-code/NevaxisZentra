using CrevaFactOS.Persistence;
using Npgsql;

namespace CrevaFactOS.DatabaseTool;

internal static class TenantIsolationVerifier
{
    private static readonly Guid TenantAId = Guid.Parse("aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaa1");
    private static readonly Guid TenantBId = Guid.Parse("bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbb2");
    private static readonly Guid InactiveTenantId = Guid.Parse("cccccccc-cccc-4ccc-8ccc-ccccccccccc3");
    private static readonly Guid ProbeAId = Guid.Parse("aaaaaaaa-0000-4000-8000-000000000001");
    private static readonly Guid ProbeBId = Guid.Parse("bbbbbbbb-0000-4000-8000-000000000002");

    public static async Task VerifyAsync()
    {
        var runtimeOptions = DatabaseConfiguration.LoadRuntime();
        var migrationOptions = DatabaseConfiguration.LoadMigrations();

        await using var administrativeConnection = new NpgsqlConnection(
            PostgreSqlConnectionFactory.BuildConnectionString(migrationOptions));
        await administrativeConnection.OpenAsync();

        await ProvisionRuntimeLoginAsync(administrativeConnection, runtimeOptions.Password);
        await SeedAsync(administrativeConnection);

        try
        {
            await AssertRuntimeRoleIsRestrictedAsync(administrativeConnection);
            await AssertTenantResolutionAsync(runtimeOptions);
            await AssertRowLevelIsolationAsync(runtimeOptions);
            await AssertPooledConnectionDoesNotLeakAsync(runtimeOptions);
        }
        finally
        {
            await CleanupAsync(administrativeConnection);
        }

        Console.WriteLine("Tenant A / tenant B PostgreSQL RLS isolation verified.");
        Console.WriteLine("Missing tenant context failed closed for reads and writes.");
        Console.WriteLine("Pooled connection reuse did not leak transaction-scoped tenant context.");
    }

    private static async Task ProvisionRuntimeLoginAsync(
        NpgsqlConnection connection,
        string runtimePassword)
    {
        await using (var setPassword = new NpgsqlCommand(
            "SELECT set_config('crevafactos.local_runtime_password', @runtime_password, false)",
            connection))
        {
            setPassword.Parameters.AddWithValue("runtime_password", runtimePassword);
            await setPassword.ExecuteScalarAsync();
        }

        try
        {
            const string alterRoleSql = """
                DO $runtime_role$
                BEGIN
                    EXECUTE format(
                        'ALTER ROLE crevafactos_runtime LOGIN PASSWORD %L',
                        current_setting('crevafactos.local_runtime_password'));
                END
                $runtime_role$;
                """;
            await using var alterRole = new NpgsqlCommand(alterRoleSql, connection);
            await alterRole.ExecuteNonQueryAsync();
        }
        finally
        {
            await using var clearPassword = new NpgsqlCommand(
                "SELECT set_config('crevafactos.local_runtime_password', '', false)",
                connection);
            await clearPassword.ExecuteScalarAsync();
        }
    }

    private static async Task SeedAsync(NpgsqlConnection connection)
    {
        const string sql = """
            DELETE FROM tenant_isolation_probes
            WHERE id IN (@probe_a_id, @probe_b_id);
            DELETE FROM tenants
            WHERE id IN (@tenant_a_id, @tenant_b_id, @inactive_tenant_id);

            INSERT INTO tenants (id, tenant_key, display_name, status, created_at_utc)
            VALUES
                (@tenant_a_id, 'isolation-a', 'Isolation Tenant A', 'Active', CURRENT_TIMESTAMP),
                (@tenant_b_id, 'isolation-b', 'Isolation Tenant B', 'Active', CURRENT_TIMESTAMP),
                (@inactive_tenant_id, 'isolation-inactive', 'Isolation Inactive', 'Inactive', CURRENT_TIMESTAMP);

            INSERT INTO tenant_isolation_probes (id, tenant_id, probe_value)
            VALUES
                (@probe_a_id, @tenant_a_id, 'tenant-a-probe'),
                (@probe_b_id, @tenant_b_id, 'tenant-b-probe');
            """;
        await using var command = new NpgsqlCommand(sql, connection);
        AddIds(command);
        await command.ExecuteNonQueryAsync();
    }

    private static async Task CleanupAsync(NpgsqlConnection connection)
    {
        const string sql = """
            DELETE FROM tenant_isolation_probes
            WHERE tenant_id IN (@tenant_a_id, @tenant_b_id, @inactive_tenant_id);
            DELETE FROM tenants
            WHERE id IN (@tenant_a_id, @tenant_b_id, @inactive_tenant_id);
            """;
        await using var command = new NpgsqlCommand(sql, connection);
        AddIds(command);
        await command.ExecuteNonQueryAsync();
    }

    private static async Task AssertRuntimeRoleIsRestrictedAsync(NpgsqlConnection connection)
    {
        const string sql = """
            SELECT rolsuper, rolcreatedb, rolcreaterole, rolbypassrls, rolinherit
            FROM pg_roles
            WHERE rolname = 'crevafactos_runtime';
            """;
        await using var command = new NpgsqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync();
        Require(await reader.ReadAsync(), "The runtime role does not exist.");
        Require(
            !reader.GetBoolean(0)
            && !reader.GetBoolean(1)
            && !reader.GetBoolean(2)
            && !reader.GetBoolean(3)
            && !reader.GetBoolean(4),
            "The runtime role has an elevated PostgreSQL privilege.");
    }

    private static async Task AssertTenantResolutionAsync(PersistenceOptions options)
    {
        await using var connection = await OpenRuntimeConnectionAsync(options);
        Require(
            await ResolveTenantAsync(connection, TenantAId.ToString()) == TenantAId,
            "Active tenant resolution by UUID failed.");
        Require(
            await ResolveTenantAsync(connection, "isolation-b") == TenantBId,
            "Active tenant resolution by key failed.");
        Require(
            await ResolveTenantAsync(connection, "isolation-inactive") is null,
            "Inactive tenant resolution did not fail closed.");
        Require(
            await ResolveTenantAsync(connection, "isolation-unknown") is null,
            "Unknown tenant resolution did not fail closed.");
    }

    private static async Task<Guid?> ResolveTenantAsync(NpgsqlConnection connection, string identifier)
    {
        await using var command = new NpgsqlCommand(
            "SELECT tenant_id FROM resolve_active_tenant(@tenant_identifier)",
            connection);
        command.Parameters.AddWithValue("tenant_identifier", identifier);
        var result = await command.ExecuteScalarAsync();
        return result is Guid tenantId ? tenantId : null;
    }

    private static async Task AssertRowLevelIsolationAsync(PersistenceOptions options)
    {
        await using var connection = await OpenRuntimeConnectionAsync(options);

        await using (var tenantATransaction = await connection.BeginTransactionAsync())
        {
            await SetTenantAsync(connection, TenantAId);
            Require(await CountVisibleProbesAsync(connection) == 1, "Tenant A did not see exactly its own probe row.");
            Require(await CountProbeAsync(connection, ProbeBId) == 0, "Tenant A read tenant B's probe row.");
            Require(await UpdateProbeAsync(connection, ProbeBId) == 0, "Tenant A updated tenant B's probe row.");
            Require(await DeleteProbeAsync(connection, ProbeBId) == 0, "Tenant A deleted tenant B's probe row.");
            await AssertMismatchedInsertRejectedAsync(connection, TenantBId);
            await tenantATransaction.RollbackAsync();
        }

        await using (var tenantBTransaction = await connection.BeginTransactionAsync())
        {
            await SetTenantAsync(connection, TenantBId);
            Require(await CountVisibleProbesAsync(connection) == 1, "Tenant B did not see exactly its own probe row.");
            await tenantBTransaction.RollbackAsync();
        }

        Require(await CountVisibleProbesAsync(connection) == 0, "Missing tenant context returned tenant-scoped rows.");
        await AssertMismatchedInsertRejectedAsync(connection, TenantAId);
    }

    private static async Task AssertPooledConnectionDoesNotLeakAsync(PersistenceOptions options)
    {
        var builder = new NpgsqlConnectionStringBuilder(
            PostgreSqlConnectionFactory.BuildConnectionString(options))
        {
            MaxPoolSize = 1,
            MinPoolSize = 0,
        };
        await using var dataSource = NpgsqlDataSource.Create(builder.ConnectionString);

        int firstProcessId;
        await using (var firstConnection = await dataSource.OpenConnectionAsync())
        {
            firstProcessId = firstConnection.ProcessID;
            await using var transaction = await firstConnection.BeginTransactionAsync();
            await SetTenantAsync(firstConnection, TenantAId);
            Require(await CountVisibleProbesAsync(firstConnection) == 1, "Tenant A pool setup failed.");
            await transaction.CommitAsync();
        }

        await using var reusedConnection = await dataSource.OpenConnectionAsync();
        Require(reusedConnection.ProcessID == firstProcessId, "The pooled-connection test did not reuse the physical connection.");
        Require(await CountVisibleProbesAsync(reusedConnection) == 0, "Tenant context leaked through the connection pool.");
    }

    private static async Task<NpgsqlConnection> OpenRuntimeConnectionAsync(PersistenceOptions options)
    {
        var connection = new NpgsqlConnection(PostgreSqlConnectionFactory.BuildConnectionString(options));
        await connection.OpenAsync();
        return connection;
    }

    private static async Task SetTenantAsync(NpgsqlConnection connection, Guid tenantId)
    {
        await using var command = new NpgsqlCommand(
            "SELECT set_config('crevafactos.tenant_id', @tenant_id, true)",
            connection);
        command.Parameters.AddWithValue("tenant_id", tenantId.ToString());
        await command.ExecuteScalarAsync();
    }

    private static async Task<long> CountVisibleProbesAsync(NpgsqlConnection connection)
    {
        await using var command = new NpgsqlCommand("SELECT COUNT(*) FROM tenant_isolation_probes", connection);
        return (long)(await command.ExecuteScalarAsync() ?? -1L);
    }

    private static async Task<long> CountProbeAsync(NpgsqlConnection connection, Guid probeId)
    {
        await using var command = new NpgsqlCommand(
            "SELECT COUNT(*) FROM tenant_isolation_probes WHERE id = @probe_id",
            connection);
        command.Parameters.AddWithValue("probe_id", probeId);
        return (long)(await command.ExecuteScalarAsync() ?? -1L);
    }

    private static async Task<int> UpdateProbeAsync(NpgsqlConnection connection, Guid probeId)
    {
        await using var command = new NpgsqlCommand(
            "UPDATE tenant_isolation_probes SET probe_value = 'cross-tenant-update' WHERE id = @probe_id",
            connection);
        command.Parameters.AddWithValue("probe_id", probeId);
        return await command.ExecuteNonQueryAsync();
    }

    private static async Task<int> DeleteProbeAsync(NpgsqlConnection connection, Guid probeId)
    {
        await using var command = new NpgsqlCommand(
            "DELETE FROM tenant_isolation_probes WHERE id = @probe_id",
            connection);
        command.Parameters.AddWithValue("probe_id", probeId);
        return await command.ExecuteNonQueryAsync();
    }

    private static async Task AssertMismatchedInsertRejectedAsync(
        NpgsqlConnection connection,
        Guid tenantId)
    {
        await using var command = new NpgsqlCommand(
            """
            INSERT INTO tenant_isolation_probes (id, tenant_id, probe_value)
            VALUES (@probe_id, @tenant_id, 'must-be-rejected')
            """,
            connection);
        command.Parameters.AddWithValue("probe_id", Guid.NewGuid());
        command.Parameters.AddWithValue("tenant_id", tenantId);

        try
        {
            await command.ExecuteNonQueryAsync();
        }
        catch (PostgresException exception) when (exception.SqlState == PostgresErrorCodes.InsufficientPrivilege)
        {
            return;
        }

        throw new InvalidOperationException("A tenant-mismatched or tenantless insert was not rejected by RLS.");
    }

    private static void AddIds(NpgsqlCommand command)
    {
        command.Parameters.AddWithValue("tenant_a_id", TenantAId);
        command.Parameters.AddWithValue("tenant_b_id", TenantBId);
        command.Parameters.AddWithValue("inactive_tenant_id", InactiveTenantId);
        command.Parameters.AddWithValue("probe_a_id", ProbeAId);
        command.Parameters.AddWithValue("probe_b_id", ProbeBId);
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
