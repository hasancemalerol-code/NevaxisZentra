using NevaxisZentra.Persistence;
using Npgsql;

namespace NevaxisZentra.DatabaseTool;

internal static class AuthenticationMembershipVerifier
{
    private const string Issuer = "https://identity.local.test";
    private static readonly Guid TenantAId = Guid.Parse("aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaa1");
    private static readonly Guid TenantBId = Guid.Parse("bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbb2");
    private static readonly Guid InactiveTenantId = Guid.Parse("cccccccc-cccc-4ccc-8ccc-ccccccccccc3");
    private static readonly Guid ActiveIdentityId = Guid.Parse("dddddddd-0000-4000-8000-000000000001");
    private static readonly Guid NoMembershipIdentityId = Guid.Parse("dddddddd-0000-4000-8000-000000000002");
    private static readonly Guid InactiveMembershipIdentityId = Guid.Parse("dddddddd-0000-4000-8000-000000000003");
    private static readonly Guid InactiveIdentityId = Guid.Parse("dddddddd-0000-4000-8000-000000000004");
    private static readonly Guid InactiveTenantIdentityId = Guid.Parse("dddddddd-0000-4000-8000-000000000005");
    private static readonly Guid[] IdentityIds =
    [
        ActiveIdentityId,
        NoMembershipIdentityId,
        InactiveMembershipIdentityId,
        InactiveIdentityId,
        InactiveTenantIdentityId,
    ];
    private static readonly Guid[] TenantIds = [TenantAId, TenantBId, InactiveTenantId];

    public static async Task VerifyAsync()
    {
        var runtimeOptions = DatabaseConfiguration.LoadRuntime();
        var migrationOptions = DatabaseConfiguration.LoadMigrations();
        await using var administrativeConnection = new NpgsqlConnection(
            PostgreSqlConnectionFactory.BuildConnectionString(migrationOptions));
        await administrativeConnection.OpenAsync();
        await LocalRuntimeRoleProvisioner.ProvisionAsync(administrativeConnection, runtimeOptions.Password);
        await SeedAsync(administrativeConnection);

        try
        {
            await using var runtimeConnection = new NpgsqlConnection(
                PostgreSqlConnectionFactory.BuildConnectionString(runtimeOptions));
            await runtimeConnection.OpenAsync();

            await AssertIdentityResolutionAsync(runtimeConnection);
            await AssertMembershipResolutionAsync(runtimeConnection);
            await AssertPreTenantBoundaryAsync(runtimeConnection);
            await AssertMembershipRlsAsync(runtimeConnection);
        }
        finally
        {
            await CleanupAsync(administrativeConnection);
        }

        Console.WriteLine("Authenticated identity and active tenant-membership resolution verified against PostgreSQL.");
        Console.WriteLine("Inactive identity, inactive membership, missing membership, inactive tenant, and cross-tenant selection failed closed.");
        Console.WriteLine("Pre-tenant identity lookup remained exact and identity subjects were not directly enumerable.");
    }

    public static async Task BootstrapAsync()
    {
        var runtimeOptions = DatabaseConfiguration.LoadRuntime();
        var migrationOptions = DatabaseConfiguration.LoadMigrations();
        await using var connection = new NpgsqlConnection(
            PostgreSqlConnectionFactory.BuildConnectionString(migrationOptions));
        await connection.OpenAsync();
        await LocalRuntimeRoleProvisioner.ProvisionAsync(connection, runtimeOptions.Password);
        await SeedAsync(connection);
        Console.WriteLine("Development authentication/membership fixtures created.");
    }

    public static async Task CleanupFixturesAsync()
    {
        var migrationOptions = DatabaseConfiguration.LoadMigrations();
        await using var connection = new NpgsqlConnection(
            PostgreSqlConnectionFactory.BuildConnectionString(migrationOptions));
        await connection.OpenAsync();
        await CleanupAsync(connection);
        Console.WriteLine("Development authentication/membership fixtures removed.");
    }

