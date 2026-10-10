import { classifyFailedResponse, failureForRefusal } from "@/lib/api/api-failure-classifier";
import "../../erp/server/erp-server-boundary";

import type { ProductionOrderRequestContext } from "../../production-execution/server/production-order-request-context";
import type {
  ProcurementDeliveryRiskDetail,
  ProcurementDeliveryRiskEvaluateResult,
  ProcurementDeliveryRiskHistoryEntry,
  ProcurementDeliveryRiskTaskDraftResult,
  ProcurementDeliveryRiskWorkspacePayload,
} from "../domain/procurement-delivery-risk";
import {
  parseProcurementDeliveryRiskDetail,
  parseProcurementDeliveryRiskEvaluateResponse,
  parseProcurementDeliveryRiskHistory,
  parseProcurementDeliveryRiskWorkspacePayload,
} from "./procurement-delivery-risk-api-contracts";

export type ProcurementDeliveryRiskListFilters = Readonly<{
  supplierReference?: string;
  status?: string;
  purchaseOrderReference?: string;
  sourceFreshness?: string;
  page?: number;
  pageSize?: number;
}>;

export type ProcurementApiResult<T> =
  | Readonly<{ status: "success"; value: T }>
  | Readonly<{ status: "failure"; code?: string; message: string; retryable?: boolean }>;

async function getJson<T>(
  context: ProductionOrderRequestContext,
  path: string,
  parser: (payload: unknown) => { status: "success"; value: T } | { status: "failure"; message: string },
  query?: Record<string, string | number | undefined>,
): Promise<ProcurementApiResult<T>> {
  try {
    const url = new URL(`${context.apiBaseUrl}${path}`);
    url.searchParams.set("facilityId", context.facilityId);
    if (query) {
      for (const [key, value] of Object.entries(query)) {
        if (value !== undefined && value !== "") {
          url.searchParams.set(key, String(value));
        }
      }
    }
    const response = await fetch(url.toString(), {
      headers: context.requestHeaders,
      cache: "no-store",
    });

    if (response.status === 401) {
      return { status: "failure", code: "unauthenticated", message: "Kimlik doğrulama gerekli.", retryable: false };
    }
    if (response.status === 403) {
      const refusal = failureForRefusal(await classifyFailedResponse(response), "Bu işlem için yetkiniz yok.");
      return { ...refusal, code: refusal.code === "permission-denied" ? "forbidden" : refusal.code };
    }
    if (!response.ok) {
      return { status: "failure", message: `API isteği başarısız (${response.status}).`, retryable: response.status >= 500 };
    }

    const payload = await response.json();
    const parsed = parser(payload);
    if (parsed.status === "failure") {
      return { status: "failure", message: parsed.message };
    }
    return { status: "success", value: parsed.value };
  } catch (error) {
    return {
      status: "failure",
      message: error instanceof Error ? error.message : "Beklenmeyen ağ hatası.",
      retryable: true,
    };
  }
}

export async function getProcurementDeliveryRiskWorkspace(
  context: ProductionOrderRequestContext,
  filters: ProcurementDeliveryRiskListFilters = {},
): Promise<ProcurementApiResult<ProcurementDeliveryRiskWorkspacePayload>> {
  return getJson(context, "/api/procurement/delivery-risks/workspace", parseProcurementDeliveryRiskWorkspacePayload, filters);
}

export async function getProcurementDeliveryRiskDetail(
  context: ProductionOrderRequestContext,
  assessmentId: string,
): Promise<ProcurementApiResult<ProcurementDeliveryRiskDetail>> {
  return getJson(context, `/api/procurement/delivery-risks/${assessmentId}`, parseProcurementDeliveryRiskDetail);
}

export async function getProcurementDeliveryRiskHistory(
  context: ProductionOrderRequestContext,
  assessmentId: string,
): Promise<ProcurementApiResult<readonly ProcurementDeliveryRiskHistoryEntry[]>> {
  return getJson(context, `/api/procurement/delivery-risks/${assessmentId}/history`, parseProcurementDeliveryRiskHistory);
}

