using CrevaFactOS.Persistence;
using Npgsql;

namespace CrevaFactOS.DatabaseTool;

internal static class PermissionAuthorizationVerifier
{
    private static readonly Guid TenantAId = Guid.Parse("a6000000-0000-4000-8000-000000000001");
    private static readonly Guid TenantBId = Guid.Parse("a6000000-0000-4000-8000-000000000002");
    private static readonly Guid IdentityAId = Guid.Parse("a6000000-0000-4000-8000-000000000011");
    private static readonly Guid IdentityBId = Guid.Parse("a6000000-0000-4000-8000-000000000012");
    private static readonly Guid IdentityAloneId = Guid.Parse("a6000000-0000-4000-8000-000000000013");
    private static readonly Guid MembershipAId = Guid.Parse("a6000000-0000-4000-8000-000000000021");
    private static readonly Guid MembershipAloneId = Guid.Parse("a6000000-0000-4000-8000-000000000022");
    private static readonly Guid MembershipBId = Guid.Parse("a6000000-0000-4000-8000-000000000023");
    private static readonly Guid ActiveRoleAId = Guid.Parse("a6000000-0000-4000-8000-000000000031");
    private static readonly Guid InactiveRoleAId = Guid.Parse("a6000000-0000-4000-8000-000000000032");
    private static readonly Guid RoleBId = Guid.Parse("a6000000-0000-4000-8000-000000000033");
    private static readonly Guid SystemDiagnosticsPermissionId = Guid.Parse("10000000-0000-4000-8000-000000000001");
    private static readonly Guid TenantContextPermissionId = Guid.Parse("10000000-0000-4000-8000-000000000002");
    private static readonly Guid AuthorizationDiagnosticsPermissionId = Guid.Parse("10000000-0000-4000-8000-000000000003");
    private static readonly Guid[] TenantIds = [TenantAId, TenantBId];
    private static readonly Guid[] IdentityIds = [IdentityAId, IdentityBId, IdentityAloneId];

    public static async Task VerifyAsync()
    {
        var runtimeOptions = DatabaseConfiguration.LoadRuntime();
        var migrationOptions = DatabaseConfiguration.LoadMigrations();
        await using var admin = new NpgsqlConnection(
            PostgreSqlConnectionFactory.BuildConnectionString(migrationOptions));
        await admin.OpenAsync();
        await LocalRuntimeRoleProvisioner.ProvisionAsync(admin, runtimeOptions.Password);
        await SeedAsync(admin);

        try
        {
            await using var runtime = new NpgsqlConnection(
                PostgreSqlConnectionFactory.BuildConnectionString(runtimeOptions));
            await runtime.OpenAsync();

            Require(!await HasPermissionAsync(runtime, IdentityAloneId, TenantAId, MembershipAloneId, "tenant.context.read"),
                "Membership alone granted a permission.");
            Require(await HasPermissionAsync(runtime, IdentityAId, TenantAId, MembershipAId, "tenant.context.read"),
                "Exact active permission assignment was denied.");
            Require(await HasPermissionAsync(runtime, IdentityAId, TenantAId, MembershipAId, "authorization.diagnostics.read"),
                "Authorization diagnostics permission was denied.");
            Require(!await HasPermissionAsync(runtime, IdentityAId, TenantAId, MembershipAId, "missing.infrastructure.read"),
                "Unassigned permission was granted.");
            Require(!await HasPermissionAsync(runtime, IdentityAId, TenantAId, MembershipAId, "system.diagnostics.read"),
                "Inactive role granted a permission.");
            Require(!await HasPermissionAsync(runtime, IdentityAId, TenantAId, MembershipAId, "Tenant.Context.Read"),
                "Permission matching was not case-sensitive.");
            Require(!await HasPermissionAsync(runtime, IdentityAId, TenantAId, MembershipAId, "tenant.context"),
                "Permission prefix matching was enabled.");
            Require(!await HasPermissionAsync(runtime, IdentityAId, TenantAId, MembershipAId, "*"),
                "Wildcard permission matching was enabled.");
            Require(!await HasPermissionAsync(runtime, IdentityAId, TenantBId, MembershipAId, "tenant.context.read"),
                "Tenant A assignment authorized tenant B.");

            await AssertInactivePermissionDeniedAsync(admin, runtime);
            await AssertRoleAssignmentLifecycleAsync(admin, runtime);
            await AssertCrossTenantAssignmentRejectedAsync(admin);
            await AssertRlsAndPooledIsolationAsync(runtime);
        }
        finally
        {
            await CleanupAsync(admin);
        }

        Console.WriteLine("Exact tenant-scoped permission evaluation verified against PostgreSQL.");
        Console.WriteLine("Membership-only, inactive, unassigned, case, prefix, wildcard, and cross-tenant checks failed closed.");
        Console.WriteLine("Cross-tenant role assignment was rejected by composite foreign keys; authorization RLS and pooled isolation passed.");
    }

