import "../../materials/server/material-server-boundary";

import type {
  MaterialAvailabilityRiskDetail,
  MaterialAvailabilityRiskEvaluateResult,
  MaterialAvailabilityRiskHistoryEntry,
  MaterialAvailabilityRiskTaskDraftResult,
  MaterialAvailabilityRiskWorkspacePayload,
} from "../domain/material-availability-risk";
import type { ProductionOrderRequestContext } from "../../production-execution/server/production-order-request-context";
import type { MaterialApiResult } from "../../materials/server/material-api-client";
import {
  parseMaterialAvailabilityRiskDetail,
  parseMaterialAvailabilityRiskEvaluateResponse,
  parseMaterialAvailabilityRiskHistory,
  parseMaterialAvailabilityRiskTaskDraftResponse,
  parseMaterialAvailabilityRiskWorkspacePayload,
} from "./material-availability-risk-api-contracts";

export type MaterialAvailabilityRiskListFilters = Readonly<{
  materialId?: string;
  status?: string;
  page?: number;
  pageSize?: number;
}>;

async function getJson<T>(
  context: ProductionOrderRequestContext,
  path: string,
  parser: (payload: unknown) => { status: "success"; value: T } | { status: "failure"; message: string },
  query?: Record<string, string | number | undefined>,
): Promise<MaterialApiResult<T>> {
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
      return { status: "failure", code: "permission-denied", message: "Yetki reddedildi.", retryable: false };
    }
    if (response.status === 404) {
      return { status: "failure", code: "not-found", message: "Kayıt bulunamadı.", retryable: false };
    }
    if (!response.ok) {
      return {
        status: "failure",
        code: "source-unavailable",
        message: `Malzeme risk isteği başarısız (${response.status}).`,
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
      message: "Malzeme risk API'si kullanılamıyor.",
      retryable: true,
    };
  }
}

async function postJson<T>(
  context: ProductionOrderRequestContext,
  path: string,
  body: unknown,
  parser: (payload: unknown) => { status: "success"; value: T } | { status: "failure"; message: string },
): Promise<MaterialApiResult<T>> {
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
        message: payload?.title ?? payload?.message ?? `Malzeme risk isteği başarısız (${response.status}).`,
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
      message: "Malzeme risk API'si kullanılamıyor.",
      retryable: true,
    };
  }
}

export function getMaterialAvailabilityRiskWorkspace(
  context: ProductionOrderRequestContext,
  filters: MaterialAvailabilityRiskListFilters = {},
): Promise<MaterialApiResult<MaterialAvailabilityRiskWorkspacePayload>> {
  return getJson(
    context,
    "/api/materials/availability-risks/workspace",
    parseMaterialAvailabilityRiskWorkspacePayload,
    {
      materialId: filters.materialId,
      status: filters.status,
      page: filters.page,
      pageSize: filters.pageSize,
    },
  );
}

export function getMaterialAvailabilityRiskDetail(
  context: ProductionOrderRequestContext,
  assessmentId: string,
): Promise<MaterialApiResult<MaterialAvailabilityRiskDetail>> {
  return getJson(
    context,
    `/api/materials/availability-risks/${encodeURIComponent(assessmentId)}`,
    parseMaterialAvailabilityRiskDetail,
  );
}

export function getMaterialAvailabilityRiskHistory(
  context: ProductionOrderRequestContext,
  assessmentId: string,
  limit = 10,
): Promise<MaterialApiResult<readonly MaterialAvailabilityRiskHistoryEntry[]>> {
  return getJson(
    context,
    `/api/materials/availability-risks/${encodeURIComponent(assessmentId)}/history`,
    parseMaterialAvailabilityRiskHistory,
    { limit },
  );
}

export function evaluateMaterialAvailabilityRisk(
  context: ProductionOrderRequestContext,
  input: Readonly<{
    materialId: string;
    requirementProfileId: string;
    idempotencyKey: string;
  }>,
): Promise<MaterialApiResult<MaterialAvailabilityRiskEvaluateResult>> {
  return postJson(
    context,
    "/api/materials/availability-risks/evaluate",
    {
      materialId: input.materialId,
      requirementProfileId: input.requirementProfileId,
      idempotencyKey: input.idempotencyKey,
    },
    parseMaterialAvailabilityRiskEvaluateResponse,
  );
}

export function createMaterialAvailabilityRiskTaskDraft(
  context: ProductionOrderRequestContext,
  assessmentId: string,
): Promise<MaterialApiResult<MaterialAvailabilityRiskTaskDraftResult>> {
  return postJson(
    context,
    `/api/materials/availability-risks/${encodeURIComponent(assessmentId)}/task-draft`,
    {},
    parseMaterialAvailabilityRiskTaskDraftResponse,
  );
}

export function transitionMaterialAvailabilityRiskFollowUp(
  context: ProductionOrderRequestContext,
  assessmentId: string,
  input: Readonly<{ expectedVersion: number; targetFollowUpStatus: string; note?: string }>,
): Promise<MaterialApiResult<MaterialAvailabilityRiskDetail | null>> {
  return postJson(
    context,
    `/api/materials/availability-risks/${encodeURIComponent(assessmentId)}/follow-up`,
    input,
    (payload) => {
      const data = payload as { assessment?: unknown };
      if (!data.assessment) return { status: "success", value: null };
      return parseMaterialAvailabilityRiskDetail(data.assessment);
    },
  );
}
