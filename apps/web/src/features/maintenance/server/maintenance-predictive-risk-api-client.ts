import type {
  MaintenancePredictiveRiskAssessment,
  MaintenancePredictiveRiskWorkspace,
} from "../domain/maintenance-predictive-risk";
import type { ProductionOrderRequestContext } from "../../production-execution/server/production-order-request-context";
import type { MaintenanceApiResult } from "./maintenance-api-client";
import {
  parseMaintenancePredictiveRiskAssessment,
  parseMaintenancePredictiveRiskWorkspace,
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
      return { status: "failure", code: "permission-denied", message: "Authorization denied.", retryable: false };
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
