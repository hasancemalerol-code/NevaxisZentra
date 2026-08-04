using CrevaFactOS.Persistence;
using Npgsql;
using NpgsqlTypes;

namespace CrevaFactOS.DatabaseTool;

internal static class AuditTrailVerifier
{
    private static readonly Guid TenantAId = Guid.Parse("a7000000-0000-4000-8000-000000000001");
    private static readonly Guid TenantBId = Guid.Parse("a7000000-0000-4000-8000-000000000002");
    private static readonly Guid IdentityAId = Guid.Parse("a7000000-0000-4000-8000-000000000011");
    private static readonly Guid IdentityBId = Guid.Parse("a7000000-0000-4000-8000-000000000012");
    private static readonly Guid IdentityWithoutPermissionId = Guid.Parse("a7000000-0000-4000-8000-000000000013");
    private static readonly Guid MembershipAId = Guid.Parse("a7000000-0000-4000-8000-000000000021");
    private static readonly Guid MembershipBId = Guid.Parse("a7000000-0000-4000-8000-000000000022");
    private static readonly Guid MembershipWithoutPermissionId = Guid.Parse("a7000000-0000-4000-8000-000000000023");
    private static readonly Guid RoleAId = Guid.Parse("a7000000-0000-4000-8000-000000000031");
    private static readonly Guid RoleBId = Guid.Parse("a7000000-0000-4000-8000-000000000032");
    private static readonly Guid AuditAId = Guid.Parse("a7000000-0000-4000-8000-000000000041");
    private static readonly Guid AuditBId = Guid.Parse("a7000000-0000-4000-8000-000000000042");
    private static readonly Guid AuditReadPermissionId = Guid.Parse("10000000-0000-4000-8000-000000000004");
    private static readonly Guid[] TenantIds = [TenantAId, TenantBId];
    private static readonly Guid[] IdentityIds = [IdentityAId, IdentityBId, IdentityWithoutPermissionId];
    private static readonly Guid[] AuditIds = [AuditAId, AuditBId];
    private static readonly string[] AdministrativeMutationSql =
    [
        "UPDATE audit_records SET action = 'authenticate' WHERE id = @id",
        "DELETE FROM audit_records WHERE id = @id",
    ];

    public static async Task VerifyAsync()
    {
        var runtimeOptions = DatabaseConfiguration.LoadRuntime();
        await using var admin = await OpenAdminAsync();
        await LocalRuntimeRoleProvisioner.ProvisionAsync(admin, runtimeOptions.Password);
        await SeedActorsAsync(admin);

        try
        {
            await using var runtime = new NpgsqlConnection(
                PostgreSqlConnectionFactory.BuildConnectionString(runtimeOptions));
            await runtime.OpenAsync();
            await AppendFixtureRecordsAsync(runtime);
            await AssertActorEvidenceAsync(admin);
            await AssertMutationTriggerRejectedAsync(admin);
            await AssertTenantIsolationAndPoolSafetyAsync(runtime);
            await AssertCrossTenantAppendRejectedAsync(runtime);
            await AssertRuntimeMutationsRejectedAsync(runtime);
            await AssertExistingRecordUnchangedAsync(admin);
            await AssertOversizedMetadataRejectedAsync(runtime);
            await AssertSystemBoundaryRejectedAsync(runtime);
        }
        finally
        {
            await CleanupAsync(admin);
        }

        Console.WriteLine("Tenant A/B audit append and forced-RLS isolation verified against PostgreSQL.");
        Console.WriteLine("Runtime UPDATE, DELETE, TRUNCATE, direct INSERT, and cross-tenant append were rejected.");
        Console.WriteLine("Actor, membership, permission, correlation, timestamp, outcome, source, and bounded metadata evidence were captured.");
    }

    public static async Task BootstrapAsync()
    {
        var runtimeOptions = DatabaseConfiguration.LoadRuntime();
        await using var admin = await OpenAdminAsync();
        await LocalRuntimeRoleProvisioner.ProvisionAsync(admin, runtimeOptions.Password);
        await SeedActorsAsync(admin);
        await using var runtime = new NpgsqlConnection(
            PostgreSqlConnectionFactory.BuildConnectionString(runtimeOptions));
        await runtime.OpenAsync();
        await AppendFixtureRecordsAsync(runtime);
        Console.WriteLine("Development immutable-audit fixtures created.");
    }

