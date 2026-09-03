"use server";

import "./ai-task-server-boundary";

import { revalidatePath } from "next/cache";

import type { CreateAiTaskInput } from "../domain/ai-task";
import {
  acknowledgeAiTask,
  assignAiTask,
  changeAiTaskDueDate,
  changeAiTaskPriority,
  createAiTask,
  createSourceLinkedAiTask,
  escalateAiTask,
  transitionAiTask,
  type CreateAiTaskPayload,
  type CreateSourceLinkedAiTaskPayload,
} from "./ai-task-api-client";
import { readAiTaskDetail, readAiTaskWorkspace } from "./ai-task-queries";
import { resolveAiTaskRequestContext } from "./ai-task-request-context";
import { denyUnlessGranted } from "../../admin-access/server/require-current-permission";

type ActionFailure = Readonly<{ status: "failure"; message: string }>;

function mapCreatePayload(input: CreateAiTaskInput): CreateAiTaskPayload {
  return {
    title: input.title,
    summary: input.summary,
    category: input.category,
    priority: input.priority,
    riskLevel: input.riskLevel,
    detectedIssue: input.detectedIssue,
    reasoning: input.reasoning,
    recommendation: {
      title: input.recommendation.title,
      description: input.recommendation.description,
      actionType: input.recommendation.actionType,
      requiresApproval: input.recommendation.requiresApproval,
      reversible: input.recommendation.reversible,
      targetSystem: input.recommendation.targetSystem ?? null,
    },
    confidence: {
      score: input.confidence.score,
      explanation: input.confidence.explanation,
      modelVersion: input.confidence.modelVersion ?? null,
    },
    impact: {
      financialAmount: input.impact.financialAmount ?? null,
      currency: input.impact.currency ?? null,
      productionQuantity: input.impact.productionQuantity ?? null,
      productionUnit: input.impact.productionUnit ?? null,
      estimatedDelayMinutes: input.impact.estimatedDelayMinutes ?? null,
      estimatedDowntimeMinutes: input.impact.estimatedDowntimeMinutes ?? null,
      estimatedSavingsAmount: input.impact.estimatedSavingsAmount ?? null,
      summary: input.impact.summary,
    },
    sources: input.sources.map((source) => ({
      sourceType: source.sourceType,
      sourceId: source.sourceId ?? null,
      sourceLabel: source.sourceLabel,
      observedAt: source.observedAt ?? null,
      valueSummary: source.valueSummary ?? null,
    })),
    siteId: input.siteId ?? null,
    productionLineId: input.productionLineId ?? null,
    machineId: input.machineId ?? null,
    moldId: input.moldId ?? null,
    productionOrderId: input.productionOrderId ?? null,
    salesOrderId: input.salesOrderId ?? null,
    materialId: input.materialId ?? null,
    supplierId: input.supplierId ?? null,
    customerId: input.customerId ?? null,
    dueAtUtc: input.dueAt ?? null,
  };
}

async function resolveCreateContext(): Promise<
  | Readonly<{ status: "success"; context: import("../../production-execution/server/production-order-request-context").ProductionOrderRequestContext }>
  | ActionFailure
> {
  const authorization = await denyUnlessGranted("ai-task.create");
  if (authorization.status === "failure") return authorization;

  const contextResult = resolveAiTaskRequestContext();
  if (contextResult.status === "failure") {
    return { status: "failure", message: contextResult.message };
  }

  return { status: "success", context: contextResult.context };
}

async function resolveReviewContext(): Promise<
  | Readonly<{ status: "success"; context: import("../../production-execution/server/production-order-request-context").ProductionOrderRequestContext }>
  | ActionFailure
> {
  const authorization = await denyUnlessGranted("ai-task.review");
  if (authorization.status === "failure") return authorization;

  const contextResult = resolveAiTaskRequestContext();
  if (contextResult.status === "failure") {
    return { status: "failure", message: contextResult.message };
  }

  return { status: "success", context: contextResult.context };
}

