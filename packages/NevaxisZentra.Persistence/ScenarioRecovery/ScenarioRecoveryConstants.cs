namespace NevaxisZentra.Persistence.ScenarioRecovery;

public static class ScenarioRecoveryPermissionKeys
{
    public const string Read = "scenario-recovery.read";
    public const string Manage = "scenario-recovery.manage";
    public const string Simulate = "scenario-recovery.simulate";
    public const string Execute = "scenario-recovery.execute";
}

/// <summary>Audit taxonomy of the explicit scenario-run creation (added to audit_records by migration).</summary>
public static class ScenarioRecoveryAuditTaxonomy
{
    public const string EventType = "scenariorecovery.run.created";
    public const string Action = "scenariorecovery.run.create";
    public const string ResourceType = "scenariorecovery.run";
}

/// <summary>Who explicitly created a scenario run (recorded in audit_records together with the run).</summary>
public sealed record ScenarioRecoveryRunAudit(
    Guid IdentitySubjectId,
    Guid MembershipId,
    string PermissionKey,
    Guid CorrelationId,
    string? RequestId);

public static class ScenarioRecoveryFailureCodes
{
    public const string NotFound = "scenario-recovery.not-found";
    public const string ValidationFailed = "scenario-recovery.validation-failed";
    public const string InsufficientContext = "scenario-recovery.insufficient-context";
    public const string ConflictedState = "scenario-recovery.conflicted-state";
    public const string Infeasible = "scenario-recovery.infeasible";
    public const string SimulationFailed = "scenario-recovery.simulation-failed";
    public const string UnsupportedAction = "scenario-recovery.unsupported-action";
    public const string ApprovalRequired = "scenario-recovery.approval-required";
    public const string UnauthorizedExecute = "scenario-recovery.unauthorized-execute";
    public const string DependencyConflict = "scenario-recovery.dependency-conflict";
    public const string StaleSnapshot = "scenario-recovery.stale-snapshot";
}

public static class ScenarioRecoveryPhases
{
    public const string Draft = "DRAFT";
    public const string SnapshotCaptured = "SNAPSHOT_CAPTURED";
    public const string OptionsGenerated = "OPTIONS_GENERATED";
    public const string Compared = "COMPARED";
    public const string Recommended = "RECOMMENDED";
    public const string Selected = "SELECTED";
    public const string Approval = "APPROVAL";
    public const string Applied = "APPLIED";
    public const string Observing = "OBSERVING";
    public const string Verified = "VERIFIED";
    public const string Closed = "CLOSED";
}

public static class ScenarioRecoveryCategories
{
    public const string ProductionRecovery = "PRODUCTION_RECOVERY";
    public const string ScheduleRecovery = "SCHEDULE_RECOVERY";
    public const string ThroughputImprovement = "THROUGHPUT_IMPROVEMENT";
    public const string ResourceReallocation = "RESOURCE_REALLOCATION";
    public const string OperatorReallocation = "OPERATOR_REALLOCATION";
    public const string BufferAdjustment = "BUFFER_ADJUSTMENT";
    public const string MaterialRecovery = "MATERIAL_RECOVERY";
    public const string MaintenanceRecovery = "MAINTENANCE_RECOVERY";
    public const string QualityRecovery = "QUALITY_RECOVERY";
    public const string ChangeoverOptimization = "CHANGEOVER_OPTIMIZATION";
    public const string CombinedIntervention = "COMBINED_INTERVENTION";
}

public static class ScenarioRecoveryEntryPoints
{
    public const string Factory = "FABRIKA";
    public const string Plan = "PLAN";
    public const string Bugun = "BUGUN";
    public const string Isler = "ISLER";
    public const string Direct = "DIRECT";
    public const string GoldenDemo = "GOLDEN_DEMO";
}

public static class ScenarioRecoveryOptionLabels
{
    public const string Baseline = "BASELINE";
    public const string OptionA = "OPTION_A";
    public const string OptionB = "OPTION_B";
    public const string OptionC = "OPTION_C";
}

public static class ScenarioRecoveryFeasibilityStates
{
    public const string Feasible = "FEASIBLE";
    public const string FeasibleWithRisk = "FEASIBLE_WITH_RISK";
    public const string Infeasible = "INFEASIBLE";
}

public static class ScenarioRecoveryExecutionStates
{
    public const string Simulated = "SIMULATED";
    public const string Applied = "APPLIED";
    public const string Observing = "OBSERVING";
    public const string Verified = "VERIFIED";
}

public static class ScenarioRecoveryVerificationOutcomes
{
    public const string VerifiedSuccess = "VERIFIED_SUCCESS";
    public const string PartiallyVerified = "PARTIALLY_VERIFIED";
    public const string NoImprovement = "NO_IMPROVEMENT";
    public const string Regression = "REGRESSION";
    public const string Inconclusive = "INCONCLUSIVE";
}

public static class ScenarioRecoveryEngineVersions
{
    public const string Orchestration = "scenario-recovery-orchestration/1.0.0";
    public const string Feasibility = "scenario-recovery-feasibility/1.0.0";
    public const string Recommendation = "scenario-recovery-recommendation/1.0.0";
}

public static class ScenarioRecoveryCorrelationPrefixes
{
    public const string LineRecovery = "scenario-recovery:line";
}

public static class ScenarioRecoveryActionTypes
{
    public const string AddOperator = "ADD_OPERATOR";
    public const string MoveOperator = "MOVE_OPERATOR";
    public const string ChangeBufferCapacity = "CHANGE_BUFFER_CAPACITY";
    public const string ResequenceOrder = "RESEQUENCE_ORDER";
    public const string ReassignResource = "REASSIGN_RESOURCE";
    public const string ExpediteMaterial = "EXPEDITE_MATERIAL";
    public const string CombinedIntervention = "COMBINED_INTERVENTION";
}

public static class ScenarioRecoveryObjectiveTypes
{
    public const string MaximizeVerifiedThroughput = "MAXIMIZE_VERIFIED_GOOD_THROUGHPUT";
    public const string ProtectOtif = "PROTECT_OTIF";
    public const string MinimizeLateness = "MINIMIZE_LATENESS";
    public const string MinimizeScrap = "MINIMIZE_SCRAP";
    public const string MinimizeChangeover = "MINIMIZE_CHANGEOVER";
    public const string RecoverCapacity = "RECOVER_LOST_CAPACITY";
    public const string ReduceWip = "REDUCE_WIP";
    public const string ProtectMaintenance = "PROTECT_MAINTENANCE_CONSTRAINT";
}

public sealed class ScenarioRecoveryException : Exception
{
    public ScenarioRecoveryException(string code, string message) : base(message)
    {
        Code = code;
    }

    public string Code { get; }
}
