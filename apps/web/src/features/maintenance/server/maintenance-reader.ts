import "./maintenance-server-boundary";

import {
  getMaintenanceWorkOrder,
  getMaintenanceWorkspaceSummary,
  listMaintenanceRequests,
  listMaintenanceWorkOrders,
  listPreventiveMaintenancePlans,
} from "./maintenance-api-client";
import { parseProductionExecutionRuntimeConfig } from "../../production-execution/server/production-order-runtime-config";
import { resolveProductionOrderRequestContext } from "../../production-execution/server/production-order-request-context";
import type {
  MaintenanceRequestSummary,
  MaintenanceWorkOrderDetail,
  MaintenanceWorkOrderSummary,
  MaintenanceWorkspaceSummary,
  PreventiveMaintenancePlanSummary,
} from "../domain/maintenance";

type ReadFailure = Readonly<{
  status: "failure";
  message: string;
}>;

export type MaintenanceDashboardResult =
  | Readonly<{
      status: "success";
      summary: MaintenanceWorkspaceSummary;
      requests: readonly MaintenanceRequestSummary[];
      workOrders: readonly MaintenanceWorkOrderSummary[];
      preventivePlans: readonly PreventiveMaintenancePlanSummary[];
    }>
  | ReadFailure;

export async function readMaintenanceDashboard(): Promise<MaintenanceDashboardResult> {
  const configResult = parseProductionExecutionRuntimeConfig();
  if (configResult.status !== "success") {
    return { status: "failure", message: configResult.message };
  }

  const contextResult = resolveProductionOrderRequestContext(configResult.config);
  if (contextResult.status === "failure") {
    return { status: "failure", message: contextResult.message };
  }

  const context = contextResult.context;
  const [summaryResult, requestsResult, workOrdersResult, plansResult] = await Promise.all([
    getMaintenanceWorkspaceSummary(context),
    listMaintenanceRequests(context),
    listMaintenanceWorkOrders(context),
    listPreventiveMaintenancePlans(context),
  ]);

  if (summaryResult.status === "failure") {
    return { status: "failure", message: summaryResult.message };
  }

  if (requestsResult.status === "failure") {
    return { status: "failure", message: requestsResult.message };
  }

  if (workOrdersResult.status === "failure") {
    return { status: "failure", message: workOrdersResult.message };
  }

  if (plansResult.status === "failure") {
    return { status: "failure", message: plansResult.message };
  }

  return {
    status: "success",
    summary: summaryResult.value,
    requests: requestsResult.value,
    workOrders: workOrdersResult.value,
    preventivePlans: plansResult.value,
  };
}

export type MaintenanceWorkOrderDetailResult =
  | Readonly<{ status: "success"; workOrder: MaintenanceWorkOrderDetail }>
  | ReadFailure;

export async function readMaintenanceWorkOrderDetail(
  workOrderId: string,
): Promise<MaintenanceWorkOrderDetailResult> {
  const configResult = parseProductionExecutionRuntimeConfig();
  if (configResult.status !== "success") {
    return { status: "failure", message: configResult.message };
  }

  const contextResult = resolveProductionOrderRequestContext(configResult.config);
  if (contextResult.status === "failure") {
    return { status: "failure", message: contextResult.message };
  }

  const result = await getMaintenanceWorkOrder(contextResult.context, workOrderId);
  if (result.status === "failure") {
    return { status: "failure", message: result.message };
  }

  return { status: "success", workOrder: result.value };
}
