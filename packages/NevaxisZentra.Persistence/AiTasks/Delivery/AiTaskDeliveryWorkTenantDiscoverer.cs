using NevaxisZentra.Persistence.Tenancy;
using Microsoft.Extensions.Options;
using Npgsql;

namespace NevaxisZentra.Persistence.AiTasks.Delivery;

public interface IAiTaskDeliveryWorkTenantDiscoverer
{
    Task<IReadOnlyList<TenantContext>> DiscoverWorkerContextsAsync(
        CancellationToken cancellationToken = default);
}

public sealed class PostgreSqlAiTaskDeliveryWorkTenantDiscoverer(
    IOptions<PersistenceOptions> options) : IAiTaskDeliveryWorkTenantDiscoverer
{
    public async Task<IReadOnlyList<TenantContext>> DiscoverWorkerContextsAsync(
        CancellationToken cancellationToken = default)
    {
        await using var connection = new NpgsqlConnection(
            PostgreSqlConnectionFactory.BuildConnectionString(options.Value));
        await connection.OpenAsync(cancellationToken);

        var tenantIds = new List<Guid>();
        await using (var discoverCommand = new NpgsqlCommand(
                         "SELECT tenant_id FROM discover_ai_task_delivery_work_tenants()",
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
                contexts.Add(new TenantContext(
                    reader.GetGuid(0),
                    reader.GetString(1),
                    reader.GetGuid(2)));
            }
        }

        return contexts;
    }
}
