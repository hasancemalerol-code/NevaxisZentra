"use server";

import { revalidatePath } from "next/cache";

import { denyUnlessGranted } from "@/features/admin-access/server/require-current-permission";
import { parseProductionExecutionRuntimeConfig } from "@/features/production-execution/server/production-order-runtime-config";
import { resolveProductionOrderRequestContext } from "@/features/production-execution/server/production-order-request-context";

function revalidateMaintenancePredictiveRiskRoutes() {
  revalidatePath("/bakim/kestirimci-riskler");
  revalidatePath("/bakim/durum-zekasi");
}

async function resolveContext() {
  const configResult = parseProductionExecutionRuntimeConfig();
  if (configResult.status !== "success") {
    return { status: "failure" as const, message: configResult.message };
  }
  const contextResult = resolveProductionOrderRequestContext(configResult.config);
  if (contextResult.status === "failure") {
    return { status: "failure" as const, message: contextResult.message };
  }
  return { status: "success" as const, context: contextResult.context };
}

export async function createMaintenancePredictiveRiskTaskDraftAction(assessmentId: string) {
  const authorization = await denyUnlessGranted("ai-task.create");
  if (authorization.status === "failure") return authorization;

  const contextResult = await resolveContext();
  if (contextResult.status === "failure") {
    return { status: "failure" as const, message: contextResult.message };
  }

  const { createMaintenancePredictiveRiskTaskDraft } = await import("./maintenance-predictive-risk-api-client");
  const result = await createMaintenancePredictiveRiskTaskDraft(contextResult.context, assessmentId);
  if (result.status === "failure") {
    return { status: "failure" as const, message: result.message, code: result.code };
  }

  revalidateMaintenancePredictiveRiskRoutes();
  return {
    status: "success" as const,
    taskId: result.value.taskId,
    assessmentId: result.value.assessmentId,
    idempotentReplay: result.value.idempotentReplay,
  };
}

export async function transitionMaintenancePredictiveRiskFollowUpAction(
  assessmentId: string,
  targetFollowUpStatus: string,
  note?: string,
) {
  const authorization = await denyUnlessGranted("maintenance.manage");
  if (authorization.status === "failure") return authorization;

  const contextResult = await resolveContext();
  if (contextResult.status === "failure") {
    return { status: "failure" as const, message: contextResult.message };
  }

  const { transitionMaintenancePredictiveRiskFollowUp } = await import("./maintenance-predictive-risk-api-client");
  const result = await transitionMaintenancePredictiveRiskFollowUp(contextResult.context, assessmentId, {
    targetFollowUpStatus,
    note,
  });
  if (result.status === "failure") {
    return { status: "failure" as const, message: result.message, code: result.code };
  }

  revalidateMaintenancePredictiveRiskRoutes();
  return { status: "success" as const };
}

export async function refreshMaintenancePredictiveRiskWorkspaceAction() {
  const authorization = await denyUnlessGranted("maintenance.read");
  if (authorization.status === "failure") return authorization;

  revalidateMaintenancePredictiveRiskRoutes();
  return { status: "success" as const };
}
