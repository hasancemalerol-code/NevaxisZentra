import type { WorksQueueCounts } from "./works-types";

export const WORKS_QUEUE_ASSIGNED = "assigned";
export const WORKS_QUEUE_TEAM = "team";
export const WORKS_QUEUE_PENDING_APPROVAL = "pending-approval";
export const WORKS_QUEUE_CRITICAL = "critical";
export const WORKS_QUEUE_BLOCKED = "blocked";
export const WORKS_QUEUE_IN_PROGRESS = "in-progress";
export const WORKS_QUEUE_OBSERVING = "observing";
export const WORKS_QUEUE_VERIFICATION_PENDING = "verification-pending";
export const WORKS_QUEUE_COMPLETED_TODAY = "completed-today";

export type WorksQueueId =
  | typeof WORKS_QUEUE_ASSIGNED
  | typeof WORKS_QUEUE_TEAM
  | typeof WORKS_QUEUE_PENDING_APPROVAL
  | typeof WORKS_QUEUE_CRITICAL
  | typeof WORKS_QUEUE_BLOCKED
  | typeof WORKS_QUEUE_IN_PROGRESS
  | typeof WORKS_QUEUE_OBSERVING
  | typeof WORKS_QUEUE_VERIFICATION_PENDING
  | typeof WORKS_QUEUE_COMPLETED_TODAY;

export type WorksQueueOption = Readonly<{
  id: WorksQueueId;
  label: string;
  description: string;
}>;

export const WORKS_QUEUE_OPTIONS: readonly WorksQueueOption[] = [
  {
    id: WORKS_QUEUE_ASSIGNED,
    label: "Bana Atanan",
    description: "Doğrudan size atanmış açık işler",
  },
  {
    id: WORKS_QUEUE_TEAM,
    label: "Ekip kuyruğu",
    description: "Tüm ekip iş kuyruğu",
  },
  {
    id: WORKS_QUEUE_PENDING_APPROVAL,
    label: "Onay Bekleyen",
    description: "İnsan kararı gerektiren aksiyonlar",
  },
  {
    id: WORKS_QUEUE_CRITICAL,
    label: "Kritik",
    description: "Kritik öncelikli işler",
  },
  {
    id: WORKS_QUEUE_BLOCKED,
    label: "Engelli",
    description: "Engel nedeniyle bekleyen işler",
  },
  {
    id: WORKS_QUEUE_IN_PROGRESS,
    label: "Devam Eden",
    description: "Aktif uygulama ve karar aşaması",
  },
  {
    id: WORKS_QUEUE_OBSERVING,
    label: "Gözlem",
    description: "Uygulama sonrası gözlem penceresi",
  },
  {
    id: WORKS_QUEUE_VERIFICATION_PENDING,
    label: "Doğrulama Bekleyen",
    description: "Sonuç doğrulaması bekleyen işler",
  },
  {
    id: WORKS_QUEUE_COMPLETED_TODAY,
    label: "Bugün Tamamlanan",
    description: "Bugün kapanan ve doğrulanan işler",
  },
] as const;

const VALID_QUEUE_IDS = new Set<string>(WORKS_QUEUE_OPTIONS.map((option) => option.id));

export function isWorksQueueId(value: string | null | undefined): value is WorksQueueId {
  return Boolean(value && VALID_QUEUE_IDS.has(value));
}

export function resolveWorksQueueId(value: string | null | undefined): WorksQueueId {
  if (isWorksQueueId(value)) {
    return value;
  }
  return WORKS_QUEUE_TEAM;
}

export function getWorksQueueLabel(queueId: WorksQueueId): string {
  return WORKS_QUEUE_OPTIONS.find((option) => option.id === queueId)?.label ?? queueId;
}

export function getWorksQueueCount(counts: WorksQueueCounts, queueId: WorksQueueId): number {
  switch (queueId) {
    case WORKS_QUEUE_ASSIGNED:
      return counts.assigned;
    case WORKS_QUEUE_TEAM:
      return counts.team;
    case WORKS_QUEUE_PENDING_APPROVAL:
      return counts.pendingApproval;
    case WORKS_QUEUE_CRITICAL:
      return counts.critical;
    case WORKS_QUEUE_BLOCKED:
      return counts.blocked;
    case WORKS_QUEUE_IN_PROGRESS:
      return counts.inProgress;
    case WORKS_QUEUE_OBSERVING:
      return counts.observing;
    case WORKS_QUEUE_VERIFICATION_PENDING:
      return counts.verificationPending;
    case WORKS_QUEUE_COMPLETED_TODAY:
      return counts.completedToday;
    default:
      return 0;
  }
}

export function buildWorksWorkspaceHref(input?: Readonly<{
  queue?: WorksQueueId;
  caseId?: string | null;
  search?: string | null;
}>): string {
  const params = new URLSearchParams();
  if (input?.queue) {
    params.set("kuyruk", input.queue);
  }
  if (input?.caseId) {
    params.set("is", input.caseId);
  }
  if (input?.search?.trim()) {
    params.set("ara", input.search.trim());
  }
  const query = params.toString();
  return query ? `/isler?${query}` : "/isler";
}
