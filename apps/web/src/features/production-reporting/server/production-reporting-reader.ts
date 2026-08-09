import "./production-reporting-server-boundary";

import {
  getProductionDowntimeReporting,
  getProductionExceptionReporting,
  getProductionReportingOverview,
  listProductionOrderPerformance,
  listProductionShiftReporting,
} from "./production-reporting-api-client";
import { parseProductionExecutionRuntimeConfig } from "../../production-execution/server/production-order-runtime-config";
import { resolveProductionOrderRequestContext } from "../../production-execution/server/production-order-request-context";
import type {
  ProductionDowntimeReportingSummary,
  ProductionExceptionReportingSummary,
  ProductionOrderPerformanceRow,
  ProductionReportingOverview,
  ProductionShiftReportingRow,
} from "../domain/production-reporting";

type ReadFailure = Readonly<{
  status: "failure";
  message: string;
}>;

export type ProductionReportingWorkspaceResult =
  | Readonly<{
      status: "success";
      overview: ProductionReportingOverview;
      orders: readonly ProductionOrderPerformanceRow[];
      shifts: readonly ProductionShiftReportingRow[];
      downtime: ProductionDowntimeReportingSummary;
      exceptions: ProductionExceptionReportingSummary;
    }>
  | ReadFailure;

export async function readProductionReportingWorkspace(): Promise<ProductionReportingWorkspaceResult> {
  const configResult = parseProductionExecutionRuntimeConfig();
  if (configResult.status !== "success") {
    return {
      status: "failure",
      message: configResult.message,
    };
  }

  const contextResult = resolveProductionOrderRequestContext(configResult.config);

  if (contextResult.status === "failure") {
    return {
      status: "failure",
      message: contextResult.message,
    };
  }

  const context = contextResult.context;
  const [overviewResult, ordersResult, shiftsResult, downtimeResult, exceptionsResult] =
    await Promise.all([
      getProductionReportingOverview(context),
      listProductionOrderPerformance(context),
      listProductionShiftReporting(context),
      getProductionDowntimeReporting(context),
      getProductionExceptionReporting(context),
    ]);

  if (overviewResult.status === "failure") {
    return { status: "failure", message: overviewResult.message };
  }

  if (ordersResult.status === "failure") {
    return { status: "failure", message: ordersResult.message };
  }

  if (shiftsResult.status === "failure") {
    return { status: "failure", message: shiftsResult.message };
  }

  if (downtimeResult.status === "failure") {
    return { status: "failure", message: downtimeResult.message };
  }

  if (exceptionsResult.status === "failure") {
    return { status: "failure", message: exceptionsResult.message };
  }

  return {
    status: "success",
    overview: overviewResult.value,
    orders: ordersResult.value,
    shifts: shiftsResult.value,
    downtime: downtimeResult.value,
    exceptions: exceptionsResult.value,
  };
}
