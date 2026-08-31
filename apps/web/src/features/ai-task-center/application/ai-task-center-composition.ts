import { isAiTaskFinalStatus } from "../domain/ai-task";
import type { AiTaskListRecord } from "../server/ai-task-mapper";
import type { RawTeamsWorkInboxItem } from "../../teams/server/teams-api-client";
import {
  AI_TASK_CENTER_SOURCE_LABELS,
  type AiTaskCenterItem,
  type AiTaskCenterItemKind,
  type AiTaskCenterTaskOriginFilter,
  type AiTaskCenterView,
  type AiTaskListOriginKind,
  resolveTaskSourceLabel,
} from "../domain/ai-task-center-item";
import {
  getAiTaskCategoryLabel,
  getAiTaskPriorityLabel,
  getAiTaskStatusLabel,
} from "../presentation/ai-task-labels";

function isAiTaskListOriginKind(value: string): value is AiTaskListOriginKind {
  return value in AI_TASK_CENTER_SOURCE_LABELS
    && value !== "approval-request"
    && value !== "shift-handover"
    && value !== "mention";
}

function resolveWorkInboxSourceLabel(
  sourceKind: string,
): AiTaskCenterItem["sourceLabel"] {
  switch (sourceKind) {
    case "approval":
      return AI_TASK_CENTER_SOURCE_LABELS["approval-request"];
    case "handover":
      return AI_TASK_CENTER_SOURCE_LABELS["shift-handover"];
    case "mention":
      return AI_TASK_CENTER_SOURCE_LABELS.mention;
    default:
      return AI_TASK_CENTER_SOURCE_LABELS.unspecified;
  }
}

function resolveWorkInboxKind(sourceKind: string): AiTaskCenterItemKind | null {
  switch (sourceKind) {
    case "ai-task":
      return "task";
    case "approval":
      return "approval";
    case "handover":
      return "handover";
    case "mention":
      return "mention";
    default:
      return null;
  }
}

function buildTaskRelatedContext(task: AiTaskListRecord): string | null {
  const parts: string[] = [];

  if (task.productionOrderId) {
    parts.push(`Üretim emri: ${task.productionOrderId}`);
  }

  if (task.machineId) {
    parts.push(`Makine: ${task.machineId}`);
  }

  if (task.moldId) {
    parts.push(`Kalıp: ${task.moldId}`);
  }

  if (task.materialId) {
    parts.push(`Malzeme: ${task.materialId}`);
  }

  if (task.sourceModule && task.sourceModule !== "teams") {
    parts.push(`Modül: ${getAiTaskCategoryLabel(task.category)}`);
  }

  return parts.length > 0 ? parts.join(" · ") : null;
}

export function mapTaskToCenterItem(
  task: AiTaskListRecord,
  facilityLabel: string | null,
): AiTaskCenterItem {
  const listOriginKind = isAiTaskListOriginKind(task.listOriginKind)
    ? task.listOriginKind
    : "unspecified";

  return {
    dedupeKey: `task:${task.id}`,
    kind: "task",
    sourceLabel: resolveTaskSourceLabel(listOriginKind),
    listOriginKind,
    title: task.title,
    facilityLabel,
    assigneeLabel: task.assignee?.displayName ?? null,
    priorityLabel: getAiTaskPriorityLabel(task.priority),
    statusLabel: getAiTaskStatusLabel(task.status),
    statusCode: task.status,
    occurredAt: task.dueAt ?? task.updatedAt ?? task.createdAt,
    relatedContext: buildTaskRelatedContext(task),
    deepLinkPath: `/ai-gorevleri/${task.id}`,
    nativeSourceKind: "ai-task",
    nativeSourceId: task.id,
  };
}

