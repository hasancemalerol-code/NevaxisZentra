import "./adaptive-planning-server-boundary";

import type { ProductionOrderRequestContext } from "@/features/production-execution/server/production-order-request-context";

export type AdaptivePlanningActivePlanApi = Readonly<{
  id: string;
  referenceCode: string;
  title: string;
  adaptiveLifecycleStatus: string;
  activePlanVersionId?: string;
}>;

export type AdaptivePlanningVersionApi = Readonly<{
  id: string;
  versionNumber: number;
  planningHorizon: string;
  lifecycleStatus: string;
  score: number;
  confidenceScore: number;
  publishedAtUtc?: string;
  concurrencyToken: number;
}>;

export type AdaptivePlanningOperationApi = Readonly<{
  id: string;
  orderId: string;
  machineId?: string;
  lineId?: string;
  moldId?: string;
  plannedStartUtc: string;
  plannedEndUtc: string;
  setupMinutes: number;
  materialReadinessState: string;
  sequenceRank: number;
  status: string;
}>;

export type AdaptivePlanningScenarioApi = Readonly<{
  id: string;
  scenarioCode: string;
  title: string;
  triggerType: string;
  score: number;
  rank: number;
  comparisonJson: string;
  status: string;
}>;

export type AdaptivePlanningDeviationApi = Readonly<{
  id: string;
  deviationType: string;
  severity: string;
  summary: string;
  detectedAtUtc: string;
}>;

export type AdaptivePlanningChangeApi = Readonly<{
  id: string;
  changeType: string;
  triggerType: string;
  diffJson: string;
  createdAtUtc: string;
}>;

export type AdaptivePlanningActivityApi = Readonly<{
  eventType: string;
  summary: string;
  occurredAtUtc: string;
}>;

export type AdaptivePlanningPolicyApi = Readonly<{
  id: string;
  frozenWindowMinutes: number;
  nervousnessCooldownMinutes: number;
  minBenefitThreshold: number;
  autonomyMode: string;
  policyEnvelopeJson: string;
  concurrencyToken: number;
}>;

export type AdaptivePlanningMetricsApi = Readonly<{
  planAdherencePercent: number;
  scheduleAdherencePercent: number;
  ordersAtRisk: number;
  lateOrders: number;
  planVersionsToday: number;
  replansToday: number;
  operationsMovedToday: number;
  frozenWindowOverridesToday: number;
  plannedUtilizationPercent: number;
  actualUtilizationPercent: number;
}>;

export type AdaptivePlanningDeadlineRiskApi = Readonly<{
  orderId: string;
  riskLevel: string;
  deadlineUtc?: string;
  expectedCompletionUtc?: string;
  confidenceScore: number;
}>;

export type AdaptivePlanningRisksApi = Readonly<{
  deadlineRisks: readonly AdaptivePlanningDeadlineRiskApi[];
  capacityOverloads: readonly string[];
  machineRisks: readonly string[];
  materialRisks: readonly string[];
  moldRisks: readonly string[];
  qualityRisks: readonly string[];
  maintenanceRisks: readonly string[];
}>;

export type AdaptivePlanningWorkspaceApi = Readonly<{
  activePlan: AdaptivePlanningActivePlanApi | null;
  activeVersion: AdaptivePlanningVersionApi | null;
  operations: readonly AdaptivePlanningOperationApi[];
  scenarios: readonly AdaptivePlanningScenarioApi[];
  deviations: readonly AdaptivePlanningDeviationApi[];
  recentChanges: readonly AdaptivePlanningChangeApi[];
  activityFeed: readonly AdaptivePlanningActivityApi[];
  policy: AdaptivePlanningPolicyApi;
  metrics: AdaptivePlanningMetricsApi;
  risks: AdaptivePlanningRisksApi;
  dataFreshnessUtc: string;
}>;

export type BuildPlanRequestApi = Readonly<{
  productionPlanId: string;
  planningHorizon: string;
  objectiveProfileJson: string;
  expectedConcurrencyToken?: number;
}>;

export type SimulatePlanRequestApi = Readonly<{
  productionPlanId: string;
  triggerType: string;
  triggerContextJson: string;
  maxAlternatives: number;
}>;

export type PublishPlanRequestApi = Readonly<{
  planVersionId: string;
  concurrencyToken: number;
  overrideFrozenWindow: boolean;
  approvalNotes?: string;
}>;

export type ApproveReplanRequestApi = Readonly<{
  planVersionId: string;
  concurrencyToken: number;
  approved: boolean;
  notes?: string;
}>;

export type EvaluateReplanRequestApi = Readonly<{
  productionPlanId: string;
  triggerType: string;
  triggerContextJson: string;
}>;

