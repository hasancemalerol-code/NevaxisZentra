import "./factory-network-server-boundary";

import type { ProductionOrderRequestContext } from "@/features/production-execution/server/production-order-request-context";

export type FactoryNetworkApi = Readonly<{
  id: string;
  name: string;
  planningHorizon: string;
  status: string;
  concurrencyToken: number;
}>;

export type FactoryNetworkFacilityApi = Readonly<{
  id: string;
  facilityId: string;
  role: string;
  sortOrder: number;
}>;

export type FacilityCapabilityProfileApi = Readonly<{
  id: string;
  facilityId: string;
  technologiesJson: string;
  linesJson: string;
  machineFamiliesJson: string;
  moldCompatibilityJson: string;
  materialsJson: string;
  qualityCertificationsJson: string;
  shiftsJson: string;
  capacityJson: string;
  riskJson: string;
}>;

export type NetworkFacilityStateApi = Readonly<{
  facilityId: string;
  facilityName: string;
  timezone: string;
  loadPercent: number;
  availableCapacityPercent: number;
  ordersAtRisk: number;
  activeCases: number;
  planningState: string;
  freshnessUtc: string;
}>;

export type NetworkCapacityRowApi = Readonly<{
  facilityId: string;
  facilityName: string;
  availablePercent: number;
  committedPercent: number;
  atRiskPercent: number;
  unusedPercent: number;
  overloadedPercent: number;
  realizable: boolean;
}>;

export type NetworkDemandRowApi = Readonly<{
  orderId: string;
  assignedFacilityId: string;
  remainingQuantity: number;
  deadlineUtc: string;
  riskLevel: string;
  requiredCapability: string;
  materialReady: boolean;
  moldReady: boolean;
}>;

export type NetworkOpportunityRowApi = Readonly<{
  opportunityCode: string;
  title: string;
  summary: string;
  sourceFacilityId: string;
  destinationFacilityId: string;
  orderId: string;
  expectedBenefitScore: number;
  dataClassification: string;
}>;

export type NetworkScenarioApi = Readonly<{
  id: string;
  scenarioCode: string;
  title: string;
  actionType: string;
  sourceFacilityId: string;
  destinationFacilityId: string;
  orderId: string;
  score: number;
  comparisonJson: string;
  status: string;
  createdAtUtc: string;
}>;

export type NetworkAllocationApi = Readonly<{
  id: string;
  networkId: string;
  actionType: string;
  status: string;
  sourceFacilityId: string;
  destinationFacilityId: string;
  triggerType: string;
  reasonJson: string;
  expectedImpactJson: string;
  actualImpactJson: string;
  publishedAtUtc?: string;
  verifiedAtUtc?: string;
  createdAtUtc: string;
  concurrencyToken: number;
}>;

export type NetworkAllocationItemApi = Readonly<{
  id: string;
  orderId: string;
  sourceFacilityId: string;
  destinationFacilityId: string;
  totalQuantity: number;
  sourceQuantity: number;
  destinationQuantity: number;
  status: string;
}>;

export type NetworkActivityEventApi = Readonly<{
  id: string;
  eventType: string;
  summary: string;
  occurredAtUtc: string;
}>;

export type FactoryNetworkPolicyApi = Readonly<{
  id: string;
  autonomyMode: string;
  policyEnvelopeJson: string;
  concurrencyToken: number;
}>;

export type NetworkMetricsApi = Readonly<{
  networkUtilizationPercent: number;
  ordersAtRisk: number;
  ordersRecovered: number;
  capacityImbalancePercent: number;
  activeCrossPlantDecisions: number;
  networkPlanAdherencePercent: number;
}>;

export type NetworkResilienceSummaryApi = Readonly<{
  capacityRedundancyScore: number;
  processRedundancyScore: number;
  moldRedundancyScore: number;
  singlePointsOfFailure: readonly string[];
  bottlenecks: readonly string[];
}>;

export type FactoryNetworkWorkspaceApi = Readonly<{
  network: FactoryNetworkApi;
  facilities: readonly FactoryNetworkFacilityApi[];
  capabilityProfiles: readonly FacilityCapabilityProfileApi[];
  facilityStates: readonly NetworkFacilityStateApi[];
  capacityMap: readonly NetworkCapacityRowApi[];
  demand: readonly NetworkDemandRowApi[];
  opportunities: readonly NetworkOpportunityRowApi[];
  scenarios: readonly NetworkScenarioApi[];
  activeAllocations: readonly NetworkAllocationApi[];
  activityFeed: readonly NetworkActivityEventApi[];
  policy: FactoryNetworkPolicyApi;
  metrics: NetworkMetricsApi;
  resilience: NetworkResilienceSummaryApi;
  dataFreshnessUtc: string;
}>;

export type FacilityComparisonRowApi = Readonly<{
  facilityId: string;
  facilityName: string;
  feasible: boolean;
  capacityScore: number;
  deadlineScore: number;
  qualityScore: number;
  materialScore: number;
  maintenanceScore: number;
  riskScore: number;
  confidenceScore: number;
  recommendationReason: string;
}>;

