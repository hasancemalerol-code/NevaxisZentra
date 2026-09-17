namespace NevaxisFactOS.Persistence.RealFactoryPilot;

public sealed class RealFactoryPilotRunRecord
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid LineId { get; set; }
    public string LineCode { get; set; } = string.Empty;
    public string CurrentPhase { get; set; } = RealFactoryPilotPhases.Connect;
    public string AcceptanceKind { get; set; } = RealFactoryPilotAcceptanceKinds.Synthetic;
    public Guid? DigitalWorkCaseId { get; set; }
    public Guid? ThroughputOptimizerRunId { get; set; }
    public string ConnectorStateJson { get; set; } = "{}";
    public string ShadowStateJson { get; set; } = "{}";
    public string CalibrationJson { get; set; } = "{}";
    public string EngineVersion { get; set; } = "real-factory-pilot-v1";
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}

public sealed class RealFactoryFieldMappingRecord
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid ConnectorId { get; set; }
    public Guid? MappingVersionId { get; set; }
    public int MappingVersionNumber { get; set; }
    public string Status { get; set; } = RealFactoryMappingStatuses.Draft;
    public string SourceField { get; set; } = string.Empty;
    public string SourceType { get; set; } = string.Empty;
    public string TargetEntity { get; set; } = string.Empty;
    public string? TargetEntityId { get; set; }
    public string CanonicalEventType { get; set; } = string.Empty;
    public string? Transformation { get; set; }
    public string? Unit { get; set; }
    public decimal? Scale { get; set; }
    public string? QualityRule { get; set; }
    public string TimestampRule { get; set; } = "source";
    public string CreatedBySubjectId { get; set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset? ActivatedAtUtc { get; set; }
}

public sealed class RealFactoryDiscoveryRecord
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid ConnectorId { get; set; }
    public string DiscoveryJson { get; set; } = "[]";
    public DateTimeOffset DiscoveredAtUtc { get; set; }
}

public sealed class RealFactoryShadowSessionRecord
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid LineId { get; set; }
    public Guid? PilotRunId { get; set; }
    public string Mode { get; set; } = RealFactoryShadowModes.ShadowIntelligence;
    public bool IsActive { get; set; }
    public bool BlocksPhysicalActions { get; set; } = true;
    public string RecommendationsJson { get; set; } = "[]";
    public DateTimeOffset StartedAtUtc { get; set; }
    public DateTimeOffset? EndedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}

public sealed class RealFactoryDataQualityRecord
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid? ConnectorId { get; set; }
    public Guid LineId { get; set; }
    public string OverallState { get; set; } = RealFactoryDataQualityStates.Insufficient;
    public string AssessmentJson { get; set; } = "{}";
    public bool BlocksAutonomousAction { get; set; } = true;
    public DateTimeOffset AssessedAtUtc { get; set; }
}

public sealed class RealFactoryShadowAccuracyRecord
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid PilotRunId { get; set; }
    public string PredictedConstraintStation { get; set; } = string.Empty;
    public string? ObservedConstraintStation { get; set; }
    public decimal? PredictedGoodThroughputPerHour { get; set; }
    public decimal? ActualGoodThroughputPerHour { get; set; }
    public decimal? PredictedDeltaPercent { get; set; }
    public decimal? ActualDeltaPercent { get; set; }
    public bool ConstraintMatched { get; set; }
    public bool ThroughputWithinTolerance { get; set; }
    public DateTimeOffset RecordedAtUtc { get; set; }
}

public sealed class Mp2CalibrationVersionRecord
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid LineId { get; set; }
    public int VersionNumber { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string EvidenceJson { get; set; } = "[]";
    public string ParametersJson { get; set; } = "{}";
    public DateTimeOffset EffectiveFromUtc { get; set; }
    public string CreatedBySubjectId { get; set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; set; }
}

public sealed class RealFactoryPilotEvidencePackRecord
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid PilotRunId { get; set; }
    public Guid LineId { get; set; }
    public string FacilityCode { get; set; } = string.Empty;
    public string LineCode { get; set; } = string.Empty;
    public DateTimeOffset PilotPeriodStartUtc { get; set; }
    public DateTimeOffset PilotPeriodEndUtc { get; set; }
    public string PackJson { get; set; } = "{}";
    public string VerificationOutcome { get; set; } = string.Empty;
    public bool KnowledgePromoted { get; set; }
    public DateTimeOffset GeneratedAtUtc { get; set; }
}
