namespace NevaxisZentra.Persistence.FactoryCopilot;

public sealed record FactoryCopilotContextInput(
    string EntryPoint,
    Guid? LineId = null,
    string? LineCode = null,
    Guid? ResourceId = null,
    string? ResourceCode = null,
    string? OrderReference = null,
    Guid? DigitalWorkCaseId = null,
    Guid? ScenarioRunId = null,
    string? SearchQuery = null);

public sealed record FactoryCopilotQueryRequest(
    string QueryText,
    FactoryCopilotContextInput Context,
    Guid? SessionId = null);

public sealed record FactoryCopilotEvidenceRef(
    string Kind,
    string ReferenceId,
    string Label,
    string? Detail = null);

public sealed record FactoryCopilotActionLink(
    string Label,
    string Href,
    string LinkType);

public sealed record FactoryCopilotStructuredResponse(
    string Answer,
    string? WhatUsed,
    IReadOnlyList<string> KeyFacts,
    IReadOnlyList<string> Causes,
    string? Impact,
    string? RecommendedNextStep,
    string? Uncertainty,
    IReadOnlyList<FactoryCopilotEvidenceRef> Evidence,
    IReadOnlyList<FactoryCopilotActionLink> RelatedActions);

public sealed record FactoryCopilotToolInvocation(
    string ToolId,
    string AccessClass,
    bool Succeeded,
    string Summary,
    long LatencyMs);

public sealed record FactoryCopilotProvenance(
    Guid? ContextSnapshotId,
    IReadOnlyList<FactoryCopilotToolInvocation> Tools,
    IReadOnlyList<string> RecordsReferenced,
    string? ProviderId,
    string? ModelName,
    bool ModelInvoked,
    string RoutingClass,
    string RoutingReason,
    string ContextQualityState,
    long TotalLatencyMs);

public sealed record FactoryCopilotQueryResult(
    string Intent,
    string RoutingClass,
    bool ModelInvoked,
    string? FailureState,
    FactoryCopilotStructuredResponse Response,
    FactoryCopilotProvenance Provenance,
    IReadOnlyList<string> SuggestedQuestions,
    FactoryCopilotContextHeader ContextHeader,
    Guid QueryId,
    DateTimeOffset CreatedAtUtc);

public sealed record FactoryCopilotContextHeader(
    string EntryPoint,
    string? FactoryLabel,
    string? LineLabel,
    string? ResourceLabel,
    string? OrderLabel,
    string? WorkLabel,
    string? ScenarioLabel);

public sealed record FactoryCopilotSuggestedQuestionsRequest(
    FactoryCopilotContextInput Context);

public sealed record FactoryCopilotSuggestedQuestionsResult(
    IReadOnlyList<string> Questions,
    FactoryCopilotContextHeader ContextHeader);

public sealed record FactoryCopilotSessionContext(
    Guid SessionId,
    FactoryCopilotContextInput Context,
    DateTimeOffset UpdatedAtUtc);
