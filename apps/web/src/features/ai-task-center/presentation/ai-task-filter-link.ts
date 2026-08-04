import {
  AI_TASK_FILTER_PRESET_VALUES,
  type AiTaskFilterPresetId,
  type AiTaskFilterValue,
} from "../application/ai-task-filter";
import {
  applyAiTaskFilterToUrl,
  areAiTaskFilterValuesEqual,
} from "./ai-task-filter-url";

export type AiTaskKpiFilterId =
  | "open"
  | "awaiting-approval"
  | "critical"
  | "overdue";

const AI_TASK_KPI_PRESET_MAP: Readonly<
  Record<
    Exclude<AiTaskKpiFilterId, "open">,
    AiTaskFilterPresetId
  >
> = {
  "awaiting-approval": "awaiting-approval",
  critical: "critical",
  overdue: "overdue",
};

export function createAiTaskFilterHref(
  value: AiTaskFilterValue,
  pathname = "/",
): string {
  const url = applyAiTaskFilterToUrl(
    new URL(pathname, "http://localhost"),
    value,
  );

  return `${url.pathname}${url.search}`;
}

export function createAiTaskPresetHref(
  presetId: AiTaskFilterPresetId,
  pathname = "/",
): string {
  return createAiTaskFilterHref(
    AI_TASK_FILTER_PRESET_VALUES[presetId],
    pathname,
  );
}

export function createAiTaskKpiHref(
  filterId: AiTaskKpiFilterId,
  pathname = "/",
): string {
  if (filterId === "open") {
    return createAiTaskPresetHref(
      "all",
      pathname,
    );
  }

  return createAiTaskPresetHref(
    AI_TASK_KPI_PRESET_MAP[filterId],
    pathname,
  );
}

export function isAiTaskKpiFilterActive(
  value: AiTaskFilterValue,
  filterId: AiTaskKpiFilterId,
): boolean {
  if (filterId === "open") {
    return areAiTaskFilterValuesEqual(
      value,
      AI_TASK_FILTER_PRESET_VALUES.all,
    );
  }

  const presetId =
    AI_TASK_KPI_PRESET_MAP[filterId];

  return areAiTaskFilterValuesEqual(
    value,
    AI_TASK_FILTER_PRESET_VALUES[presetId],
  );
}