namespace NevaxisFactOS.Persistence.ModelGateway;

public static class ModelGatewayPermissionKeys
{
    public const string Read = "model-gateway.read";
    public const string Route = "model-gateway.route";
}

public static class ModelGatewayIntelligencePaths
{
    public const string DeterministicEngine = "DETERMINISTIC_ENGINE";
    public const string LocalModel = "LOCAL_MODEL";
    public const string FrontierModel = "FRONTIER_MODEL";
}

public static class ModelGatewayRequestClassifications
{
    public const string ThroughputCalculation = "throughput-calculation";
    public const string TaktCalculation = "takt-calculation";
    public const string ConstraintDetection = "constraint-detection";
    public const string LossTree = "loss-tree";
    public const string ScenarioSimulation = "scenario-simulation";
    public const string PolicyEvaluation = "policy-evaluation";
    public const string SafetyGuard = "safety-guard";
    public const string VerificationMath = "verification-math";
    public const string OperationalExplanation = "operational-explanation";
    public const string HypothesisGeneration = "hypothesis-generation";
    public const string RecoveryPlanReasoning = "recovery-plan-reasoning";
    public const string CrossModuleDiagnosis = "cross-module-diagnosis";
}

public static class ModelGatewayFailureStates
{
    public const string ProviderUnavailable = "PROVIDER_UNAVAILABLE";
    public const string Timeout = "TIMEOUT";
    public const string RateLimit = "RATE_LIMIT";
    public const string InvalidOutput = "INVALID_OUTPUT";
    public const string SchemaFailure = "SCHEMA_FAILURE";
    public const string PolicyRejected = "POLICY_REJECTED";
    public const string InsufficientContext = "INSUFFICIENT_CONTEXT";
}

public static class ModelGatewayFailureCodes
{
    public const string NotFound = "model-gateway.not-found";
    public const string ValidationFailed = "model-gateway.validation-failed";
    public const string ProviderUnavailable = "model-gateway.provider-unavailable";
    public const string SchemaRejected = "model-gateway.schema-rejected";
    public const string PolicyRejected = "model-gateway.policy-rejected";
    public const string BudgetExceeded = "model-gateway.budget-exceeded";
    public const string InsufficientContext = "model-gateway.insufficient-context";
}

public static class ModelGatewayProviderIds
{
    public const string DeterministicEngine = "nevaxis-deterministic-engine";
    public const string LocalModel = "nevaxis-local-model";
    public const string FrontierModel = "nevaxis-frontier-model";
}

public static class ModelGatewayEngineVersions
{
    public const string Router = "model-gateway-router-v1";
    public const string ProposalValidator = "model-gateway-proposal-validator-v1";
}

public static class ModelGatewayAllowedActionTypes
{
    public static readonly HashSet<string> All = new(StringComparer.OrdinalIgnoreCase)
    {
        "OPERATOR_TASK",
        "MAINTENANCE_ACTION",
        "MATERIAL_REPLENISHMENT",
        "BUFFER_ADJUSTMENT_RECOMMENDATION",
        "OPERATION_REDISTRIBUTION",
        "RESOURCE_REASSIGNMENT",
        "CONTROLLED_PROCESS_RECOMMENDATION",
        "QUALITY_CHECK",
        "OBSERVATION_WINDOW",
        "READ_ONLY_ANALYSIS",
    };
}
