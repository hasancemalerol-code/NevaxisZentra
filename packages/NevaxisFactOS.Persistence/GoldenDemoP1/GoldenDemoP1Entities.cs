namespace NevaxisFactOS.Persistence.GoldenDemoP1;

public sealed class GoldenDemoProductionEvent
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid MachineAssetId { get; set; }
    public string MachineCode { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Status { get; set; } = GoldenDemoP1EventStatuses.Active;
    public string? MoldReference { get; set; }
    public string? ProductReference { get; set; }
    public int ShiftTargetGoodParts { get; set; }
    public DateTimeOffset ShiftStartUtc { get; set; }
    public DateTimeOffset ShiftEndUtc { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public uint Version { get; set; } = 1;
}

public sealed class GoldenDemoCycleBaseline
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid ProductionEventId { get; set; }
    public string ReferenceSource { get; set; } = GoldenDemoReferenceSources.StableHistoricalWindow;
    public string ReferenceSourceLabel { get; set; } = string.Empty;
    public decimal TotalCycleSeconds { get; set; }
    public string PhaseDurationsJson { get; set; } = "{}";
    public string ProcessParametersJson { get; set; } = "{}";
    public decimal? ReferenceKwhPerGoodPart { get; set; }
    public DateTimeOffset ValidFromUtc { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
}

public sealed class GoldenDemoCycleObservation
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid ProductionEventId { get; set; }
    public string ObservationKind { get; set; } = "current";
    public decimal TotalCycleSeconds { get; set; }
    public string PhaseDurationsJson { get; set; } = "{}";
    public string ProcessParametersJson { get; set; } = "{}";
    public int MicroStopCount { get; set; }
    public decimal MicroStopDurationSeconds { get; set; }
    public int GoodParts { get; set; }
    public int RejectParts { get; set; }
    public decimal AvailabilityPercent { get; set; }
    public decimal? KwhPerGoodPart { get; set; }
    public decimal? TotalEnergyKwh { get; set; }
    public string MachineState { get; set; } = string.Empty;
    public DateTimeOffset ObservedAtUtc { get; set; }
    public DateTimeOffset WindowStartUtc { get; set; }
    public DateTimeOffset WindowEndUtc { get; set; }
}

public sealed class GoldenDemoRecoveryPlan
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid ProductionEventId { get; set; }
    public string Status { get; set; } = GoldenDemoPlanActionStatuses.Planned;
    public string RecommendationSummary { get; set; } = string.Empty;
    public string RecommendedScenarioCode { get; set; } = GoldenDemoWhatIfScenarioCodes.ControlledInterventionNow;
    public bool RequiresApproval { get; set; } = true;
    public string? ApprovedBySubjectId { get; set; }
    public DateTimeOffset? ApprovedAtUtc { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}

public sealed class GoldenDemoRecoveryPlanAction
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid RecoveryPlanId { get; set; }
    public int SortOrder { get; set; }
    public string ActionCode { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string ResponsibleRole { get; set; } = string.Empty;
    public string Priority { get; set; } = "medium";
    public string Status { get; set; } = GoldenDemoPlanActionStatuses.Planned;
    public string EvidenceRequired { get; set; } = string.Empty;
    public int EstimatedDowntimeMinutes { get; set; }
    public bool RequiresApproval { get; set; }
    public string ValidationCriteria { get; set; } = string.Empty;
    public DateTimeOffset? CompletedAtUtc { get; set; }
}

public sealed class GoldenDemoAgentFinding
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid ProductionEventId { get; set; }
    public string AgentCode { get; set; } = string.Empty;
    public string WorkerCode { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string DetailJson { get; set; } = "{}";
    public string EvidenceClassification { get; set; } = GoldenDemoImpactClassifications.Calculated;
    public DateTimeOffset GeneratedAtUtc { get; set; }
}

public sealed class GoldenDemoVerificationRun
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid ProductionEventId { get; set; }
    public string Result { get; set; } = GoldenDemoVerificationResults.InsufficientData;
    public string CriteriaJson { get; set; } = "{}";
    public string Summary { get; set; } = string.Empty;
    public decimal RecoveredGoodPartsPerShift { get; set; }
    public string TriggeredBySubjectId { get; set; } = string.Empty;
    public DateTimeOffset VerifiedAtUtc { get; set; }
}

public sealed class GoldenDemoDigitalWorkCaseLink
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid ProductionEventId { get; set; }
    public Guid DigitalWorkCaseId { get; set; }
    public string WorkerCode { get; set; } = string.Empty;
    public DateTimeOffset LinkedAtUtc { get; set; }
}
