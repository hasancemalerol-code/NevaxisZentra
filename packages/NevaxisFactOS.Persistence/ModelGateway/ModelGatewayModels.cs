namespace NevaxisFactOS.Persistence.ModelGateway;

public sealed record ModelGatewayContextReference(
    Guid? ContextSnapshotId,
    string? CanonicalStateSummary,
    Guid? DigitalWorkCaseId,
    string? ConstraintSummary,
    string? SimulationSummary,
    string? DataQualityState);

public sealed record ModelGatewayRequest(
    Guid TenantId,
    Guid FacilityId,
    Guid? WorkerId,
    Guid? DigitalWorkCaseId,
    string RequestClassification,
    string TaskType,
    ModelGatewayContextReference Context,
    int ActionRiskLevel,
    bool RequiresStructuredOutput,
    string? PreferredPath = null,
    decimal? CostBudgetRemaining = null);

public sealed record ModelGatewayRoutingDecision(
    string IntelligencePath,
    string ProviderId,
    string ModelName,
    string ModelVersion,
    string RoutingReason,
    bool UsedFallback);

public sealed record OperationalProposalAction(
    string ActionType,
    string TargetEntityType,
    string TargetEntityId,
    string Summary,
    IReadOnlyList<string> EvidenceRefs);

public sealed record OperationalProposal(
    string Hypothesis,
    IReadOnlyList<string> EvidenceRefs,
    IReadOnlyList<OperationalProposalAction> ProposedActions,
    string ExpectedOutcome,
    IReadOnlyList<string> Assumptions,
    decimal Confidence,
    IReadOnlyList<string> Risks,
    IReadOnlyList<string> RequiredApprovals);

public sealed record ModelGatewayProposalValidationResult(
    bool Accepted,
    string ValidationStatus,
    IReadOnlyList<string> RejectionReasons,
    OperationalProposal? SanitizedProposal);

public sealed record ModelGatewayResponse(
    bool Succeeded,
    string? FailureState,
    string? FailureCode,
    ModelGatewayRoutingDecision Routing,
    OperationalProposal? Proposal,
    ModelGatewayProposalValidationResult? Validation,
    long LatencyMs,
    decimal? EstimatedCost,
    string? AuditRecordId);

public sealed record ModelCostBudgetState(
    decimal DailyRemaining,
    decimal MonthlyRemaining,
    decimal CaseRemaining,
    bool IsLimitReached);

public sealed record ModelGatewayAuditSummary(
    Guid Id,
    string ProviderId,
    string ModelName,
    string IntelligencePath,
    string RequestClassification,
    string RoutingReason,
    bool UsedFallback,
    string ValidationStatus,
    long LatencyMs,
    decimal EstimatedCost,
    Guid? ContextSnapshotId,
    Guid? DigitalWorkCaseId,
    DateTimeOffset CreatedAtUtc);
