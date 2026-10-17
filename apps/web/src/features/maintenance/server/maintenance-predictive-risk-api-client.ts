import { stripFixtureRecords } from "@/lib/runtime/fixture-data";
import { classifyFailedResponse, failureForRefusal } from "@/lib/api/api-failure-classifier";
import type {
  MaintenancePredictiveRiskAssessment,
  MaintenancePredictiveRiskWorkspace,
  MaintenancePredictiveRiskWorkspacePayload,
} from "../domain/maintenance-predictive-risk";
import type { ProductionOrderRequestContext } from "../../production-execution/server/production-order-request-context";
import type { MaintenanceApiResult } from "./maintenance-api-client";
import {
  parseMaintenancePredictiveRiskAssessment,
  parseMaintenancePredictiveRiskWorkspace,
  parseMaintenancePredictiveRiskWorkspacePayload,
} from "./maintenance-predictive-risk-api-contracts";

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
      return { status: "failure", code: "unauthenticated", message: "Authentication required.", retryable: false };
    }
    if (response.status === 403) {
      const refusal = await classifyFailedResponse(response);
      return failureForRefusal(refusal, "Authorization denied.");
    }
    if (!response.ok) {
      return {
        status: "failure",
        code: "source-unavailable",
        message: `Predictive risk request failed (${response.status}).`,
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
      message: "Predictive risk API is unavailable.",
      retryable: true,
    };
  }
}

export function getMaintenancePredictiveRiskWorkspace(
  context: ProductionOrderRequestContext,
): Promise<MaintenanceApiResult<MaintenancePredictiveRiskWorkspacePayload>> {
  return getJson(context, "/api/maintenance/predictive-risk/workspace", parseMaintenancePredictiveRiskWorkspacePayload);
}

export function getMaintenancePredictiveRiskOverview(
  context: ProductionOrderRequestContext,
): Promise<MaintenanceApiResult<MaintenancePredictiveRiskWorkspace>> {
  return getJson(context, "/api/maintenance/condition/risk/overview", parseMaintenancePredictiveRiskWorkspace);
}

export function getMaintenancePredictiveRiskAssessment(
  context: ProductionOrderRequestContext,
  assetReference: string,
): Promise<MaintenanceApiResult<MaintenancePredictiveRiskAssessment>> {
  return getJson(
    context,
    `/api/maintenance/assets/${encodeURIComponent(assetReference)}/predictive-risk`,
    parseMaintenancePredictiveRiskAssessment,
  );
}

async function postJson<T>(
  context: ProductionOrderRequestContext,
  path: string,
  body: unknown,
  parser: (payload: unknown) => T,
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
      return { status: "failure", code: "unauthenticated", message: "Authentication required.", retryable: false };
    }
    if (response.status === 403) {
      const refusal = await classifyFailedResponse(response);
      return failureForRefusal(refusal, "Authorization denied.");
    }
    if (!response.ok) {
      const payload = await response.json().catch(() => null) as { message?: string } | null;
      return {
        status: "failure",
        code: "source-unavailable",
        message: payload?.message ?? `Predictive risk request failed (${response.status}).`,
        retryable: response.status >= 500,
      };
    }
    const payload: unknown = await response.json();
    const guarded = stripFixtureRecords(parser(payload));
    if (guarded === null) {
      // A fixture record is not a customer record outside isolated environments.
      return { status: "failure", code: "not-found", message: "Kayıt bulunamadı.", retryable: false } as never;
    }
    return { status: "success", value: guarded };
  } catch {
    return {
      status: "failure",
      code: "source-unavailable",
      message: "Predictive risk API is unavailable.",
      retryable: true,
    };
  }
}

export function getMaintenancePredictiveRiskAssessmentById(
  context: ProductionOrderRequestContext,
  assessmentId: string,
): Promise<MaintenanceApiResult<MaintenancePredictiveRiskAssessment>> {
  return getJson(
    context,
    `/api/maintenance/predictive-risk/${encodeURIComponent(assessmentId)}`,
    parseMaintenancePredictiveRiskAssessment,
  );
}

export function createMaintenancePredictiveRiskTaskDraft(
  context: ProductionOrderRequestContext,
  assessmentId: string,
): Promise<MaintenanceApiResult<Readonly<{
  taskId: string;
  assessmentId: string;
  idempotentReplay: boolean;
  assessment: MaintenancePredictiveRiskAssessment;
}>>> {
  return postJson(
    context,
    `/api/maintenance/predictive-risk/${encodeURIComponent(assessmentId)}/task-draft`,
    {},
    (payload) => {
      const data = payload as {
        taskId: string;
        assessmentId: string;
        idempotentReplay?: boolean;
        assessment: unknown;
      };
      const parsed = parseMaintenancePredictiveRiskAssessment(data.assessment);
      if (parsed.status === "failure") {
        throw new Error(parsed.message);
      }
      return {
        taskId: data.taskId,
        assessmentId: data.assessmentId,
        idempotentReplay: data.idempotentReplay === true,
        assessment: parsed.value,
      };
    },
  );
}

export function transitionMaintenancePredictiveRiskFollowUp(
  context: ProductionOrderRequestContext,
  assessmentId: string,
  input: Readonly<{ targetFollowUpStatus: string; note?: string }>,
): Promise<MaintenanceApiResult<MaintenancePredictiveRiskAssessment | null>> {
  return postJson(
    context,
    `/api/maintenance/predictive-risk/${encodeURIComponent(assessmentId)}/follow-up`,
    input,
    (payload) => {
      const data = payload as { assessment?: unknown };
      if (!data.assessment) return null;
      const parsed = parseMaintenancePredictiveRiskAssessment(data.assessment);
      return parsed.status === "success" ? parsed.value : null;
    },
  );
}

export function evaluateMaintenancePredictiveRisk(
  context: ProductionOrderRequestContext,
): Promise<MaintenanceApiResult<{ evaluated: boolean }>> {
  return (async () => {
    try {
      const url = new URL(`${context.apiBaseUrl}/api/maintenance/condition/risk/evaluate`);
      url.searchParams.set("facilityId", context.facilityId);
      const response = await fetch(url.toString(), {
        method: "POST",
        headers: context.requestHeaders,
        cache: "no-store",
      });
      if (!response.ok) {
        return {
          status: "failure",
          code: "source-unavailable",
          message: `Predictive risk evaluation failed (${response.status}).`,
          retryable: response.status >= 500,
        };
      }
      const payload: unknown = await response.json();
      const body = payload as { evaluated?: boolean };
      return { status: "success", value: { evaluated: body.evaluated === true } };
    } catch {
      return {
        status: "failure",
        code: "source-unavailable",
        message: "Predictive risk evaluation is unavailable.",
        retryable: true,
      };
    }
  })();
}
