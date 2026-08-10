namespace CrevaFactOS.MachineSimulator;

/// <summary>
/// Deterministic event generation for two simulated machines: CNC-001 and PRESS-001.
/// Sequences are stable — the same sinceSequence always returns the same events.
/// No real PLC/machine connection. No write/control commands issued.
/// </summary>
public static class MachineSimulatorScenarios
{
    private static readonly DateTimeOffset BaseTimestamp =
        new DateTimeOffset(2026, 8, 10, 9, 0, 0, TimeSpan.Zero);

    public static readonly IReadOnlyList<SimulatedMachine> Machines =
    [
        new SimulatedMachine("CNC-001", "CNC Tezgâh 001", "CNC"),
        new SimulatedMachine("PRESS-001", "Pres Makinesi 001", "Press"),
    ];

    public static SimulatorEventsPage GetEvents(string machineCode, long sinceSequence, int limit)
    {
        var allEvents = GenerateEvents(machineCode);
        var filtered = allEvents.Where(e => e.SourceSequence > sinceSequence).ToList();
        var page = filtered.Take(limit).ToList();
        var nextSequence = page.Count > 0 ? page[^1].SourceSequence : sinceSequence;
        return new SimulatorEventsPage(page, HasMore: filtered.Count > limit, nextSequence);
    }

    private static List<SimulatedEvent> GenerateEvents(string machineCode)
    {
        return machineCode switch
        {
            "CNC-001" => GenerateCncEvents(),
            "PRESS-001" => GeneratePressEvents(),
            _ => [],
        };
    }

