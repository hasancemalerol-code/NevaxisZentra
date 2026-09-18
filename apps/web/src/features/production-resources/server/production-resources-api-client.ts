import "server-only";

import type { ProductionOrderRequestContext } from "@/features/production-execution/server/production-order-request-context";

export type ProductionResourceRowApi = Readonly<{
  resourceId: string;
  code: string;
  name: string;
  resourceType: string;
  machineRegistryCode?: string;
  stationId: string;
  stationCode: string;
  stationName: string;
  stationKind: string;
  lineId: string;
  lineCode: string;
  lineName: string;
  areaCode: string;
  state: string;
  operationCode?: string;
  operatorCode?: string;
  orderReference?: string;
  goodCount: number;
  rejectCount: number;
  bufferQuantity?: number;
  bufferCapacity?: number;
  isStarved: boolean;
  isBlocked: boolean;
}>;

export type ProductionResourceStatusSummaryApi = Readonly<{
  total: number;
  running: number;
  idle: number;
  waiting: number;
  stopped: number;
  fault: number;
  atRisk: number;
}>;

export type ProductionResourceWorkspaceApi = Readonly<{
  resources: readonly ProductionResourceRowApi[];
  status: ProductionResourceStatusSummaryApi;
}>;

async function productionResourcesFetch<T>(
  context: ProductionOrderRequestContext,
  path: string,
): Promise<{ status: "success"; value: T } | { status: "failure"; code: string; message: string }> {
  const url = new URL(`${context.apiBaseUrl}${path}`);
  url.searchParams.set("facilityId", context.facilityId);

  const response = await fetch(url, {
    headers: {
      Accept: "application/json",
      ...context.requestHeaders,
    },
    cache: "no-store",
  });

  if (!response.ok) {
    const body = (await response.json().catch(() => ({}))) as { message?: string; code?: string };
    return {
      status: "failure",
      code: body.code ?? `http-${response.status}`,
      message: body.message ?? `Production resources API failed (${response.status})`,
    };
  }

  return { status: "success", value: (await response.json()) as T };
}

export async function readProductionResourceWorkspace(
  context: ProductionOrderRequestContext,
): Promise<
  { status: "success"; value: ProductionResourceWorkspaceApi } | { status: "failure"; code: string; message: string }
> {
  return productionResourcesFetch<ProductionResourceWorkspaceApi>(
    context,
    "/api/mixed-production/resources/workspace",
  );
}
