"use server";

import { revalidatePath } from "next/cache";

import { denyUnlessGranted } from "@/features/admin-access/server/require-current-permission";
import { parseProductionExecutionRuntimeConfig } from "./production-order-runtime-config";
import { resolveProductionOrderRequestContextForIncomingRequest } from "./production-order-request-context";

function revalidateProductionFollowUpRoutes() {
  revalidatePath("/uretim/istisnalar-ve-gorevler");
}

async function resolveContext() {
  const configResult = parseProductionExecutionRuntimeConfig();
  if (configResult.status === "failure") {
    return { status: "failure" as const, message: configResult.message };
  }
  const contextResult = await resolveProductionOrderRequestContextForIncomingRequest(configResult.config);
  if (contextResult.status === "failure") {
    return { status: "failure" as const, message: contextResult.message };
  }
  return { status: "success" as const, context: contextResult.context };
}

export async function createProductionFollowUpTaskDraftAction(exceptionId: string) {
  const authorization = await denyUnlessGranted("ai-task.create");
  if (authorization.status === "failure") return authorization;

  const contextResult = await resolveContext();
  if (contextResult.status === "failure") {
    return { status: "failure" as const, message: contextResult.message };
  }

  const { createProductionFollowUpTaskDraft } = await import("./production-follow-up-api-client");
  const result = await createProductionFollowUpTaskDraft(contextResult.context, exceptionId);
  if (result.status === "failure") {
    return { status: "failure" as const, message: result.message, code: result.code };
  }

  revalidateProductionFollowUpRoutes();
  return {
    status: "success" as const,
    taskId: result.value.taskId,
    exceptionId: result.value.exceptionId,
    idempotentReplay: result.value.idempotentReplay,
  };
}

export async function transitionProductionFollowUpAction(
  exceptionId: string,
  expectedVersion: number,
  targetFollowUpStatus: string,
  note?: string,
) {
  const authorization = await denyUnlessGranted("production.execution.exception");
  if (authorization.status === "failure") return authorization;

  const contextResult = await resolveContext();
  if (contextResult.status === "failure") {
    return { status: "failure" as const, message: contextResult.message };
  }

  const { transitionProductionFollowUp } = await import("./production-follow-up-api-client");
  const result = await transitionProductionFollowUp(contextResult.context, exceptionId, {
    expectedVersion,
    targetFollowUpStatus,
    note,
  });
  if (result.status === "failure") {
    return { status: "failure" as const, message: result.message, code: result.code };
  }

  revalidateProductionFollowUpRoutes();
  return { status: "success" as const };
}

export async function refreshProductionFollowUpWorkspaceAction() {
  const authorization = await denyUnlessGranted("production.execution.read");
  if (authorization.status === "failure") return authorization;

  revalidateProductionFollowUpRoutes();
  return { status: "success" as const };
}

export async function acknowledgeProductionFollowUpAction(exceptionId: string, expectedVersion: number) {
  return transitionProductionFollowUpAction(exceptionId, expectedVersion, "acknowledged");
}
