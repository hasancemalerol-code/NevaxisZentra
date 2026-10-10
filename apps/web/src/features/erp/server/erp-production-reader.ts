import "./erp-server-boundary";

import { getErpProductionOrder, listErpProductionOrders } from "./erp-api-client";
import { parseProductionExecutionRuntimeConfig } from "../../production-execution/server/production-order-runtime-config";
import { resolveProductionOrderRequestContextForIncomingRequest } from "../../production-execution/server/production-order-request-context";
import type {
  CanonicalErpProductionOrderDetail,
  CanonicalErpProductionOrderSummary,
  ErpProductionWorkspaceSnapshot,
} from "../domain/erp";

type ReadFailure = Readonly<{ status: "failure"; message: string; code?: string }>;

export type ErpProductionOrdersResult =
  | Readonly<{
      status: "success";
      snapshot: ErpProductionWorkspaceSnapshot;
      productionOrders: readonly CanonicalErpProductionOrderSummary[];
    }>
  | ReadFailure;

export type ErpProductionOrderDetailResult =
  | Readonly<{ status: "success"; productionOrder: CanonicalErpProductionOrderDetail }>
  | ReadFailure;

function buildProductionSnapshot(
  productionOrders: readonly CanonicalErpProductionOrderSummary[],
): ErpProductionWorkspaceSnapshot {
  const freshRecordCount = productionOrders.filter((row) => row.freshness === "fresh").length;
  const staleRecordCount = productionOrders.filter((row) => row.freshness === "stale").length;
  const inProgressCount = productionOrders.filter(
    (row) => row.status === "in-progress" || row.status === "released",
  ).length;
  const completedCount = productionOrders.filter(
    (row) => row.status === "completed" || row.status === "closed",
  ).length;

  return {
    productionOrderCount: productionOrders.length,
    inProgressCount,
    completedCount,
    freshRecordCount,
    staleRecordCount,
  };
}

export async function readErpProductionOrders(): Promise<ErpProductionOrdersResult> {
  const configResult = parseProductionExecutionRuntimeConfig();
  if (configResult.status !== "success") {
    return { status: "failure", message: configResult.message };
  }

  const contextResult = await resolveProductionOrderRequestContextForIncomingRequest(configResult.config);
  if (contextResult.status === "failure") {
    return { status: "failure", message: contextResult.message };
  }

  const productionOrdersResult = await listErpProductionOrders(contextResult.context);
  if (productionOrdersResult.status === "failure") {
    return { status: "failure", message: productionOrdersResult.message, code: productionOrdersResult.code };
  }

  return {
    status: "success",
    snapshot: buildProductionSnapshot(productionOrdersResult.value),
    productionOrders: productionOrdersResult.value,
  };
}

export async function readErpProductionOrderDetail(
  productionOrderId: string,
): Promise<ErpProductionOrderDetailResult> {
  const configResult = parseProductionExecutionRuntimeConfig();
  if (configResult.status !== "success") {
    return { status: "failure", message: configResult.message };
  }

  const contextResult = await resolveProductionOrderRequestContextForIncomingRequest(configResult.config);
  if (contextResult.status === "failure") {
    return { status: "failure", message: contextResult.message };
  }

  const productionOrderResult = await getErpProductionOrder(
    contextResult.context,
    productionOrderId,
  );
  if (productionOrderResult.status === "failure") {
    return { status: "failure", message: productionOrderResult.message, code: productionOrderResult.code };
  }

  return { status: "success", productionOrder: productionOrderResult.value };
}
