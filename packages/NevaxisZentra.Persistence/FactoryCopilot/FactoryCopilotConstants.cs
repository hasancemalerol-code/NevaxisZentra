namespace NevaxisZentra.Persistence.FactoryCopilot;

public static class FactoryCopilotPermissionKeys
{
    public const string Read = "factory-copilot.read";
    public const string Query = "factory-copilot.query";
}

public static class FactoryCopilotEntryPoints
{
    public const string Bugun = "BUGUN";
    public const string Fabrika = "FABRIKA";
    public const string Plan = "PLAN";
    public const string Isler = "ISLER";
    public const string Scenario = "SCENARIO";
    public const string Machine = "MACHINE";
    public const string Line = "LINE";
    public const string Order = "ORDER";
}

public static class FactoryCopilotIntents
{
    public const string CurrentState = "CURRENT_STATE";
    public const string CauseAnalysis = "CAUSE_ANALYSIS";
    public const string TrendComparison = "TREND_COMPARISON";
    public const string ConstraintAnalysis = "CONSTRAINT_ANALYSIS";
    public const string OrderRisk = "ORDER_RISK";
    public const string ScheduleExplanation = "SCHEDULE_EXPLANATION";
    public const string ScenarioExplanation = "SCENARIO_EXPLANATION";
    public const string WorkStatus = "WORK_STATUS";
    public const string VerificationStatus = "VERIFICATION_STATUS";
    public const string SimilarCases = "SIMILAR_CASES";
    public const string KnowledgeLookup = "KNOWLEDGE_LOOKUP";
    public const string MaintenanceRisk = "MAINTENANCE_RISK";
    public const string QualityRisk = "QUALITY_RISK";
    public const string MaterialRisk = "MATERIAL_RISK";
    public const string ShiftSummary = "SHIFT_SUMMARY";
    public const string RecoveryOptions = "RECOVERY_OPTIONS";
    public const string ExecutiveSummary = "EXECUTIVE_SUMMARY";
    public const string OptimizationExplanation = "OPTIMIZATION_EXPLANATION";
    public const string Unsupported = "UNSUPPORTED";
}

public static class FactoryCopilotRoutingClasses
{
    public const string Deterministic = "DETERMINISTIC";
    public const string Analytic = "ANALYTIC";
    public const string Retrieval = "RETRIEVAL";
    public const string Simulation = "SIMULATION";
    public const string Knowledge = "KNOWLEDGE";
    public const string ModelRequired = "MODEL_REQUIRED";
    public const string Unsupported = "UNSUPPORTED";
}

public static class FactoryCopilotToolAccessClasses
{
    public const string Read = "READ";
    public const string Analytic = "ANALYTIC";
    public const string Simulation = "SIMULATION";
}

public static class FactoryCopilotContextQualityStates
{
    public const string Sufficient = "SUFFICIENT";
    public const string Stale = "STALE";
    public const string Conflicted = "CONFLICTED";
    public const string Insufficient = "INSUFFICIENT";
    public const string SourceUnavailable = "SOURCE_UNAVAILABLE";
}

public static class FactoryCopilotFailureCodes
{
    public const string ValidationFailed = "factory-copilot.validation-failed";
    public const string UnsupportedIntent = "factory-copilot.unsupported-intent";
    public const string InsufficientContext = "factory-copilot.insufficient-context";
    public const string ToolUnauthorized = "factory-copilot.tool-unauthorized";
    public const string ProviderUnavailable = "factory-copilot.provider-unavailable";
    public const string SchemaRejected = "factory-copilot.schema-rejected";
}

public static class FactoryCopilotToolIds
{
    public const string GetLineState = "GetLineState";
    public const string GetThroughputOverview = "GetThroughputOverview";
    public const string GetCurrentConstraint = "GetCurrentConstraint";
    public const string GetLossTree = "GetLossTree";
    public const string GetScenarioComparison = "GetScenarioComparison";
    public const string GetWorkDetail = "GetWorkDetail";
    public const string GetVerifiedKnowledge = "GetVerifiedKnowledge";
    public const string GetRecoveryOptions = "GetRecoveryOptions";
    public const string RunScenarioSimulation = "RunScenarioSimulation";
    public const string GetVerificationResult = "GetVerificationResult";
    public const string GetShiftContext = "GetShiftContext";
    public const string GetExecutiveSignals = "GetExecutiveSignals";
    public const string GetOptimizationRun = "GetOptimizationRun";
}
