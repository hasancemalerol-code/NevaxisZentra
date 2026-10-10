import { gateAccess } from "@/features/admin-access/server/require-current-permission";
import "server-only";

import { hasGrantedPermission } from "@/features/admin-access/server/admin-access-effective-access";
import { acceptanceRoleRoute, requiredRoutePermission } from "@/lib/authorization/acceptance-role-contract";
import { parseProductionExecutionRuntimeConfig } from "@/features/production-execution/server/production-order-runtime-config";
import { resolveProductionOrderRequestContextForIncomingRequest } from "@/features/production-execution/server/production-order-request-context";

import {
  readScenarioRecoveryHistory,
  readScenarioRecoveryWorkspace,
  type ScenarioRecoveryHistoryItemApi,
  type ScenarioRecoveryWorkspaceApi,
} from "./scenario-recovery-api-client";
import { buildScenarioRecoveryHref } from "../application/scenario-recovery-links";

export { buildScenarioRecoveryHref };

export type ScenarioRecoveryPageParams = Readonly<{
  runId?: string;
  lineId?: string;
  entryPoint?: string;
  orderReference?: string;
  constraintStationCode?: string;
}>;

export type ScenarioRecoveryPageResult =
  | Readonly<{ status: "success"; workspace: ScenarioRecoveryWorkspaceApi; history: readonly ScenarioRecoveryHistoryItemApi[] }>
  | Readonly<{
      status: "ready";
      lineId: string;
      entryPoint?: string;
      orderReference?: string;
      constraintStationCode?: string;
      canSimulate: boolean;
      history: readonly ScenarioRecoveryHistoryItemApi[];
    }>
  | Readonly<{ status: "forbidden"; message: string }>
  | Readonly<{ status: "failure"; message: string }>;

export async function readScenarioRecoveryPage(
  params: ScenarioRecoveryPageParams,
): Promise<ScenarioRecoveryPageResult> {
  const gate = await gateAccess([requiredRoutePermission("senaryo")]);
  if (gate.status === "failure") {
    // Only a real denial is "forbidden"; a missing session or an unavailable access service is a load failure.
    return gate.code === "authorization-denied"
      ? { status: "forbidden", message: "Senaryo kurtarma okuma izni gerekli." }
      : { status: "failure", message: gate.message };
  }

  const access = gate.access;

  const configResult = parseProductionExecutionRuntimeConfig();
  if (configResult.status !== "success") {
    return { status: "failure", message: configResult.message };
  }

  const contextResult = await resolveProductionOrderRequestContextForIncomingRequest(configResult.config);
  if (contextResult.status === "failure") {
    return { status: "failure", message: contextResult.message };
  }

  const context = contextResult.context;

  if (params.runId) {
    const workspaceResult = await readScenarioRecoveryWorkspace(context, params.runId);
    if (workspaceResult.status === "failure") {
      return { status: "failure", message: workspaceResult.message };
    }

    const historyResult = await readScenarioRecoveryHistory(context, workspaceResult.value.lineId);
    return {
      status: "success",
      workspace: workspaceResult.value,
      history: historyResult.status === "success" ? historyResult.value : [],
    };
  }

  if (!params.lineId) {
    return { status: "failure", message: "Senaryo kurtarma bir üretim hattı bağlamı gerektirir: bir hat veya mevcut bir senaryo çalıştırması seçilmeden başlatılamaz." };
  }

  // A line without a run: viewing is read-only. Creating a scenario is an explicit action (scenario-recovery-actions.ts),
  // never a side effect of loading this page.
  const historyResult = await readScenarioRecoveryHistory(context, params.lineId);
  return {
    status: "ready",
    lineId: params.lineId,
    entryPoint: params.entryPoint,
    orderReference: params.orderReference,
    constraintStationCode: params.constraintStationCode,
    canSimulate: hasGrantedPermission(access, acceptanceRoleRoute("senaryo").simulatePermissionKey ?? ""),
    history: historyResult.status === "success" ? historyResult.value : [],
  };
}
