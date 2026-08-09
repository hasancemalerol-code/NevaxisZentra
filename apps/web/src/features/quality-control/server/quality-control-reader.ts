import "./quality-control-server-boundary";

import {
  getQualityControlPlan,
  getQualityInspection,
  getQualityWorkspaceSummary,
  listQualityControlPlans,
  listQualityInspections,
} from "./quality-control-api-client";
import { parseProductionExecutionRuntimeConfig } from "../../production-execution/server/production-order-runtime-config";
import { resolveProductionOrderRequestContext } from "../../production-execution/server/production-order-request-context";
import type {
  QualityControlPlanDetail,
  QualityControlPlanSummary,
  QualityInspectionDetail,
  QualityInspectionSummary,
  QualityWorkspaceSummary,
} from "../domain/quality-control";

type ReadFailure = Readonly<{
  status: "failure";
  message: string;
}>;

export type QualityWorkspaceResult =
  | Readonly<{
      status: "success";
      summary: QualityWorkspaceSummary;
      plans: readonly QualityControlPlanSummary[];
      inspections: readonly QualityInspectionSummary[];
    }>
  | ReadFailure;

export async function readQualityWorkspace(): Promise<QualityWorkspaceResult> {
  const configResult = parseProductionExecutionRuntimeConfig();
  if (configResult.status !== "success") {
    return { status: "failure", message: configResult.message };
  }

  const contextResult = resolveProductionOrderRequestContext(configResult.config);
  if (contextResult.status === "failure") {
    return { status: "failure", message: contextResult.message };
  }

  const context = contextResult.context;
  const [summaryResult, plansResult, inspectionsResult] = await Promise.all([
    getQualityWorkspaceSummary(context),
    listQualityControlPlans(context),
    listQualityInspections(context),
  ]);

  if (summaryResult.status === "failure") {
    return { status: "failure", message: summaryResult.message };
  }

  if (plansResult.status === "failure") {
    return { status: "failure", message: plansResult.message };
  }

  if (inspectionsResult.status === "failure") {
    return { status: "failure", message: inspectionsResult.message };
  }

  return {
    status: "success",
    summary: summaryResult.value,
    plans: plansResult.value,
    inspections: inspectionsResult.value,
  };
}

export type QualityInspectionDetailResult =
  | Readonly<{ status: "success"; inspection: QualityInspectionDetail }>
  | ReadFailure;

export async function readQualityInspectionById(
  inspectionId: string,
): Promise<QualityInspectionDetailResult> {
  const configResult = parseProductionExecutionRuntimeConfig();
  if (configResult.status !== "success") {
    return { status: "failure", message: configResult.message };
  }

  const contextResult = resolveProductionOrderRequestContext(configResult.config);
  if (contextResult.status === "failure") {
    return { status: "failure", message: contextResult.message };
  }

  const result = await getQualityInspection(contextResult.context, inspectionId);
  if (result.status === "failure") {
    return { status: "failure", message: result.message };
  }

  return { status: "success", inspection: result.value };
}

export type QualityControlPlanDetailResult =
  | Readonly<{ status: "success"; plan: QualityControlPlanDetail }>
  | ReadFailure;

export async function readQualityControlPlanById(
  planId: string,
): Promise<QualityControlPlanDetailResult> {
  const configResult = parseProductionExecutionRuntimeConfig();
  if (configResult.status !== "success") {
    return { status: "failure", message: configResult.message };
  }

  const contextResult = resolveProductionOrderRequestContext(configResult.config);
  if (contextResult.status === "failure") {
    return { status: "failure", message: contextResult.message };
  }

  const result = await getQualityControlPlan(contextResult.context, planId);
  if (result.status === "failure") {
    return { status: "failure", message: result.message };
  }

  return { status: "success", plan: result.value };
}
