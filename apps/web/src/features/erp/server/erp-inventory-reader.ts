import "./erp-server-boundary";

import {
  listErpInventoryAvailability,
  listErpInventoryBalances,
  listErpPurchaseOrders,
  listErpSyncReliability,
  listErpWarehouses,
} from "./erp-api-client";
import { parseProductionExecutionRuntimeConfig } from "../../production-execution/server/production-order-runtime-config";
import { resolveProductionOrderRequestContextForIncomingRequest } from "../../production-execution/server/production-order-request-context";
import type {
  CanonicalErpInventoryBalance,
  CanonicalErpWarehouse,
  ErpInventoryAvailability,
  ErpInventoryWorkspaceSnapshot,
  ErpSyncReliability,
} from "../domain/erp";

type ReadFailure = Readonly<{
  status: "failure";
  message: string;
}>;

export type ErpInventoryWorkspaceResult =
  | Readonly<{
      status: "success";
      snapshot: ErpInventoryWorkspaceSnapshot;
      warehouses: readonly CanonicalErpWarehouse[];
      inventoryBalances: readonly CanonicalErpInventoryBalance[];
      availability: readonly ErpInventoryAvailability[];
      syncReliability: readonly ErpSyncReliability[];
    }>
  | ReadFailure;

function buildInventorySnapshot(
  warehouses: readonly CanonicalErpWarehouse[],
  inventoryBalances: readonly CanonicalErpInventoryBalance[],
  availability: readonly ErpInventoryAvailability[],
  openPurchaseOrderCount: number,
): ErpInventoryWorkspaceSnapshot {
  const freshRecordCount = inventoryBalances.filter((row) => row.freshness === "fresh").length;
  const staleRecordCount = inventoryBalances.filter((row) => row.freshness === "stale").length;
  const insufficientStockCount = availability.filter(
    (row) => row.totalAvailable != null && row.totalAvailable <= 0,
  ).length;

  return {
    warehouseCount: warehouses.length,
    inventoryRecordCount: inventoryBalances.length,
    freshRecordCount,
    staleRecordCount,
    insufficientStockCount,
    openPurchaseOrderCount,
  };
}

export async function readErpInventoryWorkspace(): Promise<ErpInventoryWorkspaceResult> {
  const configResult = parseProductionExecutionRuntimeConfig();
  if (configResult.status !== "success") {
    return { status: "failure", message: configResult.message };
  }

  const contextResult = await resolveProductionOrderRequestContextForIncomingRequest(configResult.config);
  if (contextResult.status === "failure") {
    return { status: "failure", message: contextResult.message };
  }

  const context = contextResult.context;
  const [
    warehousesResult,
    inventoryResult,
    availabilityResult,
    purchaseOrdersResult,
    syncReliabilityResult,
  ] = await Promise.all([
    listErpWarehouses(context),
    listErpInventoryBalances(context),
    listErpInventoryAvailability(context),
    listErpPurchaseOrders(context),
    listErpSyncReliability(context),
  ]);

  if (warehousesResult.status === "failure") {
    return { status: "failure", message: warehousesResult.message };
  }

  if (inventoryResult.status === "failure") {
    return { status: "failure", message: inventoryResult.message };
  }

  if (availabilityResult.status === "failure") {
    return { status: "failure", message: availabilityResult.message };
  }

  if (purchaseOrdersResult.status === "failure") {
    return { status: "failure", message: purchaseOrdersResult.message };
  }

  if (syncReliabilityResult.status === "failure") {
    return { status: "failure", message: syncReliabilityResult.message };
  }

  const openPurchaseOrderCount = purchaseOrdersResult.value.filter(
    (order) =>
      (order.status === "open" || order.status === "partially-received") && order.openLineCount > 0,
  ).length;

  return {
    status: "success",
    snapshot: buildInventorySnapshot(
      warehousesResult.value,
      inventoryResult.value,
      availabilityResult.value,
      openPurchaseOrderCount,
    ),
    warehouses: warehousesResult.value,
    inventoryBalances: inventoryResult.value,
    availability: availabilityResult.value,
    syncReliability: syncReliabilityResult.value,
  };
}