export type NetworkAllocationEvaluationApi = Readonly<{
  recommendedAction: string;
  rationale: string;
  recommendationExplanation: string;
  facilityComparisons: readonly FacilityComparisonRowApi[];
  alternatives: readonly NetworkScenarioApi[];
}>;

export type SimulateNetworkRequestApi = Readonly<{
  networkId: string;
  sourceFacilityId: string;
  destinationFacilityId: string;
  orderId: string;
  quantity: number;
  actionType: string;
  maxScenarios: number;
}>;

export type EvaluateNetworkAllocationRequestApi = Readonly<{
  networkId: string;
  triggerType: string;
  triggerContextJson: string;
}>;

export type ExecuteNetworkAllocationRequestApi = Readonly<{
  allocationId: string;
  concurrencyToken: number;
  overrideStaleGuard: boolean;
}>;

export type ApproveNetworkAllocationRequestApi = Readonly<{
  allocationId: string;
  concurrencyToken: number;
  approved: boolean;
  notes?: string;
}>;

export type UpdateFactoryNetworkPolicyRequestApi = Readonly<{
  concurrencyToken: number;
  autonomyMode?: string;
  policyEnvelopeJson?: string;
}>;

export type FeasibilityResultApi = Readonly<{
  isFeasible: boolean;
  hardViolations: readonly string[];
  softWarnings: readonly string[];
  explanation?: string;
}>;

export type NetworkSimulationResultApi = Readonly<{
  scenario: NetworkScenarioApi;
  feasibility: FeasibilityResultApi;
  comparisonJson: string;
  recommendationExplanation: string;
}>;

export type NetworkAllocationResultApi = Readonly<{
  allocation: NetworkAllocationApi;
  items: readonly NetworkAllocationItemApi[];
  approvalRequired: boolean;
  autoExecuted: boolean;
}>;

type ApiResult<T> =
  | { status: "success"; value: T }
  | { status: "failure"; code: string; message: string };

async function factoryNetworkFetch<T>(
  context: ProductionOrderRequestContext,
  path: string,
  init?: RequestInit,
): Promise<ApiResult<T>> {
  const url = new URL(`${context.apiBaseUrl}${path}`);
  url.searchParams.set("facilityId", context.facilityId);

  const response = await fetch(url, {
    ...init,
    headers: {
      Accept: "application/json",
      "Content-Type": "application/json",
      ...context.requestHeaders,
      ...init?.headers,
    },
    cache: "no-store",
  });

  const text = await response.text();
  let json: unknown;
  try {
    json = text ? JSON.parse(text) : null;
  } catch {
    return { status: "failure", code: "invalid-json", message: "API yanıtı okunamadı." };
  }

  if (!response.ok) {
    const message =
      typeof json === "object" && json !== null && "message" in json
        ? String((json as { message: string }).message)
        : `HTTP ${response.status}`;
    const code =
      typeof json === "object" && json !== null && "code" in json
        ? String((json as { code: string }).code)
        : `http-${response.status}`;
    return { status: "failure", code, message };
  }

  return { status: "success", value: json as T };
}

export async function getFactoryNetworkWorkspace(context: ProductionOrderRequestContext) {
  return factoryNetworkFetch<FactoryNetworkWorkspaceApi>(context, "/api/factory-network/workspace");
}

export async function simulateFactoryNetwork(
  context: ProductionOrderRequestContext,
  request: SimulateNetworkRequestApi,
) {
  return factoryNetworkFetch<readonly NetworkSimulationResultApi[]>(context, "/api/factory-network/simulate", {
    method: "POST",
    body: JSON.stringify(request),
  });
}

export async function evaluateNetworkAllocation(
  context: ProductionOrderRequestContext,
  request: EvaluateNetworkAllocationRequestApi,
) {
  return factoryNetworkFetch<NetworkAllocationEvaluationApi>(context, "/api/factory-network/evaluate-allocation", {
    method: "POST",
    body: JSON.stringify(request),
  });
}

export async function executeNetworkAllocation(
  context: ProductionOrderRequestContext,
  request: ExecuteNetworkAllocationRequestApi,
) {
  return factoryNetworkFetch<NetworkAllocationResultApi>(context, "/api/factory-network/execute", {
    method: "POST",
    body: JSON.stringify(request),
  });
}

export async function approveNetworkAllocation(
  context: ProductionOrderRequestContext,
  request: ApproveNetworkAllocationRequestApi,
) {
  return factoryNetworkFetch<NetworkAllocationResultApi>(context, "/api/factory-network/approve", {
    method: "POST",
    body: JSON.stringify(request),
  });
}

export async function updateFactoryNetworkPolicy(
  context: ProductionOrderRequestContext,
  request: UpdateFactoryNetworkPolicyRequestApi,
) {
  return factoryNetworkFetch<FactoryNetworkPolicyApi>(context, "/api/factory-network/policy", {
    method: "PUT",
    body: JSON.stringify(request),
  });
}
