import "./production-reporting-server-boundary";

import type {
  ProductionDowntimeReportingSummary,
  ProductionExceptionReportingSummary,
  ProductionOrderPerformanceRow,
  ProductionReportingOverview,
  ProductionShiftReportingRow,
} from "../domain/production-reporting";
import {
  parseProductionReportingOverviewApiResponse,
  parseProductionOrderPerformanceListApiResponse,
  parseProductionShiftReportingListApiResponse,
  parseProductionDowntimeReportingApiResponse,
  parseProductionExceptionReportingApiResponse,
} from "./production-reporting-api-contracts";
import type { ProductionOrderRequestContext } from "../../production-execution/server/production-order-request-context";

export type ProductionReportingApiFailure = Readonly<{
  status: "failure";
  code:
    | "source-unavailable"
    | "unauthenticated"
    | "permission-denied"
    | "configuration-error"
    | "invalid-request"
    | "invalid-response";
  message: string;
  retryable: boolean;
}>;

export type ProductionReportingApiResult<T> =
  | Readonly<{ status: "success"; value: T }>
  | ProductionReportingApiFailure;

function buildUrl(
  context: ProductionOrderRequestContext,
  path: string,
  params: Record<string, string | undefined> = {},
): string {
  const url = new URL(`${context.apiBaseUrl}${path}`);
  url.searchParams.set("facilityId", context.facilityId);
  for (const [key, value] of Object.entries(params)) {
    if (value) {
      url.searchParams.set(key, value);
    }
  }
  return url.toString();
}

async function getJson<T>(
  context: ProductionOrderRequestContext,
  path: string,
  parser: (payload: unknown) => T,
  params: Record<string, string | undefined> = {},
): Promise<ProductionReportingApiResult<T>> {
  try {
    const response = await fetch(buildUrl(context, path, params), {
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
        message: `Reporting request failed (${response.status}).`,
        retryable: response.status >= 500,
      };
    }

    const payload: unknown = await response.json();
    return { status: "success", value: parser(payload) };
  } catch {
    return {
      status: "failure",
      code: "source-unavailable",
      message: "Production reporting API is unavailable.",
      retryable: true,
    };
  }
}

export function getProductionReportingOverview(
  context: ProductionOrderRequestContext,
): Promise<ProductionReportingApiResult<ProductionReportingOverview>> {
  return getJson(
    context,
    "/api/production/reporting/overview",
    parseProductionReportingOverviewApiResponse,
  );
}

export function listProductionOrderPerformance(
  context: ProductionOrderRequestContext,
): Promise<ProductionReportingApiResult<readonly ProductionOrderPerformanceRow[]>> {
  return getJson(
    context,
    "/api/production/reporting/orders",
    parseProductionOrderPerformanceListApiResponse,
  );
}

export function listProductionShiftReporting(
  context: ProductionOrderRequestContext,
): Promise<ProductionReportingApiResult<readonly ProductionShiftReportingRow[]>> {
  return getJson(
    context,
    "/api/production/reporting/shifts",
    parseProductionShiftReportingListApiResponse,
  );
}

export function getProductionDowntimeReporting(
  context: ProductionOrderRequestContext,
): Promise<ProductionReportingApiResult<ProductionDowntimeReportingSummary>> {
  return getJson(
    context,
    "/api/production/reporting/downtime",
    parseProductionDowntimeReportingApiResponse,
  );
}

export function getProductionExceptionReporting(
  context: ProductionOrderRequestContext,
): Promise<ProductionReportingApiResult<ProductionExceptionReportingSummary>> {
  return getJson(
    context,
    "/api/production/reporting/exceptions",
    parseProductionExceptionReportingApiResponse,
  );
}