    public static async Task CleanupFixturesAsync()
    {
        await using var admin = await OpenAdminAsync();
        await CleanupAsync(admin);
        Console.WriteLine("Development immutable-audit fixtures removed through the migration boundary.");
    }

    private static async Task<NpgsqlConnection> OpenAdminAsync()
    {
        var connection = new NpgsqlConnection(
            PostgreSqlConnectionFactory.BuildConnectionString(DatabaseConfiguration.LoadMigrations()));
        await connection.OpenAsync();
        return connection;
    }

    private static async Task SeedActorsAsync(NpgsqlConnection admin)
    {
        await CleanupAsync(admin);
        const string sql = """
            INSERT INTO tenants (id, tenant_key, display_name, status, created_at_utc)
            VALUES
                (@tenant_a, 'audit-a', 'Audit Tenant A', 'Active', CURRENT_TIMESTAMP),
                (@tenant_b, 'audit-b', 'Audit Tenant B', 'Active', CURRENT_TIMESTAMP);

            INSERT INTO identity_subjects (id, external_issuer, external_subject, status, created_at_utc)
            VALUES
                (@identity_a, 'https://audit.local.test', 'tenant-a-auditor', 'Active', CURRENT_TIMESTAMP),
                (@identity_b, 'https://audit.local.test', 'tenant-b-auditor', 'Active', CURRENT_TIMESTAMP),
                (@identity_without_permission, 'https://audit.local.test', 'membership-only', 'Active', CURRENT_TIMESTAMP);

            INSERT INTO tenant_memberships (id, tenant_id, identity_subject_id, status, created_at_utc)
            VALUES
                (@membership_a, @tenant_a, @identity_a, 'Active', CURRENT_TIMESTAMP),
                (@membership_b, @tenant_b, @identity_b, 'Active', CURRENT_TIMESTAMP),
                (@membership_without_permission, @tenant_a, @identity_without_permission, 'Active', CURRENT_TIMESTAMP);

            INSERT INTO tenant_roles (id, tenant_id, key, display_name, status, created_at_utc)
            VALUES
                (@role_a, @tenant_a, 'audit-reader', 'Audit Reader', 'Active', CURRENT_TIMESTAMP),
                (@role_b, @tenant_b, 'audit-reader', 'Audit Reader', 'Active', CURRENT_TIMESTAMP);

            INSERT INTO tenant_role_permissions (tenant_id, tenant_role_id, permission_definition_id)
            VALUES
                (@tenant_a, @role_a, @audit_read_permission),
                (@tenant_b, @role_b, @audit_read_permission);

            INSERT INTO tenant_membership_roles
                (tenant_id, tenant_membership_id, tenant_role_id, status, assigned_at_utc)
            VALUES
                (@tenant_a, @membership_a, @role_a, 'Active', CURRENT_TIMESTAMP),
                (@tenant_b, @membership_b, @role_b, 'Active', CURRENT_TIMESTAMP);
            """;
        await using var command = new NpgsqlCommand(sql, admin);
        AddParameters(command);
        await command.ExecuteNonQueryAsync();
    }

    private static async Task AppendFixtureRecordsAsync(NpgsqlConnection runtime)
    {
        await AppendAsync(runtime, AuditAId, TenantAId, IdentityAId, MembershipAId, "request-a");
        await AppendAsync(runtime, AuditBId, TenantBId, IdentityBId, MembershipBId, "request-b");
    }

    private static async Task AppendAsync(
        NpgsqlConnection runtime,
        Guid auditId,
        Guid tenantId,
        Guid identityId,
        Guid membershipId,
        string requestId,
        string metadata = "{\"operation\":\"verification\"}")
    {
        await using var transaction = await runtime.BeginTransactionAsync();
        await SetTenantAsync(runtime, tenantId, transaction);
        await using var command = CreateAppendCommand(runtime, transaction);
        AddAppendParameters(command, auditId, tenantId, identityId, membershipId, requestId, metadata);
        var result = await command.ExecuteScalarAsync();
        Require(result is Guid id && id == auditId, "Audit append returned an unexpected identifier.");
        await transaction.CommitAsync();
    }

