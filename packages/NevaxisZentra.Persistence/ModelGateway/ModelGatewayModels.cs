using System.Text.Json.Serialization;

namespace NevaxisZentra.Persistence.ModelGateway;

/// <summary>One retrieved fact the model may use. The model must cite facts by <see cref="Id"/>.</summary>
public sealed record ModelGroundedFact(string Id, string Text);

/// <summary>
/// The user's question plus the bounded, tool-derived facts it must be answered from. Server-side only: it is never
/// bound from an API request body.
/// </summary>
public sealed record ModelGroundedInput(string Question, IReadOnlyList<ModelGroundedFact> Facts);

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
    decimal? CostBudgetRemaining = null,
    [property: JsonIgnore] ModelGroundedInput? Grounded = null);

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
    string? AuditRecordId,
    string? ProviderOutcome = null);

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
