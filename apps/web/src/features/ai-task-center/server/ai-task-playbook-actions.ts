"use server";

import "./ai-task-server-boundary";

import { revalidatePath } from "next/cache";

import type {
  CreateAiTaskPlaybookInput,
  StartAiTaskPlaybookRunInput,
  UpdateAiTaskPlaybookInput,
} from "../domain/ai-task-playbook";
import {
  blockAiTaskPlaybookRunStep,
  cancelAiTaskPlaybookRun,
  createAiTaskPlaybook,
  disableAiTaskPlaybook,
  enableAiTaskPlaybook,
  skipAiTaskPlaybookRunStep,
  startAiTaskPlaybookRun,
  unblockAiTaskPlaybookRunStep,
  updateAiTaskPlaybook,
  validateAiTaskPlaybookGraph,
} from "./ai-task-playbook-api-client";
import { resolveAiTaskRequestContextFromIncomingRequest } from "./ai-task-request-context";
import { denyUnlessGranted } from "../../admin-access/server/require-current-permission";

type ActionFailure = Readonly<{ status: "failure"; message: string }>;

async function resolvePlaybookContext(
  permissionKey: string,
): Promise<
  | Readonly<{ status: "success"; context: import("../../production-execution/server/production-order-request-context").ProductionOrderRequestContext }>
  | ActionFailure
> {
  const authorization = await denyUnlessGranted(permissionKey);
  if (authorization.status === "failure") return authorization;

  const contextResult = await resolveAiTaskRequestContextFromIncomingRequest();
  if (contextResult.status === "failure") {
    return { status: "failure", message: contextResult.message };
  }

  return { status: "success", context: contextResult.context };
}

function mapMutationFailure(code: string | undefined, message: string): string {
  if (code === "concurrency-conflict") {
    return "Playbook başka bir işlem tarafından güncellendi. Lütfen yenileyip tekrar deneyin.";
  }

  return message;
}

async function revalidatePlaybookPaths(playbookId?: string, runId?: string) {
  revalidatePath("/ai-gorevleri/playbooklar");
  if (playbookId) {
    revalidatePath(`/ai-gorevleri/playbooklar/${playbookId}`);
  }
  if (runId) {
    revalidatePath(`/ai-gorevleri/playbooklar/calisma/${runId}`);
  }
}

function mapStepPayload(step: CreateAiTaskPlaybookInput["steps"][number]) {
  return {
    stepKey: step.stepKey,
    name: step.name,
    description: step.description ?? null,
    sortOrder: step.sortOrder,
    stepType: step.stepType,
    required: step.required,
    defaultPriority: step.defaultPriority,
    assignmentPolicyJson: step.assignmentPolicyJson ?? null,
    slaPolicyJson: step.slaPolicyJson ?? null,
    evidencePolicyJson: step.evidencePolicyJson ?? null,
    closurePolicyJson: step.closurePolicyJson ?? null,
    condition: step.condition,
    targetDurationMinutes: step.targetDurationMinutes ?? null,
  };
}

function mapDependencyPayload(dep: CreateAiTaskPlaybookInput["dependencies"][number]) {
  return {
    stepKey: dep.stepKey,
    dependsOnStepKey: dep.dependsOnStepKey,
    dependencyType: dep.dependencyType,
  };
}

export async function createAiTaskPlaybookAction(
  input: CreateAiTaskPlaybookInput,
): Promise<
  | Readonly<{ status: "success"; playbookId: string; version: number }>
  | ActionFailure
> {
  const resolved = await resolvePlaybookContext("ai-task.playbook.manage");
  if (resolved.status === "failure") return resolved;

  const result = await createAiTaskPlaybook(resolved.context, {
    playbookKey: input.playbookKey,
    name: input.name,
    description: input.description ?? null,
    category: input.category,
    triggerContext: input.triggerContext ?? null,
    enabled: input.enabled,
    effectiveFromUtc: input.effectiveFromUtc ?? null,
    effectiveUntilUtc: input.effectiveUntilUtc ?? null,
    steps: input.steps.map(mapStepPayload),
    dependencies: input.dependencies.map(mapDependencyPayload),
  });

  if (result.status === "failure") {
    return { status: "failure", message: mapMutationFailure(result.code, result.message) };
  }

  await revalidatePlaybookPaths(result.value.id);
  return { status: "success", playbookId: result.value.id, version: result.value.version };
}

