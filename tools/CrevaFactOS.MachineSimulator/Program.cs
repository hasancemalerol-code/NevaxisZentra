using CrevaFactOS.MachineSimulator;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration["Application:Name"] ??= "CrevaFactOS MachineSimulator";

var app = builder.Build();

// Health endpoint — responds to EdgeSimulatorAdapter.CheckHealthAsync
app.MapGet("/health", () => new
{
    status = "healthy",
    service = "CrevaFactOS.MachineSimulator",
    vendor = "simulator",
    machines = MachineSimulatorScenarios.Machines.Select(m => m.Code).ToList(),
    note = "Deterministic simulator. No real PLC/machine connection. Read-only.",
    timestamp = DateTimeOffset.UtcNow,
});

// List simulated machines
app.MapGet("/simulate/machines", () => MachineSimulatorScenarios.Machines.Select(m => new
{
    code = m.Code,
    name = m.Name,
    assetClass = m.AssetClass,
}));

// Deterministic event feed — called by EdgeSimulatorAdapter.PollMachineEventsAsync
app.MapGet("/simulate/events", (string? machineCode, long? sinceSequence, int? limit) =>
{
    if (string.IsNullOrWhiteSpace(machineCode))
    {
        return Results.BadRequest(new { error = "machineCode is required." });
    }

    var knownCodes = MachineSimulatorScenarios.Machines.Select(m => m.Code).ToHashSet();
    if (!knownCodes.Contains(machineCode))
    {
        return Results.NotFound(new { error = $"Unknown machine code: {machineCode}" });
    }

    var page = MachineSimulatorScenarios.GetEvents(
        machineCode,
        sinceSequence ?? 0,
        Math.Clamp(limit ?? 20, 1, 100));

    return Results.Ok(new
    {
        events = page.Events.Select(e => new
        {
            idempotencyKey = e.IdempotencyKey,
            machineCode = e.MachineCode,
            eventKind = e.EventKind,
            sourceTimestampUtc = e.SourceTimestampUtc,
            sourceSequence = e.SourceSequence,
            payload = e.Payload,
            typedTelemetry = e.TypedTelemetry is null ? null : new
            {
                metricKey = e.TypedTelemetry.MetricKey,
                numericValue = e.TypedTelemetry.NumericValue,
                textValue = e.TypedTelemetry.TextValue,
                booleanValue = e.TypedTelemetry.BooleanValue,
                unit = e.TypedTelemetry.Unit,
                quality = e.TypedTelemetry.Quality,
            },
            typedCycle = e.TypedCycle is null ? null : new
            {
                cycleIdentity = e.TypedCycle.CycleIdentity,
                cycleDurationMs = e.TypedCycle.CycleDurationMs,
                observedCounter = e.TypedCycle.ObservedCounter,
                quality = e.TypedCycle.Quality,
            },
            typedCounter = e.TypedCounter is null ? null : new
            {
                counterKey = e.TypedCounter.CounterKey,
                counterValue = e.TypedCounter.CounterValue,
                quality = e.TypedCounter.Quality,
            },
        }),
        hasMore = page.HasMore,
        nextSequence = page.NextSequence,
    });
});

// EDGE-2: Return specific scenario events for testing
app.MapGet("/simulate/scenarios/duplicate-cycle", () =>
{
    var ev = MachineSimulatorScenarios.GetDuplicateCycleEvent();
    return Results.Ok(new { scenario = "duplicate-cycle", note = "Replay of CYCLE-000001. Idempotency key is identical. Must not increment ObservedCycleCount.", @event = new
    {
        idempotencyKey = ev.IdempotencyKey,
        machineCode = ev.MachineCode,
        eventKind = ev.EventKind,
        sourceTimestampUtc = ev.SourceTimestampUtc,
        sourceSequence = ev.SourceSequence,
        typedCycle = new { cycleIdentity = ev.TypedCycle!.CycleIdentity, cycleDurationMs = ev.TypedCycle.CycleDurationMs, observedCounter = ev.TypedCycle.ObservedCounter, quality = ev.TypedCycle.Quality },
    }});
});

