import "./erp-server-boundary";

import { getErpOrder, listErpOrders } from "./erp-api-client";
import { parseProductionExecutionRuntimeConfig } from "../../production-execution/server/production-order-runtime-config";
import { resolveProductionOrderRequestContext } from "../../production-execution/server/production-order-request-context";
import type { CanonicalErpOrderDetail, CanonicalErpOrderSummary } from "../domain/erp";

type ReadFailure = Readonly<{
  status: "failure";
  message: string;
}>;

export type ErpOrdersResult =
  | Readonly<{ status: "success"; orders: readonly CanonicalErpOrderSummary[] }>
  | ReadFailure;

export type ErpOrderDetailResult =
  | Readonly<{ status: "success"; order: CanonicalErpOrderDetail }>
  | ReadFailure;

export async function readErpOrders(): Promise<ErpOrdersResult> {
  const configResult = parseProductionExecutionRuntimeConfig();
  if (configResult.status !== "success") {
    return { status: "failure", message: configResult.message };
  }

  const contextResult = resolveProductionOrderRequestContext(configResult.config);
  if (contextResult.status === "failure") {
    return { status: "failure", message: contextResult.message };
  }

  const ordersResult = await listErpOrders(contextResult.context);
  if (ordersResult.status === "failure") {
    return { status: "failure", message: ordersResult.message };
  }

  return { status: "success", orders: ordersResult.value };
}

export async function readErpOrderDetail(orderId: string): Promise<ErpOrderDetailResult> {
  const configResult = parseProductionExecutionRuntimeConfig();
  if (configResult.status !== "success") {
    return { status: "failure", message: configResult.message };
  }

  const contextResult = resolveProductionOrderRequestContext(configResult.config);
  if (contextResult.status === "failure") {
    return { status: "failure", message: contextResult.message };
  }

  const orderResult = await getErpOrder(contextResult.context, orderId);
  if (orderResult.status === "failure") {
    return { status: "failure", message: orderResult.message };
  }

  return { status: "success", order: orderResult.value };
}
