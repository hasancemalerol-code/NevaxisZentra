import "./maintenance-server-boundary";

import type {
  MaintenanceAssetHistory,
  MaintenanceAssetReliability,
  MaintenanceCostSummary,
  MaintenanceFailureBreakdownRow,
  MaintenanceReliabilityOverview,
  MaintenanceReliabilityReport,
  MaintenanceRequestDetail,
  MaintenanceRequestSummary,
  MaintenancePartUsage,
  MaintenanceTimelineEntry,
  MaintenanceWorkNote,
  MaintenanceWorkOrderDetail,
  MaintenanceWorkOrderSummary,
  MaintenanceWorkspaceSummary,
  PreventiveMaintenanceDueDetail,
  PreventiveMaintenanceDueSummary,
  PreventiveMaintenancePlanSummary,
} from "../domain/maintenance";
import {
  parseMaintenanceAssetHistoryApiResponse,
  parseMaintenanceAssetReliabilityApiResponse,
  parseMaintenanceCostSummaryApiResponse,
  parseMaintenanceFailureBreakdownApiResponse,
  parseMaintenancePartUsageListApiResponse,
  parseMaintenanceReliabilityOverviewApiResponse,
  parseMaintenanceReliabilityReportApiResponse,
  parseMaintenanceRequestDetailApiResponse,
  parseMaintenanceRequestListApiResponse,
  parseMaintenanceTimelineApiResponse,
  parseMaintenanceWorkNoteListApiResponse,
  parseMaintenanceWorkOrderDetailApiResponse,
  parseMaintenanceWorkOrderListApiResponse,
  parseMaintenanceWorkspaceSummaryApiResponse,
  parseMaintenanceWriteApiResponse,
  parsePreventiveMaintenanceDueDetailApiResponse,
  parsePreventiveMaintenanceDueListApiResponse,
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
    | "concurrency-conflict"
    | "not-found";
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

async function postJson<T>(
  context: ProductionOrderRequestContext,
  path: string,
  body: unknown,
  parser: (payload: unknown) => T,
): Promise<MaintenanceApiResult<T>> {
  try {
    const response = await fetch(buildUrl(context, path), {
      method: "POST",
      headers: context.requestHeaders,
      body: JSON.stringify(body),
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

    if (response.status === 409) {
      return {
        status: "failure",
        code: "concurrency-conflict",
        message: "Concurrent modification detected.",
        retryable: false,
      };
    }

    if (!response.ok) {
      return {
        status: "failure",
        code: "source-unavailable",
        message: `Maintenance write failed (${response.status}).`,
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

export function listMaintenanceWorkNotes(
  context: ProductionOrderRequestContext,
  workOrderId: string,
): Promise<MaintenanceApiResult<readonly MaintenanceWorkNote[]>> {
  return getJson(
    context,
    `/api/maintenance/work-orders/${workOrderId}/notes`,
    parseMaintenanceWorkNoteListApiResponse,
  );
}

export function listMaintenancePartUsages(
  context: ProductionOrderRequestContext,
  workOrderId: string,
): Promise<MaintenanceApiResult<readonly MaintenancePartUsage[]>> {
  return getJson(
    context,
    `/api/maintenance/work-orders/${workOrderId}/parts`,
    parseMaintenancePartUsageListApiResponse,
  );
}

export function getMaintenanceWorkOrderTimeline(
  context: ProductionOrderRequestContext,
  workOrderId: string,
): Promise<MaintenanceApiResult<readonly MaintenanceTimelineEntry[]>> {
  return getJson(
    context,
    `/api/maintenance/work-orders/${workOrderId}/timeline`,
    parseMaintenanceTimelineApiResponse,
  );
}

export function getMaintenanceWorkOrderCostSummary(
  context: ProductionOrderRequestContext,
  workOrderId: string,
): Promise<MaintenanceApiResult<MaintenanceCostSummary>> {
  return getJson(
    context,
    `/api/maintenance/work-orders/${workOrderId}/cost-summary`,
    parseMaintenanceCostSummaryApiResponse,
  );
}

export type PreventiveMaintenanceDueListFilters = Readonly<{
  status?: string;
}>;

export function listPreventiveMaintenanceDues(
  context: ProductionOrderRequestContext,
  filters?: PreventiveMaintenanceDueListFilters,
): Promise<MaintenanceApiResult<readonly PreventiveMaintenanceDueSummary[]>> {
  return getJsonWithQuery(
    context,
    "/api/maintenance/preventive-dues",
    { status: filters?.status },
    parsePreventiveMaintenanceDueListApiResponse,
  );
}

export function getPreventiveMaintenanceDue(
  context: ProductionOrderRequestContext,
  dueId: string,
): Promise<MaintenanceApiResult<PreventiveMaintenanceDueDetail>> {
  return getJson(
    context,
    `/api/maintenance/preventive-dues/${dueId}`,
    parsePreventiveMaintenanceDueDetailApiResponse,
  );
}

export function getMaintenanceAssetHistory(
  context: ProductionOrderRequestContext,
  assetReference: string,
): Promise<MaintenanceApiResult<MaintenanceAssetHistory>> {
  return getJson(
    context,
    `/api/maintenance/assets/${encodeURIComponent(assetReference)}/history`,
    parseMaintenanceAssetHistoryApiResponse,
  );
}

export function createMaintenanceWorkNote(
  context: ProductionOrderRequestContext,
  workOrderId: string,
  body: Readonly<{
    noteType: string;
    text: string;
    isImportant: boolean;
  }>,
): Promise<MaintenanceApiResult<Readonly<{ id: string; version: number }>>> {
  return postJson(
    context,
    `/api/maintenance/work-orders/${workOrderId}/notes`,
    body,
    parseMaintenanceWriteApiResponse,
  );
}

export function createMaintenancePartUsage(
  context: ProductionOrderRequestContext,
  workOrderId: string,
  body: Readonly<{
    partReference: string;
    partName?: string;
    quantity: number;
    unit: string;
    sourceSystem?: string;
    sourceReference?: string;
    estimatedUnitCost?: number;
    actualUnitCost?: number;
  }>,
): Promise<MaintenanceApiResult<Readonly<{ id: string; version: number }>>> {
  return postJson(
    context,
    `/api/maintenance/work-orders/${workOrderId}/parts`,
    body,
    parseMaintenanceWriteApiResponse,
  );
}

export function generatePreventiveNextDue(
  context: ProductionOrderRequestContext,
  planId: string,
): Promise<MaintenanceApiResult<Readonly<{ id: string; version: number }>>> {
  return postJson(
    context,
    `/api/maintenance/preventive-plans/${planId}/generate-next-due`,
    {},
    parseMaintenanceWriteApiResponse,
  );
}

export function createWorkOrderFromPreventiveDue(
  context: ProductionOrderRequestContext,
  dueId: string,
  body: Readonly<{ expectedVersion: number }>,
): Promise<MaintenanceApiResult<Readonly<{ id: string; version: number }>>> {
  return postJson(
    context,
    `/api/maintenance/preventive-dues/${dueId}/create-work-order`,
    body,
    parseMaintenanceWriteApiResponse,
  );
}

export type MaintenanceReportingDateRange = Readonly<{
  dateFromUtc: string;
  dateToUtc: string;
}>;

export function getMaintenanceReliabilityOverview(
  context: ProductionOrderRequestContext,
  range: MaintenanceReportingDateRange,
): Promise<MaintenanceApiResult<MaintenanceReliabilityOverview>> {
  return getJsonWithQuery(
    context,
    "/api/maintenance/reporting/overview",
    {
      dateFrom: range.dateFromUtc,
      dateTo: range.dateToUtc,
    },
    parseMaintenanceReliabilityOverviewApiResponse,
  );
}

export function getMaintenanceReliabilityReport(
  context: ProductionOrderRequestContext,
  range: MaintenanceReportingDateRange,
): Promise<MaintenanceApiResult<MaintenanceReliabilityReport>> {
  return getJsonWithQuery(
    context,
    "/api/maintenance/reporting/reliability",
    {
      dateFrom: range.dateFromUtc,
      dateTo: range.dateToUtc,
    },
    parseMaintenanceReliabilityReportApiResponse,
  );
}

export function getMaintenanceFailureBreakdown(
  context: ProductionOrderRequestContext,
  range: MaintenanceReportingDateRange,
  groupBy = "failure-category",
): Promise<MaintenanceApiResult<readonly MaintenanceFailureBreakdownRow[]>> {
  return getJsonWithQuery(
    context,
    "/api/maintenance/reporting/failures",
    {
      dateFrom: range.dateFromUtc,
      dateTo: range.dateToUtc,
      groupBy,
    },
    parseMaintenanceFailureBreakdownApiResponse,
  );
}

export function getMaintenanceAssetReliability(
  context: ProductionOrderRequestContext,
  assetReference: string,
): Promise<MaintenanceApiResult<MaintenanceAssetReliability>> {
  return getJson(
    context,
    `/api/maintenance/assets/${encodeURIComponent(assetReference)}/reliability`,
    parseMaintenanceAssetReliabilityApiResponse,
  );
}
