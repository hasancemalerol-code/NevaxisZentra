import {
  INITIAL_AI_TASK_FILTER_VALUE,
  type AiTaskFilterValue,
} from "../application/ai-task-filter";
import {
  AI_TASK_CATEGORIES,
  AI_TASK_PRIORITIES,
  AI_TASK_STATUSES,
  type AiTaskCategory,
  type AiTaskPriority,
  type AiTaskStatus,
} from "../domain/ai-task";

export const AI_TASK_FILTER_QUERY_KEYS = [
  "q",
  "category",
  "priority",
  "status",
  "approval",
  "overdue",
] as const;

function isAiTaskCategory(
  value: string | null,
): value is AiTaskCategory {
  return (
    value !== null &&
    AI_TASK_CATEGORIES.some(
      (category) => category === value,
    )
  );
}

function isAiTaskPriority(
  value: string | null,
): value is AiTaskPriority {
  return (
    value !== null &&
    AI_TASK_PRIORITIES.some(
      (priority) => priority === value,
    )
  );
}

function isAiTaskStatus(
  value: string | null,
): value is AiTaskStatus {
  return (
    value !== null &&
    AI_TASK_STATUSES.some(
      (status) => status === value,
    )
  );
}

function parseBooleanQueryValue(
  value: string | null,
): boolean {
  return value === "1" || value === "true";
}

export function parseAiTaskFilterSearchParams(
  params: URLSearchParams,
): AiTaskFilterValue {
  const category = params.get("category");
  const priority = params.get("priority");
  const status = params.get("status");

  return {
    search: params.get("q") ?? "",
    category: isAiTaskCategory(category)
      ? category
      : INITIAL_AI_TASK_FILTER_VALUE.category,
    priority: isAiTaskPriority(priority)
      ? priority
      : INITIAL_AI_TASK_FILTER_VALUE.priority,
    status: isAiTaskStatus(status)
      ? status
      : INITIAL_AI_TASK_FILTER_VALUE.status,
    approvalPendingOnly: parseBooleanQueryValue(
      params.get("approval"),
    ),
    overdueOnly: parseBooleanQueryValue(
      params.get("overdue"),
    ),
  };
}

export function readAiTaskFilterFromLocation(
  locationSearch: string,
): AiTaskFilterValue {
  return parseAiTaskFilterSearchParams(
    new URLSearchParams(locationSearch),
  );
}

export function applyAiTaskFilterToUrl(
  url: URL,
  value: AiTaskFilterValue,
): URL {
  AI_TASK_FILTER_QUERY_KEYS.forEach((key) => {
    url.searchParams.delete(key);
  });

  const search = value.search.trim();

  if (search) {
    url.searchParams.set("q", search);
  }

  if (
    value.category !==
    INITIAL_AI_TASK_FILTER_VALUE.category
  ) {
    url.searchParams.set(
      "category",
      value.category,
    );
  }

  if (
    value.priority !==
    INITIAL_AI_TASK_FILTER_VALUE.priority
  ) {
    url.searchParams.set(
      "priority",
      value.priority,
    );
  }

  if (
    value.status !==
    INITIAL_AI_TASK_FILTER_VALUE.status
  ) {
    url.searchParams.set(
      "status",
      value.status,
    );
  }

  if (value.approvalPendingOnly) {
    url.searchParams.set("approval", "1");
  }

  if (value.overdueOnly) {
    url.searchParams.set("overdue", "1");
  }

  return url;
}

export function createAiTaskFilterRelativeUrl(
  currentUrl: string,
  value: AiTaskFilterValue,
): string {
  const url = applyAiTaskFilterToUrl(
    new URL(currentUrl),
    value,
  );

  return `${url.pathname}${url.search}${url.hash}`;
}

export function areAiTaskFilterValuesEqual(
  left: AiTaskFilterValue,
  right: AiTaskFilterValue,
): boolean {
  return (
    left.search === right.search &&
    left.category === right.category &&
    left.priority === right.priority &&
    left.status === right.status &&
    left.approvalPendingOnly ===
      right.approvalPendingOnly &&
    left.overdueOnly === right.overdueOnly
  );
}

export function hasActiveAiTaskFilters(
  value: AiTaskFilterValue,
): boolean {
  return !areAiTaskFilterValuesEqual(
    value,
    INITIAL_AI_TASK_FILTER_VALUE,
  );
}