"use server";

import { revalidatePath } from "next/cache";
import { redirect } from "next/navigation";

import {
  addShiftHandoverItem,
  createShiftHandover,
  createShiftHandoverFollowUpTask,
  postShiftHandoverAction,
} from "./shift-handover-api-client";
import { resolveShiftHandoverRequestContext } from "./shift-handover-request-context";

export async function createShiftHandoverAction(formData: FormData) {
  const contextResult = await resolveShiftHandoverRequestContext();
  if (contextResult.status === "failure") {
    return { status: "failure" as const, message: contextResult.message };
  }

  const result = await createShiftHandover(contextResult.context, {
    shiftDate: String(formData.get("shiftDate") ?? ""),
    shiftPeriod: String(formData.get("shiftPeriod") ?? "morning"),
    department: String(formData.get("department") ?? ""),
    summary: String(formData.get("summary") ?? ""),
    incomingMembershipId: formData.get("incomingMembershipId")?.toString() || null,
  });

  if (result.status === "failure") {
    return { status: "failure" as const, message: result.message };
  }

  revalidatePath("/vardiya-devir");
  redirect(`/vardiya-devir/${result.value.id}`);
}

export async function addShiftHandoverItemAction(handoverId: string, formData: FormData) {
  const contextResult = await resolveShiftHandoverRequestContext();
  if (contextResult.status === "failure") {
    return { status: "failure" as const, message: contextResult.message };
  }

  const result = await addShiftHandoverItem(contextResult.context, handoverId, {
    category: String(formData.get("category") ?? "general"),
    title: String(formData.get("title") ?? ""),
    description: String(formData.get("description") ?? ""),
    severity: String(formData.get("severity") ?? "medium"),
    requiresFollowUp: formData.get("requiresFollowUp") === "on",
  });

  if (result.status === "failure") {
    return { status: "failure" as const, message: result.message };
  }

  revalidatePath(`/vardiya-devir/${handoverId}`);
  return { status: "success" as const };
}

export async function shiftHandoverLifecycleAction(
  handoverId: string,
  action: "ready" | "handover" | "acknowledge" | "complete",
  expectedVersion: number,
) {
  const contextResult = await resolveShiftHandoverRequestContext();
  if (contextResult.status === "failure") {
    return { status: "failure" as const, message: contextResult.message };
  }

  const result = await postShiftHandoverAction(contextResult.context, handoverId, action, {
    expectedVersion,
  });

  if (result.status === "failure") {
    return { status: "failure" as const, message: result.message };
  }

  revalidatePath(`/vardiya-devir/${handoverId}`);
  revalidatePath("/vardiya-devir");
  return { status: "success" as const, version: result.value.version };
}

export async function createFollowUpTaskAction(handoverId: string, itemId: string) {
  const contextResult = await resolveShiftHandoverRequestContext();
  if (contextResult.status === "failure") {
    return { status: "failure" as const, message: contextResult.message };
  }

  const result = await createShiftHandoverFollowUpTask(contextResult.context, handoverId, itemId);
  if (result.status === "failure") {
    return { status: "failure" as const, message: result.message };
  }

  revalidatePath(`/vardiya-devir/${handoverId}`);
  return {
    status: "success" as const,
    taskId: result.value.taskId,
    deepLinkPath: result.value.deepLinkPath,
  };
}
