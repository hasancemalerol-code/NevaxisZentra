import "./maintenance-server-boundary";

import {
  evaluateMaintenanceCondition,
  getMaintenanceConditionOverview,
  getMaintenanceConditionProfile,
} from "./maintenance-condition-api-client";
import {
  evaluateMaintenancePredictiveRisk,
  getMaintenancePredictiveRiskAssessment,
  getMaintenancePredictiveRiskOverview,
} from "./maintenance-predictive-risk-api-client";
import { parseProductionExecutionRuntimeConfig } from "../../production-execution/server/production-order-runtime-config";
import {
  resolveProductionOrderRequestContextForIncomingRequest,
  type ProductionOrderRequestContext,
} from "../../production-execution/server/production-order-request-context";
import type {
  MaintenanceConditionProfile,
  MaintenanceConditionWorkspace,
} from "../domain/maintenance-condition";
import type {
  MaintenancePredictiveRiskAssessment,
  MaintenancePredictiveRiskWorkspace,
} from "../domain/maintenance-predictive-risk";
import type { MaintenanceAiDraftDetail } from "../domain/maintenance-ai-agent";
import { getMaintenanceAiCurrentDraft } from "./maintenance-ai-agent-api-client";

type ReadFailure = Readonly<{
  status: "failure";
  message: string;
}>;

export type MaintenanceConditionWorkspaceResult =
  | Readonly<{
      status: "success";
      workspace: MaintenanceConditionWorkspace;
      profile?: MaintenanceConditionProfile;
      riskWorkspace?: MaintenancePredictiveRiskWorkspace;
      riskAssessment?: MaintenancePredictiveRiskAssessment;
      riskError?: string;
      aiDraft?: MaintenanceAiDraftDetail;
      aiDraftError?: string;
      canManageMaintenance?: boolean;
    }>
  | ReadFailure;

export type MaintenanceConditionOverviewSnapshotResult =
  | Readonly<{
      status: "success";
      workspace: MaintenanceConditionWorkspace;
    }>
  | ReadFailure;

async function resolveMaintenanceConditionContext(): Promise<
  | Readonly<{ status: "success"; context: ProductionOrderRequestContext }>
  | ReadFailure
> {
  const configResult = parseProductionExecutionRuntimeConfig();
  if (configResult.status !== "success") {
    return { status: "failure", message: configResult.message };
  }

  const contextResult = await resolveProductionOrderRequestContextForIncomingRequest(configResult.config);
  if (contextResult.status === "failure") {
    return { status: "failure", message: contextResult.message };
  }

  return { status: "success", context: contextResult.context };
}

export async function readMaintenanceConditionOverviewSnapshot(): Promise<MaintenanceConditionOverviewSnapshotResult> {
  const contextResult = await resolveMaintenanceConditionContext();
  if (contextResult.status === "failure") {
    return contextResult;
  }

  const overviewResult = await getMaintenanceConditionOverview(contextResult.context);
  if (overviewResult.status === "failure") {
    return { status: "failure", message: overviewResult.message };
  }

  return { status: "success", workspace: overviewResult.value };
}

export async function readMaintenanceConditionWorkspace(
  assetReference?: string,
): Promise<MaintenanceConditionWorkspaceResult> {
  const contextResult = await resolveMaintenanceConditionContext();
  if (contextResult.status === "failure") {
    return contextResult;
  }

  const context = contextResult.context;
  await evaluateMaintenanceCondition(context);
  await evaluateMaintenancePredictiveRisk(context);

  const overviewResult = await getMaintenanceConditionOverview(context);
  if (overviewResult.status === "failure") {
    return { status: "failure", message: overviewResult.message };
  }

  const selectedAsset = assetReference ?? overviewResult.value.assets[0]?.assetReference;
  let profile: MaintenanceConditionProfile | undefined;
  if (selectedAsset) {
    const profileResult = await getMaintenanceConditionProfile(context, selectedAsset);
    if (profileResult.status === "success") {
      profile = profileResult.value;
    }
  }

  let riskWorkspace: MaintenancePredictiveRiskWorkspace | undefined;
  let riskAssessment: MaintenancePredictiveRiskAssessment | undefined;
  let riskError: string | undefined;

  const riskOverviewResult = await getMaintenancePredictiveRiskOverview(context);
  if (riskOverviewResult.status === "success") {
    riskWorkspace = riskOverviewResult.value;
  } else {
    riskError = riskOverviewResult.message;
  }

  if (selectedAsset) {
    const riskAssessmentResult = await getMaintenancePredictiveRiskAssessment(context, selectedAsset);
    if (riskAssessmentResult.status === "success") {
      riskAssessment = riskAssessmentResult.value;
    } else if (!riskError) {
      riskError = riskAssessmentResult.message;
    }
  }

  let aiDraft: MaintenanceAiDraftDetail | undefined;
  let aiDraftError: string | undefined;
  if (selectedAsset) {
    const draftResult = await getMaintenanceAiCurrentDraft(context, selectedAsset);
    if (draftResult.status === "success") {
      aiDraft = draftResult.value;
    } else if (draftResult.code !== "not-found") {
      aiDraftError = draftResult.message;
    }
  }

  const configResult = parseProductionExecutionRuntimeConfig();
  const canManageMaintenance =
    configResult.status === "success" &&
    configResult.config.mode === "development-api" &&
    configResult.config.developmentAuth?.subject === "production-pilot-operator";

  return {
    status: "success",
    workspace: overviewResult.value,
    profile,
    riskWorkspace,
    riskAssessment,
    riskError,
    aiDraft,
    aiDraftError,
    canManageMaintenance,
  };
}