    private static List<SimulatedEvent> GenerateCncEvents()
    {
        var events = new List<SimulatedEvent>();
        long seq = 1;

        // Startup sequence
        events.Add(new SimulatedEvent(
            IdempotencyKey: $"SIM-GW-001:CNC-001:{seq}",
            MachineCode: "CNC-001",
            EventKind: "heartbeat",
            SourceTimestampUtc: BaseTimestamp,
            SourceSequence: seq++,
            Payload: null));

        events.Add(new SimulatedEvent(
            IdempotencyKey: $"SIM-GW-001:CNC-001:{seq}",
            MachineCode: "CNC-001",
            EventKind: "status-change",
            SourceTimestampUtc: BaseTimestamp.AddSeconds(5),
            SourceSequence: seq++,
            Payload: """{"status":"idle","reason":"startup"}"""));

        events.Add(new SimulatedEvent(
            IdempotencyKey: $"SIM-GW-001:CNC-001:{seq}",
            MachineCode: "CNC-001",
            EventKind: "status-change",
            SourceTimestampUtc: BaseTimestamp.AddSeconds(30),
            SourceSequence: seq++,
            Payload: """{"status":"running","reason":"production-start","programId":"PROG-A42"}"""));

        events.Add(new SimulatedEvent(
            IdempotencyKey: $"SIM-GW-001:CNC-001:{seq}",
            MachineCode: "CNC-001",
            EventKind: "cycle-start",
            SourceTimestampUtc: BaseTimestamp.AddSeconds(31),
            SourceSequence: seq++,
            Payload: """{"cycleId":"CYC-001","programId":"PROG-A42","partRef":"PART-001"}"""));

        // EDGE-2: Typed telemetry for temperature_zone_1 (seq 5)
        events.Add(new SimulatedEvent(
            IdempotencyKey: $"SIM-GW-001:CNC-001:{seq}",
            MachineCode: "CNC-001",
            EventKind: "telemetry",
            SourceTimestampUtc: BaseTimestamp.AddSeconds(40),
            SourceSequence: seq++,
            Payload: null,
            TypedTelemetry: new SimulatedTelemetry("temperature_zone_1", 210.0, null, null, "°C", "good")));

        // EDGE-2: Typed telemetry for hydraulic_pressure (seq 6)
        events.Add(new SimulatedEvent(
            IdempotencyKey: $"SIM-GW-001:CNC-001:{seq}",
            MachineCode: "CNC-001",
            EventKind: "telemetry",
            SourceTimestampUtc: BaseTimestamp.AddSeconds(42),
            SourceSequence: seq++,
            Payload: null,
            TypedTelemetry: new SimulatedTelemetry("hydraulic_pressure", 85.0, null, null, "bar", "good")));

        // Legacy telemetry (seq 7)
        events.Add(new SimulatedEvent(
            IdempotencyKey: $"SIM-GW-001:CNC-001:{seq}",
            MachineCode: "CNC-001",
            EventKind: "telemetry",
            SourceTimestampUtc: BaseTimestamp.AddSeconds(45),
            SourceSequence: seq++,
            Payload: """{"spindleSpeed":2400,"feedRate":500,"coolant":true}"""));

        events.Add(new SimulatedEvent(
            IdempotencyKey: $"SIM-GW-001:CNC-001:{seq}",
            MachineCode: "CNC-001",
            EventKind: "cycle-end",
            SourceTimestampUtc: BaseTimestamp.AddSeconds(90),
            SourceSequence: seq++,
            Payload: """{"cycleId":"CYC-001","durationMs":59000,"result":"ok"}"""));

        // EDGE-2: cycle-completed event for cycle 1 (seq 9)
        events.Add(new SimulatedEvent(
            IdempotencyKey: $"SIM-GW-001:CNC-001:CYCLE-000001",
            MachineCode: "CNC-001",
            EventKind: "cycle-completed",
            SourceTimestampUtc: BaseTimestamp.AddSeconds(90),
            SourceSequence: seq++,
            Payload: null,
            TypedCycle: new SimulatedCycle("SIM-CNC-001-CYCLE-000001", 12500, 1, "good")));

        // EDGE-2: counter-observed (seq 10)
        events.Add(new SimulatedEvent(
            IdempotencyKey: $"SIM-GW-001:CNC-001:CTR-{seq}",
            MachineCode: "CNC-001",
            EventKind: "counter-observed",
            SourceTimestampUtc: BaseTimestamp.AddSeconds(91),
            SourceSequence: seq++,
            Payload: null,
            TypedCounter: new SimulatedCounter("machine_counter", 100, "good")));

        events.Add(new SimulatedEvent(
            IdempotencyKey: $"SIM-GW-001:CNC-001:{seq}",
            MachineCode: "CNC-001",
            EventKind: "cycle-start",
            SourceTimestampUtc: BaseTimestamp.AddSeconds(95),
            SourceSequence: seq++,
            Payload: """{"cycleId":"CYC-002","programId":"PROG-A42","partRef":"PART-002"}"""));

        // EDGE-2: Typed telemetry for cycle_time_ms (seq 12)
        events.Add(new SimulatedEvent(
            IdempotencyKey: $"SIM-GW-001:CNC-001:{seq}",
            MachineCode: "CNC-001",
            EventKind: "telemetry",
            SourceTimestampUtc: BaseTimestamp.AddSeconds(100),
            SourceSequence: seq++,
            Payload: null,
            TypedTelemetry: new SimulatedTelemetry("cycle_time_ms", 12300.0, null, null, "ms", "good")));

        events.Add(new SimulatedEvent(
            IdempotencyKey: $"SIM-GW-001:CNC-001:{seq}",
            MachineCode: "CNC-001",
            EventKind: "cycle-end",
            SourceTimestampUtc: BaseTimestamp.AddSeconds(155),
            SourceSequence: seq++,
            Payload: """{"cycleId":"CYC-002","durationMs":60000,"result":"ok"}"""));

        // EDGE-2: cycle-completed event for cycle 2 (seq 14)
        events.Add(new SimulatedEvent(
            IdempotencyKey: $"SIM-GW-001:CNC-001:CYCLE-000002",
            MachineCode: "CNC-001",
            EventKind: "cycle-completed",
            SourceTimestampUtc: BaseTimestamp.AddSeconds(155),
            SourceSequence: seq++,
            Payload: null,
            TypedCycle: new SimulatedCycle("SIM-CNC-001-CYCLE-000002", 12300, 2, "good")));

        // EDGE-2: counter-observed (seq 15)
        events.Add(new SimulatedEvent(
            IdempotencyKey: $"SIM-GW-001:CNC-001:CTR-{seq}",
            MachineCode: "CNC-001",
            EventKind: "counter-observed",
            SourceTimestampUtc: BaseTimestamp.AddSeconds(156),
            SourceSequence: seq++,
            Payload: null,
            TypedCounter: new SimulatedCounter("machine_counter", 101, "good")));

        events.Add(new SimulatedEvent(
            IdempotencyKey: $"SIM-GW-001:CNC-001:{seq}",
            MachineCode: "CNC-001",
            EventKind: "heartbeat",
            SourceTimestampUtc: BaseTimestamp.AddMinutes(5),
            SourceSequence: seq++,
            Payload: null));

        events.Add(new SimulatedEvent(
            IdempotencyKey: $"SIM-GW-001:CNC-001:{seq}",
            MachineCode: "CNC-001",
            EventKind: "status-change",
            SourceTimestampUtc: BaseTimestamp.AddMinutes(10),
            SourceSequence: seq++,
            Payload: """{"status":"idle","reason":"program-end"}"""));

        // EDGE-2: cycle 3 with counter-reset scenario prep
        events.Add(new SimulatedEvent(
            IdempotencyKey: $"SIM-GW-001:CNC-001:CYCLE-000003",
            MachineCode: "CNC-001",
            EventKind: "cycle-completed",
            SourceTimestampUtc: BaseTimestamp.AddMinutes(12),
            SourceSequence: seq++,
            Payload: null,
            TypedCycle: new SimulatedCycle("SIM-CNC-001-CYCLE-000003", 12400, 102, "good")));

        // EDGE-2: counter-observed after cycle 3 (seq = 19)
        events.Add(new SimulatedEvent(
            IdempotencyKey: $"SIM-GW-001:CNC-001:CTR-{seq}",
            MachineCode: "CNC-001",
            EventKind: "counter-observed",
            SourceTimestampUtc: BaseTimestamp.AddMinutes(12).AddSeconds(1),
            SourceSequence: seq++,
            Payload: null,
            TypedCounter: new SimulatedCounter("machine_counter", 102, "good")));

        return events;
    }

