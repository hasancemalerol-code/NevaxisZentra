namespace NevaxisZentra.Persistence.IntelligenceRuntime;

public static class IntelligenceRuntimePermissionKeys
{
    public const string Read = "intelligence-runtime.read";
    public const string Manage = "intelligence-runtime.manage";
    public const string Evaluate = "intelligence-runtime.evaluate";
}

public static class IntelligenceTaskTypes
{
    public const string StateLookup = "STATE_LOOKUP";
    public const string ThresholdEvaluation = "THRESHOLD_EVALUATION";
    public const string TrendAnalysis = "TREND_ANALYSIS";
    public const string AnomalyAnalysis = "ANOMALY_ANALYSIS";
    public const string Classification = "CLASSIFICATION";
    public const string Extraction = "EXTRACTION";
    public const string Summarization = "SUMMARIZATION";
    public const string CaseSimilarity = "CASE_SIMILARITY";
    public const string RootCauseCandidate = "ROOT_CAUSE_CANDIDATE";
    public const string CrossDomainDiagnosis = "CROSS_DOMAIN_DIAGNOSIS";
    public const string RecoveryPlanDraft = "RECOVERY_PLAN_DRAFT";
    public const string ScheduleExplanation = "SCHEDULE_EXPLANATION";
    public const string ScenarioExplanation = "SCENARIO_EXPLANATION";
    public const string ExecutiveSynthesis = "EXECUTIVE_SYNTHESIS";
    public const string WorkExplanation = "WORK_EXPLANATION";
    public const string VerificationExplanation = "VERIFICATION_EXPLANATION";
    public const string FactoryCopilotSynthesis = "FACTORY_COPILOT_SYNTHESIS";
}

public static class IntelligenceRouteTypes
{
    public const string Deterministic = "DETERMINISTIC";
    public const string Analytics = "ANALYTICS";
    public const string Solver = "SOLVER";
    public const string Retrieval = "RETRIEVAL";
    public const string Simulation = "SIMULATION";
    public const string NevaxisModel = "NEVAXIS_MODEL";
    public const string FrontierModel = "FRONTIER_MODEL";
}

public static class IntelligenceTenantLearningPolicies
{
    public const string NoModelTraining = "NO_MODEL_TRAINING";
    public const string TenantOnlyLearning = "TENANT_ONLY_LEARNING";
    public const string AnonymizedSharedLearning = "ANONYMIZED_SHARED_LEARNING";
    public const string GlobalEligible = "GLOBAL_ELIGIBLE";
}

public static class NevaxisModelStatuses
{
    public const string Candidate = "CANDIDATE";
    public const string Evaluating = "EVALUATING";
    public const string Rejected = "REJECTED";
    public const string Shadow = "SHADOW";
    public const string Limited = "LIMITED";
    public const string Production = "PRODUCTION";
    public const string Retired = "RETIRED";
}

public static class NevaxisModelTrainingJobStatuses
{
    public const string Created = "CREATED";
    public const string DatasetPreparing = "DATASET_PREPARING";
    public const string Ready = "READY";
    public const string Training = "TRAINING";
    public const string Validating = "VALIDATING";
    public const string Failed = "FAILED";
    public const string Completed = "COMPLETED";
    public const string Rejected = "REJECTED";
    public const string PromotionPending = "PROMOTION_PENDING";
}

public static class NevaxisDatasetReadinessStates
{
    public const string NotReady = "NOT_READY";
    public const string LimitedReady = "LIMITED_READY";
    public const string ReadyForNarrowTask = "READY_FOR_NARROW_TASK";
    public const string ReadyForTraining = "READY_FOR_TRAINING";
}

public static class IntelligenceRuntimeFailureStates
{
    public const string ProviderUnavailable = "PROVIDER_UNAVAILABLE";
    public const string ModelDisabled = "MODEL_DISABLED";
    public const string BudgetExceeded = "BUDGET_EXCEEDED";
    public const string SchemaFailure = "SCHEMA_FAILURE";
    public const string CandidateRejected = "CANDIDATE_REJECTED";
    public const string TrainingDataNotReady = "TRAINING_DATA_NOT_READY";
    public const string EvaluationFailed = "EVALUATION_FAILED";
    public const string ShadowRegression = "SHADOW_REGRESSION";
    public const string PromotionBlocked = "PROMOTION_BLOCKED";
    public const string RollbackTriggered = "ROLLBACK_TRIGGERED";
}

public static class IntelligenceRuntimeEngineVersions
{
    public const string Controller = "intelligence-runtime-controller-v1";
    public const string Taxonomy = "intelligence-task-taxonomy-v1";
    public const string Dataset = "nevaxis-verified-dataset-v1";
    public const string Evaluation = "nevaxis-model-evaluation-v1";
}

public static class IntelligenceRuntimeCorrelationPrefixes
{
    public const string RouteRecord = "ir-route";
}
