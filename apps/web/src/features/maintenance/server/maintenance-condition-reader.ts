import "./maintenance-server-boundary";

import {
  evaluateMaintenanceCondition,
  getMaintenanceConditionOverview,
  getMaintenanceConditionProfile,
} from "./maintenance-condition-api-client";
import { parseProductionExecutionRuntimeConfig } from "../../production-execution/server/production-order-runtime-config";
import {
  resolveProductionOrderRequestContext,
  type ProductionOrderRequestContext,
} from "../../production-execution/server/production-order-request-context";
import type {
  MaintenanceConditionProfile,
  MaintenanceConditionWorkspace,
} from "../domain/maintenance-condition";

type ReadFailure = Readonly<{
  status: "failure";
  message: string;
}>;

export type MaintenanceConditionWorkspaceResult =
  | Readonly<{
      status: "success";
      workspace: MaintenanceConditionWorkspace;
      profile?: MaintenanceConditionProfile;
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

  const contextResult = resolveProductionOrderRequestContext(configResult.config);
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

  return {
    status: "success",
    workspace: overviewResult.value,
    profile,
  };
}
