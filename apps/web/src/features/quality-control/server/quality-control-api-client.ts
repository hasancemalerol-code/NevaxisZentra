import "./quality-control-server-boundary";

import type {
  ProductionOrderTraceability,
  ProductionQualitySignal,
  QualityControlPlanDetail,
  QualityControlPlanSummary,
  QualityCorrectiveActionAnalytics,
  QualityExportReadiness,
  QualityFailureBreakdownItem,
  QualityHoldAnalytics,
  QualityInspectionDetail,
  QualityInspectionInternalSummary,
  QualityInspectionSummary,
  QualityNcrAnalytics,
  QualityNcrDetail,
  QualityNcrSummary,
  QualityReleaseIntelligence,
  QualityReportingOverview,
  QualityReportingTrendPeriod,
  QualityTrendPeriod,
  QualityWorkspaceSummary,
} from "../domain/quality-control";
import {
  parseLinkedNonconformanceApiResponse,
  parseProductionOrderTraceabilityApiResponse,
  parseProductionQualitySignalApiResponse,
  parseQualityControlPlanDetailApiResponse,
  parseQualityControlPlanListApiResponse,
  parseQualityCorrectiveActionAnalyticsApiResponse,
  parseQualityExportReadinessApiResponse,
  parseQualityFailureBreakdownApiResponse,
  parseQualityHoldAnalyticsApiResponse,
  parseQualityInspectionDetailApiResponse,
  parseQualityInspectionInternalSummaryApiResponse,
  parseQualityInspectionListApiResponse,
  parseQualityInspectionReportListApiResponse,
  parseQualityNcrAnalyticsApiResponse,
  parseQualityNcrDetailApiResponse,
  parseQualityNcrListApiResponse,
  parseQualityReleaseIntelligenceApiResponse,
  parseQualityReportingOverviewApiResponse,
  parseQualityTrendListApiResponse,
  parseQualityWorkspaceSummaryApiResponse,
  parseQualityWriteApiResponse,
} from "./quality-control-api-contracts";
import type { ProductionOrderRequestContext } from "../../production-execution/server/production-order-request-context";

export type QualityApiFailure = Readonly<{
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

export type QualityApiResult<T> =
  | Readonly<{ status: "success"; value: T }>
  | QualityApiFailure;

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
): Promise<QualityApiResult<T>> {
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
        message: `Quality request failed (${response.status}).`,
        retryable: response.status >= 500,
      };
    }

    const payload: unknown = await response.json();
    return { status: "success", value: parser(payload) };
  } catch {
    return {
      status: "failure",
      code: "source-unavailable",
      message: "Quality API is unavailable.",
      retryable: true,
    };
  }
}

async function getJson<T>(
  context: ProductionOrderRequestContext,
  path: string,
  parser: (payload: unknown) => T,
): Promise<QualityApiResult<T>> {
  return getJsonWithQuery(context, path, undefined, parser);
}

async function postJson<T>(
  context: ProductionOrderRequestContext,
  path: string,
  body: unknown,
  parser: (payload: unknown) => T,
): Promise<QualityApiResult<T>> {
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
        message: `Quality write failed (${response.status}).`,
        retryable: response.status >= 500,
      };
    }

    const payload: unknown = await response.json();
    return { status: "success", value: parser(payload) };
  } catch {
    return {
      status: "failure",
      code: "source-unavailable",
      message: "Quality API is unavailable.",
      retryable: true,
    };
  }
}

async function putJson<T>(
  context: ProductionOrderRequestContext,
  path: string,
  body: unknown,
  parser: (payload: unknown) => T,
): Promise<QualityApiResult<T>> {
  try {
    const response = await fetch(buildUrl(context, path), {
      method: "PUT",
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
        message: `Quality write failed (${response.status}).`,
        retryable: response.status >= 500,
      };
    }

    const payload: unknown = await response.json();
    return { status: "success", value: parser(payload) };
  } catch {
    return {
      status: "failure",
      code: "source-unavailable",
      message: "Quality API is unavailable.",
      retryable: true,
    };
  }
}

export function getQualityWorkspaceSummary(
  context: ProductionOrderRequestContext,
): Promise<QualityApiResult<QualityWorkspaceSummary>> {
  return getJson(context, "/api/quality/workspace-summary", parseQualityWorkspaceSummaryApiResponse);
}

export function listQualityControlPlans(
  context: ProductionOrderRequestContext,
): Promise<QualityApiResult<readonly QualityControlPlanSummary[]>> {
  return getJson(context, "/api/quality/control-plans", parseQualityControlPlanListApiResponse);
}

