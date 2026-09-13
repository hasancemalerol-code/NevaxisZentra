using NevaxisFactOS.Persistence.ProductionManager;
using NevaxisFactOS.Persistence.ProductionPlanning;

namespace NevaxisFactOS.Persistence.FactoryNetworkOptimization;

public sealed record FactoryNetworkWorkspaceSnapshot(
    FactoryNetwork Network,
    IReadOnlyList<FactoryNetworkFacility> Facilities,
    IReadOnlyList<FacilityCapabilityProfile> CapabilityProfiles,
    IReadOnlyList<NetworkFacilityStateRow> FacilityStates,
    IReadOnlyList<NetworkCapacityRow> CapacityMap,
    IReadOnlyList<NetworkDemandRow> Demand,
    IReadOnlyList<NetworkOpportunityRow> Opportunities,
    IReadOnlyList<NetworkScenario> Scenarios,
    IReadOnlyList<NetworkAllocation> ActiveAllocations,
    IReadOnlyList<NetworkActivityEvent> ActivityFeed,
    FactoryNetworkPolicy Policy,
    NetworkMetrics Metrics,
    NetworkResilienceSummary Resilience,
    DateTimeOffset DataFreshnessUtc);

public sealed record NetworkFacilityStateRow(
    Guid FacilityId,
    string FacilityName,
    string Timezone,
    decimal LoadPercent,
    decimal AvailableCapacityPercent,
    int OrdersAtRisk,
    int ActiveCases,
    string PlanningState,
    DateTimeOffset FreshnessUtc);

public sealed record NetworkCapacityRow(
    Guid FacilityId,
    string FacilityName,
    decimal AvailablePercent,
    decimal CommittedPercent,
    decimal AtRiskPercent,
    decimal UnusedPercent,
    decimal OverloadedPercent,
    bool Realizable);

public sealed record NetworkDemandRow(
    string OrderId,
    Guid AssignedFacilityId,
    decimal RemainingQuantity,
    DateTimeOffset DeadlineUtc,
    string RiskLevel,
    string RequiredCapability,
    bool MaterialReady,
    bool MoldReady);

public sealed record NetworkOpportunityRow(
    string OpportunityCode,
    string Title,
    string Summary,
    Guid SourceFacilityId,
    Guid DestinationFacilityId,
    string OrderId,
    decimal ExpectedBenefitScore,
    string DataClassification);

public sealed record NetworkMetrics(
    decimal NetworkUtilizationPercent,
    int OrdersAtRisk,
    int OrdersRecovered,
    decimal CapacityImbalancePercent,
    int ActiveCrossPlantDecisions,
    decimal NetworkPlanAdherencePercent);

public sealed record NetworkResilienceSummary(
    decimal CapacityRedundancyScore,
    decimal ProcessRedundancyScore,
    decimal MoldRedundancyScore,
    IReadOnlyList<string> SinglePointsOfFailure,
    IReadOnlyList<string> Bottlenecks);

public sealed record SimulateNetworkRequest(
    Guid NetworkId,
    Guid SourceFacilityId,
    Guid DestinationFacilityId,
    string OrderId,
    decimal Quantity,
    string ActionType,
    int MaxScenarios);

public sealed record EvaluateNetworkAllocationRequest(
    Guid NetworkId,
    string TriggerType,
    string TriggerContextJson);

public sealed record ExecuteNetworkAllocationRequest(
    Guid AllocationId,
    long ConcurrencyToken,
    bool OverrideStaleGuard);

public sealed record ApproveNetworkAllocationRequest(
    Guid AllocationId,
    long ConcurrencyToken,
    bool Approved,
    string? Notes);

public sealed record NetworkSimulationResult(
    NetworkScenario Scenario,
    FeasibilityEvaluation Feasibility,
    string ComparisonJson,
    string RecommendationExplanation);

public sealed record FeasibilityEvaluation(
    bool IsFeasible,
    IReadOnlyList<string> HardViolations,
    IReadOnlyList<string> SoftWarnings,
    string? Explanation);

public sealed record NetworkAllocationResult(
    NetworkAllocation Allocation,
    IReadOnlyList<NetworkAllocationItem> Items,
    bool ApprovalRequired,
    bool AutoExecuted);

public sealed record NetworkOutcomeVerificationResult(
    Guid AllocationId,
    string OutcomeStatus,
    string ExpectedImpactJson,
    string ActualImpactJson,
    decimal QuantityConservationPercent,
    Guid? LearningMemoryId);

public sealed record UpdateFactoryNetworkPolicyRequest(
    long ConcurrencyToken,
    string? AutonomyMode,
    string? PolicyEnvelopeJson);

public sealed record FacilityComparisonRow(
    Guid FacilityId,
    string FacilityName,
    bool Feasible,
    decimal CapacityScore,
    decimal DeadlineScore,
    decimal QualityScore,
    decimal MaterialScore,
    decimal MaintenanceScore,
    decimal RiskScore,
    decimal ConfidenceScore,
    string RecommendationReason);

public sealed record NetworkFacilityContext(
    Guid FacilityId,
    string FacilityName,
    string Timezone,
    FactoryStateSnapshot State,
    FacilityCapabilityProfile Profile,
    decimal LoadPercent,
    decimal AvailablePercent,
    DateTimeOffset FreshnessUtc);

public sealed record NetworkAllocationEvaluation(
    string RecommendedAction,
    string Rationale,
    string RecommendationExplanation,
    IReadOnlyList<FacilityComparisonRow> FacilityComparisons,
    IReadOnlyList<NetworkScenario> Alternatives);
