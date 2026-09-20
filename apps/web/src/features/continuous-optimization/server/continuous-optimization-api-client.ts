import "server-only";

import type { ProductionOrderRequestContext } from "@/features/production-execution/server/production-order-request-context";

export type ContinuousOptimizationRunSummaryApi = Readonly<{
  runId: string;
  currentPhase: string;
  optimizationType: string;
  triggerType: string;
  outcome?: string;
  digitalWorkCaseId?: string;
  throughputOptimizerRunId?: string;
  lineId?: string;
  lineCode?: string;
  updatedAtUtc: string;
}>;

export type OptimizationOpportunityApi = Readonly<{
  opportunityType: string;
  scopeEntityType: string;
  scopeEntityId: string;
  triggerType: string;
  title: string;
  summary: string;
  baselineValue: number;
  currentValue: number;
  estimatedBenefit: number;
  confidence: number;
  riskClass: string;
  evidence: readonly string[];
  requiredActions: readonly string[];
  constraints: readonly string[];
  expiresAtUtc: string;
}>;

export type ContinuousOptimizationVerifiedGainApi = Readonly<{
  runId: string;
  caseId?: string;
  lineCode: string;
  optimizationType: string;
  outcome: string;
  verifiedImprovementPerHour?: number;
  summary: string;
  verifiedAtUtc: string;
}>;

export type ContinuousOptimizationKpiSnapshotApi = Readonly<{
  opportunitiesDetected: number;
  opportunitiesQualified: number;
  opportunitiesSuppressed: number;
  runsActive: number;
  verifiedGains: number;
  noGainOutcomes: number;
  opportunityToActionRate: number;
  verifiedGainRate: number;
}>;

export type ContinuousOptimizationControlCenterApi = Readonly<{
  activeRuns: readonly ContinuousOptimizationRunSummaryApi[];
  activeOpportunities: readonly OptimizationOpportunityApi[];
  awaitingApproval: readonly ContinuousOptimizationRunSummaryApi[];
  observing: readonly ContinuousOptimizationRunSummaryApi[];
  verifiedGains: readonly ContinuousOptimizationVerifiedGainApi[];
  rejectedOrFailed: readonly ContinuousOptimizationRunSummaryApi[];
  kpis: ContinuousOptimizationKpiSnapshotApi;
  generatedAtUtc: string;
}>;

async function continuousOptimizationFetch<T>(
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
      message: body.message ?? `Continuous optimization API failed (${response.status})`,
    };
  }

  return { status: "success", value: (await response.json()) as T };
}

export async function readContinuousOptimizationControlCenter(
  context: ProductionOrderRequestContext,
): Promise<
  { status: "success"; value: ContinuousOptimizationControlCenterApi }
  | { status: "failure"; code: string; message: string }
> {
  return continuousOptimizationFetch<ContinuousOptimizationControlCenterApi>(
    context,
    "/api/continuous-optimization/control-center",
  );
}

export async function readContinuousOptimizationOpportunities(
  context: ProductionOrderRequestContext,
  lineId?: string,
): Promise<
  { status: "success"; value: readonly OptimizationOpportunityApi[] }
  | { status: "failure"; code: string; message: string }
> {
  const path = lineId
    ? `/api/continuous-optimization/opportunities?lineId=${encodeURIComponent(lineId)}`
    : "/api/continuous-optimization/opportunities";
  return continuousOptimizationFetch<readonly OptimizationOpportunityApi[]>(context, path);
}