export function getQualityControlPlan(
  context: ProductionOrderRequestContext,
  planId: string,
): Promise<QualityApiResult<QualityControlPlanDetail>> {
  return getJson(
    context,
    `/api/quality/control-plans/${planId}`,
    parseQualityControlPlanDetailApiResponse,
  );
}

export function listQualityInspections(
  context: ProductionOrderRequestContext,
): Promise<QualityApiResult<readonly QualityInspectionSummary[]>> {
  return getJson(context, "/api/quality/inspections", parseQualityInspectionListApiResponse);
}

export function getQualityInspection(
  context: ProductionOrderRequestContext,
  inspectionId: string,
): Promise<QualityApiResult<QualityInspectionDetail>> {
  return getJson(
    context,
    `/api/quality/inspections/${inspectionId}`,
    parseQualityInspectionDetailApiResponse,
  );
}

export function createQualityControlPlan(
  context: ProductionOrderRequestContext,
  body: unknown,
): Promise<QualityApiResult<{ id: string; version: number }>> {
  return postJson(context, "/api/quality/control-plans", body, parseQualityWriteApiResponse);
}

export function createQualityInspection(
  context: ProductionOrderRequestContext,
  body: unknown,
): Promise<QualityApiResult<{ id: string; version: number }>> {
  return postJson(context, "/api/quality/inspections", body, parseQualityWriteApiResponse);
}

export function startQualityInspection(
  context: ProductionOrderRequestContext,
  inspectionId: string,
  body: unknown,
): Promise<QualityApiResult<{ id: string; version: number }>> {
  return postJson(
    context,
    `/api/quality/inspections/${inspectionId}/start`,
    body,
    parseQualityWriteApiResponse,
  );
}

export function recordQualityInspectionResult(
  context: ProductionOrderRequestContext,
  inspectionId: string,
  body: unknown,
): Promise<QualityApiResult<{ id: string; version: number }>> {
  return postJson(
    context,
    `/api/quality/inspections/${inspectionId}/results`,
    body,
    parseQualityWriteApiResponse,
  );
}

export function completeQualityInspection(
  context: ProductionOrderRequestContext,
  inspectionId: string,
  body: unknown,
): Promise<QualityApiResult<{ id: string; version: number }>> {
  return postJson(
    context,
    `/api/quality/inspections/${inspectionId}/complete`,
    body,
    parseQualityWriteApiResponse,
  );
}

export function releaseQualityInspection(
  context: ProductionOrderRequestContext,
  inspectionId: string,
  body: unknown,
): Promise<QualityApiResult<{ id: string; version: number }>> {
  return postJson(
    context,
    `/api/quality/inspections/${inspectionId}/release`,
    body,
    parseQualityWriteApiResponse,
  );
}

export function rejectQualityInspection(
  context: ProductionOrderRequestContext,
  inspectionId: string,
  body: unknown,
): Promise<QualityApiResult<{ id: string; version: number }>> {
  return postJson(
    context,
    `/api/quality/inspections/${inspectionId}/reject`,
    body,
    parseQualityWriteApiResponse,
  );
}

export function updateQualityControlPlan(
  context: ProductionOrderRequestContext,
  planId: string,
  body: unknown,
): Promise<QualityApiResult<{ id: string; version: number }>> {
  return putJson(
    context,
    `/api/quality/control-plans/${planId}`,
    body,
    parseQualityWriteApiResponse,
  );
}

export type QualityNcrListFilters = Readonly<{
  status?: string;
  severity?: string;
  holdActive?: boolean;
  productionOrderId?: string;
}>;

export function listQualityNcrs(
  context: ProductionOrderRequestContext,
  filters?: QualityNcrListFilters,
): Promise<QualityApiResult<readonly QualityNcrSummary[]>> {
  return getJsonWithQuery(
    context,
    "/api/quality/ncrs",
    {
      status: filters?.status,
      severity: filters?.severity,
      holdActive:
        filters?.holdActive === undefined ? undefined : String(filters.holdActive),
      productionOrderId: filters?.productionOrderId,
    },
    parseQualityNcrListApiResponse,
  );
}

export function getQualityNcr(
  context: ProductionOrderRequestContext,
  ncrId: string,
): Promise<QualityApiResult<QualityNcrDetail>> {
  return getJson(context, `/api/quality/ncrs/${ncrId}`, parseQualityNcrDetailApiResponse);
}

