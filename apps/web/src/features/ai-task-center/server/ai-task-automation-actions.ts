"use server";

import "./ai-task-server-boundary";

import { revalidatePath } from "next/cache";

import type {
  CreateAiTaskAutomationRuleInput,
  UpdateAiTaskAutomationRuleInput,
} from "../domain/ai-task-automation";
import {
  createAiTaskAutomationRule,
  disableAiTaskAutomationRule,
  enableAiTaskAutomationRule,
  simulateAiTaskAutomation,
  updateAiTaskAutomationRule,
} from "./ai-task-automation-api-client";
import { resolveAiTaskRequestContextFromIncomingRequest } from "./ai-task-request-context";
import { denyUnlessGranted } from "../../admin-access/server/require-current-permission";

type ActionFailure = Readonly<{ status: "failure"; message: string }>;

async function resolveAutomationContext(
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
    return "Kural başka bir işlem tarafından güncellendi. Lütfen yenileyip tekrar deneyin.";
  }

  return message;
}

async function revalidateAutomationPaths() {
  revalidatePath("/ai-gorevleri/otomasyon");
}

function mapConditionPayload(
  condition: CreateAiTaskAutomationRuleInput["conditions"][number],
) {
  return {
    field: condition.field,
    operator: condition.operator,
    value: condition.value,
    values: condition.values,
  };
}

function mapActionPayload(
  action: CreateAiTaskAutomationRuleInput["actions"][number],
) {
  return {
    type: action.type,
    value: action.value,
    guidValue: action.guidValue,
    intValue: action.intValue,
    boolValue: action.boolValue,
    values: action.values,
  };
}

export async function createAiTaskAutomationRuleAction(
  input: CreateAiTaskAutomationRuleInput,
): Promise<
  | Readonly<{ status: "success"; ruleId: string; version: number }>
  | ActionFailure
> {
  const resolved = await resolveAutomationContext("ai-task.automation.manage");
  if (resolved.status === "failure") return resolved;

  const result = await createAiTaskAutomationRule(resolved.context, {
    ruleKey: input.ruleKey,
    name: input.name,
    description: input.description ?? null,
    triggerType: input.triggerType,
    sortOrder: input.sortOrder,
    enabled: input.enabled,
    conditions: input.conditions.map(mapConditionPayload),
    actions: input.actions.map(mapActionPayload),
    effectiveFromUtc: input.effectiveFromUtc ?? null,
    effectiveUntilUtc: input.effectiveUntilUtc ?? null,
  });

  if (result.status === "failure") {
    return { status: "failure", message: mapMutationFailure(result.code, result.message) };
  }

  await revalidateAutomationPaths();
  return { status: "success", ruleId: result.value.ruleId, version: result.value.version };
}

export async function updateAiTaskAutomationRuleAction(
  ruleId: string,
  input: UpdateAiTaskAutomationRuleInput,
): Promise<
  | Readonly<{ status: "success"; ruleId: string; version: number }>
  | ActionFailure
> {
  const resolved = await resolveAutomationContext("ai-task.automation.manage");
  if (resolved.status === "failure") return resolved;

  const result = await updateAiTaskAutomationRule(resolved.context, ruleId, {
    expectedVersion: input.expectedVersion,
    name: input.name,
    description: input.description ?? null,
    triggerType: input.triggerType,
    sortOrder: input.sortOrder,
    enabled: input.enabled,
    conditions: input.conditions.map(mapConditionPayload),
    actions: input.actions.map(mapActionPayload),
    effectiveFromUtc: input.effectiveFromUtc ?? null,
    effectiveUntilUtc: input.effectiveUntilUtc ?? null,
    changeReason: input.changeReason,
  });

  if (result.status === "failure") {
    return { status: "failure", message: mapMutationFailure(result.code, result.message) };
  }

  await revalidateAutomationPaths();
  return { status: "success", ruleId: result.value.ruleId, version: result.value.version };
}

export async function enableAiTaskAutomationRuleAction(
  ruleId: string,
  expectedVersion: number,
): Promise<
  | Readonly<{ status: "success"; version: number }>
  | ActionFailure
> {
  const resolved = await resolveAutomationContext("ai-task.automation.manage");
  if (resolved.status === "failure") return resolved;

  const result = await enableAiTaskAutomationRule(resolved.context, ruleId, expectedVersion);
  if (result.status === "failure") {
    return { status: "failure", message: mapMutationFailure(result.code, result.message) };
  }

  await revalidateAutomationPaths();
  return { status: "success", version: result.value.version };
}

export async function disableAiTaskAutomationRuleAction(
  ruleId: string,
  expectedVersion: number,
): Promise<
  | Readonly<{ status: "success"; version: number }>
  | ActionFailure
> {
  const resolved = await resolveAutomationContext("ai-task.automation.manage");
  if (resolved.status === "failure") return resolved;

  const result = await disableAiTaskAutomationRule(resolved.context, ruleId, expectedVersion);
  if (result.status === "failure") {
    return { status: "failure", message: mapMutationFailure(result.code, result.message) };
  }

  await revalidateAutomationPaths();
  return { status: "success", version: result.value.version };
}

export async function simulateAiTaskAutomationAction(
  input: Readonly<{ taskId: string; ruleId?: string | null; triggerType: string }>,
): Promise<
  | Readonly<{ status: "success"; evaluation: import("../domain/ai-task-automation").AiTaskAutomationEvaluation }>
  | ActionFailure
> {
  const resolved = await resolveAutomationContext("ai-task.automation.simulate");
  if (resolved.status === "failure") return resolved;

  const result = await simulateAiTaskAutomation(resolved.context, {
    taskId: input.taskId,
    ruleId: input.ruleId,
    triggerType: input.triggerType,
  });

  if (result.status === "failure") {
    return { status: "failure", message: mapMutationFailure(result.code, result.message) };
  }

  return { status: "success", evaluation: result.value };
}

export async function refreshAiTaskAutomationWorkspaceAction(): Promise<void> {
  const authorization = await denyUnlessGranted("ai-task.automation.read");
  if (authorization.status === "failure") return;

  await revalidateAutomationPaths();
}

export async function fetchAiTaskAutomationRuleDetailAction(
  ruleId: string,
): Promise<
  | Readonly<{ status: "success"; rule: import("../domain/ai-task-automation").AiTaskAutomationRuleDetail }>
  | ActionFailure
> {
  const { readAiTaskAutomationRuleDetail } = await import("./ai-task-automation-queries");
  const result = await readAiTaskAutomationRuleDetail(ruleId);
  if (result.status === "failure") {
    return { status: "failure", message: result.message };
  }
  return { status: "success", rule: result.rule };
}

export async function fetchAiTaskAutomationRuleHistoryAction(
  ruleId: string,
): Promise<
  | Readonly<{ status: "success"; revisions: readonly import("../domain/ai-task-automation").AiTaskAutomationRuleRevision[] }>
  | ActionFailure
> {
  const { readAiTaskAutomationRuleHistory } = await import("./ai-task-automation-queries");
  const result = await readAiTaskAutomationRuleHistory(ruleId);
  if (result.status === "failure") {
    return { status: "failure", message: result.message };
  }
  return { status: "success", revisions: result.revisions };
}
