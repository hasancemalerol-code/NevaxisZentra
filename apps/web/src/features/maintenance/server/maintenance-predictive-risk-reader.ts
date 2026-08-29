import "./maintenance-server-boundary";

import {
  evaluateMaintenancePredictiveRisk,
  getMaintenancePredictiveRiskAssessment,
  getMaintenancePredictiveRiskAssessmentById,
  getMaintenancePredictiveRiskWorkspace,
} from "./maintenance-predictive-risk-api-client";
import { parseProductionExecutionRuntimeConfig } from "../../production-execution/server/production-order-runtime-config";
import {
  resolveProductionOrderRequestContext,
} from "../../production-execution/server/production-order-request-context";
import type {
  MaintenancePredictiveRiskAssessment,
  MaintenancePredictiveRiskWorkspacePayload,
} from "../domain/maintenance-predictive-risk";

export type MaintenancePredictiveRiskWorkspaceResult =
  | Readonly<{
      status: "success";
      workspace: MaintenancePredictiveRiskWorkspacePayload;
      assessment?: MaintenancePredictiveRiskAssessment;
      assessmentError?: string;
    }>
  | Readonly<{ status: "failure"; message: string }>;

export async function readMaintenancePredictiveRiskWorkspace(
  assetReference?: string,
  assessmentId?: string,
): Promise<MaintenancePredictiveRiskWorkspaceResult> {
  const configResult = parseProductionExecutionRuntimeConfig();
  if (configResult.status !== "success") {
    return { status: "failure", message: configResult.message };
  }

  const contextResult = resolveProductionOrderRequestContext(configResult.config);
  if (contextResult.status === "failure") {
    return { status: "failure", message: contextResult.message };
  }

  const context = contextResult.context;
  await evaluateMaintenancePredictiveRisk(context);

  const workspaceResult = await getMaintenancePredictiveRiskWorkspace(context);
  if (workspaceResult.status === "failure") {
    return { status: "failure", message: workspaceResult.message };
  }

  const selectedAsset = assetReference ?? workspaceResult.value.assets[0]?.assetReference;
  let assessment: MaintenancePredictiveRiskAssessment | undefined;
  let assessmentError: string | undefined;

  if (assessmentId) {
    const byIdResult = await getMaintenancePredictiveRiskAssessmentById(context, assessmentId);
    if (byIdResult.status === "success") {
      assessment = byIdResult.value;
    } else {
      assessmentError = byIdResult.message;
    }
  } else if (selectedAsset) {
    const assessmentResult = await getMaintenancePredictiveRiskAssessment(context, selectedAsset);
    if (assessmentResult.status === "success") {
      assessment = assessmentResult.value;
    } else {
      assessmentError = assessmentResult.message;
    }
  }

  return {
    status: "success",
    workspace: workspaceResult.value,
    assessment,
    assessmentError,
  };
}
