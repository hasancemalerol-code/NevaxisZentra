import "./ai-task-server-boundary";

import type {
  AiTaskPlaybookDetail,
  AiTaskPlaybookRunDetail,
  AiTaskPlaybookRunSummary,
  AiTaskPlaybookSummary,
} from "../domain/ai-task-playbook";
import { AI_TASK_PLAYBOOK_ACTIVE_RUN_STATUSES, AI_TASK_PLAYBOOK_HISTORY_RUN_STATUSES } from "../domain/ai-task-playbook";
import {
  getAiTaskPlaybook,
  getAiTaskPlaybookRun,
  listAiTaskPlaybookRuns,
  listAiTaskPlaybooks,
} from "./ai-task-playbook-api-client";
import { resolveAiTaskRequestContext } from "./ai-task-request-context";
import { denyUnlessGranted } from "../../admin-access/server/require-current-permission";
import {
  fetchCurrentEffectiveAccess,
  hasGrantedPermission,
} from "../../admin-access/server/admin-access-effective-access";

type ReadFailure = Readonly<{ status: "failure"; message: string; code?: string }>;

export type AiTaskPlaybookCapabilities = Readonly<{
  canRead: boolean;
  canManage: boolean;
  canRun: boolean;
  canOverride: boolean;
}>;

export type AiTaskPlaybookLibraryPayload = Readonly<{
  playbooks: readonly AiTaskPlaybookSummary[];
  activeRuns: readonly AiTaskPlaybookRunSummary[];
  historyRuns: readonly AiTaskPlaybookRunSummary[];
  capabilities: AiTaskPlaybookCapabilities;
}>;

export type AiTaskPlaybookLibraryResult =
  | Readonly<{ status: "success"; library: AiTaskPlaybookLibraryPayload }>
  | ReadFailure;

export type AiTaskPlaybookDetailResult =
  | Readonly<{ status: "success"; playbook: AiTaskPlaybookDetail }>
  | ReadFailure;

export type AiTaskPlaybookRunDetailResult =
  | Readonly<{ status: "success"; run: AiTaskPlaybookRunDetail }>
  | ReadFailure;

function mapFailureMessage(code: string | undefined, message: string): ReadFailure {
  if (code === "unauthenticated") {
    return { status: "failure", message: "Oturum doğrulanamadı.", code };
  }

  if (code === "permission-denied") {
    return { status: "failure", message: "Playbook yönetim yetkiniz bulunmuyor.", code };
  }

  if (code === "not-found") {
    return { status: "failure", message: "Playbook kaydı bulunamadı.", code };
  }

  return { status: "failure", message, code };
}

async function resolveCapabilities(): Promise<AiTaskPlaybookCapabilities> {
  const access = await fetchCurrentEffectiveAccess();
  if (!access) {
    return { canRead: false, canManage: false, canRun: false, canOverride: false };
  }

  return {
    canRead: hasGrantedPermission(access, "ai-task.playbook.read"),
    canManage: hasGrantedPermission(access, "ai-task.playbook.manage"),
    canRun: hasGrantedPermission(access, "ai-task.playbook.run"),
    canOverride: hasGrantedPermission(access, "ai-task.playbook.override"),
  };
}

export async function readAiTaskPlaybookLibrary(): Promise<AiTaskPlaybookLibraryResult> {
  const authorization = await denyUnlessGranted("ai-task.playbook.read");
  if (authorization.status === "failure") {
    return { status: "failure", message: authorization.message, code: "permission-denied" };
  }

  const contextResult = resolveAiTaskRequestContext();
  if (contextResult.status === "failure") {
    return { status: "failure", message: contextResult.message, code: contextResult.code };
  }

  const capabilities = await resolveCapabilities();
  const context = contextResult.context;

  const playbooksResult = await listAiTaskPlaybooks(context);
  if (playbooksResult.status === "failure") {
    return mapFailureMessage(playbooksResult.code, playbooksResult.message);
  }

  const allRunsResult = await listAiTaskPlaybookRuns(context);
  const allRuns = allRunsResult.status === "success" ? allRunsResult.value : [];

  const activeStatusSet = new Set<string>(AI_TASK_PLAYBOOK_ACTIVE_RUN_STATUSES);
  const historyStatusSet = new Set<string>(AI_TASK_PLAYBOOK_HISTORY_RUN_STATUSES);

  const activeRuns = allRuns.filter((run) => activeStatusSet.has(run.status));
  const historyRuns = allRuns.filter((run) => historyStatusSet.has(run.status));

  return {
    status: "success",
    library: {
      playbooks: playbooksResult.value,
      activeRuns,
      historyRuns,
      capabilities,
    },
  };
}

export async function readAiTaskPlaybookDetail(
  playbookId: string,
): Promise<AiTaskPlaybookDetailResult> {
  const authorization = await denyUnlessGranted("ai-task.playbook.read");
  if (authorization.status === "failure") {
    return { status: "failure", message: authorization.message, code: "permission-denied" };
  }

  const contextResult = resolveAiTaskRequestContext();
  if (contextResult.status === "failure") {
    return { status: "failure", message: contextResult.message, code: contextResult.code };
  }

  const result = await getAiTaskPlaybook(contextResult.context, playbookId);
  if (result.status === "failure") {
    return mapFailureMessage(result.code, result.message);
  }

  return { status: "success", playbook: result.value };
}

export async function readAiTaskPlaybookRunDetail(
  runId: string,
): Promise<AiTaskPlaybookRunDetailResult> {
  const authorization = await denyUnlessGranted("ai-task.playbook.read");
  if (authorization.status === "failure") {
    return { status: "failure", message: authorization.message, code: "permission-denied" };
  }

  const contextResult = resolveAiTaskRequestContext();
  if (contextResult.status === "failure") {
    return { status: "failure", message: contextResult.message, code: contextResult.code };
  }

  const result = await getAiTaskPlaybookRun(contextResult.context, runId);
  if (result.status === "failure") {
    if (result.code === "not-found") {
      return { status: "failure", message: "Playbook çalışması bulunamadı.", code: "not-found" };
    }
    return mapFailureMessage(result.code, result.message);
  }

  return { status: "success", run: result.value };
}

export async function readAiTaskPlaybookBuilderContext(
  playbookId: string,
): Promise<
  | Readonly<{ status: "success"; playbook: AiTaskPlaybookDetail | null; capabilities: AiTaskPlaybookCapabilities }>
  | ReadFailure
> {
  const authorization = await denyUnlessGranted("ai-task.playbook.read");
  if (authorization.status === "failure") {
    return { status: "failure", message: authorization.message, code: "permission-denied" };
  }

  const capabilities = await resolveCapabilities();

  if (playbookId === "yeni") {
    return { status: "success", playbook: null, capabilities };
  }

  const detailResult = await readAiTaskPlaybookDetail(playbookId);
  if (detailResult.status === "failure") {
    return detailResult;
  }

  return {
    status: "success",
    playbook: detailResult.playbook,
    capabilities,
  };
}
