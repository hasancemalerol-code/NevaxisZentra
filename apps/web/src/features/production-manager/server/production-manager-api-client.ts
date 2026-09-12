import "./production-manager-server-boundary";

import type { ProductionOrderRequestContext } from "@/features/production-execution/server/production-order-request-context";

export type ProductionManagerWorkspaceApi = Readonly<{
  executive: Readonly<{
    factoryStatusLabel: string;
    activeRiskCount: number;
    predictedIssueCount: number;
    pendingDecisionCount: number;
    autonomousActionCount: number;
    verifiedTodayCount: number;
  }>;
  predictions: readonly ProductionManagerPredictionApi[];
  pendingDecisions: readonly ProductionManagerDecisionApi[];
  recentDecisions: readonly ProductionManagerDecisionApi[];
  bottlenecks: readonly Readonly<{
    domain: string;
    summary: string;
    severity: string;
    affectedOrders: readonly string[];
  }>[];
  policy: Readonly<{
    autonomyMode: string;
    lowConfidenceThreshold: number;
    staleDataMaxMinutes: number;
  }>;
  stateCapturedAtUtc: string;
}>;

export type ProductionManagerPredictionApi = Readonly<{
  id: string;
  predictionType: string;
  title: string;
  summary: string;
  severity: string;
  confidenceBand: string;
  confidenceScore: number;
  horizon: string;
  dataClassification: string;
  correlationKey: string;
}>;

export type ProductionManagerDecisionOptionApi = Readonly<{
  id: string;
  optionCode: string;
  title: string;
  description: string;
  score: number;
  requiresApproval: boolean;
  isRejected: boolean;
  rejectionReason?: string;
  rank: number;
}>;

export type ProductionManagerDecisionApi = Readonly<{
  id: string;
  factoryCaseId?: string;
  decisionType: string;
  title: string;
  problemSummary: string;
  status: string;
  autonomyMode: string;
  confidenceBand: string;
  confidenceScore: number;
  approvalState: string;
  selectedOptionId?: string;
  effectivenessScore?: number;
  executedAtUtc?: string;
  verifiedAtUtc?: string;
  concurrencyToken: number;
  options: readonly ProductionManagerDecisionOptionApi[];
}>;

async function productionManagerFetch<T>(
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

export async function getProductionManagerWorkspace(context: ProductionOrderRequestContext) {
  return productionManagerFetch<ProductionManagerWorkspaceApi>(context, "/api/production-manager/workspace");
}

export async function getProductionManagerDecision(
  context: ProductionOrderRequestContext,
  decisionId: string,
) {
  return productionManagerFetch<ProductionManagerDecisionApi>(
    context,
    `/api/production-manager/decisions/${decisionId}`,
  );
}

export type ProductionManagerSimulationRowApi = Readonly<{
  optionCode: string;
  label: string;
  onTimeProbability: number;
  productionCompletionPercent: number;
  capacityUtilizationPercent: number;
  downtimeHours: number;
  materialRisk: string;
  qualityRisk: string;
  requiresApproval: boolean;
  estimatedRecoveryHours: number;
}>;

export type ProductionManagerSimulationApi = Readonly<{
  scenarioType: string;
  output: string;
  comparison: string;
  rows: readonly ProductionManagerSimulationRowApi[];
  dataClassification: string;
}>;

export async function simulateProductionManagerScenario(
  context: ProductionOrderRequestContext,
  body: Readonly<{
    scenarioType: string;
    machineId?: string;
    orderId?: string;
    moldId?: string;
    materialId?: string;
    delayHours?: number;
    capacityAdjustmentPercent?: number;
  }>,
) {
  return productionManagerFetch<ProductionManagerSimulationApi>(context, "/api/production-manager/simulate", {
    method: "POST",
    body: JSON.stringify(body),
  });
}

export async function approveProductionManagerDecision(
  context: ProductionOrderRequestContext,
  decisionId: string,
  body: Readonly<{ approved: boolean; notes?: string; concurrencyToken: number }>,
) {
  return productionManagerFetch<ProductionManagerDecisionApi>(
    context,
    `/api/production-manager/decisions/${decisionId}/approve`,
    { method: "POST", body: JSON.stringify(body) },
  );
}

export async function executeProductionManagerDecision(
  context: ProductionOrderRequestContext,
  decisionId: string,
  body: Readonly<{ selectedOptionId?: string; idempotencyKey?: string; concurrencyToken: number }>,
) {
  return productionManagerFetch<ProductionManagerDecisionApi>(
    context,
    `/api/production-manager/decisions/${decisionId}/execute`,
    { method: "POST", body: JSON.stringify(body) },
  );
}

export async function verifyProductionManagerDecision(
  context: ProductionOrderRequestContext,
  decisionId: string,
  body: Readonly<{ actualOutcomeJson: string; concurrencyToken: number }>,
) {
  return productionManagerFetch<ProductionManagerDecisionApi>(
    context,
    `/api/production-manager/decisions/${decisionId}/verify`,
    { method: "POST", body: JSON.stringify(body) },
  );
}
