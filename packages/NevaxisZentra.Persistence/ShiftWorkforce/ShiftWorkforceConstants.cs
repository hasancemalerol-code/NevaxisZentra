namespace NevaxisZentra.Persistence.ShiftWorkforce;

public static class ShiftWorkforceSourceSystems
{
    public const string LocalDevelopmentPilot = "local-development-pilot";
    public const string Logo = "logo";
    public const string Mikro = "mikro";
    public const string Netsis = "netsis";
    public const string Unknown = "unknown";
}

public static class ShiftWorkforceAvailabilityStates
{
    public const string Available = "available";
    public const string Leave = "leave";
    public const string Absent = "absent";
    public const string Unknown = "unknown";
}

public static class ShiftWorkforceFreshnessStates
{
    public const string Fresh = "fresh";
    public const string Stale = "stale";
    public const string Unknown = "unknown";
}

public static class ShiftWorkforceCompetencyCodes
{
    public const string InjectionMachineOperation = "injection-machine-operation";
    public const string MoldChangeSetup = "mold-change-setup";
    public const string ProcessParameterReview = "process-parameter-review";
    public const string MaterialHandling = "material-handling";
    public const string RobotAuxiliaryEquipment = "robot-auxiliary-equipment";
    public const string QualityCheck = "quality-check";
    public const string MaintenanceSupport = "maintenance-support";
}

public static class ShiftWorkforceCompetencyLevels
{
    public const string Basic = "basic";
    public const string Competent = "competent";
    public const string Proficient = "proficient";
    public const string Expert = "expert";
}

public static class ShiftWorkforceVerificationStatuses
{
    public const string Verified = "verified";
    public const string PendingVerification = "pending-verification";
    public const string Expired = "expired";
    public const string Rejected = "rejected";
}

public static class ShiftWorkforceAuditEventTypes
{
    public const string PersonSynchronized = "shift-workforce.person.synchronized";
    public const string CompetencyCreated = "shift-workforce.competency.created";
    public const string CompetencyUpdated = "shift-workforce.competency.updated";
}

public static class ShiftWorkforceAuditActions
{
    public const string PersonSynchronized = "shift-workforce.person.synchronized";
    public const string CompetencyCreated = "shift-workforce.competency.created";
    public const string CompetencyUpdated = "shift-workforce.competency.updated";
}

public static class ShiftWorkforceEntityTypes
{
    public const string Person = "shift-workforce-person";
    public const string CompetencyAssignment = "shift-workforce-competency-assignment";
}
