import type {
  QualityNcrFollowUpFinding,
  QualityNcrFollowUpWorkspacePayload,
} from "../domain/quality-ncr-follow-up";
import type { ProductionOrderRequestContext } from "../../production-execution/server/production-order-request-context";
import type { MaintenanceApiResult } from "../../maintenance/server/maintenance-api-client";
import {
  parseQualityNcrFollowUpFinding,
  parseQualityNcrFollowUpTaskDraftResponse,
  parseQualityNcrFollowUpWorkspacePayload,
} from "./quality-ncr-follow-up-api-contracts";

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
        message: `Kalite takip isteği başarısız (${response.status}).`,
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
      message: "Kalite takip API'si kullanılamıyor.",
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
        message: payload?.title ?? payload?.message ?? `Kalite takip isteği başarısız (${response.status}).`,
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
      message: "Kalite takip API'si kullanılamıyor.",
      retryable: true,
    };
  }
}

export function getQualityNcrFollowUpWorkspace(
  context: ProductionOrderRequestContext,
): Promise<MaintenanceApiResult<QualityNcrFollowUpWorkspacePayload>> {
  return getJson(context, "/api/quality/ncr-follow-up/workspace", parseQualityNcrFollowUpWorkspacePayload);
}

export function getQualityNcrFollowUpDetail(
  context: ProductionOrderRequestContext,
  ncrId: string,
): Promise<MaintenanceApiResult<QualityNcrFollowUpFinding>> {
  return getJson(
    context,
    `/api/quality/ncr-follow-up/${encodeURIComponent(ncrId)}`,
    parseQualityNcrFollowUpFinding,
  );
}

export function createQualityNcrFollowUpTaskDraft(
  context: ProductionOrderRequestContext,
  ncrId: string,
): Promise<MaintenanceApiResult<Readonly<{
  taskId: string;
  ncrId: string;
  idempotentReplay: boolean;
  finding: QualityNcrFollowUpFinding;
}>>> {
  return postJson(
    context,
    `/api/quality/ncr-follow-up/${encodeURIComponent(ncrId)}/task-draft`,
    {},
    parseQualityNcrFollowUpTaskDraftResponse,
  );
}

export function transitionQualityNcrFollowUp(
  context: ProductionOrderRequestContext,
  ncrId: string,
  input: Readonly<{ expectedVersion: number; targetFollowUpStatus: string; note?: string }>,
): Promise<MaintenanceApiResult<QualityNcrFollowUpFinding | null>> {
  return postJson(
    context,
    `/api/quality/ncr-follow-up/${encodeURIComponent(ncrId)}/follow-up`,
    input,
    (payload) => {
      const data = payload as { finding?: unknown };
      if (!data.finding) return { status: "success", value: null };
      return parseQualityNcrFollowUpFinding(data.finding);
    },
  );
}