    public static async Task BootstrapAsync()
    {
        var runtimeOptions = DatabaseConfiguration.LoadRuntime();
        await using var admin = new NpgsqlConnection(
            PostgreSqlConnectionFactory.BuildConnectionString(DatabaseConfiguration.LoadMigrations()));
        await admin.OpenAsync();
        await LocalRuntimeRoleProvisioner.ProvisionAsync(admin, runtimeOptions.Password);
        await SeedAsync(admin);
        Console.WriteLine("Development permission-authorization fixtures created.");
    }

    public static async Task CleanupFixturesAsync()
    {
        await using var admin = new NpgsqlConnection(
            PostgreSqlConnectionFactory.BuildConnectionString(DatabaseConfiguration.LoadMigrations()));
        await admin.OpenAsync();
        await CleanupAsync(admin);
        Console.WriteLine("Development permission-authorization fixtures removed.");
    }

    private static async Task SeedAsync(NpgsqlConnection connection)
    {
        await CleanupAsync(connection);
        const string sql = """
            INSERT INTO tenants (id, tenant_key, display_name, status, created_at_utc)
            VALUES
                (@tenant_a, 'authorization-a', 'Authorization Tenant A', 'Active', CURRENT_TIMESTAMP),
                (@tenant_b, 'authorization-b', 'Authorization Tenant B', 'Active', CURRENT_TIMESTAMP);

            INSERT INTO identity_subjects (id, external_issuer, external_subject, status, created_at_utc)
            VALUES
                (@identity_a, 'https://authorization.local.test', 'tenant-a-member', 'Active', CURRENT_TIMESTAMP),
                (@identity_b, 'https://authorization.local.test', 'tenant-b-member', 'Active', CURRENT_TIMESTAMP),
                (@identity_alone, 'https://authorization.local.test', 'membership-only', 'Active', CURRENT_TIMESTAMP);

            INSERT INTO tenant_memberships (id, tenant_id, identity_subject_id, membership_reference, status, created_at_utc)
            VALUES
                (@membership_a, @tenant_a, @identity_a, 'authorization-fixture-a', 'Active', CURRENT_TIMESTAMP),
                (@membership_alone, @tenant_a, @identity_alone, 'authorization-fixture-alone', 'Active', CURRENT_TIMESTAMP),
                (@membership_b, @tenant_b, @identity_b, 'authorization-fixture-b', 'Active', CURRENT_TIMESTAMP);

            INSERT INTO tenant_roles (id, tenant_id, key, display_name, status, created_at_utc)
            VALUES
                (@active_role_a, @tenant_a, 'diagnostic-reader', 'Diagnostic Reader', 'Active', CURRENT_TIMESTAMP),
                (@inactive_role_a, @tenant_a, 'inactive-reader', 'Inactive Reader', 'Inactive', CURRENT_TIMESTAMP),
                (@role_b, @tenant_b, 'diagnostic-reader', 'Diagnostic Reader', 'Active', CURRENT_TIMESTAMP);

            INSERT INTO tenant_role_permissions (tenant_id, tenant_role_id, permission_definition_id)
            VALUES
                (@tenant_a, @active_role_a, @tenant_context_permission),
                (@tenant_a, @active_role_a, @authorization_diagnostics_permission),
                (@tenant_a, @inactive_role_a, @system_diagnostics_permission),
                (@tenant_b, @role_b, @tenant_context_permission);

            INSERT INTO tenant_membership_roles (tenant_id, tenant_membership_id, tenant_role_id, status, assigned_at_utc)
            VALUES
                (@tenant_a, @membership_a, @active_role_a, 'Active', CURRENT_TIMESTAMP),
                (@tenant_a, @membership_a, @inactive_role_a, 'Active', CURRENT_TIMESTAMP),
                (@tenant_b, @membership_b, @role_b, 'Active', CURRENT_TIMESTAMP);
            """;
        await using var command = new NpgsqlCommand(sql, connection);
        AddParameters(command);
        await command.ExecuteNonQueryAsync();
    }

