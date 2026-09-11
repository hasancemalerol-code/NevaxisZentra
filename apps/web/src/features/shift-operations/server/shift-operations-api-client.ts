import "./shift-operations-server-boundary";

import type { ProductionOrderRequestContext } from "@/features/production-execution/server/production-order-request-context";

export type ShiftInstanceApiSummary = Readonly<{
  id: string;
  facilityId: string;
  shiftCode: string;
  name: string;
  shiftDate: string;
  plannedStartUtc: string;
  plannedEndUtc: string;
  actualStartUtc: string | null;
  actualEndUtc: string | null;
  status: string;
  handoverState: string;
  supervisorLabel: string | null;
  teamLabel: string | null;
  handoverReadinessScore: number;
  concurrencyToken: number;
}>;

export type ShiftOpenItemApiSummary = Readonly<{
  id: string;
  shiftInstanceId: string;
  sourceType: string;
  sourceId: string;
  category: string;
  title: string;
  description: string;
  severity: string;
  status: string;
  handoverPriority: string;
  requiresHandover: boolean;
  verificationRequired: boolean;
  digitalWorkCaseId: string | null;
  linkedAiTaskId: string | null;
  carryoverCount: number;
  carriedFromShiftInstanceId: string | null;
  createdAtUtc: string;
  concurrencyToken: number;
}>;

export type ShiftClosingPreviewApi = Readonly<{
  shiftInstanceId: string;
  previewOnly: boolean;
  readinessScore: number;
  nextShiftMustKnow: readonly string[];
  candidates: readonly ShiftOpenItemApiSummary[];
  existingOpenItems: readonly ShiftOpenItemApiSummary[];
  conflicts: readonly { code: string; message: string }[];
}>;

export type ShiftMetricsApi = Readonly<{
  openItems: number;
  criticalItems: number;
  carriedOverItems: number;
  digitalWorkforceCases: number;
  pendingApprovals: number;
  handoverReadinessScore: number;
  verifiedHandovers: number;
  repeatedCarryoverItems: number;
}>;

async function shiftApiFetch<T>(
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
  let json: unknown = null;
  try {
    json = text ? JSON.parse(text) : null;
  } catch {
    json = text;
  }

  if (!response.ok) {
    const body = json as { code?: string; message?: string; title?: string } | null;
    return {
      status: "failure",
      code: body?.code ?? `http-${response.status}`,
      message: body?.message ?? body?.title ?? "Vardiya API isteği başarısız.",
    };
  }

  return { status: "success", value: json as T };
}

export async function fetchCurrentShift(context: ProductionOrderRequestContext) {
  return shiftApiFetch<ShiftInstanceApiSummary>(context, "/api/shifts/current");
}

export async function fetchShiftHistory(context: ProductionOrderRequestContext, limit = 30) {
  return shiftApiFetch<{ items: ShiftInstanceApiSummary[] }>(
    context,
    `/api/shifts/history?limit=${limit}`,
  );
}

export async function fetchShiftOpenItems(context: ProductionOrderRequestContext, shiftId: string) {
  return shiftApiFetch<{ items: ShiftOpenItemApiSummary[] }>(
    context,
    `/api/shifts/${shiftId}/open-items`,
  );
}

export async function fetchShiftClosingPreview(context: ProductionOrderRequestContext, shiftId: string) {
  return shiftApiFetch<ShiftClosingPreviewApi>(
    context,
    `/api/shifts/${shiftId}/closing-preview`,
  );
}

export async function fetchShiftMetrics(context: ProductionOrderRequestContext, shiftId: string) {
  return shiftApiFetch<ShiftMetricsApi>(context, `/api/shifts/${shiftId}/metrics`);
}
