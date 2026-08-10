using CrevaFactOS.MachineSimulator;
using CrevaFactOS.MachineSimulator.EdgeBuffer;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration["Application:Name"] ??= "CrevaFactOS MachineSimulator";

builder.Services.AddSingleton<EdgeReliabilityState>();
builder.Services.AddSingleton(sp =>
{
    var state = sp.GetRequiredService<EdgeReliabilityState>();
    var dbPath = Environment.GetEnvironmentVariable("EDGE_BUFFER_DB_PATH")
        ?? EdgeBufferStore.ResolveDefaultDbPath();
    return new EdgeBufferStore(dbPath, state, new EdgeRetryPolicy
    {
        UseDeterministicJitter = true,
        InitialDelay = TimeSpan.FromSeconds(2),
        MaximumDelay = TimeSpan.FromMinutes(5),
        BackoffMultiplier = 2.0,
        MaximumAttempts = EdgeBufferConstants.DefaultMaxAttempts,
    });
});
builder.Services.AddHttpClient("EdgeDelivery");
builder.Services.AddSingleton<EdgeDeliveryWorker>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<EdgeDeliveryWorker>());

var app = builder.Build();
var bufferStore = app.Services.GetRequiredService<EdgeBufferStore>();
var reliabilityState = app.Services.GetRequiredService<EdgeReliabilityState>();

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

// EDGE-3 scenario fixtures (deterministic observation events — not real machine signals)
app.MapGet("/simulate/scenarios/downtime", () =>
{
    var (start, end) = MachineSimulatorScenarios.GetDowntimeScenario();
    return Results.Ok(new
    {
        scenario = "downtime",
        note = "start seq 100 → end seq 102. Observed duration 5 minutes. Not official Production downtime.",
        events = new[] { Describe(start), Describe(end) },
    });
});

app.MapGet("/simulate/scenarios/micro-stop", () =>
{
    var (start, end) = MachineSimulatorScenarios.GetMicroStopScenario();
    return Results.Ok(new
    {
        scenario = "micro-stop",
        note = "30s episode below 60s fixture threshold → Observed micro-stop. Not official Production microstop.",
        events = new[] { Describe(start), Describe(end) },
    });
});

app.MapGet("/simulate/scenarios/alarm", () =>
{
    var (raise, clear) = MachineSimulatorScenarios.GetAlarmScenario();
    return Results.Ok(new
    {
        scenario = "alarm",
        note = "SIM-ALARM-001 severity=error then clear. No Maintenance WO.",
        events = new[] { Describe(raise), Describe(clear) },
    });
});

app.MapGet("/simulate/scenarios/multiple-alarms", () =>
{
    var (warning, critical, clearCritical, clearWarning) = MachineSimulatorScenarios.GetMultipleAlarmScenario();
    return Results.Ok(new
    {
        scenario = "multiple-alarms",
        note = "warning+critical active → clear critical → highest=warning.",
        events = new[] { Describe(warning), Describe(critical), Describe(clearCritical), Describe(clearWarning) },
    });
});

app.MapGet("/simulate/scenarios/setup", () =>
{
    var (start, complete) = MachineSimulatorScenarios.GetSetupScenario();
    return Results.Ok(new
    {
        scenario = "setup",
        note = "Gözlenen Setup / Değişim — not official Production changeover.",
        events = new[] { Describe(start), Describe(complete) },
    });
});

app.MapGet("/simulate/scenarios/scrap", () =>
{
    var scrap = MachineSimulatorScenarios.GetScrapScenario();
    return Results.Ok(new
    {
        scenario = "scrap",
        note = "quantity 2 unit=count reason=test-fixture. Observed only — not Quality disposition.",
        @event = Describe(scrap),
    });
});

// EDGE-4: Store-and-forward reliability endpoints
app.MapGet("/simulate/reliability/health", () =>
{
    var health = bufferStore.GetHealth(DateTimeOffset.UtcNow);
    return Results.Ok(new
    {
        apiConnected = health.ApiConnected,
        deliveryStatus = health.DeliveryStatus,
        pendingCount = health.PendingCount,
        retryCount = health.RetryCount,
        deadLetterCount = health.DeadLetterCount,
        deliveredCount = health.DeliveredCount,
        oldestPendingAgeMs = health.OldestPendingAge?.TotalMilliseconds,
        lastSuccessfulDeliveryAtUtc = health.LastSuccessfulDeliveryAtUtc,
        lastDeliveryAttemptAtUtc = health.LastDeliveryAttemptAtUtc,
        bufferUtilization = health.BufferUtilization,
        replayStatus = health.ReplayStatus,
        maxBufferedEvents = health.MaxBufferedEvents,
        metrics = new
        {
            eventsBuffered = health.EventsBufferedTotal,
            eventsDelivered = health.EventsDeliveredTotal,
            retries = health.RetriesTotal,
            deadLetters = health.DeadLetterCount,
            deliveryFailures = health.DeliveryFailuresTotal,
        },
        activeScenario = reliabilityState.GetActiveScenario(),
        note = "Simülatör verisi — gerçek makine bağlantısı değildir.",
    });
});