async function resolvePermissionContext(
  permissionKey: string,
): Promise<
  | Readonly<{ status: "success"; context: import("../../production-execution/server/production-order-request-context").ProductionOrderRequestContext }>
  | ActionFailure
> {
  const authorization = await denyUnlessGranted(permissionKey);
  if (authorization.status === "failure") return authorization;

  const contextResult = resolveAiTaskRequestContext();
  if (contextResult.status === "failure") {
    return { status: "failure", message: contextResult.message };
  }

  return { status: "success", context: contextResult.context };
}

async function revalidateTaskPaths(taskId: string) {
  revalidatePath("/");
  revalidatePath("/ai-gorevleri");
  revalidatePath(`/ai-gorevleri/${taskId}`);
}

async function finalizeTaskMutation(taskId: string, version: number) {
  await revalidateTaskPaths(taskId);
  const detail = await readAiTaskDetail(taskId);
  if (detail.status === "failure") {
    return { status: "success" as const, version };
  }

  return {
    status: "success" as const,
    version,
    detail: detail.detail,
    canReview: detail.canReview,
    operations: detail.operations,
  };
}

function mapMutationFailure(code: string | undefined, message: string) {
  if (code === "concurrency-conflict") {
    return "Görev başka bir işlem tarafından güncellendi. Lütfen yenileyip tekrar deneyin.";
  }

  return message;
}

export async function refreshAiTaskWorkspaceAction() {
  const authorization = await denyUnlessGranted("ai-task.read");
  if (authorization.status === "failure") return authorization;

  revalidatePath("/");
  revalidatePath("/ai-gorevleri");

  const result = await readAiTaskWorkspace();
  if (result.status === "failure") {
    return { status: "failure" as const, message: result.message };
  }

  return { status: "success" as const, workspace: result.workspace };
}

export async function createAiTaskDraftAction(input: CreateAiTaskInput) {
  const resolved = await resolveCreateContext();
  if (resolved.status === "failure") return resolved;

  const result = await createAiTask(resolved.context, mapCreatePayload(input));
  if (result.status === "failure") {
    return { status: "failure" as const, message: result.message };
  }

  revalidatePath("/");
  revalidatePath("/ai-gorevleri");
  revalidatePath(`/ai-gorevleri/${result.value.entityId}`);

  return {
    status: "success" as const,
    taskId: result.value.entityId,
    idempotentReplay: result.value.idempotentReplay,
  };
}

export async function createSourceLinkedAiTaskAction(
  input: CreateSourceLinkedAiTaskPayload,
) {
  const resolved = await resolveCreateContext();
  if (resolved.status === "failure") return resolved;

  const result = await createSourceLinkedAiTask(resolved.context, input);
  if (result.status === "failure") {
    return { status: "failure" as const, message: result.message };
  }

  revalidatePath("/");
  revalidatePath("/ai-gorevleri");
  revalidatePath(`/ai-gorevleri/${result.value.entityId}`);

  return {
    status: "success" as const,
    taskId: result.value.entityId,
    idempotentReplay: result.value.idempotentReplay,
  };
}

export async function transitionAiTaskAction(input: Readonly<{
  taskId: string;
  expectedVersion: number;
  targetStatus: string;
  note?: string;
}>) {
  const resolved = await resolveReviewContext();
  if (resolved.status === "failure") return resolved;

  const result = await transitionAiTask(resolved.context, input.taskId, {
    expectedVersion: input.expectedVersion,
    targetStatus: input.targetStatus,
    note: input.note ?? null,
  });

  if (result.status === "failure") {
    return {
      status: "failure" as const,
      message: mapMutationFailure(result.code, result.message),
    };
  }

  await revalidateTaskPaths(input.taskId);

  const detail = await readAiTaskDetail(input.taskId);
  if (detail.status === "failure") {
    return { status: "success" as const, version: result.value.version };
  }

  return {
    status: "success" as const,
    version: result.value.version,
    detail: detail.detail,
    canReview: detail.canReview,
    operations: detail.operations,
  };
}