    private static async Task AssertInactivePermissionDeniedAsync(
        NpgsqlConnection admin,
        NpgsqlConnection runtime)
    {
        await using (var deactivate = new NpgsqlCommand(
            "UPDATE permission_definitions SET status = 'Inactive' WHERE id = @permission_id",
            admin))
        {
            deactivate.Parameters.AddWithValue("permission_id", TenantContextPermissionId);
            await deactivate.ExecuteNonQueryAsync();
        }

        try
        {
            Require(!await HasPermissionAsync(runtime, IdentityAId, TenantAId, MembershipAId, "tenant.context.read"),
                "Inactive permission definition granted access.");
        }
        finally
        {
            await using var reactivate = new NpgsqlCommand(
                "UPDATE permission_definitions SET status = 'Active' WHERE id = @permission_id",
                admin);
            reactivate.Parameters.AddWithValue("permission_id", TenantContextPermissionId);
            await reactivate.ExecuteNonQueryAsync();
        }
    }

    private static async Task AssertCrossTenantAssignmentRejectedAsync(NpgsqlConnection admin)
    {
        try
        {
            await using var command = new NpgsqlCommand(
                """
                INSERT INTO tenant_membership_roles
                    (tenant_id, tenant_membership_id, tenant_role_id, status, assigned_at_utc)
                VALUES (@tenant_a, @membership_a, @role_b, 'Active', CURRENT_TIMESTAMP)
                """,
                admin);
            AddParameters(command);
            await command.ExecuteNonQueryAsync();
        }
        catch (PostgresException exception) when (exception.SqlState == PostgresErrorCodes.ForeignKeyViolation)
        {
            return;
        }

        throw new InvalidOperationException("Cross-tenant membership-role assignment was accepted.");
    }

    private static async Task AssertRoleAssignmentLifecycleAsync(
        NpgsqlConnection admin,
        NpgsqlConnection runtime)
    {
        await using (var activateRole = new NpgsqlCommand(
            "UPDATE tenant_roles SET status = 'Active' WHERE id = @role_id", admin))
        {
            activateRole.Parameters.AddWithValue("role_id", InactiveRoleAId);
            await activateRole.ExecuteNonQueryAsync();
        }

        Require(await HasPermissionAsync(runtime, IdentityAId, TenantAId, MembershipAId, "system.diagnostics.read"),
            "Active role and assignment did not grant their exact permission.");

        await using (var deactivateAssignment = new NpgsqlCommand(
            """
            UPDATE tenant_membership_roles
            SET status = 'Inactive'
            WHERE tenant_membership_id = @membership_id AND tenant_role_id = @role_id
            """, admin))
        {
            deactivateAssignment.Parameters.AddWithValue("membership_id", MembershipAId);
            deactivateAssignment.Parameters.AddWithValue("role_id", InactiveRoleAId);
            await deactivateAssignment.ExecuteNonQueryAsync();
        }

        Require(!await HasPermissionAsync(runtime, IdentityAId, TenantAId, MembershipAId, "system.diagnostics.read"),
            "Inactive membership-role assignment granted a permission.");
    }

