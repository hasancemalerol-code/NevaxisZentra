namespace NevaxisFactOS.Persistence.ProcessOee;

public static class ProcessOeeLossConstants
{
    public const string RuleSetVersion = "PROCESS-OEE-LOSS-V1";

    /// <summary>
    /// Leading loss is the category with the highest normalized OEE impact points.
    /// impactAvailability = (100 - A%) * (P%/100) * (Q%/100)
    /// impactPerformance = (A%/100) * (100 - P%) * (Q%/100)
    /// impactQuality = (A%/100) * (P%/100) * (100 - Q%)
    /// Unlike units are not compared directly; only normalized OEE point contributions are ranked.
    /// </summary>
    public const string LeadingLossMethod = "normalized-oee-impact-points-v1";
}

public static class ProcessOeeLossCategories
{
    public const string AvailabilityLoss = "availability-loss";
    public const string PerformanceLoss = "performance-loss";
    public const string QualityLoss = "quality-loss";
    public const string InsufficientData = "insufficient-data";
    public const string SourceConflict = "source-conflict";
}

public static class ProcessOeeLossReasonCodes
{
    public const string DowntimeDuration = "availability-loss-downtime-duration";
    public const string MissingDowntimeEvidence = "availability-loss-missing-downtime-evidence";
    public const string PlannedTimeMissing = "availability-loss-planned-time-missing";
    public const string SlowCycleTime = "performance-loss-slow-cycle-time";
    public const string DerivedShotEvidence = "performance-loss-derived-shots";
    public const string PerformanceAbove100 = "performance-loss-above-100-review";
    public const string MissingPerformanceEvidence = "performance-loss-insufficient-evidence";
    public const string ScrapQuantity = "quality-loss-scrap-quantity";
    public const string QuantityReconciliationConflict = "quality-loss-quantity-conflict";
    public const string MissingQualityEvidence = "quality-loss-insufficient-evidence";
    public const string SourceEvidenceConflict = "source-conflict-evidence";
    public const string StaleEvidence = "insufficient-data-stale-evidence";
    public const string MissingRequiredFields = "insufficient-data-missing-fields";
}

public static class ProcessOeeLossFreshnessStates
{
    public const string Fresh = "fresh";
    public const string Stale = "stale";
    public const string Unknown = "unknown";
}

public static class ProcessOeeLossSeverities
{
    public const string High = "high";
    public const string Medium = "medium";
    public const string Low = "low";
    public const string Informational = "informational";
}
