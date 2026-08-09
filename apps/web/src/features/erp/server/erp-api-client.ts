import "./erp-server-boundary";

import type {
  CanonicalErpCustomer,
  CanonicalErpProduct,
  ErpConnection,
  ErpSyncRun,
  ErpWorkspaceSummary,
} from "../domain/erp";
import {
  parseCanonicalErpCustomerListApiResponse,
  parseCanonicalErpProductListApiResponse,
  parseErpConnectionListApiResponse,
  parseErpSyncRunListApiResponse,
  parseErpWorkspaceSummaryApiResponse,
} from "./erp-api-contracts";
import type { ProductionOrderRequestContext } from "../../production-execution/server/production-order-request-context";

export type ErpApiFailure = Readonly<{
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

export type ErpApiResult<T> =
  | Readonly<{ status: "success"; value: T }>
  | ErpApiFailure;

function buildUrl(
  context: ProductionOrderRequestContext,
  path: string,
  query?: Record<string, string | undefined>,
): string {
  const url = new URL(`${context.apiBaseUrl}${path}`);
  url.searchParams.set("facilityId", context.facilityId);
  if (query) {
    for (const [key, value] of Object.entries(query)) {
      if (value !== undefined && value !== "") {
        url.searchParams.set(key, value);
      }
    }
  }
  return url.toString();
}

async function getJson<T>(
  context: ProductionOrderRequestContext,
  path: string,
  parser: (payload: unknown) => T,
  query?: Record<string, string | undefined>,
): Promise<ErpApiResult<T>> {
  try {
    const response = await fetch(buildUrl(context, path, query), {
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
        message: `ERP request failed (${response.status}).`,
        retryable: response.status >= 500,
      };
    }

    const payload: unknown = await response.json();
    return { status: "success", value: parser(payload) };
  } catch {
    return {
      status: "failure",
      code: "source-unavailable",
      message: "ERP API is unavailable.",
      retryable: true,
    };
  }
}

export function listErpConnections(
  context: ProductionOrderRequestContext,
): Promise<ErpApiResult<readonly ErpConnection[]>> {
  return getJson(context, "/api/erp/connections", parseErpConnectionListApiResponse);
}

export function listErpSyncRuns(
  context: ProductionOrderRequestContext,
): Promise<ErpApiResult<readonly ErpSyncRun[]>> {
  return getJson(context, "/api/erp/sync-runs", parseErpSyncRunListApiResponse);
}

export function listErpCustomers(
  context: ProductionOrderRequestContext,
): Promise<ErpApiResult<readonly CanonicalErpCustomer[]>> {
  return getJson(context, "/api/erp/customers", parseCanonicalErpCustomerListApiResponse);
}

export function listErpProducts(
  context: ProductionOrderRequestContext,
): Promise<ErpApiResult<readonly CanonicalErpProduct[]>> {
  return getJson(context, "/api/erp/products", parseCanonicalErpProductListApiResponse);
}

export function getErpWorkspaceSummary(
  context: ProductionOrderRequestContext,
): Promise<ErpApiResult<ErpWorkspaceSummary>> {
  return getJson(context, "/api/erp/workspace-summary", parseErpWorkspaceSummaryApiResponse);
}
