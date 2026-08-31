export const AI_TASK_CENTER_SOURCE_LABELS = {
  "ai-analysis": "AI Analizi",
  "manual-task": "Manuel Görev",
  "teams-message": "Teams Mesajı",
  "operational-task": "Operasyonel Görev",
  unspecified: "Kaynak belirtilmemiş",
  "approval-request": "Onay Talebi",
  "shift-handover": "Vardiya Teslimi",
  mention: "Etiketleme",
} as const;

export type AiTaskCenterSourceLabelKey = keyof typeof AI_TASK_CENTER_SOURCE_LABELS;

export const AI_TASK_LIST_ORIGIN_KINDS = {
  "ai-analysis": "ai-analysis",
  "manual-task": "manual-task",
  "teams-message": "teams-message",
  "operational-task": "operational-task",
  unspecified: "unspecified",
} as const;

export type AiTaskListOriginKind = keyof typeof AI_TASK_LIST_ORIGIN_KINDS;

export const AI_TASK_CENTER_VIEWS = {
  assigned: "Bana Atananlar",
  "all-tasks": "Tüm Görevler",
  "approvals-handovers": "Onay ve Teslimler",
  mentions: "Etiketlemeler",
  completed: "Tamamlananlar",
} as const;

export type AiTaskCenterView = keyof typeof AI_TASK_CENTER_VIEWS;

export const AI_TASK_CENTER_TASK_ORIGIN_FILTERS = {
  all: "Tümü",
  teams: "Teams mesajından oluşturulan",
} as const;

export type AiTaskCenterTaskOriginFilter = keyof typeof AI_TASK_CENTER_TASK_ORIGIN_FILTERS;

export type AiTaskCenterItemKind = "task" | "approval" | "handover" | "mention";

export type AiTaskCenterItem = Readonly<{
  dedupeKey: string;
  kind: AiTaskCenterItemKind;
  sourceLabel: (typeof AI_TASK_CENTER_SOURCE_LABELS)[AiTaskCenterSourceLabelKey];
  listOriginKind: AiTaskListOriginKind | null;
  title: string;
  facilityLabel: string | null;
  assigneeLabel: string | null;
  priorityLabel: string | null;
  statusLabel: string | null;
  statusCode: string | null;
  occurredAt: string | null;
  relatedContext: string | null;
  deepLinkPath: string;
  nativeSourceKind: string;
  nativeSourceId: string;
}>;

export function parseAiTaskCenterView(value: string | null | undefined): AiTaskCenterView {
  if (value && value in AI_TASK_CENTER_VIEWS) {
    return value as AiTaskCenterView;
  }

  return "assigned";
}

export function parseAiTaskCenterTaskOriginFilter(
  value: string | null | undefined,
): AiTaskCenterTaskOriginFilter {
  if (value === "teams") {
    return "teams";
  }

  return "all";
}

export function resolveTaskSourceLabel(
  listOriginKind: AiTaskListOriginKind,
): AiTaskCenterItem["sourceLabel"] {
  return AI_TASK_CENTER_SOURCE_LABELS[listOriginKind];
}
