using Microsoft.Extensions.Options;
using Npgsql;

namespace CrevaFactOS.Persistence.Authorization;

internal sealed class PostgreSqlPermissionEvaluator(IOptions<PersistenceOptions> options)
    : IPermissionEvaluator
{
    public async Task<PermissionCheckResult> EvaluateAsync(
        Guid identitySubjectId,
        Guid tenantId,
        Guid membershipId,
        string permissionKey,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(permissionKey))
        {
            return PermissionCheckResult.Deny("Permission key is missing.");
        }

        await using var connection = new NpgsqlConnection(
            PostgreSqlConnectionFactory.BuildConnectionString(options.Value));
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(
            "SELECT has_tenant_permission(@identity_id, @tenant_id, @membership_id, @permission_key)",
            connection);
        command.Parameters.AddWithValue("identity_id", identitySubjectId);
        command.Parameters.AddWithValue("tenant_id", tenantId);
        command.Parameters.AddWithValue("membership_id", membershipId);
        command.Parameters.AddWithValue("permission_key", permissionKey);
        var granted = (bool)(await command.ExecuteScalarAsync(cancellationToken) ?? false);
        return granted
            ? PermissionCheckResult.Grant()
            : PermissionCheckResult.Deny("No exact active tenant permission assignment found.");
    }
}
