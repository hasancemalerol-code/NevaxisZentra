namespace NevaxisFactOS.Persistence.Production;

/// <summary>
/// Configuration options for the Production Execution bounded context.
/// Stale threshold defaults are intentionally conservative for development environments.
/// Production deployments should override these values explicitly via appsettings.
/// </summary>
public sealed class ProductionExecutionOptions
{
    public const string SectionName = "ProductionExecution";

    /// <summary>
    /// Minutes after which an active (not yet ended) downtime session is considered stale.
    /// Detection is read-time deterministic — no background scheduler required.
    /// Development default: 480 minutes (8 hours). Override in production appsettings.
    /// </summary>
    public int StaleDowntimeThresholdMinutes { get; set; } = 480;

    /// <summary>
    /// Minutes after which an active (not yet completed) setup session is considered stale.
    /// Detection is read-time deterministic — no background scheduler required.
    /// Development default: 1440 minutes (24 hours). Override in production appsettings.
    /// </summary>
    public int StaleSetupThresholdMinutes { get; set; } = 1440;
}
