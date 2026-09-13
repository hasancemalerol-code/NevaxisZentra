import { denyUnlessGranted } from "@/features/admin-access/server/require-current-permission";
import { parseProductionExecutionRuntimeConfig } from "@/features/production-execution/server/production-order-runtime-config";
import { resolveProductionOrderRequestContext } from "@/features/production-execution/server/production-order-request-context";

import { getFactoryLearningWorkspace } from "./factory-learning-api-client";

async function resolveAuthorizedContext(permission = "continuous-improvement.read") {
  const authorization = await denyUnlessGranted(permission);
  if (authorization.status === "failure") {
    return {
      status: "failure" as const,
      code: "permission-denied",
      message: "Sürekli iyileştirme görüntüleme yetkiniz yok.",
    };
  }

  const configResult = parseProductionExecutionRuntimeConfig();
  if (configResult.status !== "success") {
    return { status: "failure" as const, code: "configuration-error", message: configResult.message };
  }

  const contextResult = resolveProductionOrderRequestContext(configResult.config);
  if (contextResult.status === "failure") {
    return { status: "failure" as const, code: contextResult.code, message: contextResult.message };
  }

  return { status: "success" as const, context: contextResult.context };
}

export async function readFactoryLearningWorkspace() {
  const resolved = await resolveAuthorizedContext();
  if (resolved.status === "failure") {
    return resolved;
  }

  const workspace = await getFactoryLearningWorkspace(resolved.context);
  if (workspace.status === "failure") {
    return { status: "failure" as const, code: workspace.code, message: workspace.message };
  }

  return { status: "success" as const, workspace: workspace.value };
}

export async function readFactoryLearningDashboardPanel() {
  const result = await readFactoryLearningWorkspace();
  if (result.status === "failure") {
    return { status: "failure" as const };
  }

  const { workspace } = result;
  const topPattern = workspace.patterns[0];
  const topOpportunity = workspace.opportunities[0];
  const activeExperiment = workspace.activeExperiments[0];
  const topInsight = workspace.insights[0];

  return {
    status: "success" as const,
    commandSummary: workspace.commandSummary,
    topPattern,
    topOpportunity,
    activeExperiment,
    topInsight,
  };
}
