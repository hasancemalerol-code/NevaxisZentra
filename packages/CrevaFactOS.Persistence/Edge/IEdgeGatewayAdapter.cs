namespace CrevaFactOS.Persistence.Edge;

/// <summary>
/// Contract for vendor-specific Edge gateway adapters. Strictly read-only.
/// No write/control commands are issued to machines through this interface.
/// MQTT and OPC UA implementations are deferred beyond EDGE-1.
/// </summary>
public interface IEdgeGatewayAdapter
{
    string VendorKey { get; }

    /// <summary>
    /// Always returns true. Machine write/control is explicitly prohibited in EDGE-1.
    /// </summary>
    bool IsReadOnly => true;

    Task<EdgeGatewayHealthResult> CheckHealthAsync(
        Guid gatewayId,
        string? credentialReference,
        CancellationToken cancellationToken);

    Task<EdgeAdapterPollResult> PollMachineEventsAsync(
        Guid gatewayId,
        string machineAdapterKey,
        long sinceSequence,
        int limit,
        CancellationToken cancellationToken);
}

public sealed record EdgeGatewayHealthResult(
    bool IsHealthy,
    string? Message,
    int? LatencyMs);

public sealed record EdgeAdapterMachineEvent(
    string IdempotencyKey,
    string MachineAdapterKey,
    string EventKind,
    DateTimeOffset SourceTimestampUtc,
    long SourceSequence,
    string? Payload);

public sealed record EdgeAdapterPollResult(
    IReadOnlyList<EdgeAdapterMachineEvent> Events,
    bool HasMore,
    long NextSequence);

public interface IEdgeGatewayAdapterResolver
{
    IEdgeGatewayAdapter Resolve(string vendorKey);
}

public sealed class EdgeGatewayAdapterResolver(IEnumerable<IEdgeGatewayAdapter> adapters) : IEdgeGatewayAdapterResolver
{
    private readonly Dictionary<string, IEdgeGatewayAdapter> _adapters =
        adapters.ToDictionary(a => a.VendorKey, StringComparer.OrdinalIgnoreCase);

    public IEdgeGatewayAdapter Resolve(string vendorKey)
    {
        if (_adapters.TryGetValue(vendorKey, out var adapter))
        {
            return adapter;
        }

        throw new InvalidOperationException($"No Edge gateway adapter registered for vendor key '{vendorKey}'.");
    }
}
