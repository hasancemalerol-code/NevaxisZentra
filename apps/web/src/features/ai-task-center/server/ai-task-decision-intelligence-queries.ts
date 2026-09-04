import "./ai-task-server-boundary";

import type {
  AiTaskDecisionCapabilities,
  AiTaskDecisionWorkspace,
} from "../domain/ai-task-decision-intelligence";
import { getAiTaskDecisionWorkspace } from "./ai-task-decision-intelligence-api-client";
import { resolveAiTaskRequestContext } from "./ai-task-request-context";
import { readAiTaskDetail } from "./ai-task-queries";
import {
  cleanFixtureTaskTitle,
  isSimilarityExplanation,
} from "../presentation/ai-task-decision-intelligence-presentation";
import { denyUnlessGranted } from "../../admin-access/server/require-current-permission";
import {
  fetchCurrentEffectiveAccess,
  hasGrantedPermission,
} from "../../admin-access/server/admin-access-effective-access";

type ReadFailure = Readonly<{ status: "failure"; message: string; code?: string }>;

export type AiTaskDecisionWorkspaceResult =
  | Readonly<{
      status: "success";
      workspace: AiTaskDecisionWorkspace;
      capabilities: AiTaskDecisionCapabilities;
    }>
  | ReadFailure;

async function resolveCapabilities(): Promise<AiTaskDecisionCapabilities> {
  const access = await fetchCurrentEffectiveAccess();
  return {
    canRead: hasGrantedPermission(access, "ai-task.ai.read") || hasGrantedPermission(access, "ai-task.read"),
    canAnalyze: hasGrantedPermission(access, "ai-task.ai.analyze"),
    canFeedback: hasGrantedPermission(access, "ai-task.ai.feedback"),
  };
}

export async function readAiTaskDecisionWorkspace(
  taskId: string,
): Promise<AiTaskDecisionWorkspaceResult> {
  const authorization = await denyUnlessGranted("ai-task.read");
  if (authorization.status === "failure") {
    return { status: "failure", message: authorization.message, code: "permission-denied" };
  }

  const contextResult = resolveAiTaskRequestContext();
  if (contextResult.status === "failure") {
    return { status: "failure", message: contextResult.message, code: contextResult.code };
  }

  const capabilities = await resolveCapabilities();
  if (!capabilities.canRead) {
    return {
      status: "failure",
      message: "AI değerlendirmesini görüntüleme yetkiniz bulunmuyor.",
      code: "permission-denied",
    };
  }

  const result = await getAiTaskDecisionWorkspace(contextResult.context, taskId);
  if (result.status === "failure") {
    return { status: "failure", message: result.message, code: result.code };
  }

  const workspace = await enrichDecisionWorkspace(result.value);

  return {
    status: "success",
    workspace,
    capabilities,
  };
}

async function enrichDecisionWorkspace(
  workspace: AiTaskDecisionWorkspace,
): Promise<AiTaskDecisionWorkspace> {
  if (workspace.suggestedRelated.length === 0) {
    return workspace;
  }

  const suggestedRelated = await Promise.all(
    workspace.suggestedRelated.map(async (candidate) => {
      const detail = await readAiTaskDetail(candidate.candidateTaskId);
      if (detail.status !== "success") {
        return {
          ...candidate,
          candidateTaskTitle: isSimilarityExplanation(candidate.explanation)
            ? undefined
            : cleanFixtureTaskTitle(candidate.explanation),
        };
      }

      return {
        ...candidate,
        candidateTaskTitle: detail.detail.task.title,
        candidateTaskStatus: detail.detail.task.status,
        candidateTaskPriority: detail.detail.task.priority,
      };
    }),
  );

  return {
    ...workspace,
    suggestedRelated,
  };
}
