import { denyUnlessGranted } from "@/features/admin-access/server/require-current-permission";
import { parseProductionExecutionRuntimeConfig } from "@/features/production-execution/server/production-order-runtime-config";
import { resolveProductionOrderRequestContext } from "@/features/production-execution/server/production-order-request-context";

import {
  evaluateAdaptiveReplan,
  getAdaptivePlanningWorkspace,
  type AdaptivePlanningWorkspaceApi,
  type ReplanEvaluationResultApi,
} from "./adaptive-planning-api-client";

async function resolveAuthorizedContext(permission = "production.planning.read") {
  const authorization = await denyUnlessGranted(permission);
  if (authorization.status === "failure") {
    return {
      status: "failure" as const,
      code: "permission-denied",
      message: "Uyarlanabilir planlama görüntüleme yetkiniz yok.",
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

export async function readAdaptivePlanningWorkspace() {
  const resolved = await resolveAuthorizedContext();
  if (resolved.status === "failure") {
    return resolved;
  }

  const workspace = await getAdaptivePlanningWorkspace(resolved.context);
  if (workspace.status === "failure") {
    return { status: "failure" as const, code: workspace.code, message: workspace.message };
  }

  return { status: "success" as const, workspace: workspace.value };
}

export async function readAdaptivePlanningWorkspaceWithContext() {
  const resolved = await resolveAuthorizedContext();
  if (resolved.status === "failure") {
    return resolved;
  }

  const workspace = await getAdaptivePlanningWorkspace(resolved.context);
  if (workspace.status === "failure") {
    return { status: "failure" as const, code: workspace.code, message: workspace.message };
  }

  return {
    status: "success" as const,
    context: resolved.context,
    workspace: workspace.value,
  };
}

async function readLatestReplanEvaluation(
  workspace: AdaptivePlanningWorkspaceApi,
  context: Awaited<ReturnType<typeof resolveAuthorizedContext>> & { status: "success" },
): Promise<ReplanEvaluationResultApi | null> {
  if (!workspace.activePlan) {
    return null;
  }

  const latestDeviation = workspace.deviations[0];
  const triggerType = latestDeviation?.deviationType ?? "scheduled-review";
  const triggerContextJson = latestDeviation
    ? JSON.stringify({ deviationId: latestDeviation.id, summary: latestDeviation.summary })
    : "{}";

  const evaluation = await evaluateAdaptiveReplan(context.context, {
    productionPlanId: workspace.activePlan.id,
    triggerType,
    triggerContextJson,
  });

  return evaluation.status === "success" ? evaluation.value : null;
}

export async function readAdaptivePlanningWorkspaceDetail() {
  const resolved = await resolveAuthorizedContext();
  if (resolved.status === "failure") {
    return resolved;
  }

  const workspaceResult = await getAdaptivePlanningWorkspace(resolved.context);
  if (workspaceResult.status === "failure") {
    return { status: "failure" as const, code: workspaceResult.code, message: workspaceResult.message };
  }

  const workspace = workspaceResult.value;
  const replanEvaluation = await readLatestReplanEvaluation(workspace, resolved);

  return {
    status: "success" as const,
    workspace,
    replanEvaluation,
  };
}

export async function readAdaptivePlanningDashboardPanel() {
  const result = await readAdaptivePlanningWorkspace();
  if (result.status === "failure") {
    return { status: "failure" as const };
  }

  const { workspace } = result;
  const lastReplan = workspace.recentChanges.find((change) => change.changeType === "replan") ?? workspace.recentChanges[0];
  const nextCriticalRisk =
    workspace.risks.deadlineRisks[0] ??
    (workspace.deviations[0]
      ? {
          orderId: "—",
          riskLevel: workspace.deviations[0].severity,
          deadlineUtc: workspace.deviations[0].detectedAtUtc,
          expectedCompletionUtc: undefined,
          confidenceScore: 0,
        }
      : undefined);

  return {
    status: "success" as const,
    metrics: workspace.metrics,
    activePlan: workspace.activePlan,
    activeVersion: workspace.activeVersion,
    lastReplan,
    nextCriticalRisk,
    capacityOverloads: workspace.risks.capacityOverloads,
  };
}

export async function readAdaptivePlanningShiftBrief() {
  const result = await readAdaptivePlanningWorkspace();
  if (result.status === "failure") {
    return result;
  }

  const { workspace } = result;
  const priorityOrders = workspace.risks.deadlineRisks.slice(0, 5);
  const machineAssignments = [...new Set(workspace.operations.map((op) => op.machineId).filter(Boolean))].slice(0, 6);
  const criticalMaterials = workspace.risks.materialRisks.slice(0, 4);
  const bottlenecks = workspace.risks.capacityOverloads.slice(0, 3);

  return {
    status: "success" as const,
    workspace,
    priorityOrders,
    machineAssignments,
    criticalMaterials,
    bottlenecks,
  };
}