export function createQualityNcr(
  context: ProductionOrderRequestContext,
  body: unknown,
): Promise<QualityApiResult<{ id: string; version: number }>> {
  return postJson(context, "/api/quality/ncrs", body, parseQualityWriteApiResponse);
}

export function updateQualityNcr(
  context: ProductionOrderRequestContext,
  ncrId: string,
  body: unknown,
): Promise<QualityApiResult<{ id: string; version: number }>> {
  return putJson(context, `/api/quality/ncrs/${ncrId}`, body, parseQualityWriteApiResponse);
}

export function resolveQualityNcr(
  context: ProductionOrderRequestContext,
  ncrId: string,
  body: unknown,
): Promise<QualityApiResult<{ id: string; version: number }>> {
  return postJson(
    context,
    `/api/quality/ncrs/${ncrId}/resolve`,
    body,
    parseQualityWriteApiResponse,
  );
}

export function closeQualityNcr(
  context: ProductionOrderRequestContext,
  ncrId: string,
  body: unknown,
): Promise<QualityApiResult<{ id: string; version: number }>> {
  return postJson(
    context,
    `/api/quality/ncrs/${ncrId}/close`,
    body,
    parseQualityWriteApiResponse,
  );
}

export function placeQualityHold(
  context: ProductionOrderRequestContext,
  ncrId: string,
  body: unknown,
): Promise<QualityApiResult<{ id: string; version: number }>> {
  return postJson(
    context,
    `/api/quality/ncrs/${ncrId}/hold`,
    body,
    parseQualityWriteApiResponse,
  );
}

export function releaseQualityHold(
  context: ProductionOrderRequestContext,
  ncrId: string,
  body: unknown,
): Promise<QualityApiResult<{ id: string; version: number }>> {
  return postJson(
    context,
    `/api/quality/ncrs/${ncrId}/hold/release`,
    body,
    parseQualityWriteApiResponse,
  );
}

export function decideQualityDisposition(
  context: ProductionOrderRequestContext,
  ncrId: string,
  body: unknown,
): Promise<QualityApiResult<{ id: string; version: number }>> {
  return postJson(
    context,
    `/api/quality/ncrs/${ncrId}/disposition`,
    body,
    parseQualityWriteApiResponse,
  );
}

export function createQualityCorrectiveAction(
  context: ProductionOrderRequestContext,
  ncrId: string,
  body: unknown,
): Promise<QualityApiResult<{ id: string; version: number }>> {
  return postJson(
    context,
    `/api/quality/ncrs/${ncrId}/corrective-actions`,
    body,
    parseQualityWriteApiResponse,
  );
}

export function startQualityCorrectiveAction(
  context: ProductionOrderRequestContext,
  actionId: string,
  body: unknown,
): Promise<QualityApiResult<{ id: string; version: number }>> {
  return postJson(
    context,
    `/api/quality/corrective-actions/${actionId}/start`,
    body,
    parseQualityWriteApiResponse,
  );
}

export function completeQualityCorrectiveAction(
  context: ProductionOrderRequestContext,
  actionId: string,
  body: unknown,
): Promise<QualityApiResult<{ id: string; version: number }>> {
  return postJson(
    context,
    `/api/quality/corrective-actions/${actionId}/complete`,
    body,
    parseQualityWriteApiResponse,
  );
}

export function getProductionQualitySignal(
  context: ProductionOrderRequestContext,
  productionOrderId: string,
): Promise<QualityApiResult<ProductionQualitySignal>> {
  return getJson(
    context,
    `/api/quality/production-orders/${productionOrderId}/signal`,
    parseProductionQualitySignalApiResponse,
  );
}

export function getLinkedNonconformanceForResult(
  context: ProductionOrderRequestContext,
  resultId: string,
): Promise<QualityApiResult<{ id: string; ncrNumber: string; status: string }>> {
  return getJson(
    context,
    `/api/quality/inspection-results/${resultId}/nonconformance`,
    parseLinkedNonconformanceApiResponse,
  );
}

export type QualityReportingFilters = Readonly<{
  dateFrom?: string;
  dateTo?: string;
  productReference?: string;
  productionOrderId?: string;
  controlPlanId?: string;
  overallResult?: string;
  releaseStatus?: string;
  ncrSeverity?: string;
  ncrStatus?: string;
  limit?: number;
  offset?: number;
}>;