app.MapGet("/simulate/scenarios/out-of-order-telemetry", () =>
{
    var (first, second) = MachineSimulatorScenarios.GetOutOfOrderTelemetryEvents();
    return Results.Ok(new { scenario = "out-of-order-telemetry", note = "seq 200 then seq 199. Latest projection must remain seq 200.", events = new[]
    {
        new { idempotencyKey = first.IdempotencyKey, sourceSequence = first.SourceSequence, sourceTimestampUtc = first.SourceTimestampUtc, typedTelemetry = new { metricKey = first.TypedTelemetry!.MetricKey, numericValue = first.TypedTelemetry.NumericValue, unit = first.TypedTelemetry.Unit, quality = first.TypedTelemetry.Quality } },
        new { idempotencyKey = second.IdempotencyKey, sourceSequence = second.SourceSequence, sourceTimestampUtc = second.SourceTimestampUtc, typedTelemetry = new { metricKey = second.TypedTelemetry!.MetricKey, numericValue = second.TypedTelemetry.NumericValue, unit = second.TypedTelemetry.Unit, quality = second.TypedTelemetry.Quality } },
    }});
});

app.MapGet("/simulate/scenarios/counter-reset", () =>
{
    var counters = MachineSimulatorScenarios.GetCounterResetScenario();
    return Results.Ok(new { scenario = "counter-reset", note = "100 → 101 → 102 → 5. Last observation must be flagged as counter_reset_suspected.", counters = counters.Select(c => new { counterKey = c.CounterKey, counterValue = c.CounterValue, quality = c.Quality }) });
});

// Manual trigger: push all machine events to the main API
// Used by verifiers to seed live ingestion without a real adapter poll loop.
app.MapPost("/simulate/push-to-api", async (string? apiBaseUrl, string? facilityId, HttpClient http) =>
{
    var targetApi = apiBaseUrl ?? Environment.GetEnvironmentVariable("CREVAFACTOS_API_URL") ?? "http://localhost:5000";
    if (!Guid.TryParse(facilityId ?? Environment.GetEnvironmentVariable("CREVAFACTOS_FACILITY_ID"), out var fId))
    {
        return Results.BadRequest(new { error = "facilityId must be a valid GUID." });
    }

    var accepted = 0;
    var duplicates = 0;
    var errors = new List<string>();

    foreach (var machine in MachineSimulatorScenarios.Machines)
    {
        var knownGatewayId = "bb000001-0094-4000-8000-000000000094";
        var machineIds = new Dictionary<string, string>
        {
            ["CNC-001"]   = "bb000001-0095-4000-8000-000000000095",
            ["PRESS-001"] = "bb000001-0096-4000-8000-000000000096",
        };

        if (!machineIds.TryGetValue(machine.Code, out var machineId)) continue;

        var page = MachineSimulatorScenarios.GetEvents(machine.Code, 0, 100);
        foreach (var ev in page.Events)
        {
            var payload = new
            {
                facilityId = fId,
                machineId = Guid.Parse(machineId),
                gatewayId = Guid.Parse(knownGatewayId),
                eventKind = ev.EventKind,
                idempotencyKey = ev.IdempotencyKey,
                sourceTimestampUtc = ev.SourceTimestampUtc,
                sourceSequence = ev.SourceSequence,
                payload = ev.Payload,
            };

            try
            {
                var resp = await http.PostAsJsonAsync(
                    $"{targetApi}/api/edge/events/ingest?facilityId={fId}",
                    payload);

                if (resp.IsSuccessStatusCode)
                {
                    var body = await resp.Content.ReadFromJsonAsync<PushResponse>();
                    if (body?.IsDuplicate == true) duplicates++;
                    else accepted++;
                }
                else
                {
                    errors.Add($"{machine.Code}:{ev.SourceSequence} → {(int)resp.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                errors.Add($"{machine.Code}:{ev.SourceSequence} → {ex.Message}");
            }
        }
    }

    return Results.Ok(new { accepted, duplicates, errors });
});

app.Run();

internal sealed record PushResponse(bool Accepted, bool IsDuplicate, bool IsOutOfOrder, Guid? EventId);