app.MapGet("/simulate/reliability/buffer", (string? status, int? limit) =>
{
    var events = bufferStore.ListEvents(status, limit ?? 50);
    return Results.Ok(new
    {
        events = events.Select(MapBufferedEvent),
        note = "Operational view — no raw secrets, no credentials.",
    });
});

app.MapGet("/simulate/reliability/dead-letters", (int? limit) =>
{
    var events = bufferStore.ListDeadLetters(limit ?? 50);
    return Results.Ok(new
    {
        deadLetters = events.Select(e => new
        {
            eventType = e.EventType,
            machineCode = e.MachineCode,
            idempotencyKey = e.IdempotencyKey,
            reason = e.LastErrorCode,
            attempts = e.AttemptCount,
            lastFailureAtUtc = e.LastAttemptAtUtc ?? e.DeadLetteredAtUtc,
            sourceTimestampUtc = e.SourceTimestampUtc,
        }),
    });
});

app.MapPost("/simulate/reliability/network", (NetworkControlRequest? body) =>
{
    if (body?.Available is bool available)
    {
        reliabilityState.SetApiAvailable(available);
        reliabilityState.SetScenario(available ? "recover-replay" : "offline-buffer");
    }
    return Results.Ok(new
    {
        apiAvailable = reliabilityState.IsApiAvailable(),
        scenario = reliabilityState.GetActiveScenario(),
        message = reliabilityState.IsApiAvailable()
            ? "Bağlantı geri geldi — bekleyen olaylar aktarılıyor."
            : "Gateway çevrimdışı — olaylar yerel kuyrukta saklanıyor.",
    });
});

