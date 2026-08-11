import type { MaintenanceConditionProfile, MaintenanceConditionWorkspace } from "../domain/maintenance-condition";
import type { ProductionOrderRequestContext } from "../../production-execution/server/production-order-request-context";
import type { MaintenanceApiResult } from "./maintenance-api-client";
import {
  parseMaintenanceConditionAnomalyList,
  parseMaintenanceConditionProfile,
  parseMaintenanceConditionWorkspace,
} from "./maintenance-condition-api-contracts";

async function getJson<T>(
  context: ProductionOrderRequestContext,
  path: string,
  parser: (payload: unknown) => T,
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
        message: `Maintenance condition request failed (${response.status}).`,
        retryable: response.status >= 500,
      };
    }

    const payload: unknown = await response.json();
    return { status: "success", value: parser(payload) };
  } catch {
    return {
      status: "failure",
      code: "source-unavailable",
      message: "Maintenance condition API is unavailable.",
      retryable: true,
    };
  }
}

export function getMaintenanceConditionOverview(
  context: ProductionOrderRequestContext,
): Promise<MaintenanceApiResult<MaintenanceConditionWorkspace>> {
  return getJson(context, "/api/maintenance/condition/overview", parseMaintenanceConditionWorkspace);
}

export function getMaintenanceConditionProfile(
  context: ProductionOrderRequestContext,
  assetReference: string,
): Promise<MaintenanceApiResult<MaintenanceConditionProfile>> {
  return getJson(
    context,
    `/api/maintenance/assets/${encodeURIComponent(assetReference)}/condition-intelligence`,
    parseMaintenanceConditionProfile,
  );
}

export function evaluateMaintenanceCondition(
  context: ProductionOrderRequestContext,
): Promise<MaintenanceApiResult<{ evaluated: boolean }>> {
  return (async () => {
    try {
      const url = new URL(`${context.apiBaseUrl}/api/maintenance/condition/evaluate`);
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
          message: `Maintenance condition evaluation failed (${response.status}).`,
          retryable: response.status >= 500,
        };
      }
      const payload: unknown = await response.json();
      const body = payload as { evaluated?: boolean };
      if (body.evaluated !== true) {
        return {
          status: "failure",
          code: "invalid-response",
          message: "Evaluation response invalid.",
          retryable: false,
        };
      }
      return { status: "success", value: { evaluated: true } };
    } catch {
      return {
        status: "failure",
        code: "source-unavailable",
        message: "Maintenance condition evaluation is unavailable.",
        retryable: true,
      };
    }
  })();
}

export function listMaintenanceConditionAnomalies(
  context: ProductionOrderRequestContext,
  assetReference: string,
): Promise<MaintenanceApiResult<readonly import("../domain/maintenance-condition").MaintenanceConditionAnomaly[]>> {
  return getJson(
    context,
    `/api/maintenance/assets/${encodeURIComponent(assetReference)}/anomalies`,
    parseMaintenanceConditionAnomalyList,
  );
}
