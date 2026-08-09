import "./quality-control-server-boundary";

import {
  getQualityControlPlan,
  getQualityInspection,
  getQualityNcr,
  getQualityWorkspaceSummary,
  listQualityControlPlans,
  listQualityInspections,
  listQualityNcrs,
  type QualityNcrListFilters,
} from "./quality-control-api-client";
import { parseProductionExecutionRuntimeConfig } from "../../production-execution/server/production-order-runtime-config";
import { resolveProductionOrderRequestContext } from "../../production-execution/server/production-order-request-context";
import type {
  QualityControlPlanDetail,
  QualityControlPlanSummary,
  QualityInspectionDetail,
  QualityInspectionSummary,
  QualityNcrDetail,
  QualityNcrSummary,
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

export type QualityNcrListResult =
  | Readonly<{ status: "success"; ncrs: readonly QualityNcrSummary[] }>
  | ReadFailure;

export async function readQualityNcrList(
  filters?: QualityNcrListFilters,
): Promise<QualityNcrListResult> {
  const configResult = parseProductionExecutionRuntimeConfig();
  if (configResult.status !== "success") {
    return { status: "failure", message: configResult.message };
  }

  const contextResult = resolveProductionOrderRequestContext(configResult.config);
  if (contextResult.status === "failure") {
    return { status: "failure", message: contextResult.message };
  }

  const result = await listQualityNcrs(contextResult.context, filters);
  if (result.status === "failure") {
    return { status: "failure", message: result.message };
  }

  return { status: "success", ncrs: result.value };
}

export type QualityNcrDetailResult =
  | Readonly<{ status: "success"; ncr: QualityNcrDetail }>
  | ReadFailure;

export async function readQualityNcrById(ncrId: string): Promise<QualityNcrDetailResult> {
  const configResult = parseProductionExecutionRuntimeConfig();
  if (configResult.status !== "success") {
    return { status: "failure", message: configResult.message };
  }

  const contextResult = resolveProductionOrderRequestContext(configResult.config);
  if (contextResult.status === "failure") {
    return { status: "failure", message: contextResult.message };
  }

  const result = await getQualityNcr(contextResult.context, ncrId);
  if (result.status === "failure") {
    return { status: "failure", message: result.message };
  }

  return { status: "success", ncr: result.value };
}