function toReportingQuery(
  filters?: QualityReportingFilters,
): Record<string, string | undefined> {
  return {
    dateFrom: filters?.dateFrom,
    dateTo: filters?.dateTo,
    productReference: filters?.productReference,
    productionOrderId: filters?.productionOrderId,
    controlPlanId: filters?.controlPlanId,
    overallResult: filters?.overallResult,
    releaseStatus: filters?.releaseStatus,
    ncrSeverity: filters?.ncrSeverity,
    ncrStatus: filters?.ncrStatus,
    limit: filters?.limit === undefined ? undefined : String(filters.limit),
    offset: filters?.offset === undefined ? undefined : String(filters.offset),
  };
}

export function getQualityReportingOverview(
  context: ProductionOrderRequestContext,
  filters?: QualityReportingFilters,
): Promise<QualityApiResult<QualityReportingOverview>> {
  return getJsonWithQuery(
    context,
    "/api/quality/reporting/overview",
    toReportingQuery(filters),
    parseQualityReportingOverviewApiResponse,
  );
}

export function listQualityReportingInspections(
  context: ProductionOrderRequestContext,
  filters?: QualityReportingFilters,
): Promise<QualityApiResult<readonly QualityInspectionInternalSummary[]>> {
  return getJsonWithQuery(
    context,
    "/api/quality/reporting/inspections",
    toReportingQuery(filters),
    parseQualityInspectionReportListApiResponse,
  );
}

export function getQualityNcrAnalytics(
  context: ProductionOrderRequestContext,
  filters?: QualityReportingFilters,
): Promise<QualityApiResult<QualityNcrAnalytics>> {
  return getJsonWithQuery(
    context,
    "/api/quality/reporting/ncrs",
    toReportingQuery(filters),
    parseQualityNcrAnalyticsApiResponse,
  );
}

export function getQualityHoldAnalytics(
  context: ProductionOrderRequestContext,
  filters?: QualityReportingFilters,
): Promise<QualityApiResult<QualityHoldAnalytics>> {
  return getJsonWithQuery(
    context,
    "/api/quality/reporting/holds",
    toReportingQuery(filters),
    parseQualityHoldAnalyticsApiResponse,
  );
}

export function getQualityCorrectiveActionAnalytics(
  context: ProductionOrderRequestContext,
  filters?: QualityReportingFilters,
): Promise<QualityApiResult<QualityCorrectiveActionAnalytics>> {
  return getJsonWithQuery(
    context,
    "/api/quality/reporting/corrective-actions",
    toReportingQuery(filters),
    parseQualityCorrectiveActionAnalyticsApiResponse,
  );
}

export function listQualityReportingTrends(
  context: ProductionOrderRequestContext,
  period: QualityReportingTrendPeriod,
  filters?: QualityReportingFilters,
): Promise<QualityApiResult<readonly QualityTrendPeriod[]>> {
  return getJsonWithQuery(
    context,
    "/api/quality/reporting/trends",
    { ...toReportingQuery(filters), period },
    parseQualityTrendListApiResponse,
  );
}

export function listQualityFailureBreakdown(
  context: ProductionOrderRequestContext,
  filters?: QualityReportingFilters,
): Promise<QualityApiResult<readonly QualityFailureBreakdownItem[]>> {
  return getJsonWithQuery(
    context,
    "/api/quality/reporting/failures",
    toReportingQuery(filters),
    parseQualityFailureBreakdownApiResponse,
  );
}

export function getProductionOrderTraceability(
  context: ProductionOrderRequestContext,
  productionOrderId: string,
): Promise<QualityApiResult<ProductionOrderTraceability>> {
  return getJson(
    context,
    `/api/quality/traceability/production-orders/${productionOrderId}`,
    parseProductionOrderTraceabilityApiResponse,
  );
}

export function getQualityInspectionInternalSummary(
  context: ProductionOrderRequestContext,
  inspectionId: string,
): Promise<QualityApiResult<QualityInspectionInternalSummary>> {
  return getJson(
    context,
    `/api/quality/inspections/${inspectionId}/summary`,
    parseQualityInspectionInternalSummaryApiResponse,
  );
}

export function getQualityReleaseIntelligence(
  context: ProductionOrderRequestContext,
  inspectionId: string,
): Promise<QualityApiResult<QualityReleaseIntelligence>> {
  return getJson(
    context,
    `/api/quality/inspections/${inspectionId}/release-intelligence`,
    parseQualityReleaseIntelligenceApiResponse,
  );
}

export function getProductionOrderExportReadiness(
  context: ProductionOrderRequestContext,
  productionOrderId: string,
): Promise<QualityApiResult<QualityExportReadiness>> {
  return getJson(
    context,
    `/api/quality/production-orders/${productionOrderId}/export-readiness`,
    parseQualityExportReadinessApiResponse,
  );
}
