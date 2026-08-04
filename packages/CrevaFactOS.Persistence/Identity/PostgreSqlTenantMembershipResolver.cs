using CrevaFactOS.Persistence.Tenancy;
using Microsoft.Extensions.Options;
using Npgsql;

namespace CrevaFactOS.Persistence.Identity;

internal sealed class PostgreSqlTenantMembershipResolver(IOptions<PersistenceOptions> options)
    : ITenantMembershipResolver
{
    public async Task<TenantContext?> ResolveActiveMembershipAsync(
        Guid identitySubjectId,
        string tenantIdentifier,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantIdentifier);

        await using var connection = new NpgsqlConnection(
            PostgreSqlConnectionFactory.BuildConnectionString(options.Value));
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand(
            """
            SELECT tenant_id, tenant_key
            FROM resolve_active_tenant_membership(@identity_subject_id, @tenant_identifier)
            """,
            connection);
        command.Parameters.AddWithValue("identity_subject_id", identitySubjectId);
        command.Parameters.AddWithValue("tenant_identifier", tenantIdentifier);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        return await reader.ReadAsync(cancellationToken)
            ? new TenantContext(reader.GetGuid(0), reader.GetString(1))
            : null;
    }
}
