using NevaxisZentra.Persistence.DigitalWorkforce;

namespace NevaxisZentra.Persistence.FactoryOrchestration;

public sealed record FactoryDomainEvent(
    string EventType,
    string Title,
    string Description,
    string Severity,
    string SourceClassification,
    Guid FacilityId,
    string SourceType,
    string SourceId,
    string? MachineId,
    string? OrderId,
    string? MoldId,
    string? MaterialId,
    string? MetadataJson);

public sealed record FactoryEventRouteResult(
    DigitalWorkCase Case,
    bool CreatedNew,
    bool EnrichedExisting,
    string CorrelationKey,
    IReadOnlyList<string> ParticipatingDomains);

public sealed record FactoryCaseBlocker(
    string Code,
    string Message,
    string? OwnerType,
    string? OwnerId,
    DateTimeOffset? SinceUtc,
    bool IsStale);

public sealed record FactoryRecoveryPlan(
    string Objective,
    IReadOnlyList<string> Actions,
    IReadOnlyList<string> Dependencies,
    IReadOnlyList<string> VerificationCriteria,
    string? Fallback);

public sealed record FactoryCaseDetailSnapshot(
    DigitalWorkCase RootCase,
    IReadOnlyList<DigitalWorkCase> ChildCases,
    IReadOnlyList<FactoryCaseDependency> Dependencies,
    IReadOnlyList<FactoryCaseBlocker> Blockers,
    FactoryRecoveryPlan RecoveryPlan,
    IReadOnlyList<DigitalWorkActivity> Timeline,
    string? NextAction,
    string? BlockerExplanation,
    bool HasDeadlock,
    bool HasConflict);

public sealed record VerifyFactoryOutcomeRequest(
    bool MachineOperational,
    bool MaintenanceComplete,
    bool QualityAcceptable,
    bool ProductionResumed,
    string? Notes,
    long ConcurrencyToken);

public sealed record ReopenFactoryCaseRequest(string Reason, long ConcurrencyToken);
