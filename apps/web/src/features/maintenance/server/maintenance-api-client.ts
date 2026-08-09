import "./maintenance-server-boundary";

import type {
  MaintenanceRequestDetail,
  MaintenanceRequestSummary,
  MaintenanceWorkOrderDetail,
  MaintenanceWorkOrderSummary,
  MaintenanceWorkspaceSummary,
  PreventiveMaintenancePlanSummary,
} from "../domain/maintenance";
import {
  parseMaintenanceRequestDetailApiResponse,
  parseMaintenanceRequestListApiResponse,
  parseMaintenanceWorkOrderDetailApiResponse,
  parseMaintenanceWorkOrderListApiResponse,
  parseMaintenanceWorkspaceSummaryApiResponse,
  parsePreventiveMaintenancePlanListApiResponse,
} from "./maintenance-api-contracts";
import type { ProductionOrderRequestContext } from "../../production-execution/server/production-order-request-context";

export type MaintenanceApiFailure = Readonly<{
  status: "failure";
  code:
    | "source-unavailable"
    | "unauthenticated"
    | "permission-denied"
    | "configuration-error"
    | "invalid-request"
    | "invalid-response"
    | "concurrency-conflict";
  message: string;
  retryable: boolean;
}>;

export type MaintenanceApiResult<T> =
  | Readonly<{ status: "success"; value: T }>
  | MaintenanceApiFailure;

function buildUrl(
  context: ProductionOrderRequestContext,
  path: string,
  query?: Record<string, string | undefined>,
): string {
  const url = new URL(`${context.apiBaseUrl}${path}`);
  url.searchParams.set("facilityId", context.facilityId);
  if (query) {
    for (const [key, value] of Object.entries(query)) {
      if (value !== undefined && value !== "") {
        url.searchParams.set(key, value);
      }
    }
  }
  return url.toString();
}

async function getJsonWithQuery<T>(
  context: ProductionOrderRequestContext,
  path: string,
  query: Record<string, string | undefined> | undefined,
  parser: (payload: unknown) => T,
): Promise<MaintenanceApiResult<T>> {
  try {
    const response = await fetch(buildUrl(context, path, query), {
      headers: context.requestHeaders,
      cache: "no-store",
    });

    if (response.status === 401) {
      return {
        status: "failure",
        code: "unauthenticated",
        message: "Authentication required.",
        retryable: false,
      };
    }

    if (response.status === 403) {
      return {
        status: "failure",
        code: "permission-denied",
        message: "Authorization denied.",
        retryable: false,
      };
    }

    if (!response.ok) {
      return {
        status: "failure",
        code: "source-unavailable",
        message: `Maintenance request failed (${response.status}).`,
        retryable: response.status >= 500,
      };
    }

    const payload: unknown = await response.json();
    return { status: "success", value: parser(payload) };
  } catch {
    return {
      status: "failure",
      code: "source-unavailable",
      message: "Maintenance API is unavailable.",
      retryable: true,
    };
  }
}

async function getJson<T>(
  context: ProductionOrderRequestContext,
  path: string,
  parser: (payload: unknown) => T,
): Promise<MaintenanceApiResult<T>> {
  return getJsonWithQuery(context, path, undefined, parser);
}

export function getMaintenanceWorkspaceSummary(
  context: ProductionOrderRequestContext,
): Promise<MaintenanceApiResult<MaintenanceWorkspaceSummary>> {
  return getJson(
    context,
    "/api/maintenance/workspace-summary",
    parseMaintenanceWorkspaceSummaryApiResponse,
  );
}

export type MaintenanceRequestListFilters = Readonly<{
  status?: string;
}>;

export function listMaintenanceRequests(
  context: ProductionOrderRequestContext,
  filters?: MaintenanceRequestListFilters,
): Promise<MaintenanceApiResult<readonly MaintenanceRequestSummary[]>> {
  return getJsonWithQuery(
    context,
    "/api/maintenance/requests",
    { status: filters?.status },
    parseMaintenanceRequestListApiResponse,
  );
}

export function getMaintenanceRequest(
  context: ProductionOrderRequestContext,
  requestId: string,
): Promise<MaintenanceApiResult<MaintenanceRequestDetail>> {
  return getJson(
    context,
    `/api/maintenance/requests/${requestId}`,
    parseMaintenanceRequestDetailApiResponse,
  );
}

export type MaintenanceWorkOrderListFilters = Readonly<{
  status?: string;
  priority?: string;
}>;

export function listMaintenanceWorkOrders(
  context: ProductionOrderRequestContext,
  filters?: MaintenanceWorkOrderListFilters,
): Promise<MaintenanceApiResult<readonly MaintenanceWorkOrderSummary[]>> {
  return getJsonWithQuery(
    context,
    "/api/maintenance/work-orders",
    {
      status: filters?.status,
      priority: filters?.priority,
    },
    parseMaintenanceWorkOrderListApiResponse,
  );
}

export function getMaintenanceWorkOrder(
  context: ProductionOrderRequestContext,
  workOrderId: string,
): Promise<MaintenanceApiResult<MaintenanceWorkOrderDetail>> {
  return getJson(
    context,
    `/api/maintenance/work-orders/${workOrderId}`,
    parseMaintenanceWorkOrderDetailApiResponse,
  );
}

export function listPreventiveMaintenancePlans(
  context: ProductionOrderRequestContext,
): Promise<MaintenanceApiResult<readonly PreventiveMaintenancePlanSummary[]>> {
  return getJson(
    context,
    "/api/maintenance/preventive-plans",
    parsePreventiveMaintenancePlanListApiResponse,
  );
}