    private static List<SimulatedEvent> GeneratePressEvents()
    {
        var events = new List<SimulatedEvent>();
        long seq = 1;

        events.Add(new SimulatedEvent(
            IdempotencyKey: $"SIM-GW-001:PRESS-001:{seq}",
            MachineCode: "PRESS-001",
            EventKind: "heartbeat",
            SourceTimestampUtc: BaseTimestamp,
            SourceSequence: seq++,
            Payload: null));

        events.Add(new SimulatedEvent(
            IdempotencyKey: $"SIM-GW-001:PRESS-001:{seq}",
            MachineCode: "PRESS-001",
            EventKind: "status-change",
            SourceTimestampUtc: BaseTimestamp.AddSeconds(3),
            SourceSequence: seq++,
            Payload: """{"status":"idle","reason":"startup"}"""));

        events.Add(new SimulatedEvent(
            IdempotencyKey: $"SIM-GW-001:PRESS-001:{seq}",
            MachineCode: "PRESS-001",
            EventKind: "status-change",
            SourceTimestampUtc: BaseTimestamp.AddMinutes(1),
            SourceSequence: seq++,
            Payload: """{"status":"running","reason":"press-start","toolId":"TOOL-022"}"""));

        events.Add(new SimulatedEvent(
            IdempotencyKey: $"SIM-GW-001:PRESS-001:{seq}",
            MachineCode: "PRESS-001",
            EventKind: "cycle-start",
            SourceTimestampUtc: BaseTimestamp.AddMinutes(1).AddSeconds(2),
            SourceSequence: seq++,
            Payload: """{"cycleId":"PCYC-001","toolId":"TOOL-022","tonnage":150}"""));

        // EDGE-2: Typed telemetry for hydraulic_pressure on PRESS (seq 5)
        events.Add(new SimulatedEvent(
            IdempotencyKey: $"SIM-GW-001:PRESS-001:{seq}",
            MachineCode: "PRESS-001",
            EventKind: "telemetry",
            SourceTimestampUtc: BaseTimestamp.AddMinutes(1).AddSeconds(5),
            SourceSequence: seq++,
            Payload: null,
            TypedTelemetry: new SimulatedTelemetry("hydraulic_pressure", 210.0, null, null, "bar", "good")));

        events.Add(new SimulatedEvent(
            IdempotencyKey: $"SIM-GW-001:PRESS-001:{seq}",
            MachineCode: "PRESS-001",
            EventKind: "cycle-end",
            SourceTimestampUtc: BaseTimestamp.AddMinutes(1).AddSeconds(12),
            SourceSequence: seq++,
            Payload: """{"cycleId":"PCYC-001","durationMs":10000,"result":"ok"}"""));

        // EDGE-2: cycle-completed for PRESS (seq 7)
        events.Add(new SimulatedEvent(
            IdempotencyKey: $"SIM-GW-001:PRESS-001:CYCLE-000001",
            MachineCode: "PRESS-001",
            EventKind: "cycle-completed",
            SourceTimestampUtc: BaseTimestamp.AddMinutes(1).AddSeconds(12),
            SourceSequence: seq++,
            Payload: null,
            TypedCycle: new SimulatedCycle("SIM-PRESS-001-CYCLE-000001", 10000, 1, "good")));

        // EDGE-2: counter-observed for PRESS (seq 8)
        events.Add(new SimulatedEvent(
            IdempotencyKey: $"SIM-GW-001:PRESS-001:CTR-{seq}",
            MachineCode: "PRESS-001",
            EventKind: "counter-observed",
            SourceTimestampUtc: BaseTimestamp.AddMinutes(1).AddSeconds(13),
            SourceSequence: seq++,
            Payload: null,
            TypedCounter: new SimulatedCounter("machine_counter", 1, "good")));

        events.Add(new SimulatedEvent(
            IdempotencyKey: $"SIM-GW-001:PRESS-001:{seq}",
            MachineCode: "PRESS-001",
            EventKind: "heartbeat",
            SourceTimestampUtc: BaseTimestamp.AddMinutes(5),
            SourceSequence: seq++,
            Payload: null));

        events.Add(new SimulatedEvent(
            IdempotencyKey: $"SIM-GW-001:PRESS-001:{seq}",
            MachineCode: "PRESS-001",
            EventKind: "status-change",
            SourceTimestampUtc: BaseTimestamp.AddMinutes(6),
            SourceSequence: seq++,
            Payload: """{"status":"idle","reason":"batch-complete"}"""));

        return events;
    }

