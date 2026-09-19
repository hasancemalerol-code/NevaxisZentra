import "server-only";

import type { ProductionOrderRequestContext } from "@/features/production-execution/server/production-order-request-context";

export type ScenarioRecoveryObjectiveApi = Readonly<{
  objectiveType: string;
  label: string;
  priority: number;
  isHardConstraint: boolean;
}>;

export type ScenarioRecoveryProblemApi = Readonly<{
  summary: string;
  orderReference?: string;
  lineCode?: string;
  constraintStationCode?: string;
  baselineGoodThroughputPerHour?: number;
  targetGoodThroughputPerHour?: number;
  contributingFactors: readonly string[];
}>;

export type ScenarioRecoveryOptionApi = Readonly<{
  optionLabel: string;
  title: string;
  category: string;
  feasibilityState: string;
  feasibilityReasons: readonly string[];
  requiresApproval: boolean;
  isDominated: boolean;
  recommendationScore: number;
  simulation: Readonly<{
    predictedGoodThroughputPerHour: number;
    baselineGoodThroughputPerHour: number;
    deltaGoodThroughputPerHour: number;
    predictedScrapRatePercent: number;
    predictedWipAverage: number;
    operationalRiskScore: number;
    qualityImpactScore: number;
    constraintBefore?: Readonly<{ stationCode?: string; constraintType?: string }>;
    constraintAfter?: Readonly<{ stationCode?: string; constraintType?: string }>;
    assumptions: readonly string[];
  }>;
  metrics: readonly Readonly<{
    metricCode: string;
    label: string;
    baselineValue?: number;
    optionValue?: number;
    delta?: number;
    unit?: string;
    direction?: string;
  }>[];
}>;

export type ScenarioRecoveryComparisonApi = Readonly<{
  metricCodes: readonly string[];
  options: readonly ScenarioRecoveryOptionApi[];
  tradeOffs: readonly Readonly<{
    optionLabel: string;
    advantages: readonly string[];
    disadvantages: readonly string[];
    computedFacts: readonly string[];
  }>[];
  recommendation: Readonly<{
    recommendedOptionLabel: string;
    scoreComponents: readonly string[];
    guardrailNotes: readonly string[];
    isPolicyDriven: boolean;
  }>;
  predictedConstraintBefore?: string;
  predictedConstraintAfterRecommended?: string;
}>;

export type ScenarioRecoveryWorkspaceApi = Readonly<{
  runId: string;
  digitalWorkCaseId?: string;
  lineId: string;
  lineCode: string;
  currentPhase: string;
  executionState: string;
  category: string;
  entryPoint: string;
  problem: ScenarioRecoveryProblemApi;
  objectives: readonly ScenarioRecoveryObjectiveApi[];
  snapshot?: Readonly<{
    snapshotId: string;
    dataQuality: string;
    capturedAtUtc: string;
    lineCode?: string;
    assumptions: readonly string[];
  }>;
  comparison?: ScenarioRecoveryComparisonApi;
  selectedOptionLabel?: string;
  recoveryPlan?: Readonly<{
    objectiveSummary: string;
    selectedOptionLabel: string;
    executionMode: string;
    observationWindowMinutes: number;
    verificationCriteria: readonly string[];
    approvalRequirements: readonly string[];
  }>;
  verification?: Readonly<{
    outcome: string;
    predictedGoodThroughputPerHour: number;
    throughputPredictionError?: number;
    qualityGuardPassed: boolean;
    safetyGuardPassed: boolean;
    actual?: Readonly<{ goodThroughputPerHour: number }>;
  }>;
  hasPendingApproval: boolean;
  isVerifiedClosed: boolean;
  engineVersion: string;
  updatedAtUtc: string;
}>;

export type ScenarioRecoveryHistoryItemApi = Readonly<{
  runId: string;
  problemSummary: string;
  category: string;
  selectedOptionLabel: string;
  phase: string;
  verificationOutcome?: string;
  predictionError?: number;
  createdAtUtc: string;
  updatedAtUtc: string;
}>;

