using NevaxisFactOS.Persistence.DigitalWorkforce;
using NevaxisFactOS.Persistence.FactoryOrchestration;

namespace NevaxisFactOS.Persistence.FactoryOutcomeExecution;

public sealed record FactoryEventQualificationResult(
    bool IsQualified,
    bool ShouldEnrichExisting,
    string? RejectionReason);

public sealed record FactoryOutcomeImpactSnapshot(
    string OutcomeAtRisk,
    int? QuantityAtRisk,
    int? ProjectedGap,
    string? OrderReference,
    string? MachineCode,
    string? MaterialCode,
    string? TimeToBreachLabel,
    decimal Confidence,
    string? FinancialImpactTry,
    IReadOnlyList<FactoryImpactNode> DomainImpacts);

public sealed record FactoryOutcomeRecoveryStep(
    int Order,
    string ActionKey,
    string Title,
    string WorkerCode,
    string WorkerName,
    IReadOnlyList<string> DependsOnActionKeys,
    bool RequiresApproval,
    string VerificationCriterion,
    string? LinkedCaseId = null);

public sealed record FactoryOutcomeRecoveryPlanOption(
    string PlanKey,
    string Strategy,
    string Summary,
    int? ExpectedRecoveredQuantity,
    string? ProjectedCompletionLabel,
    string OperationalRisk,
    IReadOnlyList<string> RequiredApprovals,
    IReadOnlyList<FactoryOutcomeRecoveryStep> Steps);

public sealed record FactoryOutcomeRecoveryPlanDocument(
    string Objective,
    string SelectedPlanKey,
    IReadOnlyList<FactoryOutcomeRecoveryPlanOption> Plans,
    IReadOnlyList<string> VerificationCriteria);

public sealed record FactoryOutcomeVerificationCriterion(
    string Code,
    string Label,
    string ExpectedValue,
    string? MeasuredValue,
    string? Threshold,
    string Result,
    string? EvidenceSummary,
    DateTimeOffset? RecordedAtUtc);

public sealed record FactoryOutcomeVerificationSnapshot(
    bool Passed,
    IReadOnlyList<FactoryOutcomeVerificationCriterion> Criteria,
    string Summary);

public sealed record FactoryOutcomeExecutionProgress(
    Guid RootCaseId,
    int TotalActions,
    int CompletedActions,
    int FailedActions,
    int BlockedActions,
    string? CurrentBlockerCode,
    bool ReadyForVerification);

public sealed record FactoryOutcomeSimilarCase(
    Guid LearningRecordId,
    string ProblemPattern,
    string SelectedStrategyKey,
    int? ActualRecoveredQuantity,
    string VerificationResult,
    DateTimeOffset VerifiedAtUtc);

public sealed record FactoryOutcomeRouteContext(
    FactoryDomainEvent EventData,
    string SubjectId,
    DigitalWorkCase? ExistingCase,
    string CorrelationKey,
    bool IsDuplicateEvent);
