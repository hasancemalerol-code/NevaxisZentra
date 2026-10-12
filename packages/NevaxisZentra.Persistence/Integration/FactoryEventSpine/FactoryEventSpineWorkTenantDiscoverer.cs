using Microsoft.Extensions.Options;
using Npgsql;
using NevaxisZentra.Persistence.Tenancy;

namespace NevaxisZentra.Persistence.Integration.FactoryEventSpine;

/// <summary>
/// Finds the tenants that have factory event spine work (received or retry-due inbound events, pending, retry-due or
/// stalled canonical events), independent of any other module's backlog.
/// </summary>
public interface IFactoryEventSpineWorkTenantDiscoverer
{
    Task<IReadOnlyList<TenantContext>> DiscoverWorkerContextsAsync(CancellationToken cancellationToken = default);
}

public sealed class PostgreSqlFactoryEventSpineWorkTenantDiscoverer(
    IOptions<PersistenceOptions> options) : IFactoryEventSpineWorkTenantDiscoverer
{
    public async Task<IReadOnlyList<TenantContext>> DiscoverWorkerContextsAsync(
        CancellationToken cancellationToken = default)
    {
        await using var connection = new NpgsqlConnection(
            PostgreSqlConnectionFactory.BuildConnectionString(options.Value));
        await connection.OpenAsync(cancellationToken);

        var tenantIds = new List<Guid>();
        await using (var discoverCommand = new NpgsqlCommand(
                         "SELECT tenant_id FROM discover_factory_event_spine_work_tenants()",
                         connection))
        {
            await using var reader = await discoverCommand.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                tenantIds.Add(reader.GetGuid(0));
            }
        }

        var contexts = new List<TenantContext>();
        foreach (var tenantId in tenantIds)
        {
            await using var contextCommand = new NpgsqlCommand(
                """
                SELECT tenant_id, tenant_key, membership_id
                FROM resolve_ai_task_delivery_worker_context(@tenant_id)
                """,
                connection);
            contextCommand.Parameters.AddWithValue("tenant_id", tenantId);
            await using var reader = await contextCommand.ExecuteReaderAsync(cancellationToken);
            if (await reader.ReadAsync(cancellationToken))
            {
                contexts.Add(new TenantContext(reader.GetGuid(0), reader.GetString(1), reader.GetGuid(2)));
            }
        }

        return contexts;
    }
}
