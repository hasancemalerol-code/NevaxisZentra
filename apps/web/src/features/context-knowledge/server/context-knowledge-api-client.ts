import "server-only";

import type { ProductionOrderRequestContext } from "@/features/production-execution/server/production-order-request-context";

export type WorkerContextPackageApi = Readonly<{
  scope: Readonly<{
    tenantId: string;
    facilityId: string;
    entityType: string;
    entityId: string;
    lineId?: string;
    stationId?: string;
    digitalWorkCaseId?: string;
  }>;
  canonicalState?: Readonly<{
    entityType: string;
    entityId: string;
    resolutionStatus: string;
    resolvedStateValue?: string;
    confidence: number;
    conflictReasons: readonly string[];
    evaluatedAtUtc: string;
  }>;
  items: readonly Readonly<{
    itemKind: string;
    referenceId: string;
    summary: string;
    priorityRank: number;
    dataTier: string;
  }>[];
  verifiedKnowledge: readonly Readonly<{
    id: string;
    problemSymptom: string;
    rootCause: string;
    verifiedOutcome: string;
    confidence: number;
    promotionStatus: string;
  }>[];
  metrics: Readonly<{
    buildDurationMs: number;
    recordsConsidered: number;
    recordsRejected: number;
    duplicateCount: number;
    staleCount: number;
    conflictCount: number;
    retrievedKnowledgeCount: number;
    finalItemCount: number;
    finalPayloadBytes: number;
    failureState?: string;
  }>;
  failureState?: string;
  builtAtUtc: string;
}>;

async function contextKnowledgeFetch<T>(
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
      message: body.message ?? `Context API failed (${response.status})`,
    };
  }

  const value = (await response.json()) as T;
  return { status: "success", value };
}

export async function buildWorkerContext(
  context: ProductionOrderRequestContext,
  payload: Readonly<{
    entityType: string;
    entityId: string;
    lineId?: string;
    stationId?: string;
    digitalWorkCaseId?: string;
  }>,
): Promise<
  { status: "success"; value: WorkerContextPackageApi } | { status: "failure"; code: string; message: string }
> {
  return contextKnowledgeFetch<WorkerContextPackageApi>(context, "/api/context-knowledge/build", {
    method: "POST",
    body: JSON.stringify({
      scope: {
        tenantId: "00000000-0000-0000-0000-000000000000",
        facilityId: context.facilityId,
        entityType: payload.entityType,
        entityId: payload.entityId,
        lineId: payload.lineId,
        stationId: payload.stationId,
        digitalWorkCaseId: payload.digitalWorkCaseId,
      },
      budget: {
        maxEventCount: 100,
        maxHistoricalCases: 10,
        maxKnowledgeRecords: 10,
        maxTimeWindowHours: 24,
        maxPayloadBytes: 256000,
      },
      digitalWorkCaseId: payload.digitalWorkCaseId,
      includeVerifiedKnowledge: true,
      includeRelatedCases: true,
    }),
  });
}
