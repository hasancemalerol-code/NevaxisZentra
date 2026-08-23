namespace CrevaFactOS.Persistence.OperationalRisk;

public sealed class OperationalRiskDetectionProfile
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public string ProfileCode { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Status { get; set; } = OperationalRiskDetectionProfileStatuses.Draft;
    public string RuleSetVersion { get; set; } = OperationalRiskDetectionConstants.RuleSetVersion;
    public string SourceProvenance { get; set; } = string.Empty;
    public string? CreateIdempotencyKey { get; set; }
    public string? CreateIdempotencyPayloadFingerprint { get; set; }
    public uint Version { get; set; } = 1;
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public string UpdatedBy { get; set; } = string.Empty;

    public ICollection<OperationalRiskDetectionRule> Rules { get; set; } = [];
}

public sealed class OperationalRiskDetectionRule
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid ProfileId { get; set; }
    public string RuleCode { get; set; } = string.Empty;
    public string SourceModule { get; set; } = string.Empty;
    public string SourceOutcome { get; set; } = string.Empty;
    public string RiskCategory { get; set; } = string.Empty;
    public int DefaultLikelihood { get; set; }
    public int DefaultImpact { get; set; }
    public decimal? ThresholdVarianceBand { get; set; }
    public string DriverReasonCode { get; set; } = string.Empty;
    public int PriorityOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public string SourceProvenance { get; set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }

    public OperationalRiskDetectionProfile? Profile { get; set; }
}

public sealed class OperationalRiskDetectionRun
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public string RunReference { get; set; } = string.Empty;
    public Guid ProfileId { get; set; }
    public string ProfileVersion { get; set; } = string.Empty;
    public DateTimeOffset WindowStartUtc { get; set; }
    public DateTimeOffset WindowEndUtc { get; set; }
    public string Status { get; set; } = OperationalRiskDetectionRunStatuses.Completed;
    public int SourceCount { get; set; }
    public int ResultCount { get; set; }
    public string RuleSetVersion { get; set; } = OperationalRiskDetectionConstants.RuleSetVersion;
    public string? IdempotencyKey { get; set; }
    public string? IdempotencyPayloadFingerprint { get; set; }
    public string SourceProvenance { get; set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset? CompletedAtUtc { get; set; }
    public string CreatedBy { get; set; } = string.Empty;

    public OperationalRiskDetectionProfile? Profile { get; set; }
    public ICollection<OperationalRiskDetectionResult> Results { get; set; } = [];
}

public sealed class OperationalRiskDetectionResult
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid RunId { get; set; }
    public string SourceModule { get; set; } = string.Empty;
    public string SourceReference { get; set; } = string.Empty;
    public string RiskCategory { get; set; } = string.Empty;
    public string ScopeType { get; set; } = string.Empty;
    public string? ScopeReference { get; set; }
    public string EvidenceState { get; set; } = string.Empty;
    public string SourceOutcome { get; set; } = string.Empty;
    public string DetectionOutcome { get; set; } = string.Empty;
    public decimal? ActualValue { get; set; }
    public decimal? ThresholdValue { get; set; }
    public string? Unit { get; set; }
    public decimal? NormalizedVariance { get; set; }
    public int? LikelihoodLevel { get; set; }
    public int? ImpactLevel { get; set; }
    public int? RiskScore { get; set; }
    public string? SeverityBand { get; set; }
    public string? LeadingDriverCode { get; set; }
    public Guid? LinkedCaseId { get; set; }
    public string DetectionKey { get; set; } = string.Empty;
    public string SourceAssessmentReference { get; set; } = string.Empty;
    public DateTimeOffset EvaluatedAtUtc { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }

    public OperationalRiskDetectionRun? Run { get; set; }
    public ICollection<OperationalRiskDetectionDriver> Drivers { get; set; } = [];
}

public sealed class OperationalRiskDetectionDriver
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid ResultId { get; set; }
    public string ReasonCode { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public decimal? ActualValue { get; set; }
    public decimal? ThresholdValue { get; set; }
    public string? Unit { get; set; }
    public decimal? NormalizedVariance { get; set; }
    public string SupportingText { get; set; } = string.Empty;
    public string SourceProvenance { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }

    public OperationalRiskDetectionResult? Result { get; set; }
}
