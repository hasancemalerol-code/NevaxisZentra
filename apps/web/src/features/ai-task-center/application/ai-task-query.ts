import type {
  AiTask,
  AiTaskCategory,
  AiTaskPriority,
  AiTaskRiskLevel,
  AiTaskStatus,
} from "../domain/ai-task";
import { isAiTaskFinalStatus } from "../domain/ai-task";

export type AiTaskFilter = Readonly<{
  search?: string;
  categories?: readonly AiTaskCategory[];
  priorities?: readonly AiTaskPriority[];
  riskLevels?: readonly AiTaskRiskLevel[];
  statuses?: readonly AiTaskStatus[];
  assigneeUserId?: string;
  approvalPendingOnly?: boolean;
  overdueOnly?: boolean;
}>;

export type AiTaskKpiSummary = Readonly<{
  total: number;
  open: number;
  awaitingReview: number;
  awaitingApproval: number;
  inProgress: number;
  criticalRisk: number;
  overdue: number;
  failed: number;
  completed: number;
}>;

const PRIORITY_ORDER: Readonly<
  Record<AiTaskPriority, number>
> = {
  critical: 4,
  high: 3,
  medium: 2,
  low: 1,
};

const RISK_ORDER: Readonly<
  Record<AiTaskRiskLevel, number>
> = {
  critical: 5,
  high: 4,
  medium: 3,
  low: 2,
  information: 1,
};

function normalizeSearchValue(value: string): string {
  return value
    .toLocaleLowerCase("tr-TR")
    .trim();
}

function includesSearchValue(
  task: AiTask,
  search: string,
): boolean {
  const normalizedSearch = normalizeSearchValue(search);

  if (!normalizedSearch) {
    return true;
  }

  const searchableValues = [
    task.id,
    task.title,
    task.summary,
    task.detectedIssue,
    task.reasoning,
    task.recommendation.title,
    task.recommendation.description,
    task.assignee?.displayName,
    task.productionOrderId,
    task.salesOrderId,
    task.machineId,
    task.moldId,
    task.materialId,
    task.supplierId,
    task.customerId,
  ];

  return searchableValues.some((value) =>
    value
      ? normalizeSearchValue(value).includes(
          normalizedSearch,
        )
      : false,
  );
}

export function isAiTaskOverdue(
  task: Pick<AiTask, "dueAt" | "status"> & Partial<Pick<AiTask, "slaState" | "sla">>,
  now: Date = new Date(),
): boolean {
  if (isAiTaskFinalStatus(task.status)) {
    return false;
  }

  // Canonical SLA truth wins: the backend-derived state decides overdue so card, list, detail and KPIs agree.
  // A task whose SLA is not applicable (no target) is never overdue.
  // "Overdue" means the due / resolution deadline passed; an acknowledgement/response breach with the deadline
  // still ahead is an SLA breach, not a delay.
  if (task.slaState !== undefined) {
    if (task.slaState === "overdue") {
      return true;
    }

    if (task.slaState !== "breached") {
      return false;
    }

    if (task.sla?.breachType === "resolution") {
      return true;
    }
  }

  if (!task.dueAt) {
    return false;
  }

  const dueAt = new Date(task.dueAt);

  if (Number.isNaN(dueAt.getTime())) {
    return false;
  }

  return dueAt.getTime() < now.getTime();
}

export function filterAiTasks(
  tasks: readonly AiTask[],
  filter: AiTaskFilter,
  now: Date = new Date(),
): readonly AiTask[] {
  return tasks.filter((task) => {
    if (
      filter.search &&
      !includesSearchValue(task, filter.search)
    ) {
      return false;
    }

    if (
      filter.categories?.length &&
      !filter.categories.includes(task.category)
    ) {
      return false;
    }

    if (
      filter.priorities?.length &&
      !filter.priorities.includes(task.priority)
    ) {
      return false;
    }

    if (
      filter.riskLevels?.length &&
      !filter.riskLevels.includes(task.riskLevel)
    ) {
      return false;
    }

    if (
      filter.statuses?.length &&
      !filter.statuses.includes(task.status)
    ) {
      return false;
    }

    if (
      filter.assigneeUserId &&
      task.assignee?.userId !== filter.assigneeUserId
    ) {
      return false;
    }

    if (
      filter.approvalPendingOnly &&
      task.approval.state !== "pending"
    ) {
      return false;
    }

    if (
      filter.overdueOnly &&
      !isAiTaskOverdue(task, now)
    ) {
      return false;
    }

    return true;
  });
}

export function sortAiTasksByUrgency(
  tasks: readonly AiTask[],
  now: Date = new Date(),
): readonly AiTask[] {
  return [...tasks].sort((left, right) => {
    const leftOverdue = isAiTaskOverdue(left, now);
    const rightOverdue = isAiTaskOverdue(right, now);

    if (leftOverdue !== rightOverdue) {
      return leftOverdue ? -1 : 1;
    }

    const riskDifference =
      RISK_ORDER[right.riskLevel] -
      RISK_ORDER[left.riskLevel];

    if (riskDifference !== 0) {
      return riskDifference;
    }

    const priorityDifference =
      PRIORITY_ORDER[right.priority] -
      PRIORITY_ORDER[left.priority];

    if (priorityDifference !== 0) {
      return priorityDifference;
    }

    const leftDueAt = left.dueAt
      ? new Date(left.dueAt).getTime()
      : Number.POSITIVE_INFINITY;

    const rightDueAt = right.dueAt
      ? new Date(right.dueAt).getTime()
      : Number.POSITIVE_INFINITY;

    if (leftDueAt !== rightDueAt) {
      return leftDueAt - rightDueAt;
    }

    return (
      new Date(right.createdAt).getTime() -
      new Date(left.createdAt).getTime()
    );
  });
}

export function calculateAiTaskKpis(
  tasks: readonly AiTask[],
  now: Date = new Date(),
): AiTaskKpiSummary {
  return tasks.reduce<AiTaskKpiSummary>(
    (summary, task) => ({
      total: summary.total + 1,
      open:
        summary.open +
        (isAiTaskFinalStatus(task.status) ? 0 : 1),
      awaitingReview:
        summary.awaitingReview +
        (task.status === "awaiting-review" ? 1 : 0),
      awaitingApproval:
        summary.awaitingApproval +
        (task.status === "awaiting-approval" ? 1 : 0),
      inProgress:
        summary.inProgress +
        (task.status === "in-progress" ? 1 : 0),
      criticalRisk:
        summary.criticalRisk +
        (task.riskLevel === "critical" ? 1 : 0),
      overdue:
        summary.overdue +
        (isAiTaskOverdue(task, now) ? 1 : 0),
      failed:
        summary.failed +
        (task.status === "failed" ? 1 : 0),
      completed:
        summary.completed +
        (task.status === "completed" ? 1 : 0),
    }),
    {
      total: 0,
      open: 0,
      awaitingReview: 0,
      awaitingApproval: 0,
      inProgress: 0,
      criticalRisk: 0,
      overdue: 0,
      failed: 0,
      completed: 0,
    },
  );
}

export function getAiTaskById(
  tasks: readonly AiTask[],
  taskId: string,
): AiTask | undefined {
  return tasks.find((task) => task.id === taskId);
}