export async function evaluateProcurementDeliveryRisk(
  context: ProductionOrderRequestContext,
  body: Readonly<{ purchaseOrderLineId: string; idempotencyKey?: string }>,
): Promise<ProcurementApiResult<ProcurementDeliveryRiskEvaluateResult>> {
  try {
    const url = new URL(`${context.apiBaseUrl}/api/procurement/delivery-risks/evaluate`);
    url.searchParams.set("facilityId", context.facilityId);
    const response = await fetch(url.toString(), {
      method: "POST",
      headers: { ...context.requestHeaders, "Content-Type": "application/json" },
      body: JSON.stringify(body),
      cache: "no-store",
    });

    if (response.status === 401) {
      return { status: "failure", code: "unauthenticated", message: "Kimlik doğrulama gerekli.", retryable: false };
    }
    if (response.status === 403) {
      const refusal = failureForRefusal(await classifyFailedResponse(response), "Değerlendirme yetkisi yok.");
      return { ...refusal, code: refusal.code === "permission-denied" ? "forbidden" : refusal.code };
    }
    if (response.status === 409) {
      return { status: "failure", code: "conflict", message: "Idempotency anahtarı uyumsuz.", retryable: false };
    }
    if (!response.ok) {
      return { status: "failure", message: `Değerlendirme başarısız (${response.status}).`, retryable: response.status >= 500 };
    }

    const payload = await response.json();
    const parsed = parseProcurementDeliveryRiskEvaluateResponse(payload);
    if (parsed.status === "failure") {
      return { status: "failure", message: parsed.message };
    }
    return { status: "success", value: parsed.value };
  } catch (error) {
    return {
      status: "failure",
      message: error instanceof Error ? error.message : "Beklenmeyen ağ hatası.",
      retryable: true,
    };
  }
}

function parseTaskDraftResponse(payload: unknown): ProcurementApiResult<ProcurementDeliveryRiskTaskDraftResult> {
  if (!payload || typeof payload !== "object") {
    return { status: "failure", message: "task draft response is invalid." };
  }
  const row = payload as Record<string, unknown>;
  if (typeof row.taskId !== "string" || typeof row.assessmentId !== "string") {
    return { status: "failure", message: "task draft response is invalid." };
  }
  return {
    status: "success",
    value: {
      taskId: row.taskId,
      assessmentId: row.assessmentId,
      idempotentReplay: row.idempotentReplay === true,
    },
  };
}

export async function createProcurementDeliveryRiskTaskDraft(
  context: ProductionOrderRequestContext,
  assessmentId: string,
): Promise<ProcurementApiResult<ProcurementDeliveryRiskTaskDraftResult>> {
  try {
    const url = new URL(`${context.apiBaseUrl}/api/procurement/delivery-risks/${assessmentId}/task-draft`);
    url.searchParams.set("facilityId", context.facilityId);
    const response = await fetch(url.toString(), {
      method: "POST",
      headers: context.requestHeaders,
      cache: "no-store",
    });

    if (response.status === 401) {
      return { status: "failure", code: "unauthenticated", message: "Kimlik doğrulama gerekli.", retryable: false };
    }
    if (response.status === 403) {
      const refusal = failureForRefusal(await classifyFailedResponse(response), "Görev taslağı oluşturma yetkisi yok.");
      return { ...refusal, code: refusal.code === "permission-denied" ? "forbidden" : refusal.code };
    }
    if (!response.ok) {
      return { status: "failure", message: `Görev taslağı oluşturma başarısız (${response.status}).`, retryable: response.status >= 500 };
    }

    const payload = await response.json();
    return parseTaskDraftResponse(payload);
  } catch (error) {
    return {
      status: "failure",
      message: error instanceof Error ? error.message : "Beklenmeyen ağ hatası.",
      retryable: true,
    };
  }
}

export async function transitionProcurementDeliveryRiskFollowUp(
  context: ProductionOrderRequestContext,
  assessmentId: string,
  body: Readonly<{ expectedVersion: number; targetFollowUpStatus: string; note?: string }>,
): Promise<ProcurementApiResult<void>> {
  try {
    const url = new URL(`${context.apiBaseUrl}/api/procurement/delivery-risks/${assessmentId}/follow-up`);
    url.searchParams.set("facilityId", context.facilityId);
    const response = await fetch(url.toString(), {
      method: "POST",
      headers: { ...context.requestHeaders, "Content-Type": "application/json" },
      body: JSON.stringify(body),
      cache: "no-store",
    });

    if (response.status === 401) {
      return { status: "failure", code: "unauthenticated", message: "Kimlik doğrulama gerekli.", retryable: false };
    }
    if (response.status === 403) {
      const refusal = failureForRefusal(await classifyFailedResponse(response), "Takip güncelleme yetkisi yok.");
      return { ...refusal, code: refusal.code === "permission-denied" ? "forbidden" : refusal.code };
    }
    if (response.status === 409) {
      return { status: "failure", code: "conflict", message: "Takip durumu geçişi uyumsuz.", retryable: false };
    }
    if (!response.ok) {
      return { status: "failure", message: `Takip güncelleme başarısız (${response.status}).`, retryable: response.status >= 500 };
    }

    return { status: "success", value: undefined };
  } catch (error) {
    return {
      status: "failure",
      message: error instanceof Error ? error.message : "Beklenmeyen ağ hatası.",
      retryable: true,
    };
  }
}