export async function assignAiTaskAction(input: Readonly<{
  taskId: string;
  expectedVersion: number;
  targetMembershipId?: string | null;
  targetTeamChannelId?: string | null;
  reason: string;
  source?: string | null;
}>) {
  const resolved = await resolvePermissionContext("ai-task.assign");
  if (resolved.status === "failure") return resolved;

  const result = await assignAiTask(resolved.context, input.taskId, {
    expectedVersion: input.expectedVersion,
    targetMembershipId: input.targetMembershipId ?? null,
    targetTeamChannelId: input.targetTeamChannelId ?? null,
    reason: input.reason,
    source: input.source ?? null,
  });

  if (result.status === "failure") {
    return {
      status: "failure" as const,
      message: mapMutationFailure(result.code, result.message),
    };
  }

  return finalizeTaskMutation(input.taskId, result.value.version);
}

export async function acknowledgeAiTaskAction(input: Readonly<{
  taskId: string;
  expectedVersion: number;
  note?: string;
}>) {
  const resolved = await resolvePermissionContext("ai-task.acknowledge");
  if (resolved.status === "failure") return resolved;

  const result = await acknowledgeAiTask(resolved.context, input.taskId, {
    expectedVersion: input.expectedVersion,
    note: input.note ?? null,
  });

  if (result.status === "failure") {
    return {
      status: "failure" as const,
      message: mapMutationFailure(result.code, result.message),
    };
  }

  return finalizeTaskMutation(input.taskId, result.value.version);
}

export async function changeAiTaskPriorityAction(input: Readonly<{
  taskId: string;
  expectedVersion: number;
  priority: string;
  reason?: string;
}>) {
  const resolved = await resolvePermissionContext("ai-task.manage");
  if (resolved.status === "failure") return resolved;

  const result = await changeAiTaskPriority(resolved.context, input.taskId, {
    expectedVersion: input.expectedVersion,
    priority: input.priority,
    reason: input.reason ?? null,
  });

  if (result.status === "failure") {
    return {
      status: "failure" as const,
      message: mapMutationFailure(result.code, result.message),
    };
  }

  return finalizeTaskMutation(input.taskId, result.value.version);
}

export async function changeAiTaskDueDateAction(input: Readonly<{
  taskId: string;
  expectedVersion: number;
  dueAtUtc?: string | null;
  reason?: string;
}>) {
  const resolved = await resolvePermissionContext("ai-task.manage");
  if (resolved.status === "failure") return resolved;

  const result = await changeAiTaskDueDate(resolved.context, input.taskId, {
    expectedVersion: input.expectedVersion,
    dueAtUtc: input.dueAtUtc ?? null,
    reason: input.reason ?? null,
  });

  if (result.status === "failure") {
    return {
      status: "failure" as const,
      message: mapMutationFailure(result.code, result.message),
    };
  }

  return finalizeTaskMutation(input.taskId, result.value.version);
}

export async function escalateAiTaskAction(input: Readonly<{
  taskId: string;
  expectedVersion: number;
  breachType: string;
  note?: string;
}>) {
  const resolved = await resolvePermissionContext("ai-task.escalate");
  if (resolved.status === "failure") return resolved;

  const result = await escalateAiTask(resolved.context, input.taskId, {
    expectedVersion: input.expectedVersion,
    breachType: input.breachType,
    note: input.note ?? null,
  });

  if (result.status === "failure") {
    return {
      status: "failure" as const,
      message: mapMutationFailure(result.code, result.message),
    };
  }

  return finalizeTaskMutation(input.taskId, result.value.version);
}

export async function loadAiTaskDetailAction(taskId: string) {
  const authorization = await denyUnlessGranted("ai-task.read");
  if (authorization.status === "failure") return authorization;

  const result = await readAiTaskDetail(taskId);
  if (result.status === "failure") {
    return { status: "failure" as const, message: result.message, code: result.code };
  }

  return {
    status: "success" as const,
    detail: result.detail,
    canReview: result.canReview,
    operations: result.operations,
  };
}
