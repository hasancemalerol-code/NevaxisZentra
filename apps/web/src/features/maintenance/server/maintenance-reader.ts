import "./maintenance-server-boundary";

import {
  getMaintenanceAssetHistory,
  getMaintenanceWorkOrder,
  getMaintenanceWorkOrderCostSummary,
  getMaintenanceWorkOrderTimeline,
  getMaintenanceWorkspaceSummary,
  listMaintenancePartUsages,
  listMaintenanceRequests,
  listMaintenanceWorkNotes,
  listMaintenanceWorkOrders,
  listPreventiveMaintenanceDues,
  listPreventiveMaintenancePlans,
} from "./maintenance-api-client";
import { parseProductionExecutionRuntimeConfig } from "../../production-execution/server/production-order-runtime-config";
import { resolveProductionOrderRequestContext } from "../../production-execution/server/production-order-request-context";
import type {
  MaintenanceAssetHistory,
  MaintenanceCostSummary,
  MaintenancePartUsage,
  MaintenanceRequestSummary,
  MaintenanceTimelineEntry,
  MaintenanceWorkNote,
  MaintenanceWorkOrderDetail,
  MaintenanceWorkOrderSummary,
  MaintenanceWorkspaceSummary,
  PreventiveMaintenanceDueSummary,
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

export type MaintenanceWorkOrderExecutionResult =
  | Readonly<{
      status: "success";
      notes: readonly MaintenanceWorkNote[];
      parts: readonly MaintenancePartUsage[];
      timeline: readonly MaintenanceTimelineEntry[];
      costSummary: MaintenanceCostSummary | null;
    }>
  | ReadFailure;

export async function readMaintenanceWorkOrderExecution(
  workOrderId: string,
): Promise<MaintenanceWorkOrderExecutionResult> {
  const configResult = parseProductionExecutionRuntimeConfig();
  if (configResult.status !== "success") {
    return { status: "failure", message: configResult.message };
  }

  const contextResult = resolveProductionOrderRequestContext(configResult.config);
  if (contextResult.status === "failure") {
    return { status: "failure", message: contextResult.message };
  }

  const context = contextResult.context;
  const [notesResult, partsResult, timelineResult, costResult] = await Promise.all([
    listMaintenanceWorkNotes(context, workOrderId),
    listMaintenancePartUsages(context, workOrderId),
    getMaintenanceWorkOrderTimeline(context, workOrderId),
    getMaintenanceWorkOrderCostSummary(context, workOrderId),
  ]);

  if (notesResult.status === "failure") {
    return { status: "failure", message: notesResult.message };
  }

  if (partsResult.status === "failure") {
    return { status: "failure", message: partsResult.message };
  }

  if (timelineResult.status === "failure") {
    return { status: "failure", message: timelineResult.message };
  }

  return {
    status: "success",
    notes: notesResult.value,
    parts: partsResult.value,
    timeline: timelineResult.value,
    costSummary: costResult.status === "success" ? costResult.value : null,
  };
}

export type MaintenancePlanningResult =
  | Readonly<{
      status: "success";
      dues: readonly PreventiveMaintenanceDueSummary[];
      plans: readonly PreventiveMaintenancePlanSummary[];
    }>
  | ReadFailure;

export async function readMaintenancePlanning(): Promise<MaintenancePlanningResult> {
  const configResult = parseProductionExecutionRuntimeConfig();
  if (configResult.status !== "success") {
    return { status: "failure", message: configResult.message };
  }

  const contextResult = resolveProductionOrderRequestContext(configResult.config);
  if (contextResult.status === "failure") {
    return { status: "failure", message: contextResult.message };
  }

  const context = contextResult.context;
  const [duesResult, plansResult] = await Promise.all([
    listPreventiveMaintenanceDues(context),
    listPreventiveMaintenancePlans(context),
  ]);

  if (duesResult.status === "failure") {
    return { status: "failure", message: duesResult.message };
  }

  if (plansResult.status === "failure") {
    return { status: "failure", message: plansResult.message };
  }

  return {
    status: "success",
    dues: duesResult.value,
    plans: plansResult.value,
  };
}

export type MaintenanceAssetHistoryResult =
  | Readonly<{ status: "success"; history: MaintenanceAssetHistory }>
  | ReadFailure;

export async function readMaintenanceAssetHistory(
  assetReference: string,
): Promise<MaintenanceAssetHistoryResult> {
  const configResult = parseProductionExecutionRuntimeConfig();
  if (configResult.status !== "success") {
    return { status: "failure", message: configResult.message };
  }

  const contextResult = resolveProductionOrderRequestContext(configResult.config);
  if (contextResult.status === "failure") {
    return { status: "failure", message: contextResult.message };
  }

  const result = await getMaintenanceAssetHistory(contextResult.context, assetReference);
  if (result.status === "failure") {
    return { status: "failure", message: result.message };
  }

  return { status: "success", history: result.value };
}
