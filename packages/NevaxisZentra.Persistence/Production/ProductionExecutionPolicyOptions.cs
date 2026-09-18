namespace NevaxisZentra.Persistence.Production;

/// <summary>
/// Facility-level execution policy flags that govern start/completion safety rules.
/// Configured in appsettings under "ProductionExecutionPolicy" or similar.
/// These defaults match NevaxisZentra pilot requirements.
/// </summary>
public sealed class ProductionExecutionPolicyOptions
{
    public const string SectionName = "ProductionExecutionPolicy";

    /// <summary>
    /// When true, a Start command is rejected unless a primary-operator assignment
    /// is active for the production order.
    /// Default: true (per NevaxisZentra pilot mandate — section 12 of PRODUCTION-5L).
    /// </summary>
    public bool RequirePrimaryOperatorForStart { get; set; } = true;

    /// <summary>
    /// When true, a Start command is rejected unless a shift assignment is active.
    /// Default: false (shift is recommended but not mandatory at pilot).
    /// </summary>
    public bool RequireShiftForStart { get; set; }
}