    private static NpgsqlCommand CreateAppendCommand(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction) =>
        new(
            """
            SELECT append_tenant_audit_record(
                @id, @tenant_id, @identity_id, @membership_id,
                'authorization.granted', 'authorize', 'infrastructure.audit', @resource_id,
                'Success', 'system.audit.read', @correlation_id, @causation_id,
                @request_id, 'Api', @occurred_at, @metadata, NULL)
            """,
            connection,
            transaction);

    private static void AddAppendParameters(
        NpgsqlCommand command,
        Guid auditId,
        Guid tenantId,
        Guid identityId,
        Guid membershipId,
        string requestId,
        string metadata)
    {
        command.Parameters.AddWithValue("id", auditId);
        command.Parameters.AddWithValue("tenant_id", tenantId);
        command.Parameters.AddWithValue("identity_id", identityId);
        command.Parameters.AddWithValue("membership_id", membershipId);
        command.Parameters.AddWithValue("resource_id", auditId.ToString());
        command.Parameters.AddWithValue("correlation_id", Guid.NewGuid());
        command.Parameters.AddWithValue("causation_id", Guid.NewGuid());
        command.Parameters.AddWithValue("request_id", requestId);
        command.Parameters.AddWithValue("occurred_at", DateTimeOffset.UtcNow);
        command.Parameters.Add("metadata", NpgsqlDbType.Jsonb).Value = metadata;
    }

    private static async Task AssertActorEvidenceAsync(NpgsqlConnection admin)
    {
        await using var command = new NpgsqlCommand(
            """
            SELECT tenant_id, identity_subject_id, tenant_membership_id, permission_key,
                   correlation_id, request_id, outcome, source,
                   occurred_at_utc, recorded_at_utc, metadata_json->>'operation'
            FROM audit_records WHERE id = @id
            """,
            admin);
        command.Parameters.AddWithValue("id", AuditAId);
        await using var reader = await command.ExecuteReaderAsync();
        Require(await reader.ReadAsync(), "Tenant A audit record was missing.");
        Require(reader.GetGuid(0) == TenantAId, "Audit tenant was not captured.");
        Require(reader.GetGuid(1) == IdentityAId, "Audit identity was not captured.");
        Require(reader.GetGuid(2) == MembershipAId, "Audit membership was not captured.");
        Require(reader.GetString(3) == "system.audit.read", "Audit permission was not captured.");
        Require(reader.GetGuid(4) != Guid.Empty, "Audit correlation ID was missing.");
        Require(reader.GetString(5) == "request-a", "Audit request ID was not captured.");
        Require(reader.GetString(6) == "Success" && reader.GetString(7) == "Api", "Audit outcome or source was incorrect.");
        Require(reader.GetFieldValue<DateTimeOffset>(8).Offset == TimeSpan.Zero, "Occurred timestamp was not UTC.");
        Require(reader.GetFieldValue<DateTimeOffset>(9).Offset == TimeSpan.Zero, "Recorded timestamp was not UTC.");
        Require(reader.GetString(10) == "verification", "Safe audit metadata was not captured.");
    }

    private static async Task AssertTenantIsolationAndPoolSafetyAsync(NpgsqlConnection runtime)
    {
        await using (var transactionA = await runtime.BeginTransactionAsync())
        {
            await SetTenantAsync(runtime, TenantAId, transactionA);
            Require(await CountVisibleAsync(runtime, transactionA) == 1, "Tenant A audit RLS count was incorrect.");
            await transactionA.RollbackAsync();
        }

        await using (var transactionB = await runtime.BeginTransactionAsync())
        {
            await SetTenantAsync(runtime, TenantBId, transactionB);
            Require(await CountVisibleAsync(runtime, transactionB) == 1, "Tenant B audit RLS count was incorrect.");
            await transactionB.RollbackAsync();
        }

        await using var noContext = await runtime.BeginTransactionAsync();
        Require(await CountVisibleAsync(runtime, noContext) == 0, "Pooled connection leaked audit tenant context.");
        await noContext.RollbackAsync();
    }

