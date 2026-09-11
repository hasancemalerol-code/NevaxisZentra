import { denyUnlessGranted } from "@/features/admin-access/server/require-current-permission";
import { parseProductionExecutionRuntimeConfig } from "@/features/production-execution/server/production-order-runtime-config";
import { resolveProductionOrderRequestContext } from "@/features/production-execution/server/production-order-request-context";

import { getProductionManagerDecision, getProductionManagerWorkspace } from "./production-manager-api-client";

async function resolveAuthorizedContext(permission = "production-manager.read") {
  const authorization = await denyUnlessGranted(permission);
  if (authorization.status === "failure") {
    return {
      status: "failure" as const,
      code: "permission-denied",
      message: "AI Üretim Yöneticisi görüntüleme yetkiniz yok.",
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

export async function readProductionManagerWorkspace() {
  const resolved = await resolveAuthorizedContext();
  if (resolved.status === "failure") {
    return resolved;
  }

  const workspace = await getProductionManagerWorkspace(resolved.context);
  if (workspace.status === "failure") {
    return { status: "failure" as const, code: workspace.code, message: workspace.message };
  }

  return { status: "success" as const, workspace: workspace.value };
}

export async function readProductionManagerDashboardPanel() {
  const result = await readProductionManagerWorkspace();
  if (result.status === "failure") {
    return { status: "failure" as const };
  }

  const { workspace } = result;
  const topPrediction = workspace.predictions[0];
  const topRecommendation = workspace.pendingDecisions[0] ?? workspace.recentDecisions[0];

  return {
    status: "success" as const,
    topPrediction,
    topRecommendation,
    pendingApprovalCount: workspace.executive.pendingDecisionCount,
    autonomousActionCount: workspace.executive.autonomousActionCount,
    verifiedTodayCount: workspace.executive.verifiedTodayCount,
  };
}

export async function readProductionManagerDecisionDetail(decisionId: string) {
  const resolved = await resolveAuthorizedContext();
  if (resolved.status === "failure") {
    return resolved;
  }

  const decision = await getProductionManagerDecision(resolved.context, decisionId);
  if (decision.status === "failure") {
    return { status: "failure" as const, code: decision.code, message: decision.message };
  }

  return { status: "success" as const, decision: decision.value };
}
