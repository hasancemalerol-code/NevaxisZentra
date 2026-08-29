import type {
  MoldFollowUpDetail,
  MoldFollowUpWorkspacePayload,
} from "../domain/mold-follow-up";
import type { ProductionOrderRequestContext } from "../../production-execution/server/production-order-request-context";
import type { MaintenanceApiResult } from "../../maintenance/server/maintenance-api-client";
import {
  parseMoldFollowUpDetail,
  parseMoldFollowUpTaskDraftResponse,
  parseMoldFollowUpWorkspacePayload,
} from "./mold-follow-up-api-contracts";

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
        message: `Kalıp takip isteği başarısız (${response.status}).`,
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
      message: "Kalıp takip API'si kullanılamıyor.",
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
        message: payload?.title ?? payload?.message ?? `Kalıp takip isteği başarısız (${response.status}).`,
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
      message: "Kalıp takip API'si kullanılamıyor.",
      retryable: true,
    };
  }
}

export function getMoldFollowUpWorkspace(
  context: ProductionOrderRequestContext,
): Promise<MaintenanceApiResult<MoldFollowUpWorkspacePayload>> {
  return getJson(context, "/api/molds/follow-up/workspace", parseMoldFollowUpWorkspacePayload);
}

export function getMoldFollowUpDetail(
  context: ProductionOrderRequestContext,
  moldId: string,
): Promise<MaintenanceApiResult<MoldFollowUpDetail>> {
  return getJson(
    context,
    `/api/molds/follow-up/${encodeURIComponent(moldId)}`,
    parseMoldFollowUpDetail,
  );
}

export function createMoldFollowUpTaskDraft(
  context: ProductionOrderRequestContext,
  moldId: string,
): Promise<MaintenanceApiResult<Readonly<{
  taskId: string;
  moldId: string;
  idempotentReplay: boolean;
  mold: MoldFollowUpDetail;
}>>> {
  return postJson(
    context,
    `/api/molds/follow-up/${encodeURIComponent(moldId)}/task-draft`,
    {},
    parseMoldFollowUpTaskDraftResponse,
  );
}

export function transitionMoldFollowUp(
  context: ProductionOrderRequestContext,
  moldId: string,
  input: Readonly<{ expectedVersion: number; targetFollowUpStatus: string; note?: string }>,
): Promise<MaintenanceApiResult<MoldFollowUpDetail | null>> {
  return postJson(
    context,
    `/api/molds/follow-up/${encodeURIComponent(moldId)}/follow-up`,
    input,
    (payload) => {
      const data = payload as { mold?: unknown };
      if (!data.mold) return { status: "success", value: null };
      return parseMoldFollowUpDetail(data.mold);
    },
  );
}