    private static async Task SeedAsync(NpgsqlConnection connection)
    {
        const string sql = """
            DELETE FROM tenant_memberships WHERE identity_subject_id = ANY(@identity_ids);
            DELETE FROM identity_subjects WHERE id = ANY(@identity_ids);
            DELETE FROM tenants WHERE id = ANY(@tenant_ids);

            INSERT INTO tenants (id, tenant_key, display_name, status, created_at_utc)
            VALUES
                (@tenant_a_id, 'auth-a', 'Authentication Tenant A', 'Active', CURRENT_TIMESTAMP),
                (@tenant_b_id, 'auth-b', 'Authentication Tenant B', 'Active', CURRENT_TIMESTAMP),
                (@inactive_tenant_id, 'auth-inactive', 'Authentication Inactive Tenant', 'Inactive', CURRENT_TIMESTAMP);

            INSERT INTO identity_subjects (id, external_issuer, external_subject, status, created_at_utc)
            VALUES
                (@active_identity_id, @issuer, 'active-member', 'Active', CURRENT_TIMESTAMP),
                (@no_membership_identity_id, @issuer, 'no-membership', 'Active', CURRENT_TIMESTAMP),
                (@inactive_membership_identity_id, @issuer, 'inactive-membership', 'Active', CURRENT_TIMESTAMP),
                (@inactive_identity_id, @issuer, 'inactive-identity', 'Inactive', CURRENT_TIMESTAMP),
                (@inactive_tenant_identity_id, @issuer, 'inactive-tenant', 'Active', CURRENT_TIMESTAMP);

            INSERT INTO tenant_memberships (id, tenant_id, identity_subject_id, membership_reference, status, created_at_utc)
            VALUES
                ('eeeeeeee-0000-4000-8000-000000000001', @tenant_a_id, @active_identity_id, 'authentication-fixture-active', 'Active', CURRENT_TIMESTAMP),
                ('eeeeeeee-0000-4000-8000-000000000002', @tenant_a_id, @inactive_membership_identity_id, 'authentication-fixture-inactive-membership', 'Inactive', CURRENT_TIMESTAMP),
                ('eeeeeeee-0000-4000-8000-000000000003', @tenant_a_id, @inactive_identity_id, 'authentication-fixture-inactive-identity', 'Active', CURRENT_TIMESTAMP),
                ('eeeeeeee-0000-4000-8000-000000000004', @inactive_tenant_id, @inactive_tenant_identity_id, 'authentication-fixture-inactive-tenant', 'Active', CURRENT_TIMESTAMP);
            """;
        await using var command = new NpgsqlCommand(sql, connection);
        AddFixtureParameters(command);
        await command.ExecuteNonQueryAsync();
    }

    private static async Task CleanupAsync(NpgsqlConnection connection)
    {
        const string sql = """
            DELETE FROM tenant_memberships WHERE identity_subject_id = ANY(@identity_ids);
            DELETE FROM identity_subjects WHERE id = ANY(@identity_ids);
            DELETE FROM tenants WHERE id = ANY(@tenant_ids);
            """;
        await using var command = new NpgsqlCommand(sql, connection);
        AddFixtureParameters(command);
        await command.ExecuteNonQueryAsync();
    }

    private static async Task AssertIdentityResolutionAsync(NpgsqlConnection connection)
    {
        Require(await ResolveIdentityAsync(connection, "active-member") == ActiveIdentityId, "Active identity resolution failed.");
        Require(await ResolveIdentityAsync(connection, "inactive-identity") is null, "Inactive identity resolved.");
        Require(await ResolveIdentityAsync(connection, "unknown-identity") is null, "Unknown identity resolved.");
    }

    private static async Task<Guid?> ResolveIdentityAsync(NpgsqlConnection connection, string subject)
    {
        await using var command = new NpgsqlCommand(
            "SELECT identity_subject_id FROM resolve_active_identity(@issuer, @subject)",
            connection);
        command.Parameters.AddWithValue("issuer", Issuer);
        command.Parameters.AddWithValue("subject", subject);
        var result = await command.ExecuteScalarAsync();
        return result is Guid identitySubjectId ? identitySubjectId : null;
    }

    private static async Task AssertMembershipResolutionAsync(NpgsqlConnection connection)
    {
        Require(await ResolveMembershipAsync(connection, ActiveIdentityId, "auth-a") == TenantAId, "Tenant key selection failed.");
        Require(await ResolveMembershipAsync(connection, ActiveIdentityId, TenantAId.ToString()) == TenantAId, "Tenant UUID selection failed.");
        Require(await ResolveMembershipAsync(connection, ActiveIdentityId, "auth-b") is null, "Tenant A identity selected tenant B.");
        Require(await ResolveMembershipAsync(connection, NoMembershipIdentityId, "auth-a") is null, "Missing membership resolved.");
        Require(await ResolveMembershipAsync(connection, InactiveMembershipIdentityId, "auth-a") is null, "Inactive membership resolved.");
        Require(await ResolveMembershipAsync(connection, InactiveIdentityId, "auth-a") is null, "Inactive identity membership resolved.");
        Require(await ResolveMembershipAsync(connection, InactiveTenantIdentityId, "auth-inactive") is null, "Inactive tenant resolved.");
        Require(await ResolveMembershipAsync(connection, ActiveIdentityId, "auth-unknown") is null, "Unknown tenant resolved.");
    }

