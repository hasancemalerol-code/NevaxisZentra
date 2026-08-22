namespace CrevaFactOS.Persistence.IotPlc;

public static class IotPlcIntelligenceConstants
{
    public const string RuleSetVersion = "IOT-PLC-2.0";
    public const string DevelopmentPilotProvenance =
        "Deterministic local Development IoT/PLC intelligence data — simulated, non-production, not real PLC or customer connectivity evidence.";
}

public static class IotPlcQualityProfileStatuses
{
    public const string Approved = "approved";
    public const string Inactive = "inactive";
}

public static class IotPlcIntelligenceTargetTypes
{
    public const string Endpoint = "endpoint";
    public const string Signal = "signal";
    public const string MappedMachine = "mapped-machine";
    public const string AuxiliaryEquipment = "auxiliary-equipment";
    public const string Facility = "facility";
}

public static class IotPlcIntelligenceEvidenceModes
{
    public const string Simulated = "simulated";
    public const string Measured = "measured";
}

public static class IotPlcIntelligenceOutcomes
{
    public const string Healthy = "healthy";
    public const string Attention = "attention";
    public const string Degraded = "degraded";
    public const string Unavailable = "unavailable";
    public const string InsufficientData = "insufficient-data";
    public const string SourceConflict = "source-conflict";
}

public static class IotPlcIntelligenceDriverReasonCodes
{
    public const string WithinQualityProfile = "within-quality-profile";
    public const string MissingQualityProfile = "missing-quality-profile";
    public const string InsufficientObservations = "insufficient-observations";
    public const string ObservationStale = "observation-stale";
    public const string ObservationFreshnessExceeded = "observation-freshness-exceeded";
    public const string ExpectedCadenceMissed = "expected-cadence-missed";
    public const string ObservationGapDetected = "observation-gap-detected";
    public const string SourceSequenceGap = "source-sequence-gap";
    public const string ClockSkewExceeded = "clock-skew-exceeded";
    public const string BadQualityRatioExceeded = "bad-quality-ratio-exceeded";
    public const string UncertainQualityRatioExceeded = "uncertain-quality-ratio-exceeded";
    public const string SourceConflictRatioExceeded = "source-conflict-ratio-exceeded";
    public const string EndpointHealthDegraded = "endpoint-health-degraded";
    public const string EndpointOffline = "endpoint-offline";
    public const string EndpointHealthUnknown = "endpoint-health-unknown";
    public const string NoRecentObservation = "no-recent-observation";
    public const string DuplicateObservationEvidence = "duplicate-observation-evidence";
    public const string MappedAssetMismatch = "mapped-asset-mismatch";
    public const string SourceConflict = "source-conflict";
}

public static class IotPlcIntelligenceAssessmentKinds
{
    public const string Connectivity = "connectivity";
    public const string SignalQuality = "signal-quality";
}

public static class IotPlcIntelligenceFailureCodes
{
    public const string ValidationFailed = "iot-plc.intelligence.validation-failed";
    public const string ProfileNotFound = "iot-plc.intelligence.profile-not-found";
    public const string AssessmentNotFound = "iot-plc.intelligence.assessment-not-found";
    public const string ConcurrencyConflict = "iot-plc.intelligence.concurrency-conflict";
    public const string IdempotencyConflict = "iot-plc.intelligence.idempotency-conflict";
    public const string IdempotencyLegacyUnverifiable = "iot-plc.intelligence.idempotency-legacy-unverifiable";
    public const string TargetNotFound = "iot-plc.intelligence.target-not-found";
}
