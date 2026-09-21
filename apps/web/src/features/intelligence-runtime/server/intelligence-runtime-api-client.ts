import "./intelligence-runtime-server-boundary";

import type { ProductionOrderRequestContext } from "@/features/production-execution/server/production-order-request-context";

export type IntelligenceDependencyMetricsApi = Readonly<{
  deterministicRouteRate: number;
  analyticsRouteRate: number;
  solverRouteRate: number;
  retrievalRouteRate: number;
  nevaxisModelCallRate: number;
  frontierModelCallRate: number;
  frontierFallbackRate: number;
  frontierCostPerCase: number;
  frontierCostPerVerifiedOutcome?: number;
  totalRequests: number;
  modelInvocations: number;
  totalEstimatedCost: number;
}>;

export type IntelligenceRouteSummaryApi = Readonly<{
  routeId: string;
  taskType: string;
  routeType: string;
  routingReason: string;
  whyModelCalledOrSkipped: string;
  modelInvoked: boolean;
  estimatedCost: number;
  latencyMs: number;
  createdAtUtc: string;
}>;

export type NevaxisDatasetVersionSummaryApi = Readonly<{
  datasetVersionId: string;
  versionCode: string;
  schemaVersion: string;
  readinessState: string;
  includedCaseCount: number;
  excludedCaseCount: number;
  tenantPolicy: string;
  createdAtUtc: string;
}>;

export type NevaxisModelRegistrySummaryApi = Readonly<{
  modelId: string;
  modelCode: string;
  version: string;
  baseModel: string;
  status: string;
  evaluationStatus: string;
  safetyStatus: string;
  taskCapabilities: readonly string[];
  trainingDatasetVersion?: string;
  updatedAtUtc: string;
}>;

export type NevaxisModelEvaluationSummaryApi = Readonly<{
  evaluationRunId: string;
  modelId: string;
  modelCode: string;
  evaluationMode: string;
  outcome: string;
  structuredOutputValidityRate: number;
  safetyPassRate: number;
  hallucinatedEntityRate: number;
  estimatedCost: number;
  latencyMs: number;
  createdAtUtc: string;
}>;

export type VerifiedOutcomeCostSnapshotApi = Readonly<{
  digitalWorkCaseId?: string;
  optimizationRunId?: string;
  scenarioRunId?: string;
  outcome: string;
  totalIntelligenceCost: number;
  modelCallCount: number;
  deterministicRouteCount: number;
  costPerVerifiedOutcome?: number;
  verifiedAtUtc?: string;
}>;

export type IntelligenceRuntimeControlCenterApi = Readonly<{
  metrics: IntelligenceDependencyMetricsApi;
  recentRoutes: readonly IntelligenceRouteSummaryApi[];
  models: readonly NevaxisModelRegistrySummaryApi[];
  datasetVersions: readonly NevaxisDatasetVersionSummaryApi[];
  evaluations: readonly NevaxisModelEvaluationSummaryApi[];
  verifiedOutcomeCosts: readonly VerifiedOutcomeCostSnapshotApi[];
  generatedAtUtc: string;
}>;

async function intelligenceRuntimeFetch<T>(
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
      message: body.message ?? `Intelligence runtime API failed (${response.status})`,
    };
  }

  return { status: "success", value: (await response.json()) as T };
}

export async function readIntelligenceRuntimeControlCenter(
  context: ProductionOrderRequestContext,
): Promise<
  { status: "success"; value: IntelligenceRuntimeControlCenterApi }
  | { status: "failure"; code: string; message: string }
> {
  return intelligenceRuntimeFetch<IntelligenceRuntimeControlCenterApi>(
    context,
    "/api/intelligence-runtime/control-center",
  );
}
