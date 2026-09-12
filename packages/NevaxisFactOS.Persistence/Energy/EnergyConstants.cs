namespace NevaxisFactOS.Persistence.Energy;

public static class EnergyConstants
{
    public const string RuleSetVersion = "ENERGY-1.0";
    public const int EnergyDecimalPlaces = 4;
    public const string DevelopmentPilotProvenance =
        "Deterministic local Development energy data — simulated, not real customer meter evidence.";
    public const string DevelopmentTariffProvenance =
        "Development-only illustrative tariff — not a real electricity price.";
}

public static class EnergyFailureCodes
{
    public const string ValidationFailed = "energy.validation-failed";
    public const string NotFound = "energy.not-found";
    public const string ConcurrencyConflict = "energy.concurrency-conflict";
    public const string IdempotencyConflict = "energy.idempotency-conflict";
    public const string IdempotencyLegacyUnverifiable = "energy.idempotency-legacy-unverifiable";
    public const string MeasurementPointNotFound = "energy.measurement-point-not-found";
    public const string MachineNotFound = "energy.machine-not-found";
    public const string OverlappingWindow = "energy.overlapping-window";
    public const string InvalidWindow = "energy.invalid-window";
    public const string FacilityScopeDenied = "energy.facility-scope-denied";
}

public static class EnergyMeasurementScopes
{
    public const string Facility = "facility";
    public const string Line = "line";
    public const string Machine = "machine";
    public const string AuxiliaryEquipment = "auxiliary-equipment";
}

public static class EnergyEquipmentCategories
{
    public const string InjectionMachine = "injection-machine";
    public const string Chiller = "chiller";
    public const string Compressor = "compressor";
    public const string MaterialDryer = "material-dryer";
    public const string Robot = "robot";
    public const string MoldTemperatureController = "mold-temperature-controller";
    public const string Other = "other";
}

public static class EnergyEvidenceStates
{
    public const string Measured = "measured";
    public const string Calculated = "calculated";
    public const string Estimated = "estimated";
    public const string Simulated = "simulated";
    public const string InsufficientData = "insufficient-data";
    public const string SourceConflict = "source-conflict";
}

public static class EnergyCalculationMethods
{
    public const string IntervalSum = "interval-sum";
    public const string CumulativeDelta = "cumulative-delta";
    public const string Mixed = "mixed";
    public const string Unavailable = "unavailable";
}

public static class EnergyAllocationTargetTypes
{
    public const string Machine = "machine";
    public const string ProductionOrder = "production-order";
    public const string Shift = "shift";
    public const string AuxiliaryEquipment = "auxiliary-equipment";
    public const string Unallocated = "unallocated";
}

public static class EnergyMeasurementPointStatuses
{
    public const string Active = "active";
    public const string Inactive = "inactive";
}

public static class EnergyFreshnessStates
{
    public const string Fresh = "fresh";
    public const string Stale = "stale";
    public const string Unknown = "unknown";
}

public static class EnergyAllocationMethods
{
    public const string ProportionalRuntime = "proportional-runtime";
    public const string DirectAssignment = "direct-assignment";
    public const string Residual = "residual";
    public const string Manual = "manual";
}

public static class EnergyAuditEventTypes
{
    public const string ReadingIngested = "energy.reading.ingested";
    public const string ConsumptionEvaluated = "energy.consumption.evaluated";
    public const string MeasurementPointCreated = "energy.measurement-point.created";
    public const string MeasurementPointUpdated = "energy.measurement-point.updated";
}
