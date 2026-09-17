import "server-only";

import type { ProductionOrderRequestContext } from "@/features/production-execution/server/production-order-request-context";

export type ThroughputOptimizerExecutiveCaseApi = Readonly<{
  caseId: string;
  lineId: string;
  lineCode: string;
  currentPhase: string;
  caseStatus: string;
  currentConstraintStationCode?: string;
  currentConstraintType?: string;
  selectedInterventionTitle?: string;
  selectedInterventionType?: string;
  hasPendingApproval: boolean;
  observationState: string;
  verificationOutcome?: string;
  verifiedThroughputGainPerHour?: number;
  isVerifiedClosed: boolean;
  updatedAtUtc: string;
}>;

export type ThroughputOptimizerExecutiveSummaryApi = Readonly<{
  activeCaseCount: number;
  pendingApprovalCount: number;
  verifiedImprovementTodayCount: number;
  activeCases: readonly ThroughputOptimizerExecutiveCaseApi[];
  verifiedToday: readonly ThroughputOptimizerExecutiveCaseApi[];
}>;

export type ThroughputOptimizerWorkspaceApi = Readonly<{
  caseId: string;
  lineId: string;
  lineCode: string;
  currentPhase: string;
  caseStatus: string;
  observation?: Readonly<{
    currentGoodPerHour: number;
    targetGoodPerHour: number;
    gapPerHour: number;
    currentConstraintStationCode?: string;
    currentConstraintType?: string;
    dataQuality: string;
  }>;
  diagnosis?: Readonly<{
    currentConstraintStationCode: string;
    constraintType: string;
    rootCauseSummary: string;
    intelligencePath: string;
    confidence: number;
  }>;
  selection?: Readonly<{
    selected: Readonly<{
      scenarioCode: string;
      interventionType: string;
      title: string;
      predictedGoodThroughputPerHour: number;
      deltaGoodThroughputPerHour: number;
      requiresApproval: boolean;
    }>;
  }>;
  improvement?: Readonly<{
    verificationOutcome: string;
    baseline: Readonly<{ goodThroughputPerHour: number; rejectCount: number }>;
    actual?: Readonly<{ goodThroughputPerHour: number; rejectCount: number }>;
    targetImprovementPerHour: number;
    interventionSummary: string;
  }>;
  contextSnapshotId?: string;
  modelIntelligencePath?: string;
  hasPendingApproval: boolean;
  isVerifiedClosed: boolean;
}>;

async function throughputOptimizerFetch<T>(
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
      message: body.message ?? `Throughput optimizer API failed (${response.status})`,
    };
  }

  return { status: "success", value: (await response.json()) as T };
}

export async function readThroughputOptimizerWorkspace(
  context: ProductionOrderRequestContext,
  caseId: string,
): Promise<
  { status: "success"; value: ThroughputOptimizerWorkspaceApi } | { status: "failure"; code: string; message: string }
> {
  return throughputOptimizerFetch<ThroughputOptimizerWorkspaceApi>(
    context,
    `/api/throughput-optimizer/cases/${caseId}/workspace`,
  );
}

export async function readThroughputOptimizerExecutiveSummary(
  context: ProductionOrderRequestContext,
): Promise<
  { status: "success"; value: ThroughputOptimizerExecutiveSummaryApi } | { status: "failure"; code: string; message: string }
> {
  return throughputOptimizerFetch<ThroughputOptimizerExecutiveSummaryApi>(
    context,
    "/api/throughput-optimizer/executive-summary",
  );
}

export async function observeThroughputGap(
  context: ProductionOrderRequestContext,
  lineId: string,
): Promise<
  { status: "success"; value: ThroughputOptimizerWorkspaceApi } | { status: "failure"; code: string; message: string }
> {
  return throughputOptimizerFetch<ThroughputOptimizerWorkspaceApi>(
    context,
    `/api/throughput-optimizer/lines/${lineId}/observe`,
    { method: "POST" },
  );
}