    // ── EDGE-2 deterministic scenario events ──────────────────────────────

    /// <summary>
    /// Returns a duplicate cycle event identical to CYCLE-000001 from CNC-001.
    /// Used to prove idempotency: replaying must not increment ObservedCycleCount.
    /// </summary>
    public static SimulatedEvent GetDuplicateCycleEvent() =>
        new SimulatedEvent(
            IdempotencyKey: "SIM-GW-001:CNC-001:CYCLE-000001",
            MachineCode: "CNC-001",
            EventKind: "cycle-completed",
            SourceTimestampUtc: BaseTimestamp.AddSeconds(90),
            SourceSequence: 9,
            Payload: null,
            TypedCycle: new SimulatedCycle("SIM-CNC-001-CYCLE-000001", 12500, 1, "good"));

    /// <summary>
    /// Returns out-of-order telemetry events for CNC-001.
    /// seq 200 → seq 199 (out of order).
    /// Latest projection must remain seq 200.
    /// </summary>
    public static (SimulatedEvent First, SimulatedEvent Second) GetOutOfOrderTelemetryEvents()
    {
        var first = new SimulatedEvent(
            IdempotencyKey: "SIM-OOO:CNC-001:TEMP-200",
            MachineCode: "CNC-001",
            EventKind: "telemetry",
            SourceTimestampUtc: BaseTimestamp.AddMinutes(20),
            SourceSequence: 200,
            Payload: null,
            TypedTelemetry: new SimulatedTelemetry("temperature_zone_1", 215.0, null, null, "°C", "good"));

        var second = new SimulatedEvent(
            IdempotencyKey: "SIM-OOO:CNC-001:TEMP-199",
            MachineCode: "CNC-001",
            EventKind: "telemetry",
            SourceTimestampUtc: BaseTimestamp.AddMinutes(19),
            SourceSequence: 199,
            Payload: null,
            TypedTelemetry: new SimulatedTelemetry("temperature_zone_1", 212.0, null, null, "°C", "uncertain"));

        return (first, second);
    }

