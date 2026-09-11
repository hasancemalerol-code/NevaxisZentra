import "./factory-orchestration-server-boundary";

import type { ProductionOrderRequestContext } from "@/features/production-execution/server/production-order-request-context";

export type FactoryCaseApiSummary = Readonly<{
  id: string;
  title: string;
  severity: string;
  status: string;
  primaryDomain: string;
  participatingDomains: string;
  currentBlockerCode: string;
  primaryWorkerId: string;
  primaryWorkerCode?: string;
  primaryWorkerName?: string;
  requiresApproval: boolean;
  verificationState: string;
  startedAtUtc: string | null;
  updatedAtUtc: string;
  targetAtUtc: string | null;
  businessImpact: string;
  correlationKey: string | null;
  reopenCount: number;
  administrativelyClosed: boolean;
  concurrencyToken: number;
}>;

export type FactoryCaseDependencyApi = Readonly<{
  fromNodeKey: string;
  toNodeKey: string;
  fromNodeType: string;
  toNodeType: string;
  edgeType: string;
  linkedCaseId?: string;
  linkedWorkerId?: string;
}>;

export type FactoryCaseBlockerApi = Readonly<{
  code: string;
  message: string;
  ownerType?: string;
  ownerId?: string;
  sinceUtc?: string;
  isStale: boolean;
}>;

export type FactoryCaseTimelineEntryApi = Readonly<{
  id: string;
  actionType: string;
  description: string;
  createdAtUtc: string;
}>;

export type FactoryCaseDetailApi = Readonly<{
  rootCase: FactoryCaseApiSummary;
  childCases: FactoryCaseApiSummary[];
  dependencies: FactoryCaseDependencyApi[];
  blockers: FactoryCaseBlockerApi[];
  recoveryPlan: Readonly<{
    objective: string;
    actions: string[];
    dependencies: string[];
    verificationCriteria: string[];
    fallback?: string;
  }>;
  timeline: FactoryCaseTimelineEntryApi[];
  nextAction?: string;
  blockerExplanation?: string;
  hasDeadlock: boolean;
  hasConflict: boolean;
}>;

async function factoryApiFetch<T>(
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

export async function listFactoryCases(
  context: ProductionOrderRequestContext,
  limit = 50,
) {
  return factoryApiFetch<{ items: FactoryCaseApiSummary[] }>(
    context,
    `/api/factory-orchestration/cases?limit=${limit}`,
  );
}

export async function getFactoryCaseDetail(
  context: ProductionOrderRequestContext,
  caseId: string,
) {
  return factoryApiFetch<FactoryCaseDetailApi>(
    context,
    `/api/factory-orchestration/cases/${caseId}`,
  );
}
