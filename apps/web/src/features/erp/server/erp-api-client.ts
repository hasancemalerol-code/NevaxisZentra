import "./erp-server-boundary";

import type {
  CanonicalErpCustomer,
  CanonicalErpDocumentDetail,
  CanonicalErpDocumentSummary,
  CanonicalErpInventoryBalance,
  CanonicalErpOrderDetail,
  CanonicalErpOrderSummary,
  CanonicalErpProduct,
  CanonicalErpProductionOrderDetail,
  CanonicalErpProductionOrderSummary,
  CanonicalErpPurchaseOrderDetail,
  CanonicalErpPurchaseOrderSummary,
  CanonicalErpWarehouse,
  ErpConnection,
  ErpCustomerReconciliation,
  ErpInventoryAvailability,
  ErpOrderMaterialAvailability,
  ErpProductReconciliation,
  ErpSyncReliability,
  ErpSyncRun,
  ErpWriteSuccess,
  ErpWritebackDetail,
  ErpWritebackSummary,
  ErpWorkspaceSummary,
} from "../domain/erp";
import {
  parseCanonicalErpCustomerListApiResponse,
  parseCanonicalErpDocumentDetailApiResponse,
  parseCanonicalErpDocumentListApiResponse,
  parseCanonicalErpInventoryBalanceListApiResponse,
  parseCanonicalErpOrderDetailApiResponse,
  parseCanonicalErpOrderListApiResponse,
  parseCanonicalErpProductListApiResponse,
  parseCanonicalErpProductionOrderDetailApiResponse,
  parseCanonicalErpProductionOrderListApiResponse,
  parseCanonicalErpPurchaseOrderDetailApiResponse,
  parseCanonicalErpPurchaseOrderListApiResponse,
  parseCanonicalErpWarehouseListApiResponse,
  parseErpConnectionListApiResponse,
  parseErpCustomerReconciliationListApiResponse,
  parseErpInventoryAvailabilityListApiResponse,
  parseErpOrderMaterialAvailabilityApiResponse,
  parseErpProductReconciliationListApiResponse,
  parseErpSyncReliabilityListApiResponse,
  parseErpSyncRunListApiResponse,
  parseErpWriteSuccessApiResponse,
  parseErpWritebackDetailApiResponse,
  parseErpWritebackListApiResponse,
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
    | "invalid-response"
    | "concurrency-conflict";
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

async function postJson<T>(
  context: ProductionOrderRequestContext,
  path: string,
  body: unknown,
  parser: (payload: unknown) => T,
): Promise<ErpApiResult<T>> {
  try {
    const response = await fetch(buildUrl(context, path), {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
        Accept: "application/json",
        ...context.requestHeaders,
      },
      body: JSON.stringify(body),
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

    if (response.status === 409) {
      return {
        status: "failure",
        code: "concurrency-conflict",
        message: "Concurrent modification detected.",
        retryable: false,
      };
    }

    if (!response.ok) {
      return {
        status: "failure",
        code: "source-unavailable",
        message: `ERP write failed (${response.status}).`,
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

export function listErpOrders(
  context: ProductionOrderRequestContext,
): Promise<ErpApiResult<readonly CanonicalErpOrderSummary[]>> {
  return getJson(context, "/api/erp/orders", parseCanonicalErpOrderListApiResponse);
}

export function getErpOrder(
  context: ProductionOrderRequestContext,
  orderId: string,
): Promise<ErpApiResult<CanonicalErpOrderDetail>> {
  return getJson(context, `/api/erp/orders/${orderId}`, parseCanonicalErpOrderDetailApiResponse);
}

export function listErpCustomerReconciliation(
  context: ProductionOrderRequestContext,
): Promise<ErpApiResult<readonly ErpCustomerReconciliation[]>> {
  return getJson(
    context,
    "/api/erp/reconciliation/customers",
    parseErpCustomerReconciliationListApiResponse,
  );
}

export function listErpProductReconciliation(
  context: ProductionOrderRequestContext,
): Promise<ErpApiResult<readonly ErpProductReconciliation[]>> {
  return getJson(
    context,
    "/api/erp/reconciliation/products",
    parseErpProductReconciliationListApiResponse,
  );
}

export function listErpWarehouses(
  context: ProductionOrderRequestContext,
): Promise<ErpApiResult<readonly CanonicalErpWarehouse[]>> {
  return getJson(context, "/api/erp/warehouses", parseCanonicalErpWarehouseListApiResponse);
}

export function listErpInventoryBalances(
  context: ProductionOrderRequestContext,
): Promise<ErpApiResult<readonly CanonicalErpInventoryBalance[]>> {
  return getJson(context, "/api/erp/inventory", parseCanonicalErpInventoryBalanceListApiResponse);
}

export function listErpInventoryAvailability(
  context: ProductionOrderRequestContext,
): Promise<ErpApiResult<readonly ErpInventoryAvailability[]>> {
  return getJson(
    context,
    "/api/erp/inventory/availability",
    parseErpInventoryAvailabilityListApiResponse,
  );
}

export function listErpPurchaseOrders(
  context: ProductionOrderRequestContext,
): Promise<ErpApiResult<readonly CanonicalErpPurchaseOrderSummary[]>> {
  return getJson(
    context,
    "/api/erp/purchase-orders",
    parseCanonicalErpPurchaseOrderListApiResponse,
  );
}

export function getErpPurchaseOrder(
  context: ProductionOrderRequestContext,
  purchaseOrderId: string,
): Promise<ErpApiResult<CanonicalErpPurchaseOrderDetail>> {
  return getJson(
    context,
    `/api/erp/purchase-orders/${purchaseOrderId}`,
    parseCanonicalErpPurchaseOrderDetailApiResponse,
  );
}

export function listErpSyncReliability(
  context: ProductionOrderRequestContext,
): Promise<ErpApiResult<readonly ErpSyncReliability[]>> {
  return getJson(context, "/api/erp/sync-reliability", parseErpSyncReliabilityListApiResponse);
}

export function getErpOrderMaterialAvailability(
  context: ProductionOrderRequestContext,
  orderId: string,
): Promise<ErpApiResult<ErpOrderMaterialAvailability>> {
  return getJson(
    context,
    `/api/erp/orders/${orderId}/material-availability`,
    parseErpOrderMaterialAvailabilityApiResponse,
  );
}

export function listErpProductionOrders(
  context: ProductionOrderRequestContext,
): Promise<ErpApiResult<readonly CanonicalErpProductionOrderSummary[]>> {
  return getJson(
    context,
    "/api/erp/production-orders",
    parseCanonicalErpProductionOrderListApiResponse,
  );
}

export function getErpProductionOrder(
  context: ProductionOrderRequestContext,
  productionOrderId: string,
): Promise<ErpApiResult<CanonicalErpProductionOrderDetail>> {
  return getJson(
    context,
    `/api/erp/production-orders/${productionOrderId}`,
    parseCanonicalErpProductionOrderDetailApiResponse,
  );
}

export function listErpDocuments(
  context: ProductionOrderRequestContext,
): Promise<ErpApiResult<readonly CanonicalErpDocumentSummary[]>> {
  return getJson(context, "/api/erp/documents", parseCanonicalErpDocumentListApiResponse);
}

export function getErpDocument(
  context: ProductionOrderRequestContext,
  documentId: string,
): Promise<ErpApiResult<CanonicalErpDocumentDetail>> {
  return getJson(
    context,
    `/api/erp/documents/${documentId}`,
    parseCanonicalErpDocumentDetailApiResponse,
  );
}

export function listErpWritebacks(
  context: ProductionOrderRequestContext,
): Promise<ErpApiResult<readonly ErpWritebackSummary[]>> {
  return getJson(context, "/api/erp/writebacks", parseErpWritebackListApiResponse);
}

export function getErpWriteback(
  context: ProductionOrderRequestContext,
  writebackId: string,
): Promise<ErpApiResult<ErpWritebackDetail>> {
  return getJson(
    context,
    `/api/erp/writebacks/${writebackId}`,
    parseErpWritebackDetailApiResponse,
  );
}

export function submitErpWriteback(
  context: ProductionOrderRequestContext,
  writebackId: string,
  expectedVersion: number,
): Promise<ErpApiResult<ErpWriteSuccess>> {
  return postJson(
    context,
    `/api/erp/writebacks/${writebackId}/submit`,
    { facilityId: context.facilityId, expectedVersion },
    parseErpWriteSuccessApiResponse,
  );
}

export function approveErpWriteback(
  context: ProductionOrderRequestContext,
  writebackId: string,
  expectedVersion: number,
  comment?: string,
): Promise<ErpApiResult<ErpWriteSuccess>> {
  return postJson(
    context,
    `/api/erp/writebacks/${writebackId}/approve`,
    { facilityId: context.facilityId, expectedVersion, comment: comment ?? null },
    parseErpWriteSuccessApiResponse,
  );
}

export function rejectErpWriteback(
  context: ProductionOrderRequestContext,
  writebackId: string,
  expectedVersion: number,
  comment?: string,
): Promise<ErpApiResult<ErpWriteSuccess>> {
  return postJson(
    context,
    `/api/erp/writebacks/${writebackId}/reject`,
    { facilityId: context.facilityId, expectedVersion, comment: comment ?? null },
    parseErpWriteSuccessApiResponse,
  );
}

export function executeErpWriteback(
  context: ProductionOrderRequestContext,
  writebackId: string,
  expectedVersion: number,
): Promise<ErpApiResult<ErpWriteSuccess>> {
  return postJson(
    context,
    `/api/erp/writebacks/${writebackId}/execute`,
    { facilityId: context.facilityId, expectedVersion },
    parseErpWriteSuccessApiResponse,
  );
}