export type UpdateAdaptivePlanningPolicyRequestApi = Readonly<{
  concurrencyToken: number;
  frozenWindowMinutes?: number;
  nervousnessCooldownMinutes?: number;
  minBenefitThreshold?: number;
  autonomyMode?: string;
  policyEnvelopeJson?: string;
}>;

export type PlanDiffRowApi = Readonly<{
  orderId: string;
  field: string;
  oldValue: string;
  newValue: string;
  impactSummary: string;
}>;

export type ReplanEvaluationResultApi = Readonly<{
  recommendedAction: string;
  rationale: string;
  nervousnessBlocked: boolean;
  frozenWindowBlocked: boolean;
  alternatives: readonly AdaptivePlanningScenarioApi[];
}>;

export type FeasibilityResultApi = Readonly<{
  isFeasible: boolean;
  hardViolations: readonly string[];
  softWarnings: readonly string[];
  explanation?: string;
}>;

export type BuildPlanResultApi = Readonly<{
  version: AdaptivePlanningVersionApi;
  operations: readonly AdaptivePlanningOperationApi[];
  feasibility: FeasibilityResultApi;
  score: number;
  comparisonLabel: string;
}>;

export type PlanSimulationResultApi = Readonly<{
  scenario: AdaptivePlanningScenarioApi;
  metrics: readonly Record<string, unknown>[];
  comparisonJson: string;
}>;

export type PlanPublishResultApi = Readonly<{
  version: AdaptivePlanningVersionApi;
  planId: string;
  autoPublished: boolean;
  change: AdaptivePlanningChangeApi;
}>;

export type PlanOutcomeVerificationResultApi = Readonly<{
  planVersionId: string;
  outcomeStatus: string;
  expectedImpactJson: string;
  actualImpactJson: string;
  adherencePercent: number;
  learningMemoryId?: string;
}>;

type ApiResult<T> =
  | { status: "success"; value: T }
  | { status: "failure"; code: string; message: string };

async function adaptivePlanningFetch<T>(
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

export async function getAdaptivePlanningWorkspace(context: ProductionOrderRequestContext) {
  return adaptivePlanningFetch<AdaptivePlanningWorkspaceApi>(context, "/api/adaptive-planning/workspace");
}

export async function buildAdaptivePlan(
  context: ProductionOrderRequestContext,
  request: BuildPlanRequestApi,
) {
  return adaptivePlanningFetch<BuildPlanResultApi>(context, "/api/adaptive-planning/build", {
    method: "POST",
    body: JSON.stringify(request),
  });
}

export async function simulateAdaptivePlan(
  context: ProductionOrderRequestContext,
  request: SimulatePlanRequestApi,
) {
  return adaptivePlanningFetch<readonly PlanSimulationResultApi[]>(context, "/api/adaptive-planning/simulate", {
    method: "POST",
    body: JSON.stringify(request),
  });
}

export async function evaluateAdaptiveReplan(
  context: ProductionOrderRequestContext,
  request: EvaluateReplanRequestApi,
) {
  return adaptivePlanningFetch<ReplanEvaluationResultApi>(context, "/api/adaptive-planning/evaluate-replan", {
    method: "POST",
    body: JSON.stringify(request),
  });
}

export async function publishAdaptivePlan(
  context: ProductionOrderRequestContext,
  request: PublishPlanRequestApi,
) {
  return adaptivePlanningFetch<PlanPublishResultApi>(context, "/api/adaptive-planning/publish", {
    method: "POST",
    body: JSON.stringify(request),
  });
}

export async function approveAdaptiveReplan(
  context: ProductionOrderRequestContext,
  request: ApproveReplanRequestApi,
) {
  return adaptivePlanningFetch<PlanPublishResultApi>(context, "/api/adaptive-planning/approve", {
    method: "POST",
    body: JSON.stringify(request),
  });
}

export async function verifyAdaptivePlanOutcome(
  context: ProductionOrderRequestContext,
  planVersionId: string,
) {
  const url = `/api/adaptive-planning/verify-outcome?planVersionId=${encodeURIComponent(planVersionId)}`;
  return adaptivePlanningFetch<PlanOutcomeVerificationResultApi>(context, url, {
    method: "POST",
  });
}

export async function updateAdaptivePlanningPolicy(
  context: ProductionOrderRequestContext,
  request: UpdateAdaptivePlanningPolicyRequestApi,
) {
  return adaptivePlanningFetch<AdaptivePlanningPolicyApi>(context, "/api/adaptive-planning/policy", {
    method: "PUT",
    body: JSON.stringify(request),
  });
}

export async function getAdaptivePlanDiff(
  context: ProductionOrderRequestContext,
  fromVersionId: string,
  toVersionId: string,
) {
  const url = `/api/adaptive-planning/diff?fromVersionId=${encodeURIComponent(fromVersionId)}&toVersionId=${encodeURIComponent(toVersionId)}`;
  return adaptivePlanningFetch<readonly PlanDiffRowApi[]>(context, url);
}