export function mapWorkInboxItemToCenterItem(
  item: RawTeamsWorkInboxItem,
  facilityLabel: string | null,
): AiTaskCenterItem | null {
  const kind = resolveWorkInboxKind(item.sourceKind);
  if (!kind || kind === "task") {
    return null;
  }

  return {
    dedupeKey: `${item.sourceKind}:${item.sourceId}`,
    kind,
    sourceLabel: resolveWorkInboxSourceLabel(item.sourceKind),
    listOriginKind: null,
    title: item.title,
    facilityLabel,
    assigneeLabel: null,
    priorityLabel: null,
    statusLabel: item.reason,
    statusCode: null,
    occurredAt: item.occurredAt,
    relatedContext: null,
    deepLinkPath: item.deepLinkPath,
    nativeSourceKind: item.sourceKind,
    nativeSourceId: item.sourceId,
  };
}

export type ComposeAiTaskCenterItemsInput = Readonly<{
  tasks: readonly AiTaskListRecord[];
  workInboxItems: readonly RawTeamsWorkInboxItem[];
  facilityLabel: string | null;
}>;

export function composeAiTaskCenterItems(
  input: ComposeAiTaskCenterItemsInput,
): readonly AiTaskCenterItem[] {
  const taskIds = new Set(input.tasks.map((task) => task.id));
  const items = new Map<string, AiTaskCenterItem>();

  for (const task of input.tasks) {
    items.set(`task:${task.id}`, mapTaskToCenterItem(task, input.facilityLabel));
  }

  for (const inboxItem of input.workInboxItems) {
    if (inboxItem.sourceKind === "ai-task" && taskIds.has(inboxItem.sourceId)) {
      continue;
    }

    const mapped = mapWorkInboxItemToCenterItem(inboxItem, input.facilityLabel);
    if (mapped) {
      items.set(mapped.dedupeKey, mapped);
    }
  }

  return [...items.values()].sort((left, right) => {
    const leftTime = left.occurredAt ? Date.parse(left.occurredAt) : 0;
    const rightTime = right.occurredAt ? Date.parse(right.occurredAt) : 0;
    return rightTime - leftTime;
  });
}

export function collectAssignedTaskIds(
  workInboxItems: readonly RawTeamsWorkInboxItem[],
): ReadonlySet<string> {
  return new Set(
    workInboxItems
      .filter((item) => item.sourceKind === "ai-task")
      .map((item) => item.sourceId),
  );
}

function matchesTaskOriginFilter(
  item: AiTaskCenterItem,
  originFilter: AiTaskCenterTaskOriginFilter,
): boolean {
  if (originFilter === "all") {
    return true;
  }

  return item.kind === "task" && item.listOriginKind === "teams-message";
}

export function filterAiTaskCenterItems(
  items: readonly AiTaskCenterItem[],
  view: AiTaskCenterView,
  assignedTaskIds: ReadonlySet<string>,
  originFilter: AiTaskCenterTaskOriginFilter,
): readonly AiTaskCenterItem[] {
  const viewFiltered = items.filter((item) => {
    switch (view) {
      case "assigned":
        return item.kind !== "task" || assignedTaskIds.has(item.nativeSourceId);
      case "all-tasks":
        return item.kind === "task" && !isTaskItemFinal(item);
      case "approvals-handovers":
        return item.kind === "approval" || item.kind === "handover";
      case "mentions":
        return item.kind === "mention";
      case "completed":
        return item.kind === "task" && isTaskItemFinal(item);
      default:
        return true;
    }
  });

  if (view === "all-tasks" || view === "assigned" || view === "completed") {
    if (originFilter === "all") {
      return viewFiltered;
    }

    return viewFiltered.filter((item) => matchesTaskOriginFilter(item, originFilter));
  }

  return viewFiltered;
}

function isTaskItemFinal(item: AiTaskCenterItem): boolean {
  if (item.kind !== "task" || !item.statusCode) {
    return false;
  }

  return isAiTaskFinalStatus(item.statusCode as never);
}

export function hasAnyTaskCenterColumnValue(
  items: readonly AiTaskCenterItem[],
  selector: (item: AiTaskCenterItem) => string | null,
): boolean {
  return items.some((item) => {
    const value = selector(item);
    return value !== null && value.trim().length > 0;
  });
}