export async function updateAiTaskPlaybookAction(
  playbookId: string,
  input: UpdateAiTaskPlaybookInput,
): Promise<
  | Readonly<{ status: "success"; playbookId: string; version: number }>
  | ActionFailure
> {
  const resolved = await resolvePlaybookContext("ai-task.playbook.manage");
  if (resolved.status === "failure") return resolved;

  const result = await updateAiTaskPlaybook(resolved.context, playbookId, {
    expectedVersion: input.expectedVersion,
    name: input.name,
    description: input.description ?? null,
    category: input.category,
    triggerContext: input.triggerContext ?? null,
    enabled: input.enabled,
    effectiveFromUtc: input.effectiveFromUtc ?? null,
    effectiveUntilUtc: input.effectiveUntilUtc ?? null,
    steps: input.steps.map(mapStepPayload),
    dependencies: input.dependencies.map(mapDependencyPayload),
    changeReason: input.changeReason,
  });

  if (result.status === "failure") {
    return { status: "failure", message: mapMutationFailure(result.code, result.message) };
  }

  await revalidatePlaybookPaths(playbookId);
  return { status: "success", playbookId: result.value.id, version: result.value.version };
}

export async function enableAiTaskPlaybookAction(
  playbookId: string,
  expectedVersion: number,
): Promise<
  | Readonly<{ status: "success"; version: number }>
  | ActionFailure
> {
  const resolved = await resolvePlaybookContext("ai-task.playbook.manage");
  if (resolved.status === "failure") return resolved;

  const result = await enableAiTaskPlaybook(resolved.context, playbookId, expectedVersion);
  if (result.status === "failure") {
    return { status: "failure", message: mapMutationFailure(result.code, result.message) };
  }

  await revalidatePlaybookPaths(playbookId);
  return { status: "success", version: result.value.version };
}

export async function disableAiTaskPlaybookAction(
  playbookId: string,
  expectedVersion: number,
): Promise<
  | Readonly<{ status: "success"; version: number }>
  | ActionFailure
> {
  const resolved = await resolvePlaybookContext("ai-task.playbook.manage");
  if (resolved.status === "failure") return resolved;

  const result = await disableAiTaskPlaybook(resolved.context, playbookId, expectedVersion);
  if (result.status === "failure") {
    return { status: "failure", message: mapMutationFailure(result.code, result.message) };
  }

  await revalidatePlaybookPaths(playbookId);
  return { status: "success", version: result.value.version };
}

export async function validateAiTaskPlaybookGraphAction(
  input: Readonly<{
    steps: CreateAiTaskPlaybookInput["steps"];
    dependencies: CreateAiTaskPlaybookInput["dependencies"];
  }>,
): Promise<
  | Readonly<{ status: "success"; isValid: boolean; errors: readonly string[] }>
  | ActionFailure
> {
  const resolved = await resolvePlaybookContext("ai-task.playbook.manage");
  if (resolved.status === "failure") return resolved;

  const result = await validateAiTaskPlaybookGraph(resolved.context, {
    steps: input.steps.map(mapStepPayload),
    dependencies: input.dependencies.map(mapDependencyPayload),
  });

  if (result.status === "failure") {
    return { status: "failure", message: mapMutationFailure(result.code, result.message) };
  }

  return {
    status: "success",
    isValid: result.value.isValid,
    errors: result.value.errors,
  };
}

export async function startAiTaskPlaybookRunAction(
  playbookId: string,
  input?: StartAiTaskPlaybookRunInput,
): Promise<
  | Readonly<{ status: "success"; runId: string }>
  | ActionFailure