    private static async Task AssertRlsAndPooledIsolationAsync(NpgsqlConnection runtime)
    {
        await using (var transactionA = await runtime.BeginTransactionAsync())
        {
            await SetTenantAsync(runtime, TenantAId);
            Require(await CountAsync(runtime, "tenant_roles") == 2, "Tenant A role RLS count was incorrect.");
            Require(await CountAsync(runtime, "tenant_role_permissions") == 3, "Tenant A role-permission RLS count was incorrect.");
            Require(await CountAsync(runtime, "tenant_membership_roles") == 2, "Tenant A assignment RLS count was incorrect.");
            await transactionA.RollbackAsync();
        }

        await using (var transactionB = await runtime.BeginTransactionAsync())
        {
            await SetTenantAsync(runtime, TenantBId);
            Require(await CountAsync(runtime, "tenant_roles") == 1, "Tenant B role RLS count was incorrect.");
            Require(await CountAsync(runtime, "tenant_role_permissions") == 1, "Tenant B role-permission RLS count was incorrect.");
            Require(await CountAsync(runtime, "tenant_membership_roles") == 1, "Tenant B assignment RLS count was incorrect.");
            await transactionB.RollbackAsync();
        }

        Require(await CountAsync(runtime, "tenant_roles") == 0, "Pooled connection leaked tenant role context.");
        Require(await CountAsync(runtime, "tenant_membership_roles") == 0, "Pooled connection leaked role assignments.");
    }

    private static async Task<bool> HasPermissionAsync(
        NpgsqlConnection connection,
        Guid identityId,
        Guid tenantId,
        Guid membershipId,
        string permissionKey)
    {
        await using var command = new NpgsqlCommand(
            "SELECT has_tenant_permission(@identity_id, @tenant_id, @membership_id, @permission_key)",
            connection);
        command.Parameters.AddWithValue("identity_id", identityId);
        command.Parameters.AddWithValue("tenant_id", tenantId);
        command.Parameters.AddWithValue("membership_id", membershipId);
        command.Parameters.AddWithValue("permission_key", permissionKey);
        return (bool)(await command.ExecuteScalarAsync() ?? false);
    }

    private static async Task SetTenantAsync(NpgsqlConnection connection, Guid tenantId)
    {
        await using var command = new NpgsqlCommand(
            "SELECT set_config('crevafactos.tenant_id', @tenant_id, true)", connection);
        command.Parameters.AddWithValue("tenant_id", tenantId.ToString());
        await command.ExecuteScalarAsync();
    }

    private static async Task<long> CountAsync(NpgsqlConnection connection, string tableName)
    {
        await using var command = new NpgsqlCommand($"SELECT COUNT(*) FROM {tableName}", connection);
        return (long)(await command.ExecuteScalarAsync() ?? -1L);
    }

    private static async Task CleanupAsync(NpgsqlConnection connection)
    {
        const string sql = """
            DELETE FROM tenant_membership_roles WHERE tenant_id = ANY(@tenant_ids);
            DELETE FROM tenant_role_permissions WHERE tenant_id = ANY(@tenant_ids);
            DELETE FROM tenant_roles WHERE tenant_id = ANY(@tenant_ids);
            DELETE FROM tenant_memberships WHERE identity_subject_id = ANY(@identity_ids);
            DELETE FROM identity_subjects WHERE id = ANY(@identity_ids);
            DELETE FROM tenants WHERE id = ANY(@tenant_ids);
            """;
        await using var command = new NpgsqlCommand(sql, connection);
        AddParameters(command);
        await command.ExecuteNonQueryAsync();
    }

    private static void AddParameters(NpgsqlCommand command)
    {
        command.Parameters.AddWithValue("tenant_a", TenantAId);
        command.Parameters.AddWithValue("tenant_b", TenantBId);
        command.Parameters.AddWithValue("identity_a", IdentityAId);
        command.Parameters.AddWithValue("identity_b", IdentityBId);
        command.Parameters.AddWithValue("identity_alone", IdentityAloneId);
        command.Parameters.AddWithValue("membership_a", MembershipAId);
        command.Parameters.AddWithValue("membership_alone", MembershipAloneId);
        command.Parameters.AddWithValue("membership_b", MembershipBId);
        command.Parameters.AddWithValue("active_role_a", ActiveRoleAId);
        command.Parameters.AddWithValue("inactive_role_a", InactiveRoleAId);
        command.Parameters.AddWithValue("role_b", RoleBId);
        command.Parameters.AddWithValue("system_diagnostics_permission", SystemDiagnosticsPermissionId);
        command.Parameters.AddWithValue("tenant_context_permission", TenantContextPermissionId);
        command.Parameters.AddWithValue("authorization_diagnostics_permission", AuthorizationDiagnosticsPermissionId);
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
