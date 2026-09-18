using NevaxisZentra.Persistence.Edge;

namespace NevaxisZentra.Persistence.ProcessOee;

/// <summary>
/// Deterministic, versioned thresholds for PROCESS-OEE-STABILITY-V1.
/// Median + MAD robust dispersion; early/late window drift comparison.
/// </summary>
public static class ProcessOeeStabilityConstants
{
    public const string RuleSetVersion = "PROCESS-OEE-STABILITY-V1";

    /// <summary>Minimum good-quality cycle samples required for stability classification.</summary>
    public const int MinimumSampleCount = 8;

    /// <summary>Evidence older than window end minus this many hours is stale.</summary>
    public const int StaleEvidenceThresholdHours = 24;

    /// <summary>Micro-stop boundary aligned with Edge ingestion (60 seconds).</summary>
    public const long MicroStopBoundaryMs = EdgeObservationDefaults.MicroStopThresholdMs;

    /// <summary>MAD/median ratio below this indicates stable dispersion.</summary>
    public const decimal StableMadRatioThreshold = 0.10m;

    /// <summary>MAD/median ratio at or above this indicates variable dispersion.</summary>
    public const decimal VariableMadRatioThreshold = 0.25m;

    /// <summary>Relative median change between early and late windows indicating drift.</summary>
    public const decimal DriftRatioThreshold = 0.15m;

    /// <summary>Fraction of window used for early/late drift comparison (each segment).</summary>
    public const decimal DriftWindowFraction = 0.25m;
}

public static class ProcessOeeStabilityStates
{
    public const string Stable = "stable";
    public const string Variable = "variable";
    public const string Drifting = "drifting";
    public const string ReviewRequired = "review-required";
    public const string InsufficientData = "insufficient-data";
}

public static class ProcessOeeStabilityCategories
{
    public const string CycleVariability = "cycle-variability";
    public const string CycleDrift = "cycle-drift";
    public const string MicroStop = "micro-stop";
    public const string DowntimeConcentration = "downtime-concentration";
    public const string ShiftComparison = "shift-comparison";
    public const string InsufficientData = "insufficient-data";
    public const string SourceConflict = "source-conflict";
}

public static class ProcessOeeStabilityReasonCodes
{
    public const string StableCycles = "cycle-stability-stable";
    public const string VariableCycles = "cycle-variability-high";
    public const string DriftIncreasing = "cycle-drift-increasing";
    public const string DriftDecreasing = "cycle-drift-decreasing";
    public const string InsufficientSamples = "insufficient-data-cycle-samples";
    public const string StaleCycles = "insufficient-data-stale-cycles";
    public const string ExcludedBadQuality = "cycle-evidence-bad-quality-excluded";
    public const string ExcludedInvalidDuration = "cycle-evidence-invalid-duration-excluded";
    public const string ExcludedDuplicates = "cycle-evidence-duplicates-excluded";
    public const string DerivedCycleEvidence = "cycle-evidence-derived";
    public const string MicroStopSummary = "micro-stop-summary";
    public const string DowntimeConcentration = "downtime-concentration-leading-reason";
    public const string SourceConflict = "source-conflict-evidence";
    public const string ShiftComparisonSummary = "shift-comparison-summary";
}

public static class ProcessOeeStabilityDriftDirections
{
    public const string None = "none";
    public const string Increasing = "increasing";
    public const string Decreasing = "decreasing";
}

public static class ProcessOeeStabilityFreshnessStates
{
    public const string Fresh = "fresh";
    public const string Stale = "stale";
    public const string Unknown = "unknown";
}

public static class ProcessOeeStabilitySeverities
{
    public const string High = "high";
    public const string Medium = "medium";
    public const string Low = "low";
    public const string Informational = "informational";
}

public static class ProcessOeeDowntimeReasonCodes
{
    public const string Unknown = "unknown";
}
