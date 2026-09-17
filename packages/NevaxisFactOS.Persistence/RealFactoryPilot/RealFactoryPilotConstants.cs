namespace NevaxisFactOS.Persistence.RealFactoryPilot;

public static class RealFactoryPilotPermissionKeys
{
    public const string Read = "real-factory-pilot.read";
    public const string Manage = "real-factory-pilot.manage";
    public const string Run = "real-factory-pilot.run";
}

public static class RealFactoryPilotFailureCodes
{
    public const string NotFound = "real-factory-pilot.not-found";
    public const string ValidationFailed = "real-factory-pilot.validation-failed";
    public const string ConnectorInactive = "real-factory-pilot.connector-inactive";
    public const string MappingInvalid = "real-factory-pilot.mapping-invalid";
    public const string ShadowRequired = "real-factory-pilot.shadow-required";
    public const string DataQualityBlocked = "real-factory-pilot.data-quality-blocked";
    public const string UnsafeActionBlocked = "real-factory-pilot.unsafe-action-blocked";
    public const string ConnectorStale = "real-factory-pilot.connector-stale";
}

public static class RealFactoryPilotFixtureCodes
{
    public const string FactoryCode = "FACTORY-PILOT-01";
    public const string AreaCode = "AREA-PILOT-01";
    public const string LineCode = "LINE-PILOT-01";
    public const string ProductCode = "PILOT-PROD-01";
    public const string PlcConnectorName = "PILOT-PLC-SOURCE";
    public const string MesConnectorName = "PILOT-MES-SOURCE";
}

public static class RealFactoryConnectorHealthStates
{
    public const string Disconnected = "DISCONNECTED";
    public const string Connecting = "CONNECTING";
    public const string Connected = "CONNECTED";
    public const string Degraded = "DEGRADED";
    public const string Stale = "STALE";
    public const string Failed = "FAILED";
    public const string Paused = "PAUSED";
}

public static class RealFactoryMappingStatuses
{
    public const string Draft = "DRAFT";
    public const string Validated = "VALIDATED";
    public const string Active = "ACTIVE";
    public const string Retired = "RETIRED";
}

public static class RealFactoryDataQualityStates
{
    public const string Good = "GOOD";
    public const string Degraded = "DEGRADED";
    public const string Insufficient = "INSUFFICIENT";
    public const string Invalid = "INVALID";
}

public static class RealFactoryShadowModes
{
    public const string ObserveOnly = "OBSERVE_ONLY";
    public const string ShadowIntelligence = "SHADOW_INTELLIGENCE";
    public const string Recommendation = "RECOMMENDATION";
    public const string ControlledApproval = "CONTROLLED_APPROVAL";
    public const string LimitedAction = "LIMITED_ACTION";
    public const string VerifiedOperation = "VERIFIED_OPERATION";
}

public static class RealFactoryActionSafetyClasses
{
    public const string Informational = "CLASS_A";
    public const string ReversibleOperational = "CLASS_B";
    public const string ControlledProduction = "CLASS_C";
    public const string PhysicalProcessChange = "CLASS_D";
}

public static class RealFactoryPilotPhases
{
    public const string Connect = "CONNECT";
    public const string Discover = "DISCOVER";
    public const string Map = "MAP";
    public const string Validate = "VALIDATE";
    public const string Observe = "OBSERVE";
    public const string Shadow = "SHADOW";
    public const string Recommend = "RECOMMEND";
    public const string Approve = "APPROVE";
    public const string Act = "ACT";
    public const string ObserveResult = "OBSERVE_RESULT";
    public const string Verify = "VERIFY";
    public const string Learn = "LEARN";
    public const string Closed = "CLOSED";
}

public static class RealFactoryPilotAcceptanceKinds
{
    public const string Synthetic = "SYNTHETIC_PILOT_PASS";
    public const string RealFactory = "REAL_FACTORY_ACCEPTANCE";
}
