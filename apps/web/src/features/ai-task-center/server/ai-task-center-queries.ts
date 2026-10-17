import "./ai-task-server-boundary";

import {
  collectAssignedTaskIds,
  composeAiTaskCenterItems,
} from "../application/ai-task-center-composition";
import { calculateAiTaskKpis } from "../application/ai-task-query";
import type { AiTaskClosureQueueKey } from "../domain/ai-task";
import { AI_TASK_CLOSURE_QUEUE_KEYS } from "../domain/ai-task";
import type { AiTaskCenterItem } from "../domain/ai-task-center-item";
import { denyUnlessAnyGranted, denyUnlessGranted } from "../../admin-access/server/require-current-permission";
import { readTeamsWorkInbox } from "../../teams/server/teams-queries";
import type { AiTaskListRecord } from "./ai-task-mapper";
import { listAiTasks } from "./ai-task-api-client";
import { mapRawAiTaskSummary } from "./ai-task-mapper";
import { resolveAiTaskRequestContextFromIncomingRequest } from "./ai-task-request-context";
import { listTeamsChannels } from "../../teams/server/teams-api-client";
import { resolveTeamsRequestContext } from "../../teams/server/teams-request-context";

type ReadFailure = Readonly<{ status: "failure"; message: string; code?: string }>;

export type AiTaskCenterUnifiedPayload = Readonly<{
  items: readonly AiTaskCenterItem[];
  assignedTaskIds: readonly string[];
  tasks: readonly AiTaskListRecord[];
  totalTaskCount: number;
  ruleSetVersion: string;
  provenanceDisclaimer: string;
  workInboxDisclaimer: string | null;
  canCreate: boolean;
  canReview: boolean;
  canReadTasks: boolean;
  canReadTeamsInbox: boolean;
  sourceAvailability: Readonly<{
    aiTasks: boolean;
    approvals: boolean;
    handovers: boolean;
    mentions: boolean;
  }>;
  kpis: ReturnType<typeof calculateAiTaskKpis>;
  closureQueueCounts: Readonly<Record<AiTaskClosureQueueKey, number>>;
}>;

export type AiTaskCenterUnifiedResult =
  | Readonly<{ status: "success"; center: AiTaskCenterUnifiedPayload }>
  | ReadFailure;

function mapFailureMessage(code: string | undefined, message: string): ReadFailure {
  if (code === "unauthenticated") {
    return { status: "failure", message: "Oturum doğrulanamadı.", code };
  }

  if (code === "permission-denied") {
    return {
      status: "failure",
      message: "Görev Merkezi görüntüleme yetkiniz bulunmuyor.",
      code,
    };
  }

  if (code === "subscription-required") {
    return {
      status: "failure",
      message: "Görev Merkezi bu kurulumun lisansı kapsamında etkin değil. Lisans durumunu sistem yöneticinizle kontrol edin.",
      code,
    };
  }

  return { status: "failure", message, code };
}

async function readClosureQueueCounts(
  context: import("../../production-execution/server/production-order-request-context").ProductionOrderRequestContext,
): Promise<Readonly<Record<AiTaskClosureQueueKey, number>>> {
  const counts = {} as Record<AiTaskClosureQueueKey, number>;

  await Promise.all(
    AI_TASK_CLOSURE_QUEUE_KEYS.map(async (queue) => {
      const result = await listAiTasks(context, {
        closureQueue: queue,
        page: 1,
        pageSize: 1,
      });
      counts[queue] = result.status === "success" ? result.value.totalCount : 0;
    }),
  );

  return counts;
}

export async function readAiTaskCenterUnified(): Promise<AiTaskCenterUnifiedResult> {
  const authorization = await denyUnlessAnyGranted(["ai-task.read", "teams.read"]);
  if (authorization.status === "failure") {
    return { status: "failure", message: authorization.message, code: "permission-denied" };
  }

  const canReadTasks = (await denyUnlessGranted("ai-task.read")).status === "granted";
  const canReadTeamsInbox = (await denyUnlessGranted("teams.read")).status === "granted";

  let tasks: readonly AiTaskListRecord[] = [];
  let totalTaskCount = 0;
  let ruleSetVersion = "";
  let provenanceDisclaimer = "";
  let canCreate = false;
  let canReview = false;
  let closureQueueCounts = {
    "awaiting-approval": 0,
    "missing-evidence": 0,
    "revision-pending": 0,
    reopened: 0,
  } as Record<AiTaskClosureQueueKey, number>;

  if (canReadTasks) {
    const contextResult = await resolveAiTaskRequestContextFromIncomingRequest();
    if (contextResult.status === "failure") {
      return { status: "failure", message: contextResult.message, code: contextResult.code };
    }

    const result = await listAiTasks(contextResult.context, { page: 1, pageSize: 100 });
    if (result.status === "failure") {
      return mapFailureMessage(result.code, result.message);
    }

    tasks = result.value.items.map(mapRawAiTaskSummary);
    totalTaskCount = result.value.totalCount;
    ruleSetVersion = result.value.ruleSetVersion;
    provenanceDisclaimer = result.value.provenanceDisclaimer;
    canCreate = result.value.capabilities.canCreate;
    canReview = result.value.capabilities.canReview;
    closureQueueCounts = await readClosureQueueCounts(contextResult.context);
  }

  let workInboxItems: readonly import("../../teams/server/teams-api-client").RawTeamsWorkInboxItem[] = [];
  let workInboxDisclaimer: string | null = null;
  let sourceAvailability = {
    aiTasks: false,
    approvals: false,
    handovers: false,
    mentions: false,
  };

  if (canReadTeamsInbox) {
    const inboxResult = await readTeamsWorkInbox();
    if (inboxResult.status === "failure") {
      return mapFailureMessage(inboxResult.code, inboxResult.message);
    }

    workInboxItems = inboxResult.value.items;
    workInboxDisclaimer = inboxResult.value.disclaimer;
    sourceAvailability = inboxResult.value.sources;
  }

  const assignedTaskIds = collectAssignedTaskIds(workInboxItems);

  const teamChannelLabels: Record<string, string> = {};
  if (canReadTeamsInbox) {
    const teamsContext = await resolveTeamsRequestContext();
    if (teamsContext.status === "success") {
      const channelsResult = await listTeamsChannels(teamsContext.context);
      if (channelsResult.status === "success") {
        for (const channel of channelsResult.value.items) {
          teamChannelLabels[channel.id] = channel.name;
        }
      }
    }
  }

  const items = composeAiTaskCenterItems({
    tasks,
    workInboxItems,
    facilityLabel: null,
    teamChannelLabels,
  });

  return {
    status: "success",
    center: {
      items,
      assignedTaskIds: [...assignedTaskIds],
      tasks,
      totalTaskCount,
      ruleSetVersion,
      provenanceDisclaimer,
      workInboxDisclaimer,
      canCreate,
      canReview,
      canReadTasks,
      canReadTeamsInbox,
      sourceAvailability,
      kpis: calculateAiTaskKpis(tasks),
      closureQueueCounts,
    },
  };
}
