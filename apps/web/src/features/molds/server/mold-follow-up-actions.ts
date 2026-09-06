"use server";

import { revalidatePath } from "next/cache";

import { denyUnlessGranted } from "@/features/admin-access/server/require-current-permission";
import { resolveMoldRequestContext } from "./mold-request-context";

function revalidateMoldFollowUpRoutes() {
  revalidatePath("/kaliplar/takip-ve-gorevler");
}

async function resolveContext() {
  const contextResult = await resolveMoldRequestContext();
  if (contextResult.status === "failure") {
    return { status: "failure" as const, message: contextResult.message };
  }
  return { status: "success" as const, context: contextResult.context };
}

export async function createMoldFollowUpTaskDraftAction(moldId: string) {
  const authorization = await denyUnlessGranted("ai-task.create");
  if (authorization.status === "failure") return authorization;

  const contextResult = await resolveContext();
  if (contextResult.status === "failure") {
    return { status: "failure" as const, message: contextResult.message };
  }

  const { createMoldFollowUpTaskDraft } = await import("./mold-follow-up-api-client");
  const result = await createMoldFollowUpTaskDraft(contextResult.context, moldId);
  if (result.status === "failure") {
    return { status: "failure" as const, message: result.message, code: result.code };
  }

  revalidateMoldFollowUpRoutes();
  return {
    status: "success" as const,
    taskId: result.value.taskId,
    moldId: result.value.moldId,
    idempotentReplay: result.value.idempotentReplay,
  };
}

export async function transitionMoldFollowUpAction(
  moldId: string,
  expectedVersion: number,
  targetFollowUpStatus: string,
  note?: string,
) {
  const authorization = await denyUnlessGranted("mold.manage");
  if (authorization.status === "failure") return authorization;

  const contextResult = await resolveContext();
  if (contextResult.status === "failure") {
    return { status: "failure" as const, message: contextResult.message };
  }

  const { transitionMoldFollowUp } = await import("./mold-follow-up-api-client");
  const result = await transitionMoldFollowUp(contextResult.context, moldId, {
    expectedVersion,
    targetFollowUpStatus,
    note,
  });
  if (result.status === "failure") {
    return { status: "failure" as const, message: result.message, code: result.code };
  }

  revalidateMoldFollowUpRoutes();
  return { status: "success" as const };
}

export async function refreshMoldFollowUpWorkspaceAction() {
  const authorization = await denyUnlessGranted("mold.read");
  if (authorization.status === "failure") return authorization;

  revalidateMoldFollowUpRoutes();
  return { status: "success" as const };
}

export async function acknowledgeMoldFollowUpAction(moldId: string, expectedVersion: number) {
  return transitionMoldFollowUpAction(moldId, expectedVersion, "acknowledged");
}

export async function dismissMoldFollowUpAction(
  moldId: string,
  expectedVersion: number,
  reason: string,
) {
  if (!reason.trim()) {
    return { status: "failure" as const, message: "Red gerekçesi zorunludur." };
  }
  return transitionMoldFollowUpAction(moldId, expectedVersion, "dismissed", reason.trim());
}
