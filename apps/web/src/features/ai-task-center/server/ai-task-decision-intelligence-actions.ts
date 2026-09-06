"use server";

import "./ai-task-server-boundary";

import { revalidatePath } from "next/cache";

import {
  acceptAiTaskRecommendation,
  analyzeAiTaskDecision,
  confirmAiTaskRelationship,
  rejectAiTaskRecommendation,
  reviewAiTaskClosure,
} from "./ai-task-decision-intelligence-api-client";
import { resolveAiTaskRequestContextFromIncomingRequest } from "./ai-task-request-context";
import { denyUnlessGranted } from "../../admin-access/server/require-current-permission";

type ActionFailure = Readonly<{ status: "failure"; message: string }>;

async function resolveContext(permissionKey: string) {
  const authorization = await denyUnlessGranted(permissionKey);
  if (authorization.status === "failure") return authorization;

  const contextResult = await resolveAiTaskRequestContextFromIncomingRequest();
  if (contextResult.status === "failure") {
    return { status: "failure" as const, message: contextResult.message };
  }

  return { status: "success" as const, context: contextResult.context };
}

function revalidateTask(taskId: string) {
  revalidatePath(`/ai-gorevleri/${taskId}`);
  revalidatePath("/ai-gorevleri");
}

export async function analyzeAiTaskDecisionAction(
  taskId: string,
  idempotencyKey: string,
  analysisType = "triage",
  triggerReason?: string,
) {
  const resolved = await resolveContext("ai-task.ai.analyze");
  if (resolved.status === "failure") return resolved;

  const result = await analyzeAiTaskDecision(resolved.context, taskId, {
    analysisType,
    idempotencyKey,
    triggerReason,
  });

  if (result.status === "failure") {
    return { status: "failure" as const, message: result.message };
  }

  revalidateTask(taskId);
  return {
    status: "success" as const,
    analysis: result.value.analysis,
    taskVersion: result.value.taskVersion,
    idempotentReplay: result.value.idempotentReplay,
  };
}

export async function acceptAiTaskRecommendationAction(
  taskId: string,
  recommendationId: string,
  expectedVersion: number,
  reason?: string,
) {
  const resolved = await resolveContext("ai-task.ai.feedback");
  if (resolved.status === "failure") return resolved;

  const result = await acceptAiTaskRecommendation(
    resolved.context,
    taskId,
    recommendationId,
    { expectedVersion, reason },
  );

  if (result.status === "failure") {
    return { status: "failure" as const, message: result.message };
  }

  revalidateTask(taskId);
  return { status: "success" as const, taskVersion: result.value.taskVersion };
}

export async function rejectAiTaskRecommendationAction(
  taskId: string,
  recommendationId: string,
  reason: string,
) {
  const resolved = await resolveContext("ai-task.ai.feedback");
  if (resolved.status === "failure") return resolved;

  const result = await rejectAiTaskRecommendation(
    resolved.context,
    taskId,
    recommendationId,
    reason,
  );

  if (result.status === "failure") {
    return { status: "failure" as const, message: result.message };
  }

  revalidateTask(taskId);
  return { status: "success" as const, taskVersion: result.value.taskVersion };
}

export async function confirmAiTaskRelationshipAction(
  taskId: string,
  targetTaskId: string,
  relationshipType: string,
  status: string,
  explanation?: string,
) {
  const resolved = await resolveContext("ai-task.ai.feedback");
  if (resolved.status === "failure") return resolved;

  const result = await confirmAiTaskRelationship(resolved.context, taskId, {
    targetTaskId,
    relationshipType,
    status,
    explanation,
  });

  if (result.status === "failure") {
    return { status: "failure" as const, message: result.message };
  }

  revalidateTask(taskId);
  return { status: "success" as const, taskVersion: result.value.taskVersion };
}

export async function reviewAiTaskClosureAction(taskId: string, idempotencyKey: string) {
  const resolved = await resolveContext("ai-task.ai.analyze");
  if (resolved.status === "failure") return resolved;

  const result = await reviewAiTaskClosure(resolved.context, taskId, idempotencyKey);
  if (result.status === "failure") {
    return { status: "failure" as const, message: result.message };
  }

  revalidateTask(taskId);
  return {
    status: "success" as const,
    review: result.value.review,
    idempotentReplay: result.value.idempotentReplay,
  };
}
