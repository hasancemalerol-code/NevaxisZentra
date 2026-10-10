import "./erp-server-boundary";

import { getErpOrder, getErpOrderMaterialAvailability, listErpOrders } from "./erp-api-client";
import { parseProductionExecutionRuntimeConfig } from "../../production-execution/server/production-order-runtime-config";
import { resolveProductionOrderRequestContextForIncomingRequest } from "../../production-execution/server/production-order-request-context";
import type {
  CanonicalErpOrderDetail,
  CanonicalErpOrderSummary,
  ErpOrderMaterialAvailability,
} from "../domain/erp";

type ReadFailure = Readonly<{ status: "failure"; message: string; code?: string }>;

export type ErpOrdersResult =
  | Readonly<{ status: "success"; orders: readonly CanonicalErpOrderSummary[] }>
  | ReadFailure;

export type ErpOrderDetailResult =
  | Readonly<{
      status: "success";
      order: CanonicalErpOrderDetail;
      materialAvailability: ErpOrderMaterialAvailability | null;
    }>
  | ReadFailure;

export async function readErpOrders(): Promise<ErpOrdersResult> {
  const configResult = parseProductionExecutionRuntimeConfig();
  if (configResult.status !== "success") {
    return { status: "failure", message: configResult.message };
  }

  const contextResult = await resolveProductionOrderRequestContextForIncomingRequest(configResult.config);
  if (contextResult.status === "failure") {
    return { status: "failure", message: contextResult.message };
  }

  const ordersResult = await listErpOrders(contextResult.context);
  if (ordersResult.status === "failure") {
    return { status: "failure", message: ordersResult.message, code: ordersResult.code };
  }

  return { status: "success", orders: ordersResult.value };
}

export async function readErpOrderDetail(orderId: string): Promise<ErpOrderDetailResult> {
  const configResult = parseProductionExecutionRuntimeConfig();
  if (configResult.status !== "success") {
    return { status: "failure", message: configResult.message };
  }

  const contextResult = await resolveProductionOrderRequestContextForIncomingRequest(configResult.config);
  if (contextResult.status === "failure") {
    return { status: "failure", message: contextResult.message };
  }

  const [orderResult, materialAvailabilityResult] = await Promise.all([
    getErpOrder(contextResult.context, orderId),
    getErpOrderMaterialAvailability(contextResult.context, orderId),
  ]);

  if (orderResult.status === "failure") {
    return { status: "failure", message: orderResult.message, code: orderResult.code };
  }

  const materialAvailability =
    materialAvailabilityResult.status === "success" ? materialAvailabilityResult.value : null;

  return {
    status: "success",
    order: orderResult.value,
    materialAvailability,
  };
}
