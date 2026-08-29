import type {
  ProductionFollowUpDetail,
  ProductionFollowUpWorkspacePayload,
} from "../domain/production-follow-up";
import type { ProductionOrderRequestContext } from "./production-order-request-context";
import type { MaintenanceApiResult } from "../../maintenance/server/maintenance-api-client";
import {
  parseProductionFollowUpDetail,
  parseProductionFollowUpTaskDraftResponse,
  parseProductionFollowUpWorkspacePayload,
} from "./production-follow-up-api-contracts";

async function getJson<T>(
  context: ProductionOrderRequestContext,
  path: string,
  parser: (payload: unknown) => { status: "success"; value: T } | { status: "failure"; message: string },
): Promise<MaintenanceApiResult<T>> {
  try {
    const url = new URL(`${context.apiBaseUrl}${path}`);
    url.searchParams.set("facilityId", context.facilityId);
    const response = await fetch(url.toString(), {
      headers: context.requestHeaders,
      cache: "no-store",
    });

    if (response.status === 401) {
      return { status: "failure", code: "unauthenticated", message: "Kimlik doğrulama gerekli.", retryable: false };
    }
    if (response.status === 403) {
      return { status: "failure", code: "permission-denied", message: "Yetki reddedildi.", retryable: false };
    }
    if (!response.ok) {
      return {
        status: "failure",
        code: "source-unavailable",
        message: `Üretim takip isteği başarısız (${response.status}).`,
        retryable: response.status >= 500,
      };
    }

    const payload: unknown = await response.json();
    const parsed = parser(payload);
    if (parsed.status === "failure") {
      return { status: "failure", code: "invalid-response", message: parsed.message, retryable: false };
    }
    return { status: "success", value: parsed.value };
  } catch {
    return {
      status: "failure",
      code: "source-unavailable",
      message: "Üretim takip API'si kullanılamıyor.",
      retryable: true,
    };
  }
}

async function postJson<T>(
  context: ProductionOrderRequestContext,
  path: string,
  body: unknown,
  parser: (payload: unknown) => { status: "success"; value: T } | { status: "failure"; message: string },
): Promise<MaintenanceApiResult<T>> {
  try {
    const url = new URL(`${context.apiBaseUrl}${path}`);
    url.searchParams.set("facilityId", context.facilityId);
    const response = await fetch(url.toString(), {
      method: "POST",
      headers: { ...context.requestHeaders, "Content-Type": "application/json" },
      body: JSON.stringify(body ?? {}),
      cache: "no-store",
    });
    if (response.status === 401) {
      return { status: "failure", code: "unauthenticated", message: "Kimlik doğrulama gerekli.", retryable: false };
    }
    if (response.status === 403) {
      return { status: "failure", code: "permission-denied", message: "Yetki reddedildi.", retryable: false };
    }
    if (!response.ok) {
      const payload = await response.json().catch(() => null) as { message?: string; title?: string } | null;
      return {
        status: "failure",
        code: "source-unavailable",
        message: payload?.title ?? payload?.message ?? `Üretim takip isteği başarısız (${response.status}).`,
        retryable: response.status >= 500,
      };
    }
    const payload: unknown = await response.json();
    const parsed = parser(payload);
    if (parsed.status === "failure") {
      return { status: "failure", code: "invalid-response", message: parsed.message, retryable: false };
    }
    return { status: "success", value: parsed.value };
  } catch {
    return {
      status: "failure",
      code: "source-unavailable",
      message: "Üretim takip API'si kullanılamıyor.",
      retryable: true,
    };
  }
}

export function getProductionFollowUpWorkspace(
  context: ProductionOrderRequestContext,
): Promise<MaintenanceApiResult<ProductionFollowUpWorkspacePayload>> {
  return getJson(context, "/api/production/follow-up/workspace", parseProductionFollowUpWorkspacePayload);
}

export function getProductionFollowUpDetail(
  context: ProductionOrderRequestContext,
  exceptionId: string,
): Promise<MaintenanceApiResult<ProductionFollowUpDetail>> {
  return getJson(
    context,
    `/api/production/follow-up/${encodeURIComponent(exceptionId)}`,
    parseProductionFollowUpDetail,
  );
}

export function createProductionFollowUpTaskDraft(
  context: ProductionOrderRequestContext,
  exceptionId: string,
): Promise<MaintenanceApiResult<Readonly<{
  taskId: string;
  exceptionId: string;
  idempotentReplay: boolean;
  exception: ProductionFollowUpDetail;
}>>> {
  return postJson(
    context,
    `/api/production/follow-up/${encodeURIComponent(exceptionId)}/task-draft`,
    {},
    parseProductionFollowUpTaskDraftResponse,
  );
}

export function transitionProductionFollowUp(
  context: ProductionOrderRequestContext,
  exceptionId: string,
  input: Readonly<{ expectedVersion: number; targetFollowUpStatus: string; note?: string }>,
): Promise<MaintenanceApiResult<ProductionFollowUpDetail | null>> {
  return postJson(
    context,
    `/api/production/follow-up/${encodeURIComponent(exceptionId)}/follow-up`,
    input,
    (payload) => {
      const data = payload as { exception?: unknown };
      if (!data.exception) return { status: "success", value: null };
      return parseProductionFollowUpDetail(data.exception);
    },
  );
}