    private static async Task AssertCrossTenantAppendRejectedAsync(NpgsqlConnection runtime)
    {
        await using var transaction = await runtime.BeginTransactionAsync();
        await SetTenantAsync(runtime, TenantAId, transaction);
        await using var command = CreateAppendCommand(runtime, transaction);
        AddAppendParameters(command, Guid.NewGuid(), TenantBId, IdentityBId, MembershipBId, "cross-tenant", "{}");
        await AssertPostgresRejectedAsync(command, PostgresErrorCodes.InsufficientPrivilege, "Cross-tenant audit append succeeded.");
        await transaction.RollbackAsync();
    }

    private static async Task AssertRuntimeMutationsRejectedAsync(NpgsqlConnection runtime)
    {
        await AssertRuntimeSqlRejectedAsync(runtime, "UPDATE audit_records SET action = 'authenticate' WHERE id = @id", "Runtime updated an audit record.");
        await AssertRuntimeSqlRejectedAsync(runtime, "DELETE FROM audit_records WHERE id = @id", "Runtime deleted an audit record.");
        await AssertRuntimeSqlRejectedAsync(runtime, "TRUNCATE TABLE audit_records", "Runtime truncated audit records.");
        await AssertRuntimeSqlRejectedAsync(
            runtime,
            "INSERT INTO audit_records (id) VALUES (@id)",
            "Runtime bypassed the append function.");
    }

    private static async Task AssertRuntimeSqlRejectedAsync(
        NpgsqlConnection runtime,
        string sql,
        string failureMessage)
    {
        try
        {
            await using var command = new NpgsqlCommand(sql, runtime);
            command.Parameters.AddWithValue("id", AuditAId);
            await command.ExecuteNonQueryAsync();
        }
        catch (PostgresException exception) when (exception.SqlState == PostgresErrorCodes.InsufficientPrivilege)
        {
            return;
        }

        throw new InvalidOperationException(failureMessage);
    }

    private static async Task AssertExistingRecordUnchangedAsync(NpgsqlConnection admin)
    {
        await using var command = new NpgsqlCommand(
            "SELECT action FROM audit_records WHERE id = @id", admin);
        command.Parameters.AddWithValue("id", AuditAId);
        Require(Equals(await command.ExecuteScalarAsync(), "authorize"), "Existing audit evidence changed.");
    }

    private static async Task AssertMutationTriggerRejectedAsync(NpgsqlConnection admin)
    {
        foreach (var sql in AdministrativeMutationSql)
        {
            try
            {
                await using var command = new NpgsqlCommand(sql, admin);
                command.Parameters.AddWithValue("id", AuditAId);
                await command.ExecuteNonQueryAsync();
            }
            catch (PostgresException exception) when (exception.SqlState == PostgresErrorCodes.InsufficientPrivilege)
            {
                continue;
            }

            throw new InvalidOperationException("Audit mutation trigger did not reject an administrative mutation.");
        }
    }

    private static async Task AssertOversizedMetadataRejectedAsync(NpgsqlConnection runtime)
    {
        await using var transaction = await runtime.BeginTransactionAsync();
        await SetTenantAsync(runtime, TenantAId, transaction);
        await using var command = CreateAppendCommand(runtime, transaction);
        AddAppendParameters(
            command,
            Guid.NewGuid(),
            TenantAId,
            IdentityAId,
            MembershipAId,
            "oversized",
            "{\"value\":\"" + new string('x', 5000) + "\"}");
        await AssertPostgresRejectedAsync(command, PostgresErrorCodes.CheckViolation, "Oversized audit metadata was stored.");
        await transaction.RollbackAsync();
    }

    private static async Task AssertSystemBoundaryRejectedAsync(NpgsqlConnection runtime)
    {
        await using var transaction = await runtime.BeginTransactionAsync();
        await SetTenantAsync(runtime, TenantAId, transaction);
        await using var command = CreateAppendCommand(runtime, transaction);
        command.CommandText = command.CommandText.Replace("'Api'", "'System'", StringComparison.Ordinal);
        AddAppendParameters(command, Guid.NewGuid(), TenantAId, IdentityAId, MembershipAId, "system", "{}");
        await AssertPostgresRejectedAsync(command, PostgresErrorCodes.InsufficientPrivilege, "Runtime wrote a system audit event.");
        await transaction.RollbackAsync();
    }

