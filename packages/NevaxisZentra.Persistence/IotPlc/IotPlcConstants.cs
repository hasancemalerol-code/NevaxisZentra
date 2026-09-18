namespace NevaxisZentra.Persistence.IotPlc;

public static class IotPlcConstants
{
    public const string DevelopmentPilotProvenance =
        "Deterministic local Development industrial connectivity data — simulated, non-production, not real PLC or customer evidence.";

    public const string NonProductionSecretPlaceholder = "dev-placeholder-no-real-secret";
}

public static class IotPlcProtocolTypes
{
    public const string OpcUa = "opc-ua";
    public const string ModbusTcp = "modbus-tcp";
    public const string Mqtt = "mqtt";
    public const string HttpPush = "http-push";
    public const string EdgeAgent = "edge-agent";
    public const string ManualDevelopmentSimulator = "manual-development-simulator";
}

public static class IotPlcConnectionDirections
{
    public const string ReadOnlyPull = "read-only-pull";
    public const string InboundPush = "inbound-push";
    public const string EdgeRelay = "edge-relay";
}

public static class IotPlcEndpointStatuses
{
    public const string Draft = "draft";
    public const string Active = "active";
    public const string Suspended = "suspended";
    public const string Retired = "retired";
}

public static class IotPlcConnectionStates
{
    public const string Online = "online";
    public const string Degraded = "degraded";
    public const string Offline = "offline";
    public const string Unknown = "unknown";
}

public static class IotPlcDataTypes
{
    public const string Boolean = "boolean";
#pragma warning disable CA1720
    public const string Integer = "integer";
    public const string Decimal = "decimal";
#pragma warning restore CA1720
    public const string Text = "text";
    public const string Timestamp = "timestamp";
}

public static class IotPlcSemanticTypes
{
    public const string MachineState = "machine-state";
    public const string CycleCounter = "cycle-counter";
    public const string GoodPartCounter = "good-part-counter";
    public const string ScrapCounter = "scrap-counter";
    public const string EnergyReading = "energy-reading";
    public const string PowerReading = "power-reading";
    public const string Temperature = "temperature";
    public const string Pressure = "pressure";
    public const string Speed = "speed";
    public const string RuntimeCounter = "runtime-counter";
    public const string GenericObservation = "generic-observation";
}

public static class IotPlcAssetTypes
{
    public const string Machine = "machine";
    public const string AuxiliaryEquipment = "auxiliary-equipment";
    public const string Facility = "facility";
    public const string Line = "line";
    public const string Unmapped = "unmapped";
}

public static class IotPlcQualityStates
{
    public const string Good = "good";
    public const string Uncertain = "uncertain";
    public const string Bad = "bad";
    public const string Stale = "stale";
    public const string SourceConflict = "source-conflict";
}

public static class IotPlcEvidenceStates
{
    public const string Measured = "measured";
    public const string Calculated = "calculated";
    public const string Estimated = "estimated";
    public const string Simulated = "simulated";
    public const string InsufficientData = "insufficient-data";
    public const string SourceConflict = "source-conflict";
}

public static class IotPlcFailureCodes
{
    public const string ValidationFailed = "iot-plc.validation-failed";
    public const string NotFound = "iot-plc.not-found";
    public const string EndpointNotFound = "iot-plc.endpoint-not-found";
    public const string SignalNotFound = "iot-plc.signal-not-found";
    public const string ConcurrencyConflict = "iot-plc.concurrency-conflict";
    public const string IdempotencyConflict = "iot-plc.idempotency-conflict";
    public const string IdempotencyLegacyUnverifiable = "iot-plc.idempotency-legacy-unverifiable";
    public const string SourceReferenceConflict = "iot-plc.source-reference-conflict";
    public const string DuplicateObservationTimestamp = "iot-plc.duplicate-observation-timestamp";
    public const string MachineScopeDenied = "iot-plc.machine-scope-denied";
    public const string FacilityScopeDenied = "iot-plc.facility-scope-denied";
}
