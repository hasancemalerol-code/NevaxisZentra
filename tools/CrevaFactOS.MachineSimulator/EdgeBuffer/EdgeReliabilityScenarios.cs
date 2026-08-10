namespace CrevaFactOS.MachineSimulator.EdgeBuffer;

/// <summary>
/// Deterministic EDGE-4 reliability scenario event generation.
/// </summary>
public static class EdgeReliabilityScenarios
{
    public static readonly Guid PilotTenantId = Guid.Parse("bb000001-0001-4000-8000-000000000001");
    public static readonly Guid PilotFacilityId = Guid.Parse("bb000001-0005-4000-8000-000000000005");
    public static readonly Guid SimulatorGatewayId = Guid.Parse("bb000001-0094-4000-8000-000000000094");
    public static readonly Guid CncMachineId = Guid.Parse("bb000001-0095-4000-8000-000000000095");
    public static readonly Guid PressMachineId = Guid.Parse("bb000001-0096-4000-8000-000000000096");

    private static readonly DateTimeOffset OfflineBase =
        new(2026, 8, 10, 14, 0, 0, TimeSpan.Zero);

    public static IReadOnlyList<EdgeBufferedEvent> BuildOfflineBufferEvents(DateTimeOffset createdAtUtc)
    {
        var events = new List<EdgeBufferedEvent>();
        long seq = 9001;

        void Add(string machineCode, Guid machineId, string kind, string? payload = null)
        {
            events.Add(CreateEvent(
                machineCode,
                machineId,
                kind,
                seq++,
                OfflineBase.AddSeconds(seq - 9001),
                payload,
                createdAtUtc));
        }

        Add("CNC-001", CncMachineId, "heartbeat");
        Add("CNC-001", CncMachineId, "telemetry", """{"spindleSpeed":1800,"feedRate":420}""");
        Add("CNC-001", CncMachineId, "cycle-start", """{"cycleId":"OFF-CYC-001"}""");
        Add("CNC-001", CncMachineId, "cycle-end", """{"cycleId":"OFF-CYC-001","durationMs":45000}""");
        Add("CNC-001", CncMachineId, "counter-observed", """{"counterKey":"machine_counter","counterValue":205}""");
        Add("CNC-001", CncMachineId, "machine.downtime.started", """{"reasonCode":"OFFLINE-TEST","reasonCategory":"machine"}""");
        Add("CNC-001", CncMachineId, "machine.downtime.ended", """{"reasonCode":"OFFLINE-TEST"}""");
        Add("CNC-001", CncMachineId, "machine.alarm.raised", """{"alarmIdentity":"SIM-ALARM-OFFLINE","severity":"warning"}""");
        Add("PRESS-001", PressMachineId, "heartbeat");
        Add("PRESS-001", PressMachineId, "telemetry", """{"pressure":92.5,"strokeCount":44}""");
        Add("PRESS-001", PressMachineId, "machine.setup.started", """{"setupType":"changeover","fromReference":"A","toReference":"B"}""");
        Add("PRESS-001", PressMachineId, "machine.setup.completed", """{"setupType":"changeover"}""");
        Add("PRESS-001", PressMachineId, "machine.scrap.observed", """{"quantity":1,"unit":"count","reasonCode":"offline-fixture"}""");

        return events;
    }

    public static EdgeBufferedEvent CreatePoisonEvent(DateTimeOffset createdAtUtc) =>
        CreateEvent(
            "CNC-001",
            CncMachineId,
            "telemetry",
            99999,
            OfflineBase.AddMinutes(30),
            """{"unsupported":true}""",
            createdAtUtc,
            payloadVersion: 99,
            idempotencyKey: "SIM-EDGE4:POISON:001");

    public static IReadOnlyList<EdgeBufferedEvent> BuildPartialFailureBatch(DateTimeOffset createdAtUtc)
    {
        var a = CreateEvent("CNC-001", CncMachineId, "heartbeat", 8801, OfflineBase.AddMinutes(10), null, createdAtUtc, idempotencyKey: "SIM-EDGE4:PARTIAL:A");
        var b = CreateEvent("CNC-001", CncMachineId, "telemetry", 8802, OfflineBase.AddMinutes(11), """{"temp":42}""", createdAtUtc, idempotencyKey: "SIM-EDGE4:PARTIAL:B");
        var c = CreateEvent("CNC-001", CncMachineId, "telemetry", 8803, OfflineBase.AddMinutes(12), """{"bad":true}""", createdAtUtc, idempotencyKey: "SIM-EDGE4:PARTIAL:C");
        var d = CreateEvent("CNC-001", CncMachineId, "heartbeat", 8804, OfflineBase.AddMinutes(13), null, createdAtUtc, idempotencyKey: "SIM-EDGE4:PARTIAL:D");
        return [a, b, c, d];
    }

    public static EdgeBufferedEvent CreateNormalEvent(DateTimeOffset createdAtUtc) =>
        CreateEvent(
            "CNC-001",
            CncMachineId,
            "heartbeat",
            8701,
            OfflineBase.AddMinutes(5),
            null,
            createdAtUtc,
            idempotencyKey: $"SIM-EDGE4:NORMAL:{createdAtUtc.ToUnixTimeMilliseconds()}");

    public static EdgeBufferedEvent CreateEvent(
        string machineCode,
        Guid machineId,
        string eventType,
        long sourceSequence,
        DateTimeOffset sourceTimestampUtc,
        string? payload,
        DateTimeOffset createdAtUtc,
        int payloadVersion = 1,
        string? idempotencyKey = null)
    {
        var eventId = Guid.NewGuid();
        return new EdgeBufferedEvent(
            Id: Guid.NewGuid(),
            TenantId: PilotTenantId,
            FacilityId: PilotFacilityId,
            GatewayId: SimulatorGatewayId,
            MachineId: machineId,
            MachineCode: machineCode,
            EventId: eventId,
            EventType: eventType,
            IdempotencyKey: idempotencyKey ?? $"SIM-EDGE4:{machineCode}:{sourceSequence}",
            SourceTimestampUtc: sourceTimestampUtc,
            SourceSequence: sourceSequence,
            PayloadVersion: payloadVersion,
            Payload: payload,
            CreatedAtUtc: createdAtUtc,
            DeliveryStatus: EdgeDeliveryStatuses.Pending,
            AttemptCount: 0,
            LastAttemptAtUtc: null,
            NextAttemptAtUtc: createdAtUtc,
            LastErrorCode: null,
            DeliveredAtUtc: null,
            AcknowledgementId: null,
            DeadLetteredAtUtc: null,
            InFlightClaimedAtUtc: null,
            InFlightLeaseId: null);
    }
}
