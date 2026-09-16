namespace NevaxisFactOS.Persistence.ContextKnowledge;

public sealed record ContextScope(
    Guid TenantId,
    Guid FacilityId,
    string EntityType,
    string EntityId,
    Guid? LineId = null,
    Guid? StationId = null,
    Guid? DigitalWorkCaseId = null,
    DateTimeOffset? WindowStartUtc = null,
    DateTimeOffset? WindowEndUtc = null);

public sealed record ContextBudget(
    int MaxEventCount = 200,
    int MaxHistoricalCases = 20,
    int MaxKnowledgeRecords = 15,
    int MaxTimeWindowHours = 24,
    int MaxPayloadBytes = 512_000,
    int? MaxTokenBudget = null);

public sealed record ContextSourceClaim(
    string SourceType,
    string SourceRecordId,
    string StateValue,
    DateTimeOffset ObservedAtUtc,
    DateTimeOffset? IngestedAtUtc,
    decimal Reliability,
    bool IsQuarantined = false);

public sealed record CanonicalOperationalStateResult(
    string EntityType,
    string EntityId,
    string ResolutionStatus,
    string? ResolvedStateValue,
    decimal Confidence,
    IReadOnlyList<ContextSourceClaim> SourceClaims,
    IReadOnlyList<string> ConflictReasons,
    DateTimeOffset EvaluatedAtUtc);

public sealed record ContextDataQualityResult(
    bool Accepted,
    string ReasonCode,
    string? Detail);

public sealed record ContextProvenance(
    string SourceType,
    string SourceRecordId,
    Guid TenantId,
    string EntityType,
    string EntityId,
    DateTimeOffset TimestampUtc,
    DateTimeOffset? IngestedAtUtc,
    decimal FreshnessScore,
    decimal Reliability,
    string VerificationStatus,
    string DataTier);

public sealed record ContextItem(
    string ItemKind,
    string ReferenceId,
    string Summary,
    ContextProvenance Provenance,
    int PriorityRank,
    string DataTier);

public sealed record ContextBuildRequest(
    ContextScope Scope,
    ContextBudget Budget,
    Guid? DigitalWorkCaseId = null,
    bool IncludeVerifiedKnowledge = true,
    bool IncludeRelatedCases = true);

public sealed record ContextBuildMetrics(
    long BuildDurationMs,
    int RecordsConsidered,
    int RecordsRejected,
    int DuplicateCount,
    int StaleCount,
    int ConflictCount,
    int RetrievedKnowledgeCount,
    int FinalItemCount,
    int FinalPayloadBytes,
    string? FailureState);

public sealed record WorkerContextPackage(
    ContextScope Scope,
    CanonicalOperationalStateResult? CanonicalState,
    IReadOnlyList<ContextItem> Items,
    IReadOnlyList<FactoryKnowledgeRecordSummary> VerifiedKnowledge,
    ContextBuildMetrics Metrics,
    string? FailureState,
    DateTimeOffset BuiltAtUtc);

public sealed record FactoryKnowledgeRecordSummary(
    Guid Id,
    Guid? SourceCaseId,
    string ProblemSymptom,
    string RootCause,
    string DecisionSummary,
    string VerifiedOutcome,
    decimal Confidence,
    string PromotionStatus);

public sealed record KnowledgePromotionRequest(
    Guid SourceCaseId,
    string ProblemSymptom,
    string RootCause,
    string DecisionSummary,
    IReadOnlyList<string> Actions,
    IReadOnlyList<string> EvidenceRefs,
    string BeforeStateJson,
    string AfterStateJson,
    string VerifiedOutcome,
    bool HasVerifiedOutcome,
    bool HasSufficientEvidence,
    bool HasUnresolvedConflict,
    bool ObservationCompleted,
    decimal Confidence,
    string ApplicabilityConstraintsJson);

public sealed record KnowledgePromotionResult(
    bool Promoted,
    string PromotionStatus,
    Guid? KnowledgeRecordId,
    IReadOnlyList<string> RejectionReasons);

public sealed record ContextSnapshotRequest(
    ContextScope Scope,
    WorkerContextPackage ContextPackage,
    Guid? DigitalWorkCaseId,
    string CreatedBySubjectId,
    string? SnapshotCode = null);

public sealed record ContextSnapshotSummary(
    Guid Id,
    string SnapshotCode,
    Guid? DigitalWorkCaseId,
    DateTimeOffset CreatedAtUtc,
    int ItemCount,
    string MaterializedContextJson,
    string ProvenanceJson,
    string MetricsJson);

public sealed record HybridRetrievalQuery(
    ContextScope Scope,
    string? CorrelationKey = null,
    IReadOnlyList<string>? EntityTypes = null,
    IReadOnlyList<string>? EventTypes = null,
    int MaxResults = 100);

public sealed record DataLifecyclePolicyDefinition(
    string DataType,
    int HotRetentionHours,
    int WarmRetentionDays,
    int ColdArchiveDays,
    bool IsActive = true);

public sealed record TelemetryDownsampleInput(
    string ResourceId,
    string SignalType,
    decimal Value,
    DateTimeOffset ObservedAtUtc,
    bool PreserveRawWindow = false);

public sealed record TelemetryAggregateBucket(
    string Granularity,
    DateTimeOffset WindowStartUtc,
    DateTimeOffset WindowEndUtc,
    decimal AggregateValue,
    int SampleCount,
    bool PreserveRawWindow);
