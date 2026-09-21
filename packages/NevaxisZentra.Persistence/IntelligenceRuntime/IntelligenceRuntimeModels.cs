namespace NevaxisZentra.Persistence.IntelligenceRuntime;

public sealed record IntelligenceTaskDefinition(
    string TaskType,
    string PreferredRoute,
    string FallbackRoute,
    string RiskLevel,
    bool StructuredOutputRequired,
    bool NevaxisModelEligible,
    bool FrontierRequired,
    string CostSensitivity);

public sealed record IntelligenceRoutingDecision(
    string RouteType,
    string IntelligencePath,
    string ProviderId,
    string ModelName,
    string ModelVersion,
    string RoutingReason,
    string WhyModelCalledOrSkipped,
    bool ModelInvoked,
    bool UsedFallback);

public sealed record IntelligenceBudgetPolicy(
    int MaxModelCalls,
    int MaxContextTokens,
    int MaxOutputTokens,
    decimal MaxEstimatedCost,
    int MaxRetries,
    int MaxSimulationCount);

public sealed record IntelligenceRouteRequest(
    string TaskType,
    string RequestClassification,
    string? PreferredPath,
    int ActionRiskLevel,
    bool RequiresStructuredOutput,
    string? ContextQualityState,
    decimal? CostBudgetRemaining,
    Guid? WorkerId,
    Guid? DigitalWorkCaseId,
    Guid? OptimizationRunId,
    Guid? ScenarioRunId,
    Guid? ContextSnapshotId);

public sealed record IntelligenceRouteResult(
    IntelligenceRoutingDecision Routing,
    string? FailureState,
    bool BudgetExceeded,
    long LatencyMs);

public sealed record IntelligenceDependencyMetrics(
    decimal DeterministicRouteRate,
    decimal AnalyticsRouteRate,
    decimal SolverRouteRate,
    decimal RetrievalRouteRate,
    decimal NevaxisModelCallRate,
    decimal FrontierModelCallRate,
    decimal FrontierFallbackRate,
    decimal FrontierCostPerCase,
    decimal? FrontierCostPerVerifiedOutcome,
    int TotalRequests,
    int ModelInvocations,
    decimal TotalEstimatedCost);

public sealed record VerifiedOutcomeCostSnapshot(
    Guid? DigitalWorkCaseId,
    Guid? OptimizationRunId,
    Guid? ScenarioRunId,
    string Outcome,
    decimal TotalIntelligenceCost,
    int ModelCallCount,
    int DeterministicRouteCount,
    decimal? CostPerVerifiedOutcome,
    DateTimeOffset? VerifiedAtUtc);

public sealed record IntelligenceRuntimeControlCenter(
    IntelligenceDependencyMetrics Metrics,
    IReadOnlyList<IntelligenceRouteSummary> RecentRoutes,
    IReadOnlyList<NevaxisModelRegistrySummary> Models,
    IReadOnlyList<NevaxisDatasetVersionSummary> DatasetVersions,
    IReadOnlyList<NevaxisModelEvaluationSummary> Evaluations,
    IReadOnlyList<VerifiedOutcomeCostSnapshot> VerifiedOutcomeCosts,
    DateTimeOffset GeneratedAtUtc);

public sealed record IntelligenceRouteSummary(
    Guid RouteId,
    string TaskType,
    string RouteType,
    string RoutingReason,
    string WhyModelCalledOrSkipped,
    bool ModelInvoked,
    decimal EstimatedCost,
    long LatencyMs,
    DateTimeOffset CreatedAtUtc);

public sealed record ProviderPriceSnapshot(
    string ProviderId,
    string ModelName,
    decimal InputPricePer1K,
    decimal OutputPricePer1K,
    string Currency,
    string PriceVersion,
    DateTimeOffset EffectiveFromUtc);

public sealed record NevaxisVerifiedDatasetEntry(
    Guid SourceCaseId,
    Guid? VerifiedImprovementId,
    string Problem,
    string ContextSummary,
    string ConstraintSummary,
    string RootCause,
    string SelectedDecision,
    string ActionSummary,
    string VerifiedOutcome,
    string ApplicabilityConstraints,
    string Provenance,
    string SanitizationVersion,
    bool NegativeLearning);

public sealed record NevaxisDatasetVersionSummary(
    Guid DatasetVersionId,
    string VersionCode,
    string SchemaVersion,
    string ReadinessState,
    int IncludedCaseCount,
    int ExcludedCaseCount,
    string TenantPolicy,
    DateTimeOffset CreatedAtUtc);

public sealed record NevaxisDatasetBuildResult(
    Guid DatasetVersionId,
    string VersionCode,
    string ReadinessState,
    int EligibleCount,
    int ExcludedCount,
    int DerivedRecordCount,
    string Message);

public sealed record NevaxisModelRegistrySummary(
    Guid ModelId,
    string ModelCode,
    string Version,
    string BaseModel,
    string Status,
    string EvaluationStatus,
    string SafetyStatus,
    IReadOnlyList<string> TaskCapabilities,
    string? TrainingDatasetVersion,
    DateTimeOffset UpdatedAtUtc);

public sealed record NevaxisModelEvaluationSummary(
    Guid EvaluationRunId,
    Guid ModelId,
    string ModelCode,
    string EvaluationMode,
    string Outcome,
    decimal StructuredOutputValidityRate,
    decimal SafetyPassRate,
    decimal HallucinatedEntityRate,
    decimal EstimatedCost,
    long LatencyMs,
    DateTimeOffset CreatedAtUtc);

public sealed record NevaxisModelPromotionSimulationResult(
    Guid ModelId,
    string FinalStatus,
    string Outcome,
    bool RollbackTriggered,
    string Message);

public sealed record IntelligenceGoldenRoutingResult(
    IReadOnlyList<IntelligenceGoldenRoutingQueryResult> Queries,
    string Message);

public sealed record IntelligenceGoldenRoutingQueryResult(
    string QueryLabel,
    string TaskType,
    string ExpectedRoute,
    string ActualRoute,
    int ModelCalls,
    bool Passed);

public sealed record IntelligenceGoldenDatasetResult(
    Guid DatasetVersionId,
    Guid EvaluationRunId,
    Guid ShadowRunId,
    string PromotionSimulationOutcome,
    string Message);

public sealed record TenantIntelligenceLearningPolicySnapshot(
    Guid PolicyId,
    string LearningPolicy,
    bool AllowExport,
    bool AllowTraining,
    bool AllowShadow,
    DateTimeOffset UpdatedAtUtc);

public sealed record UpsertTenantIntelligenceLearningPolicyRequest(
    string LearningPolicy,
    bool AllowExport,
    bool AllowTraining,
    bool AllowShadow);

public sealed record RegisterNevaxisModelRequest(
    string ModelCode,
    string Version,
    string BaseModel,
    IReadOnlyList<string> TaskCapabilities,
    string DeploymentType,
    int ContextLimit);

public sealed record StartModelEvaluationRequest(
    Guid ModelId,
    string EvaluationMode,
    bool IncludeSafetyBenchmark);

public sealed record StartModelShadowRequest(
    Guid ModelId,
    string TaskType);

public sealed record PromoteModelRequest(
    Guid ModelId,
    string TargetStatus,
    string? ApprovalNote);

public sealed record OpenAiLiveValidationResult(
    string Status,
    bool ProviderConfigured,
    bool LiveCallSucceeded,
    string? ModelName,
    int? InputTokens,
    int? OutputTokens,
    decimal? EstimatedCost,
    string Message);
