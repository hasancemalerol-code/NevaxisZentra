using System.Net.Http.Json;

namespace NevaxisFactOS.Persistence.Edge;

/// <summary>
/// Deterministic machine simulator adapter. Calls the MachineSimulator tool's HTTP endpoints
/// to retrieve pre-scripted event sequences for CNC-001 and PRESS-001.
/// No real PLC/machine connection is made. No write/control commands are issued.
/// </summary>
public sealed class EdgeSimulatorAdapter(IHttpClientFactory httpClientFactory) : IEdgeGatewayAdapter
{
    public string VendorKey => EdgeGatewayVendorKeys.Simulator;

    public async Task<EdgeGatewayHealthResult> CheckHealthAsync(
        Guid gatewayId,
        string? credentialReference,
        CancellationToken cancellationToken)
    {
        try
        {
            var client = httpClientFactory.CreateClient(EdgeSimulatorClientNames.Simulator);
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var response = await client.GetAsync("/health", cancellationToken);
            sw.Stop();

            return response.IsSuccessStatusCode
                ? new EdgeGatewayHealthResult(true, "Simulator online", (int)sw.ElapsedMilliseconds)
                : new EdgeGatewayHealthResult(false, $"Simulator returned {(int)response.StatusCode}", null);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or OperationCanceledException)
        {
            return new EdgeGatewayHealthResult(false, "Simulator unreachable", null);
        }
    }

    public async Task<EdgeAdapterPollResult> PollMachineEventsAsync(
        Guid gatewayId,
        string machineAdapterKey,
        long sinceSequence,
        int limit,
        CancellationToken cancellationToken)
    {
        try
        {
            var client = httpClientFactory.CreateClient(EdgeSimulatorClientNames.Simulator);
            var url = $"/simulate/events?machineCode={Uri.EscapeDataString(machineAdapterKey)}" +
                      $"&sinceSequence={sinceSequence}&limit={limit}";

            var dto = await client.GetFromJsonAsync<SimulatorEventsDto>(url, cancellationToken);
            if (dto is null)
            {
                return new EdgeAdapterPollResult([], false, sinceSequence);
            }

            var events = dto.Events.Select(e => new EdgeAdapterMachineEvent(
                e.IdempotencyKey,
                e.MachineCode,
                e.EventKind,
                e.SourceTimestampUtc,
                e.SourceSequence,
                e.Payload)).ToList();

            return new EdgeAdapterPollResult(events, dto.HasMore, dto.NextSequence);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException
            or OperationCanceledException or System.Text.Json.JsonException)
        {
            return new EdgeAdapterPollResult([], false, sinceSequence);
        }
    }
}

public static class EdgeSimulatorClientNames
{
    public const string Simulator = "MachineSimulator";
}

// DTOs for deserialising MachineSimulator HTTP responses
internal sealed record SimulatorEventsDto(
    IReadOnlyList<SimulatorEventDto> Events,
    bool HasMore,
    long NextSequence);

internal sealed record SimulatorEventDto(
    string IdempotencyKey,
    string MachineCode,
    string EventKind,
    DateTimeOffset SourceTimestampUtc,
    long SourceSequence,
    string? Payload);
