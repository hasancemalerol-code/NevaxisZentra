import "./erp-server-boundary";

import { getErpPurchaseOrder, listErpPurchaseOrders } from "./erp-api-client";
import { parseProductionExecutionRuntimeConfig } from "../../production-execution/server/production-order-runtime-config";
import { resolveProductionOrderRequestContextForIncomingRequest } from "../../production-execution/server/production-order-request-context";
import type {
  CanonicalErpPurchaseOrderDetail,
  CanonicalErpPurchaseOrderSummary,
} from "../domain/erp";

type ReadFailure = Readonly<{
  status: "failure";
  message: string;
}>;

export type ErpPurchaseOrdersResult =
  | Readonly<{ status: "success"; purchaseOrders: readonly CanonicalErpPurchaseOrderSummary[] }>
  | ReadFailure;

export type ErpPurchaseOrderDetailResult =
  | Readonly<{ status: "success"; purchaseOrder: CanonicalErpPurchaseOrderDetail }>
  | ReadFailure;

export async function readErpPurchaseOrders(): Promise<ErpPurchaseOrdersResult> {
  const configResult = parseProductionExecutionRuntimeConfig();
  if (configResult.status !== "success") {
    return { status: "failure", message: configResult.message };
  }

  const contextResult = await resolveProductionOrderRequestContextForIncomingRequest(configResult.config);
  if (contextResult.status === "failure") {
    return { status: "failure", message: contextResult.message };
  }

  const purchaseOrdersResult = await listErpPurchaseOrders(contextResult.context);
  if (purchaseOrdersResult.status === "failure") {
    return { status: "failure", message: purchaseOrdersResult.message };
  }

  return { status: "success", purchaseOrders: purchaseOrdersResult.value };
}

export async function readErpPurchaseOrderDetail(
  purchaseOrderId: string,
): Promise<ErpPurchaseOrderDetailResult> {
  const configResult = parseProductionExecutionRuntimeConfig();
  if (configResult.status !== "success") {
    return { status: "failure", message: configResult.message };
  }

  const contextResult = await resolveProductionOrderRequestContextForIncomingRequest(configResult.config);
  if (contextResult.status === "failure") {
    return { status: "failure", message: contextResult.message };
  }

  const purchaseOrderResult = await getErpPurchaseOrder(contextResult.context, purchaseOrderId);
  if (purchaseOrderResult.status === "failure") {
    return { status: "failure", message: purchaseOrderResult.message };
  }

  return { status: "success", purchaseOrder: purchaseOrderResult.value };
}