async function scenarioRecoveryFetch<T>(
  context: ProductionOrderRequestContext,
  path: string,
  init?: RequestInit,
): Promise<{ status: "success"; value: T } | { status: "failure"; code: string; message: string }> {
  const url = new URL(`${context.apiBaseUrl}${path}`);
  url.searchParams.set("facilityId", context.facilityId);

  const response = await fetch(url, {
    ...init,
    headers: {
      Accept: "application/json",
      "Content-Type": "application/json",
      ...context.requestHeaders,
      ...(init?.headers ?? {}),
    },
    cache: "no-store",
  });

  if (!response.ok) {
    const body = (await response.json().catch(() => ({}))) as { message?: string; code?: string };
    return {
      status: "failure",
      code: body.code ?? `http-${response.status}`,
      message: body.message ?? `Scenario recovery API failed (${response.status})`,
    };
  }

  return { status: "success", value: (await response.json()) as T };
}

export async function readScenarioRecoveryWorkspace(
  context: ProductionOrderRequestContext,
  runId: string,
): Promise<
  { status: "success"; value: ScenarioRecoveryWorkspaceApi } | { status: "failure"; code: string; message: string }
> {
  return scenarioRecoveryFetch<ScenarioRecoveryWorkspaceApi>(
    context,
    `/api/scenario-recovery/runs/${runId}/workspace`,
  );
}

export async function createScenarioRecoveryRun(
  context: ProductionOrderRequestContext,
  body: unknown,
): Promise<
  { status: "success"; value: ScenarioRecoveryWorkspaceApi } | { status: "failure"; code: string; message: string }
> {
  return scenarioRecoveryFetch<ScenarioRecoveryWorkspaceApi>(
    context,
    "/api/scenario-recovery/runs",
    { method: "POST", body: JSON.stringify(body) },
  );
}

export async function generateScenarioRecoveryOptions(
  context: ProductionOrderRequestContext,
  runId: string,
): Promise<
  { status: "success"; value: ScenarioRecoveryWorkspaceApi } | { status: "failure"; code: string; message: string }
> {
  return scenarioRecoveryFetch<ScenarioRecoveryWorkspaceApi>(
    context,
    `/api/scenario-recovery/runs/${runId}/generate-options`,
    { method: "POST" },
  );
}

export async function selectScenarioRecoveryOption(
  context: ProductionOrderRequestContext,
  runId: string,
  optionLabel: string,
): Promise<
  { status: "success"; value: ScenarioRecoveryWorkspaceApi } | { status: "failure"; code: string; message: string }
> {
  return scenarioRecoveryFetch<ScenarioRecoveryWorkspaceApi>(
    context,
    `/api/scenario-recovery/runs/${runId}/select-option`,
    { method: "POST", body: JSON.stringify({ optionLabel }) },
  );
}

export async function advanceScenarioRecoveryRun(
  context: ProductionOrderRequestContext,
  runId: string,
): Promise<
  { status: "success"; value: ScenarioRecoveryWorkspaceApi } | { status: "failure"; code: string; message: string }
> {
  return scenarioRecoveryFetch<ScenarioRecoveryWorkspaceApi>(
    context,
    `/api/scenario-recovery/runs/${runId}/advance`,
    { method: "POST" },
  );
}

export async function readScenarioRecoveryHistory(
  context: ProductionOrderRequestContext,
  lineId?: string,
): Promise<
  { status: "success"; value: readonly ScenarioRecoveryHistoryItemApi[] } | { status: "failure"; code: string; message: string }
> {
  const path = lineId
    ? `/api/scenario-recovery/history?lineId=${encodeURIComponent(lineId)}`
    : "/api/scenario-recovery/history";
  return scenarioRecoveryFetch<readonly ScenarioRecoveryHistoryItemApi[]>(context, path);
}

export async function runGoldenRecoveryDemo(
  context: ProductionOrderRequestContext,
): Promise<
  { status: "success"; value: unknown } | { status: "failure"; code: string; message: string }
> {
  return scenarioRecoveryFetch(context, "/api/scenario-recovery/dev/golden-recovery-demo", { method: "POST" });
}
