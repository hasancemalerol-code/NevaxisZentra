import "./ai-task-server-boundary";

import type { AiTask } from "../domain/ai-task";
import type {
  AiTaskAutomationEvaluation,
  AiTaskAutomationExecution,
  AiTaskAutomationRuleDetail,
  AiTaskAutomationRuleRevision,
  AiTaskAutomationRuleSummary,
} from "../domain/ai-task-automation";
import {
  AI_TASK_POLICY_LIBRARY_CLOSURE_POLICIES,
  AI_TASK_POLICY_LIBRARY_SLA_POLICIES,
} from "../domain/ai-task-policy-library";
import {
  getAiTaskAutomationRule,
  listAiTaskAutomationExecutions,
  listAiTaskAutomationRuleHistory,
  listAiTaskAutomationRules,
  simulateAiTaskAutomation,
} from "./ai-task-automation-api-client";
import { listAiTasks } from "./ai-task-api-client";
import { mapRawAiTaskSummary } from "./ai-task-mapper";
import { resolveAiTaskRequestContext } from "./ai-task-request-context";
import { denyUnlessGranted } from "../../admin-access/server/require-current-permission";
import {
  fetchCurrentEffectiveAccess,
  hasGrantedPermission,
} from "../../admin-access/server/admin-access-effective-access";

type ReadFailure = Readonly<{ status: "failure"; message: string; code?: string }>;

export type AiTaskAutomationCapabilities = Readonly<{
  canRead: boolean;
  canManage: boolean;
  canSimulate: boolean;
}>;

export type AiTaskAutomationWorkspacePayload = Readonly<{
  rules: readonly AiTaskAutomationRuleSummary[];
  executions: readonly AiTaskAutomationExecution[];
  taskOptions: readonly AiTask[];
  slaPolicies: typeof AI_TASK_POLICY_LIBRARY_SLA_POLICIES;
  closurePolicies: typeof AI_TASK_POLICY_LIBRARY_CLOSURE_POLICIES;
  capabilities: AiTaskAutomationCapabilities;
}>;

export type AiTaskAutomationWorkspaceResult =
  | Readonly<{ status: "success"; workspace: AiTaskAutomationWorkspacePayload }>
  | ReadFailure;

export type AiTaskAutomationRuleDetailResult =
  | Readonly<{ status: "success"; rule: AiTaskAutomationRuleDetail }>
  | ReadFailure;

export type AiTaskAutomationRuleHistoryResult =
  | Readonly<{ status: "success"; revisions: readonly AiTaskAutomationRuleRevision[] }>
  | ReadFailure;

export type AiTaskAutomationSimulationResult =
  | Readonly<{ status: "success"; evaluation: AiTaskAutomationEvaluation }>
  | ReadFailure;

function mapFailureMessage(code: string | undefined, message: string): ReadFailure {
  if (code === "unauthenticated") {
    return { status: "failure", message: "Oturum doğrulanamadı.", code };
  }

  if (code === "permission-denied") {
    return { status: "failure", message: "Otomasyon yönetim yetkiniz bulunmuyor.", code };
  }

  return { status: "failure", message, code };
}

async function resolveCapabilities(): Promise<AiTaskAutomationCapabilities> {
  const access = await fetchCurrentEffectiveAccess();
  if (!access) {
    return { canRead: false, canManage: false, canSimulate: false };
  }

  return {
    canRead: hasGrantedPermission(access, "ai-task.automation.read"),
    canManage: hasGrantedPermission(access, "ai-task.automation.manage"),
    canSimulate: hasGrantedPermission(access, "ai-task.automation.simulate"),
  };
}

export async function readAiTaskAutomationWorkspace(): Promise<AiTaskAutomationWorkspaceResult> {
  const authorization = await denyUnlessGranted("ai-task.automation.read");
  if (authorization.status === "failure") {
    return { status: "failure", message: authorization.message, code: "permission-denied" };
  }

  const contextResult = resolveAiTaskRequestContext();
  if (contextResult.status === "failure") {
    return { status: "failure", message: contextResult.message, code: contextResult.code };
  }

  const capabilities = await resolveCapabilities();
  const context = contextResult.context;

  const rulesResult = await listAiTaskAutomationRules(context);
  if (rulesResult.status === "failure") {
    return mapFailureMessage(rulesResult.code, rulesResult.message);
  }

  const executionsResult = await listAiTaskAutomationExecutions(context, { page: 1, pageSize: 50 });
  const executions = executionsResult.status === "success" ? executionsResult.value : [];

  const tasksResult = await listAiTasks(context, { page: 1, pageSize: 100 });
  const taskOptions = tasksResult.status === "success"
    ? tasksResult.value.items.map(mapRawAiTaskSummary)
    : [];

  return {
    status: "success",
    workspace: {
      rules: rulesResult.value,
      executions,
      taskOptions,
      slaPolicies: AI_TASK_POLICY_LIBRARY_SLA_POLICIES,
      closurePolicies: AI_TASK_POLICY_LIBRARY_CLOSURE_POLICIES,
      capabilities,
    },
  };
}

export async function readAiTaskAutomationRuleDetail(
  ruleId: string,
): Promise<AiTaskAutomationRuleDetailResult> {
  const authorization = await denyUnlessGranted("ai-task.automation.read");
  if (authorization.status === "failure") {
    return { status: "failure", message: authorization.message, code: "permission-denied" };
  }

  const contextResult = resolveAiTaskRequestContext();
  if (contextResult.status === "failure") {
    return { status: "failure", message: contextResult.message, code: contextResult.code };
  }

  const result = await getAiTaskAutomationRule(contextResult.context, ruleId);
  if (result.status === "failure") {
    return mapFailureMessage(result.code, result.message);
  }

  return { status: "success", rule: result.value };
}

export async function readAiTaskAutomationRuleHistory(
  ruleId: string,
): Promise<AiTaskAutomationRuleHistoryResult> {
  const authorization = await denyUnlessGranted("ai-task.automation.read");
  if (authorization.status === "failure") {
    return { status: "failure", message: authorization.message, code: "permission-denied" };
  }

  const contextResult = resolveAiTaskRequestContext();
  if (contextResult.status === "failure") {
    return { status: "failure", message: contextResult.message, code: contextResult.code };
  }

  const result = await listAiTaskAutomationRuleHistory(contextResult.context, ruleId);
  if (result.status === "failure") {
    return mapFailureMessage(result.code, result.message);
  }

  return { status: "success", revisions: result.value };
}

export async function readAiTaskAutomationSimulation(
  input: Readonly<{ taskId: string; ruleId?: string | null; triggerType: string }>,
): Promise<AiTaskAutomationSimulationResult> {
  const authorization = await denyUnlessGranted("ai-task.automation.simulate");
  if (authorization.status === "failure") {
    return { status: "failure", message: authorization.message, code: "permission-denied" };
  }

  const contextResult = resolveAiTaskRequestContext();
  if (contextResult.status === "failure") {
    return { status: "failure", message: contextResult.message, code: contextResult.code };
  }

  const result = await simulateAiTaskAutomation(contextResult.context, {
    taskId: input.taskId,
    ruleId: input.ruleId,
    triggerType: input.triggerType,
  });

  if (result.status === "failure") {
    return mapFailureMessage(result.code, result.message);
  }

  return { status: "success", evaluation: result.value };
}