    private static async Task<Guid?> ResolveMembershipAsync(
        NpgsqlConnection connection,
        Guid identitySubjectId,
        string selector)
    {
        await using var command = new NpgsqlCommand(
            """
            SELECT tenant_id
            FROM resolve_active_tenant_membership(@identity_subject_id, @tenant_identifier)
            """,
            connection);
        command.Parameters.AddWithValue("identity_subject_id", identitySubjectId);
        command.Parameters.AddWithValue("tenant_identifier", selector);
        var result = await command.ExecuteScalarAsync();
        return result is Guid tenantId ? tenantId : null;
    }

    private static async Task AssertPreTenantBoundaryAsync(NpgsqlConnection connection)
    {
        await AssertInsufficientPrivilegeAsync(
            connection,
            "SELECT COUNT(*) FROM identity_subjects",
            "Runtime role enumerated identity subjects.");
        await AssertInsufficientPrivilegeAsync(
            connection,
            "SELECT tenant_id FROM resolve_active_tenant('auth-a')",
            "The legacy tenant resolver remained callable by the runtime role.");
    }

    private static async Task AssertMembershipRlsAsync(NpgsqlConnection connection)
    {
        await using (var tenantATransaction = await connection.BeginTransactionAsync())
        {
            await SetTenantAsync(connection, TenantAId);
            Require(await CountMembershipsAsync(connection) == 3, "Tenant A membership RLS count was incorrect.");
            await tenantATransaction.RollbackAsync();
        }

        await using (var tenantBTransaction = await connection.BeginTransactionAsync())
        {
            await SetTenantAsync(connection, TenantBId);
            Require(await CountMembershipsAsync(connection) == 0, "Tenant B read tenant A memberships.");
            await tenantBTransaction.RollbackAsync();
        }

        Require(await CountMembershipsAsync(connection) == 0, "Missing tenant context exposed memberships.");
    }

    private static async Task SetTenantAsync(NpgsqlConnection connection, Guid tenantId)
    {
        await using var command = new NpgsqlCommand(
            "SELECT set_config('NevaxisZentra.tenant_id', @tenant_id, true)",
            connection);
        command.Parameters.AddWithValue("tenant_id", tenantId.ToString());
        await command.ExecuteScalarAsync();
    }

    private static async Task<long> CountMembershipsAsync(NpgsqlConnection connection)
    {
        await using var command = new NpgsqlCommand("SELECT COUNT(*) FROM tenant_memberships", connection);
        return (long)(await command.ExecuteScalarAsync() ?? -1L);
    }

    private static async Task AssertInsufficientPrivilegeAsync(
        NpgsqlConnection connection,
        string sql,
        string failureMessage)
    {
        try
        {
            await using var command = new NpgsqlCommand(sql, connection);
            await command.ExecuteNonQueryAsync();
        }
        catch (PostgresException exception) when (exception.SqlState == PostgresErrorCodes.InsufficientPrivilege)
        {
            return;
        }

        throw new InvalidOperationException(failureMessage);
    }

    private static void AddFixtureParameters(NpgsqlCommand command)
    {
        command.Parameters.AddWithValue("issuer", Issuer);
        command.Parameters.AddWithValue("tenant_a_id", TenantAId);
        command.Parameters.AddWithValue("tenant_b_id", TenantBId);
        command.Parameters.AddWithValue("inactive_tenant_id", InactiveTenantId);
        command.Parameters.AddWithValue("active_identity_id", ActiveIdentityId);
        command.Parameters.AddWithValue("no_membership_identity_id", NoMembershipIdentityId);
        command.Parameters.AddWithValue("inactive_membership_identity_id", InactiveMembershipIdentityId);
        command.Parameters.AddWithValue("inactive_identity_id", InactiveIdentityId);
        command.Parameters.AddWithValue("inactive_tenant_identity_id", InactiveTenantIdentityId);
        command.Parameters.AddWithValue("identity_ids", IdentityIds);
        command.Parameters.AddWithValue("tenant_ids", TenantIds);
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
