import "./quality-control-server-boundary";

import type {
  QualityControlPlanDetail,
  QualityControlPlanSummary,
  QualityInspectionDetail,
  QualityInspectionSummary,
  QualityWorkspaceSummary,
} from "../domain/quality-control";
import {
  parseQualityControlPlanDetailApiResponse,
  parseQualityControlPlanListApiResponse,
  parseQualityInspectionDetailApiResponse,
  parseQualityInspectionListApiResponse,
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
): string {
  const url = new URL(`${context.apiBaseUrl}${path}`);
  url.searchParams.set("facilityId", context.facilityId);
  return url.toString();
}

async function getJson<T>(
  context: ProductionOrderRequestContext,
  path: string,
  parser: (payload: unknown) => T,
): Promise<QualityApiResult<T>> {
  try {
    const response = await fetch(buildUrl(context, path), {
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
