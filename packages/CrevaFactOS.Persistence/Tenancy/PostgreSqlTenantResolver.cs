using Microsoft.Extensions.Options;
using Npgsql;

namespace CrevaFactOS.Persistence.Tenancy;

internal sealed class PostgreSqlTenantResolver(IOptions<PersistenceOptions> options)
    : ITenantResolver
{
    public async Task<TenantContext?> ResolveActiveTenantAsync(
        string tenantIdentifier,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantIdentifier);

        await using var connection = new NpgsqlConnection(
            PostgreSqlConnectionFactory.BuildConnectionString(options.Value));
        await connection.OpenAsync(cancellationToken);

        await using var command = new NpgsqlCommand(
            "SELECT tenant_id, tenant_key FROM resolve_active_tenant(@tenant_identifier)",
            connection);
        command.Parameters.AddWithValue("tenant_identifier", tenantIdentifier);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken)
            ? new TenantContext(reader.GetGuid(0), reader.GetString(1))
            : null;
    }
}
