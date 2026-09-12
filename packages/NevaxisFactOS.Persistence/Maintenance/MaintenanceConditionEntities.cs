namespace NevaxisFactOS.Persistence.Maintenance;

public static class MaintenanceConditionStatuses
{
    public const string Normal = "normal";
    public const string Watch = "watch";
    public const string Degraded = "degraded";
    public const string Critical = "critical";
    public const string Unknown = "unknown";
}

public static class MaintenanceConditionDataSufficiencyLevels
{
    public const string High = "high";
    public const string Medium = "medium";
    public const string Low = "low";
    public const string Insufficient = "insufficient";
}

public static class MaintenanceConditionDataFreshnessLevels
{
    public const string Fresh = "fresh";
    public const string Stale = "stale";
    public const string Unknown = "unknown";
}

public static class MaintenanceConditionTrendStates
{
    public const string Stable = "stable";
    public const string Increasing = "increasing";
    public const string Decreasing = "decreasing";
    public const string Volatile = "volatile";
    public const string Insufficient = "insufficient";
}

public static class MaintenanceAnomalySeverities
{
    public const string Info = "info";
    public const string Warning = "warning";
    public const string High = "high";
    public const string Critical = "critical";
}

public static class MaintenanceAnomalyDirections
{
    public const string High = "high";
    public const string Low = "low";
    public const string Volatile = "volatile";
    public const string TrendUp = "trend-up";
    public const string TrendDown = "trend-down";
    public const string Unknown = "unknown";
}

public static class MaintenanceAnomalyStatuses
{
    public const string Open = "open";
    public const string Normalized = "normalized";
    public const string Acknowledged = "acknowledged";
}

public static class MaintenanceConditionBaselineMethods
{
    public const string Median = "median";
    public const string Mean = "mean";
}

public sealed class MaintenanceAssetMachineMapping
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public string AssetType { get; set; } = string.Empty;
    public string? AssetId { get; set; }
    public string AssetReference { get; set; } = string.Empty;
    public string? AssetName { get; set; }
    public Guid MachineId { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public uint Version { get; set; }
}

public sealed class MaintenanceConditionMetricDefinition
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public string MetricKey { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? ExpectedUnit { get; set; }
    public int EvaluationWindowHours { get; set; }
    public int MinimumSamples { get; set; }
    public string BaselineMethod { get; set; } = MaintenanceConditionBaselineMethods.Median;
    public decimal DeviationThresholdPercent { get; set; }
    public decimal TrendThresholdPercent { get; set; }
    public decimal? VolatilityThresholdPercent { get; set; }
    public bool Enabled { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public uint Version { get; set; }
}

public sealed class MaintenanceConditionProfile
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public string AssetReference { get; set; } = string.Empty;
    public Guid MachineId { get; set; }
    public string ConditionStatus { get; set; } = MaintenanceConditionStatuses.Unknown;
    public decimal? OverallConditionScore { get; set; }
    public DateTimeOffset? LastEvaluatedAtUtc { get; set; }
    public string DataFreshness { get; set; } = MaintenanceConditionDataFreshnessLevels.Unknown;
    public string DataSufficiency { get; set; } = MaintenanceConditionDataSufficiencyLevels.Insufficient;
    public int ActiveAnomalyCount { get; set; }
    public string? HighestAnomalySeverity { get; set; }
    public string TrendState { get; set; } = MaintenanceConditionTrendStates.Insufficient;
    public string? EvaluationIdentity { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public uint Version { get; set; }
}

public sealed class MaintenanceConditionAnomaly
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid FacilityId { get; set; }
    public string AssetReference { get; set; } = string.Empty;
    public Guid MachineId { get; set; }
    public string MetricKey { get; set; } = string.Empty;
    public DateTimeOffset DetectedAtUtc { get; set; }
    public DateTimeOffset WindowStartUtc { get; set; }
    public DateTimeOffset WindowEndUtc { get; set; }
    public decimal ObservedValue { get; set; }
    public decimal BaselineValue { get; set; }
    public decimal DeviationPercent { get; set; }
    public string Severity { get; set; } = MaintenanceAnomalySeverities.Info;
    public string Direction { get; set; } = MaintenanceAnomalyDirections.Unknown;
    public string EvidenceSummary { get; set; } = string.Empty;
    public string SourceFreshness { get; set; } = MaintenanceConditionDataFreshnessLevels.Unknown;
    public string? DataQuality { get; set; }
    public string Status { get; set; } = MaintenanceAnomalyStatuses.Open;
    public string EvaluationIdentity { get; set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; set; }
}
