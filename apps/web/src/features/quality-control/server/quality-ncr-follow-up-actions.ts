"use server";

import { revalidatePath } from "next/cache";

import { denyUnlessGranted } from "@/features/admin-access/server/require-current-permission";
import { parseProductionExecutionRuntimeConfig } from "@/features/production-execution/server/production-order-runtime-config";
import { resolveProductionOrderRequestContext } from "@/features/production-execution/server/production-order-request-context";

function revalidateQualityNcrFollowUpRoutes() {
  revalidatePath("/kalite/takip-ve-gorevler");
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

export async function createQualityNcrFollowUpTaskDraftAction(ncrId: string) {
  const authorization = await denyUnlessGranted("ai-task.create");
  if (authorization.status === "failure") return authorization;

  const contextResult = await resolveContext();
  if (contextResult.status === "failure") {
    return { status: "failure" as const, message: contextResult.message };
  }

  const { createQualityNcrFollowUpTaskDraft } = await import("./quality-ncr-follow-up-api-client");
  const result = await createQualityNcrFollowUpTaskDraft(contextResult.context, ncrId);
  if (result.status === "failure") {
    return { status: "failure" as const, message: result.message, code: result.code };
  }

  revalidateQualityNcrFollowUpRoutes();
  return {
    status: "success" as const,
    taskId: result.value.taskId,
    ncrId: result.value.ncrId,
    idempotentReplay: result.value.idempotentReplay,
  };
}

export async function transitionQualityNcrFollowUpAction(
  ncrId: string,
  expectedVersion: number,
  targetFollowUpStatus: string,
  note?: string,
) {
  const authorization = await denyUnlessGranted("quality.ncr.manage");
  if (authorization.status === "failure") return authorization;

  const contextResult = await resolveContext();
  if (contextResult.status === "failure") {
    return { status: "failure" as const, message: contextResult.message };
  }

  const { transitionQualityNcrFollowUp } = await import("./quality-ncr-follow-up-api-client");
  const result = await transitionQualityNcrFollowUp(contextResult.context, ncrId, {
    expectedVersion,
    targetFollowUpStatus,
    note,
  });
  if (result.status === "failure") {
    return { status: "failure" as const, message: result.message, code: result.code };
  }

  revalidateQualityNcrFollowUpRoutes();
  return { status: "success" as const };
}

export async function refreshQualityNcrFollowUpWorkspaceAction() {
  const authorization = await denyUnlessGranted("quality.read");
  if (authorization.status === "failure") return authorization;

  revalidateQualityNcrFollowUpRoutes();
  return { status: "success" as const };
}

export async function acknowledgeQualityNcrFollowUpAction(ncrId: string, expectedVersion: number) {
  return transitionQualityNcrFollowUpAction(ncrId, expectedVersion, "acknowledged");
}

export async function dismissQualityNcrFollowUpAction(
  ncrId: string,
  expectedVersion: number,
  reason: string,
) {
  if (!reason.trim()) {
    return { status: "failure" as const, message: "Red gerekçesi zorunludur." };
  }
  return transitionQualityNcrFollowUpAction(ncrId, expectedVersion, "dismissed", reason.trim());
}
