using System.Globalization;
using Npgsql;

namespace NevaxisZentra.Persistence.Authorization;

public static class AcceptanceRoleContractVerifier
{
    public static async Task VerifyAsync(
        NpgsqlConnection connection,
        CancellationToken cancellationToken = default)
    {
        var activeKeys = await ReadStringsAsync(
            connection,
            "SELECT key FROM permission_definitions WHERE status = 'Active' ORDER BY key;",
            cancellationToken);
        var active = activeKeys.ToHashSet(StringComparer.Ordinal);
        var missing = AcceptanceRoleContract.RequiredPermissionKeys
            .Where(key => !active.Contains(key))
            .ToArray();
        if (missing.Length > 0)
        {
            throw new AcceptanceRoleContractException(
                "Acceptance role catalog contract failed. Missing active permission keys: "
                + string.Join(", ", missing));
        }

        var tenantCount = await ReadCountAsync(connection, "SELECT COUNT(*) FROM tenants;", cancellationToken);
        var roleCount = await ReadCountAsync(
            connection,
            "SELECT COUNT(*) FROM tenant_roles WHERE key = @role_key;",
            cancellationToken,
            ("role_key", AcceptanceRoleContract.RoleKey));
        if (roleCount == 0)
        {
            if (tenantCount == 0)
            {
                Console.WriteLine(
                    $"Acceptance role catalog contract PASS ({AcceptanceRoleContract.CatalogVersion}). "
                    + "Template has no tenant yet; role grants are applied by bootstrap.");
                return;
            }

            throw new AcceptanceRoleContractException(
                $"Acceptance role '{AcceptanceRoleContract.RoleKey}' is missing while {tenantCount} tenant row(s) exist.");
        }

        var granted = (await ReadStringsAsync(
            connection,
            """
            SELECT permission.key
            FROM tenant_role_permissions grant_row
            JOIN tenant_roles role ON role.id = grant_row.tenant_role_id
            JOIN permission_definitions permission ON permission.id = grant_row.permission_definition_id
            WHERE role.key = @role_key
              AND permission.status = 'Active';
            """,
            cancellationToken,
            ("role_key", AcceptanceRoleContract.RoleKey))).ToHashSet(StringComparer.Ordinal);
        var plan = AcceptanceRoleContract.Reconcile(active, granted);
        if (!plan.IsSatisfied)
        {
            throw new AcceptanceRoleContractException(
                "Acceptance role grant contract failed. Missing grants: "
                + string.Join(", ", plan.GrantsToAdd)
                + ". Forbidden grants still present: "
                + string.Join(", ", plan.ForbiddenGrantsToRemove));
        }

        Console.WriteLine(
            $"Acceptance role grant contract PASS ({AcceptanceRoleContract.CatalogVersion}) "
            + $"role={AcceptanceRoleContract.RoleKey} grants={granted.Count}.");
    }

    private static async Task<List<string>> ReadStringsAsync(
        NpgsqlConnection connection,
        string sql,
        CancellationToken cancellationToken,
        params (string Name, string Value)[] parameters)
    {
        await using var command = new NpgsqlCommand(sql, connection);
        foreach (var parameter in parameters)
        {
            command.Parameters.AddWithValue(parameter.Name, parameter.Value);
        }

        var values = new List<string>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            values.Add(reader.GetString(0));
        }

        return values;
    }

    private static async Task<long> ReadCountAsync(
        NpgsqlConnection connection,
        string sql,
        CancellationToken cancellationToken,
        params (string Name, string Value)[] parameters)
    {
        await using var command = new NpgsqlCommand(sql, connection);
        foreach (var parameter in parameters)
        {
            command.Parameters.AddWithValue(parameter.Name, parameter.Value);
        }

        var scalar = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt64(scalar, CultureInfo.InvariantCulture);
    }
}