    /// <summary>
    /// Returns out-of-order cycle events for CNC-001.
    /// seq 100 → seq 102 → seq 101.
    /// Latest cycle projection must remain seq 102.
    /// </summary>
    public static (SimulatedEvent Seq100, SimulatedEvent Seq102, SimulatedEvent Seq101) GetOutOfOrderCycleEvents()
    {
        var e100 = new SimulatedEvent(
            IdempotencyKey: "SIM-OOO:CNC-001:CYCLE-100",
            MachineCode: "CNC-001",
            EventKind: "cycle-completed",
            SourceTimestampUtc: BaseTimestamp.AddMinutes(30),
            SourceSequence: 100,
            Payload: null,
            TypedCycle: new SimulatedCycle("SIM-CNC-001-CYCLE-OOO-100", 12400, 200, "good"));

        var e102 = new SimulatedEvent(
            IdempotencyKey: "SIM-OOO:CNC-001:CYCLE-102",
            MachineCode: "CNC-001",
            EventKind: "cycle-completed",
            SourceTimestampUtc: BaseTimestamp.AddMinutes(31),
            SourceSequence: 102,
            Payload: null,
            TypedCycle: new SimulatedCycle("SIM-CNC-001-CYCLE-OOO-102", 12350, 202, "good"));

        var e101 = new SimulatedEvent(
            IdempotencyKey: "SIM-OOO:CNC-001:CYCLE-101",
            MachineCode: "CNC-001",
            EventKind: "cycle-completed",
            SourceTimestampUtc: BaseTimestamp.AddMinutes(30).AddSeconds(30),
            SourceSequence: 101,
            Payload: null,
            TypedCycle: new SimulatedCycle("SIM-CNC-001-CYCLE-OOO-101", 12380, 201, "good"));

        return (e100, e102, e101);
    }

    /// <summary>
    /// Returns counter events that simulate a counter reset scenario.
    /// 100 → 101 → 102 → 5 (reset suspected).
    /// </summary>
    public static IReadOnlyList<SimulatedCounter> GetCounterResetScenario() =>
    [
        new SimulatedCounter("machine_counter", 100, "good"),
        new SimulatedCounter("machine_counter", 101, "good"),
        new SimulatedCounter("machine_counter", 102, "good"),
        new SimulatedCounter("machine_counter", 5, "good"),
    ];
}

public sealed record SimulatedMachine(
    string Code,
    string Name,
    string AssetClass);

/// <summary>
/// Canonical typed telemetry — exactly one value field populated.
/// Metric keys are simulator fixture values only. NOT real machine tags.
/// </summary>
public sealed record SimulatedTelemetry(
    string MetricKey,
    double? NumericValue,
    string? TextValue,
    bool? BooleanValue,
    string? Unit,
    string Quality);

/// <summary>
/// Canonical cycle-completed observation. Duration must be > 0.
/// ObservedCycleCount is NOT official Production quantity.
/// </summary>
public sealed record SimulatedCycle(
    string CycleIdentity,
    long CycleDurationMs,
    long? ObservedCounter,
    string Quality);

/// <summary>
/// Counter observation. May reset/wrap. No automatic Production quantity inference.
/// </summary>
public sealed record SimulatedCounter(
    string CounterKey,
    long CounterValue,
    string Quality);

public sealed record SimulatedEvent(
    string IdempotencyKey,
    string MachineCode,
    string EventKind,
    DateTimeOffset SourceTimestampUtc,
    long SourceSequence,
    string? Payload,
    SimulatedTelemetry? TypedTelemetry = null,
    SimulatedCycle? TypedCycle = null,
    SimulatedCounter? TypedCounter = null);

public sealed record SimulatorEventsPage(
    IReadOnlyList<SimulatedEvent> Events,
    bool HasMore,
    long NextSequence);
