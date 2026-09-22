import "server-only";

import { fetchCurrentEffectiveAccess, hasGrantedPermission } from "@/features/admin-access/server/admin-access-effective-access";
import { acceptanceRoleRoute, requiredRoutePermission } from "@/lib/authorization/acceptance-role-contract";
import { parseProductionExecutionRuntimeConfig } from "@/features/production-execution/server/production-order-runtime-config";
import { resolveProductionOrderRequestContext } from "@/features/production-execution/server/production-order-request-context";

import {
  createScenarioRecoveryRun,
  generateScenarioRecoveryOptions,
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
  | Readonly<{ status: "forbidden"; message: string }>
  | Readonly<{ status: "failure"; message: string }>;

export async function readScenarioRecoveryPage(
  params: ScenarioRecoveryPageParams,
): Promise<ScenarioRecoveryPageResult> {
  const access = await fetchCurrentEffectiveAccess();
  if (!hasGrantedPermission(access, requiredRoutePermission("senaryo"))) {
    return { status: "forbidden", message: "Senaryo kurtarma okuma izni gerekli." };
  }

  const configResult = parseProductionExecutionRuntimeConfig();
  if (configResult.status !== "success") {
    return { status: "failure", message: configResult.message };
  }

  const contextResult = resolveProductionOrderRequestContext(configResult.config);
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
    return { status: "failure", message: "Hat veya senaryo tanımlayıcısı gerekli." };
  }

  if (!hasGrantedPermission(access, acceptanceRoleRoute("senaryo").simulatePermissionKey ?? "")) {
    return { status: "forbidden", message: "Senaryo simülasyon izni gerekli." };
  }

  const createResult = await createScenarioRecoveryRun(context, {
    lineId: params.lineId,
    category: "PRODUCTION_RECOVERY",
    entryPoint: params.entryPoint ?? "DIRECT",
    problem: {
      summary: params.orderReference
        ? `Order ${params.orderReference} delivery risk with operational constraint`
        : "Operational recovery scenario",
      orderReference: params.orderReference,
      constraintStationCode: params.constraintStationCode,
      contributingFactors: ["throughput-gap", "constraint-detected"],
    },
    objectives: [
      { objectiveType: "PROTECT_OTIF", label: "Protect delivery commitment", priority: 3, isHardConstraint: true },
      { objectiveType: "MAXIMIZE_VERIFIED_GOOD_THROUGHPUT", label: "Increase good throughput", priority: 2, isHardConstraint: false },
    ],
    sourceEntityType: params.entryPoint?.toLowerCase() ?? "direct",
    sourceEntityId: params.lineId,
  });

  if (createResult.status === "failure") {
    return { status: "failure", message: createResult.message };
  }

  const optionsResult = await generateScenarioRecoveryOptions(context, createResult.value.runId);
  if (optionsResult.status === "failure") {
    return { status: "failure", message: optionsResult.message };
  }

  const historyResult = await readScenarioRecoveryHistory(context, params.lineId);
  return {
    status: "success",
    workspace: optionsResult.value,
    history: historyResult.status === "success" ? historyResult.value : [],
  };
}
