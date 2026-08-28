import "./ai-task-server-boundary";

import { calculateAiTaskKpis } from "../application/ai-task-query";
import type { AiTask } from "../domain/ai-task";
import {
  getAiTask,
  listAiTasks,
  type RawAiTaskListResponse,
} from "./ai-task-api-client";
import {
  mapRawAiTaskDetailViewModel,
  mapRawAiTaskSummary,
  type AiTaskDetailViewModel,
} from "./ai-task-mapper";
import { resolveAiTaskRequestContext } from "./ai-task-request-context";
import { denyUnlessGranted } from "../../admin-access/server/require-current-permission";

type ReadFailure = Readonly<{ status: "failure"; message: string; code?: string }>;

export type AiTaskWorkspacePayload = Readonly<{
  tasks: readonly AiTask[];
  totalCount: number;
  page: number;
  pageSize: number;
  ruleSetVersion: string;
  provenanceDisclaimer: string;
  canCreate: boolean;
  canReview: boolean;
  kpis: ReturnType<typeof calculateAiTaskKpis>;
}>;

export type AiTaskWorkspaceResult =
  | Readonly<{ status: "success"; workspace: AiTaskWorkspacePayload }>
  | ReadFailure;

export type AiTaskDetailResult =
  | Readonly<{ status: "success"; detail: AiTaskDetailViewModel; canReview: boolean }>
  | ReadFailure;

function mapFailureMessage(code: string | undefined, message: string): ReadFailure {
  if (code === "unauthenticated") {
    return { status: "failure", message: "Oturum doğrulanamadı.", code };
  }

  if (code === "permission-denied") {
    return { status: "failure", message: "AI görevlerini görüntüleme yetkiniz bulunmuyor.", code };
  }

  return { status: "failure", message, code };
}

function buildWorkspace(list: RawAiTaskListResponse): AiTaskWorkspacePayload {
  const tasks = list.items.map(mapRawAiTaskSummary);

  return {
    tasks,
    totalCount: list.totalCount,
    page: list.page,
    pageSize: list.pageSize,
    ruleSetVersion: list.ruleSetVersion,
    provenanceDisclaimer: list.provenanceDisclaimer,
    canCreate: list.capabilities.canCreate,
    canReview: list.capabilities.canReview,
    kpis: calculateAiTaskKpis(tasks),
  };
}

export async function readAiTaskWorkspace(
  filters?: Readonly<{
    status?: string;
    category?: string;
    priority?: string;
    sourceModule?: string;
    page?: number;
    pageSize?: number;
  }>,
): Promise<AiTaskWorkspaceResult> {
  const authorization = await denyUnlessGranted("ai-task.read");
  if (authorization.status === "failure") {
    return { status: "failure", message: authorization.message, code: "permission-denied" };
  }

  const contextResult = resolveAiTaskRequestContext();
  if (contextResult.status === "failure") {
    return { status: "failure", message: contextResult.message, code: contextResult.code };
  }

  const result = await listAiTasks(contextResult.context, filters);
  if (result.status === "failure") {
    return mapFailureMessage(result.code, result.message);
  }

  return { status: "success", workspace: buildWorkspace(result.value) };
}

export async function readAiTaskDetail(taskId: string): Promise<AiTaskDetailResult> {
  const authorization = await denyUnlessGranted("ai-task.read");
  if (authorization.status === "failure") {
    return { status: "failure", message: authorization.message, code: "permission-denied" };
  }

  const contextResult = resolveAiTaskRequestContext();
  if (contextResult.status === "failure") {
    return { status: "failure", message: contextResult.message, code: contextResult.code };
  }

  const listResult = await listAiTasks(contextResult.context, { page: 1, pageSize: 1 });
  const canReview = listResult.status === "success"
    ? listResult.value.capabilities.canReview
    : false;

  const result = await getAiTask(contextResult.context, taskId);
  if (result.status === "failure") {
    if (result.code === "not-found") {
      return { status: "failure", message: "AI görevi bulunamadı.", code: result.code };
    }

    return mapFailureMessage(result.code, result.message);
  }

  return {
    status: "success",
    detail: mapRawAiTaskDetailViewModel(result.value),
    canReview,
  };
}
