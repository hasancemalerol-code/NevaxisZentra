namespace NevaxisZentra.Persistence.ShiftOperations;

public sealed record ShiftClosingPreview(
    Guid ShiftInstanceId,
    IReadOnlyList<ShiftOpenItem> Candidates,
    IReadOnlyList<ShiftOpenItem> ExistingOpenItems,
    IReadOnlyList<ShiftHandoverConflict> Conflicts,
    decimal ReadinessScore,
    IReadOnlyList<string> NextShiftMustKnow,
    bool PreviewOnly);

public sealed record ShiftHandoverPackage(
    Guid ShiftInstanceId,
    Guid? FactoryHandoverId,
    string Summary,
    IReadOnlyList<ShiftOpenItem> Items,
    IReadOnlyList<string> NextShiftMustKnow,
    IReadOnlyList<ShiftHandoverConflict> Conflicts,
    decimal ReadinessScore,
    string? AiSummaryJson);

public sealed record DispositionOpenItemRequest(
    string Disposition,
    string? Reason,
    long ConcurrencyToken);

public sealed record AcceptHandoverRequest(
    Guid IncomingMembershipId,
    string? IncomingSupervisorLabel,
    string? IncomingAcknowledgement,
    long ShiftConcurrencyToken);

public sealed record ShiftMetricsSnapshot(
    int OpenItems,
    int CriticalItems,
    int CarriedOverItems,
    int DigitalWorkforceCases,
    int PendingApprovals,
    decimal HandoverReadinessScore,
    int VerifiedHandovers,
    int RepeatedCarryoverItems);