    private static async Task AssertPostgresRejectedAsync(
        NpgsqlCommand command,
        string sqlState,
        string failureMessage)
    {
        try
        {
            await command.ExecuteNonQueryAsync();
        }
        catch (PostgresException exception) when (exception.SqlState == sqlState)
        {
            return;
        }

        throw new InvalidOperationException(failureMessage);
    }

    private static async Task SetTenantAsync(
        NpgsqlConnection connection,
        Guid tenantId,
        NpgsqlTransaction transaction)
    {
        await using var command = new NpgsqlCommand(
            "SELECT set_config('crevafactos.tenant_id', @tenant_id, true)", connection, transaction);
        command.Parameters.AddWithValue("tenant_id", tenantId.ToString());
        await command.ExecuteScalarAsync();
    }

    private static async Task<long> CountVisibleAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction)
    {
        await using var command = new NpgsqlCommand("SELECT COUNT(*) FROM audit_records", connection, transaction);
        return (long)(await command.ExecuteScalarAsync() ?? -1L);
    }

    private static async Task CleanupAsync(NpgsqlConnection admin)
    {
        if (await TableExistsAsync(admin, "audit_records"))
        {
            await using var auditCleanup = new NpgsqlCommand(
                """
                ALTER TABLE audit_records DISABLE TRIGGER audit_records_reject_mutation;
                DELETE FROM audit_records WHERE id = ANY(@audit_ids);
                ALTER TABLE audit_records ENABLE TRIGGER audit_records_reject_mutation;
                """,
                admin);
            auditCleanup.Parameters.AddWithValue("audit_ids", AuditIds);
            await auditCleanup.ExecuteNonQueryAsync();
        }

        const string actorCleanupSql = """
            DELETE FROM tenant_membership_roles WHERE tenant_id = ANY(@tenant_ids);
            DELETE FROM tenant_role_permissions WHERE tenant_id = ANY(@tenant_ids);
            DELETE FROM tenant_roles WHERE tenant_id = ANY(@tenant_ids);
            DELETE FROM tenant_memberships WHERE identity_subject_id = ANY(@identity_ids);
            DELETE FROM identity_subjects WHERE id = ANY(@identity_ids);
            DELETE FROM tenants WHERE id = ANY(@tenant_ids);
            """;
        await using var actorCleanup = new NpgsqlCommand(actorCleanupSql, admin);
        AddParameters(actorCleanup);
        await actorCleanup.ExecuteNonQueryAsync();
    }

    private static async Task<bool> TableExistsAsync(NpgsqlConnection admin, string tableName)
    {
        await using var command = new NpgsqlCommand("SELECT to_regclass(@table_name) IS NOT NULL", admin);
        command.Parameters.AddWithValue("table_name", $"public.{tableName}");
        return (bool)(await command.ExecuteScalarAsync() ?? false);
    }

    private static void AddParameters(NpgsqlCommand command)
    {
        command.Parameters.AddWithValue("tenant_a", TenantAId);
        command.Parameters.AddWithValue("tenant_b", TenantBId);
        command.Parameters.AddWithValue("identity_a", IdentityAId);
        command.Parameters.AddWithValue("identity_b", IdentityBId);
        command.Parameters.AddWithValue("identity_without_permission", IdentityWithoutPermissionId);
        command.Parameters.AddWithValue("membership_a", MembershipAId);
        command.Parameters.AddWithValue("membership_b", MembershipBId);
        command.Parameters.AddWithValue("membership_without_permission", MembershipWithoutPermissionId);
        command.Parameters.AddWithValue("role_a", RoleAId);
        command.Parameters.AddWithValue("role_b", RoleBId);
        command.Parameters.AddWithValue("audit_read_permission", AuditReadPermissionId);
        command.Parameters.AddWithValue("tenant_ids", TenantIds);
        command.Parameters.AddWithValue("identity_ids", IdentityIds);
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