app.MapPost("/simulate/reliability/scenarios/{mode}", async (string mode, EdgeDeliveryWorker worker, CancellationToken ct) =>
{
    var now = DateTimeOffset.UtcNow;
    reliabilityState.Reset(mode);

    switch (mode.ToLowerInvariant())
    {
        case "normal":
            reliabilityState.SetApiAvailable(true);
            var normal = EdgeReliabilityScenarios.CreateNormalEvent(now);
            var normalResult = bufferStore.Enqueue(normal);
            if (!normalResult.Accepted)
            {
                return Results.BadRequest(normalResult);
            }
            await worker.ProcessPendingBatchAsync(ct);
            break;

        case "offline-buffer":
            reliabilityState.SetApiAvailable(false);
            reliabilityState.SetScenario("offline-buffer");
            foreach (var ev in EdgeReliabilityScenarios.BuildOfflineBufferEvents(now))
            {
                var r = bufferStore.Enqueue(ev);
                if (!r.Accepted)
                {
                    return Results.Json(new { error = r.ErrorCode, accepted = false }, statusCode: 507);
                }
            }
            break;

        case "recover-replay":
            reliabilityState.SetApiAvailable(true);
            reliabilityState.SetScenario("recover-replay");
            await worker.ProcessPendingBatchAsync(ct);
            break;

        case "duplicate-replay":
            reliabilityState.SetApiAvailable(true);
            reliabilityState.EnableDuplicateReplay();
            bufferStore.ResetDeliveredForReplay();
            await worker.ProcessPendingBatchAsync(ct);
            break;

        case "partial-failure":
            reliabilityState.SetApiAvailable(true);
            reliabilityState.SetScenario("partial-failure");
            foreach (var ev in EdgeReliabilityScenarios.BuildPartialFailureBatch(now))
            {
                bufferStore.Enqueue(ev);
            }

            var poison = bufferStore.GetByIdempotencyKey("SIM-EDGE4:PARTIAL:C");
            if (poison is not null)
            {
                bufferStore.MarkDeadLetter(poison.Id, 1, now, EdgeBufferErrorCodes.MalformedEvent);
            }

            foreach (var key in ReliabilityScenarioKeys.PartialFailureValidKeys)
            {
                var evt = bufferStore.GetByIdempotencyKey(key);
                if (evt is not null)
                {
                    bufferStore.MarkDelivered(evt.Id, now, $"sim-ack-{key}");
                }
            }
            break;

        case "poison-event":
            reliabilityState.SetApiAvailable(true);
            reliabilityState.SetScenario("poison-event");
            bufferStore.Enqueue(EdgeReliabilityScenarios.CreatePoisonEvent(now));
            await worker.ProcessPendingBatchAsync(ct);
            break;

        case "restart-recovery":
            reliabilityState.SetApiAvailable(false);
            reliabilityState.SetScenario("restart-recovery");
            foreach (var ev in EdgeReliabilityScenarios.BuildOfflineBufferEvents(now).Take(5))
            {
                bufferStore.Enqueue(ev);
            }
            break;

        case "api-interruption":
            reliabilityState.SetApiAvailable(true);
            reliabilityState.SetScenario("api-interruption");
            reliabilityState.SetSimulatedHttpStatus(503);
            foreach (var ev in EdgeReliabilityScenarios.BuildOfflineBufferEvents(now).Take(3))
            {
                bufferStore.Enqueue(ev);
            }
            await worker.ProcessPendingBatchAsync(ct);
            reliabilityState.SetSimulatedHttpStatus(0);
            reliabilityState.SetApiAvailable(true);
            await worker.ProcessPendingBatchAsync(ct);
            break;

        case "buffer-capacity-test":
            reliabilityState.SetApiAvailable(false);
            reliabilityState.SetScenario("buffer-capacity-test");
            reliabilityState.SetTestMaxBufferedEvents(5);
            var accepted = 0;
            string? capacityError = null;
            foreach (var ev in EdgeReliabilityScenarios.BuildOfflineBufferEvents(now))
            {
                var r = bufferStore.Enqueue(ev);
                if (r.Accepted) accepted++;
                else { capacityError = r.ErrorCode; break; }
            }
            return Results.Ok(new
            {
                scenario = mode,
                accepted,
                capacityError,
                health = bufferStore.GetHealth(now),
            });

        default:
            return Results.BadRequest(new { error = $"Unknown reliability mode: {mode}" });
    }

    return Results.Ok(new
    {
        scenario = mode,
        health = bufferStore.GetHealth(now),
        pending = bufferStore.CountByStatus(EdgeDeliveryStatuses.Pending),
        retryWait = bufferStore.CountByStatus(EdgeDeliveryStatuses.RetryWait),
        delivered = bufferStore.CountByStatus(EdgeDeliveryStatuses.Delivered),
        deadLetter = bufferStore.CountByStatus(EdgeDeliveryStatuses.DeadLetter),
    });
});

app.MapPost("/simulate/reliability/reset", () =>
{
    bufferStore.ClearAll();
    reliabilityState.Reset("normal");
    return Results.Ok(new { reset = true, scenario = "normal" });
});

app.MapPost("/simulate/reliability/deliver-now", async (EdgeDeliveryWorker worker, CancellationToken ct) =>
{
    var processed = await worker.ProcessPendingBatchAsync(ct);
    return Results.Ok(new { processed, health = bufferStore.GetHealth(DateTimeOffset.UtcNow) });
});

// Manual trigger: push all machine events to the main API
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

static object Describe(SimulatedEvent ev) => new
{
    idempotencyKey = ev.IdempotencyKey,
    machineCode = ev.MachineCode,
    eventKind = ev.EventKind,
    sourceTimestampUtc = ev.SourceTimestampUtc,
    sourceSequence = ev.SourceSequence,
    payload = ev.Payload,
};

static object MapBufferedEvent(EdgeBufferedEvent e) => new
{
    eventType = e.EventType,
    machineCode = e.MachineCode,
    sourceTimestampUtc = e.SourceTimestampUtc,
    queueTimeUtc = e.CreatedAtUtc,
    attempts = e.AttemptCount,
    status = e.DeliveryStatus,
    lastError = e.LastErrorCode,
    deliveryDelayMs = e.DeliveredAtUtc.HasValue
        ? (e.DeliveredAtUtc.Value - e.SourceTimestampUtc).TotalMilliseconds
        : (double?)null,
    sourceSequence = e.SourceSequence,
    payloadVersion = e.PayloadVersion,
};

internal sealed record PushResponse(bool Accepted, bool IsDuplicate, bool IsOutOfOrder, Guid? EventId);
internal sealed record NetworkControlRequest(bool? Available);

file static class ReliabilityScenarioKeys
{
    public static readonly string[] PartialFailureValidKeys =
        ["SIM-EDGE4:PARTIAL:A", "SIM-EDGE4:PARTIAL:B", "SIM-EDGE4:PARTIAL:D"];
}
