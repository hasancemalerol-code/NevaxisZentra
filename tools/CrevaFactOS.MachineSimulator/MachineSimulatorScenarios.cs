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

        events.Add(new SimulatedEvent(
            IdempotencyKey: $"SIM-GW-001:CNC-001:{seq}",
            MachineCode: "CNC-001",
            EventKind: "cycle-start",
            SourceTimestampUtc: BaseTimestamp.AddSeconds(95),
            SourceSequence: seq++,
            Payload: """{"cycleId":"CYC-002","programId":"PROG-A42","partRef":"PART-002"}"""));

        events.Add(new SimulatedEvent(
            IdempotencyKey: $"SIM-GW-001:CNC-001:{seq}",
            MachineCode: "CNC-001",
            EventKind: "cycle-end",
            SourceTimestampUtc: BaseTimestamp.AddSeconds(155),
            SourceSequence: seq++,
            Payload: """{"cycleId":"CYC-002","durationMs":60000,"result":"ok"}"""));

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

        events.Add(new SimulatedEvent(
            IdempotencyKey: $"SIM-GW-001:PRESS-001:{seq}",
            MachineCode: "PRESS-001",
            EventKind: "telemetry",
            SourceTimestampUtc: BaseTimestamp.AddMinutes(1).AddSeconds(5),
            SourceSequence: seq++,
            Payload: """{"tonnage":148,"strokeCount":1,"hydraulicPressure":210}"""));

        events.Add(new SimulatedEvent(
            IdempotencyKey: $"SIM-GW-001:PRESS-001:{seq}",
            MachineCode: "PRESS-001",
            EventKind: "cycle-end",
            SourceTimestampUtc: BaseTimestamp.AddMinutes(1).AddSeconds(12),
            SourceSequence: seq++,
            Payload: """{"cycleId":"PCYC-001","durationMs":10000,"result":"ok"}"""));

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
}

public sealed record SimulatedMachine(
    string Code,
    string Name,
    string AssetClass);

public sealed record SimulatedEvent(
    string IdempotencyKey,
    string MachineCode,
    string EventKind,
    DateTimeOffset SourceTimestampUtc,
    long SourceSequence,
    string? Payload);

public sealed record SimulatorEventsPage(
    IReadOnlyList<SimulatedEvent> Events,
    bool HasMore,
    long NextSequence);
