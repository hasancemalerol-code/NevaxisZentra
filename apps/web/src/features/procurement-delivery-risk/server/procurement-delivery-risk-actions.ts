"use server";

import { revalidatePath } from "next/cache";

import { denyUnlessGranted } from "@/features/admin-access/server/require-current-permission";
import { parseProductionExecutionRuntimeConfig } from "../../production-execution/server/production-order-runtime-config";
import { resolveProductionOrderRequestContext } from "../../production-execution/server/production-order-request-context";

function revalidateProcurementDeliveryRiskRoutes() {
  revalidatePath("/satin-alma/tedarik-riskleri");
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

export async function evaluateProcurementDeliveryRiskAction(
  purchaseOrderLineId: string,
  idempotencyKey: string,
) {
  const authorization = await denyUnlessGranted("erp.manage");
  if (authorization.status === "failure") return authorization;

  const contextResult = await resolveContext();
  if (contextResult.status === "failure") {
    return { status: "failure" as const, message: contextResult.message };
  }

  const { evaluateProcurementDeliveryRisk } = await import("./procurement-delivery-risk-api-client");
  const result = await evaluateProcurementDeliveryRisk(contextResult.context, {
    purchaseOrderLineId,
    idempotencyKey,
  });
  if (result.status === "failure") {
    return { status: "failure" as const, message: result.message, code: result.code };
  }

  revalidateProcurementDeliveryRiskRoutes();
  return {
    status: "success" as const,
    assessmentId: result.value.assessmentId,
    version: result.value.version,
    idempotentReplay: result.value.idempotentReplay,
  };
}

export async function refreshProcurementDeliveryRiskWorkspaceAction() {
  const authorization = await denyUnlessGranted("erp.read");
  if (authorization.status === "failure") return authorization;

  revalidateProcurementDeliveryRiskRoutes();
  return { status: "success" as const };
}

export async function createProcurementDeliveryRiskTaskDraftAction(assessmentId: string) {
  const authorization = await denyUnlessGranted("ai-task.create");
  if (authorization.status === "failure") return authorization;

  const contextResult = await resolveContext();
  if (contextResult.status === "failure") {
    return { status: "failure" as const, message: contextResult.message };
  }

  const { createProcurementDeliveryRiskTaskDraft } = await import("./procurement-delivery-risk-api-client");
  const result = await createProcurementDeliveryRiskTaskDraft(contextResult.context, assessmentId);
  if (result.status === "failure") {
    return { status: "failure" as const, message: result.message, code: result.code };
  }

  revalidateProcurementDeliveryRiskRoutes();
  return {
    status: "success" as const,
    taskId: result.value.taskId,
    assessmentId: result.value.assessmentId,
    idempotentReplay: result.value.idempotentReplay,
  };
}

export async function acknowledgeProcurementDeliveryRiskFollowUpAction(
  assessmentId: string,
  expectedVersion: number,
) {
  const authorization = await denyUnlessGranted("erp.manage");
  if (authorization.status === "failure") return authorization;

  const contextResult = await resolveContext();
  if (contextResult.status === "failure") {
    return { status: "failure" as const, message: contextResult.message };
  }

  const { transitionProcurementDeliveryRiskFollowUp } = await import("./procurement-delivery-risk-api-client");
  const result = await transitionProcurementDeliveryRiskFollowUp(contextResult.context, assessmentId, {
    expectedVersion,
    targetFollowUpStatus: "acknowledged",
  });
  if (result.status === "failure") {
    return { status: "failure" as const, message: result.message, code: result.code };
  }

  revalidateProcurementDeliveryRiskRoutes();
  return { status: "success" as const };
}