> {
  const resolved = await resolvePlaybookContext("ai-task.playbook.run");
  if (resolved.status === "failure") return resolved;

  const result = await startAiTaskPlaybookRun(resolved.context, playbookId, {
    idempotencyKey: input?.idempotencyKey ?? null,
    triggerSource: input?.triggerSource ?? "manual",
    sourceTaskId: input?.sourceTaskId ?? null,
    sourceEntityJson: input?.sourceEntityJson ?? null,
    correlationId: input?.correlationId ?? null,
    contextValues: input?.contextValues ?? null,
  });

  if (result.status === "failure") {
    return { status: "failure", message: mapMutationFailure(result.code, result.message) };
  }

  const runId = result.value.runId;
  if (!runId) {
    return { status: "failure", message: "Çalışma başlatıldı ancak çalışma kimliği alınamadı." };
  }

  await revalidatePlaybookPaths(playbookId, runId);
  return { status: "success", runId };
}

export async function cancelAiTaskPlaybookRunAction(
  runId: string,
  reason: string,
): Promise<
  | Readonly<{ status: "success" }>
  | ActionFailure
> {
  const resolved = await resolvePlaybookContext("ai-task.playbook.override");
  if (resolved.status === "failure") return resolved;

  const result = await cancelAiTaskPlaybookRun(resolved.context, runId, reason);
  if (result.status === "failure") {
    return { status: "failure", message: mapMutationFailure(result.code, result.message) };
  }

  await revalidatePlaybookPaths(undefined, runId);
  return { status: "success" };
}

export async function blockAiTaskPlaybookRunStepAction(
  runId: string,
  stepId: string,
  reason: string,
): Promise<
  | Readonly<{ status: "success" }>
  | ActionFailure
> {
  const resolved = await resolvePlaybookContext("ai-task.playbook.override");
  if (resolved.status === "failure") return resolved;

  const result = await blockAiTaskPlaybookRunStep(resolved.context, runId, stepId, reason);
  if (result.status === "failure") {
    return { status: "failure", message: mapMutationFailure(result.code, result.message) };
  }

  await revalidatePlaybookPaths(undefined, runId);
  return { status: "success" };
}

export async function unblockAiTaskPlaybookRunStepAction(
  runId: string,
  stepId: string,
  reason: string,
): Promise<
  | Readonly<{ status: "success" }>
  | ActionFailure
> {
  const resolved = await resolvePlaybookContext("ai-task.playbook.override");
  if (resolved.status === "failure") return resolved;

  const result = await unblockAiTaskPlaybookRunStep(resolved.context, runId, stepId, reason);
  if (result.status === "failure") {
    return { status: "failure", message: mapMutationFailure(result.code, result.message) };
  }

  await revalidatePlaybookPaths(undefined, runId);
  return { status: "success" };
}

export async function skipAiTaskPlaybookRunStepAction(
  runId: string,
  stepId: string,
  reason: string,
): Promise<
  | Readonly<{ status: "success" }>
  | ActionFailure
> {
  const resolved = await resolvePlaybookContext("ai-task.playbook.override");
  if (resolved.status === "failure") return resolved;

  const result = await skipAiTaskPlaybookRunStep(resolved.context, runId, stepId, reason);
  if (result.status === "failure") {
    return { status: "failure", message: mapMutationFailure(result.code, result.message) };
  }

  await revalidatePlaybookPaths(undefined, runId);
  return { status: "success" };
}

export async function refreshAiTaskPlaybookLibraryAction(): Promise<void> {
  const authorization = await denyUnlessGranted("ai-task.playbook.read");
  if (authorization.status === "failure") return;

  await revalidatePlaybookPaths();
}

export async function fetchAiTaskPlaybookDetailAction(
  playbookId: string,
): Promise<
  | Readonly<{ status: "success"; playbook: import("../domain/ai-task-playbook").AiTaskPlaybookDetail }>
  | ActionFailure
> {
  const { readAiTaskPlaybookDetail } = await import("./ai-task-playbook-queries");
  const result = await readAiTaskPlaybookDetail(playbookId);
  if (result.status === "failure") {
    return { status: "failure", message: result.message };
  }
  return { status: "success", playbook: result.playbook };
}
