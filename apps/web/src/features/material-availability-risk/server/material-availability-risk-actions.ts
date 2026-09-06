"use server";

import { revalidatePath } from "next/cache";

import { denyUnlessGranted } from "@/features/admin-access/server/require-current-permission";
import { resolveMaterialRequestContext } from "../../materials/server/material-request-context";

function revalidateMaterialAvailabilityRiskRoutes() {
  revalidatePath("/stok-ve-depo/malzeme-riskleri");
}

async function resolveContext() {
  const contextResult = await resolveMaterialRequestContext();
  if (contextResult.status === "failure") {
    return { status: "failure" as const, message: contextResult.message };
  }
  return { status: "success" as const, context: contextResult.context };
}

export async function evaluateMaterialAvailabilityRiskAction(
  materialId: string,
  requirementProfileId: string,
  idempotencyKey: string,
) {
  const authorization = await denyUnlessGranted("material.manage");
  if (authorization.status === "failure") return authorization;

  const contextResult = await resolveContext();
  if (contextResult.status === "failure") {
    return { status: "failure" as const, message: contextResult.message };
  }

  const { evaluateMaterialAvailabilityRisk } = await import("./material-availability-risk-api-client");
  const result = await evaluateMaterialAvailabilityRisk(contextResult.context, {
    materialId,
    requirementProfileId,
    idempotencyKey,
  });
  if (result.status === "failure") {
    return { status: "failure" as const, message: result.message, code: result.code };
  }

  revalidateMaterialAvailabilityRiskRoutes();
  return {
    status: "success" as const,
    assessmentId: result.value.assessmentId,
    version: result.value.version,
    idempotentReplay: result.value.idempotentReplay,
  };
}

export async function refreshMaterialAvailabilityRiskWorkspaceAction() {
  const authorization = await denyUnlessGranted("material.read");
  if (authorization.status === "failure") return authorization;

  revalidateMaterialAvailabilityRiskRoutes();
  return { status: "success" as const };
}

export async function createMaterialAvailabilityRiskTaskDraftAction(assessmentId: string) {
  const authorization = await denyUnlessGranted("ai-task.create");
  if (authorization.status === "failure") return authorization;

  const contextResult = await resolveContext();
  if (contextResult.status === "failure") {
    return { status: "failure" as const, message: contextResult.message };
  }

  const { createMaterialAvailabilityRiskTaskDraft } = await import("./material-availability-risk-api-client");
  const result = await createMaterialAvailabilityRiskTaskDraft(contextResult.context, assessmentId);
  if (result.status === "failure") {
    return { status: "failure" as const, message: result.message, code: result.code };
  }

  revalidateMaterialAvailabilityRiskRoutes();
  return {
    status: "success" as const,
    taskId: result.value.taskId,
    assessmentId: result.value.assessmentId,
    idempotentReplay: result.value.idempotentReplay,
  };
}

export async function transitionMaterialAvailabilityRiskFollowUpAction(
  assessmentId: string,
  expectedVersion: number,
  targetFollowUpStatus: string,
  note?: string,
) {
  const authorization = await denyUnlessGranted("material.manage");
  if (authorization.status === "failure") return authorization;

  const contextResult = await resolveContext();
  if (contextResult.status === "failure") {
    return { status: "failure" as const, message: contextResult.message };
  }

  const { transitionMaterialAvailabilityRiskFollowUp } = await import("./material-availability-risk-api-client");
  const result = await transitionMaterialAvailabilityRiskFollowUp(contextResult.context, assessmentId, {
    expectedVersion,
    targetFollowUpStatus,
    note,
  });
  if (result.status === "failure") {
    return { status: "failure" as const, message: result.message, code: result.code };
  }

  revalidateMaterialAvailabilityRiskRoutes();
  return { status: "success" as const };
}

export async function acknowledgeMaterialAvailabilityRiskFollowUpAction(
  assessmentId: string,
  expectedVersion: number,
) {
  return transitionMaterialAvailabilityRiskFollowUpAction(assessmentId, expectedVersion, "acknowledged");
}

export async function dismissMaterialAvailabilityRiskFollowUpAction(
  assessmentId: string,
  expectedVersion: number,
  reason: string,
) {
  if (!reason.trim()) {
    return { status: "failure" as const, message: "Red gerekçesi zorunludur." };
  }
  return transitionMaterialAvailabilityRiskFollowUpAction(
    assessmentId,
    expectedVersion,
    "dismissed",
    reason.trim(),
  );
}
