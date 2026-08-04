import type {
  AiTaskFilterPresetId,
  AiTaskFilterValue,
} from "./ai-task-filter";
import {
  AI_TASK_FILTER_PRESET_VALUES,
  createAiTaskFilterPresetValue,
} from "./ai-task-filter";
import type { AiTaskFilter } from "./ai-task-query";

export function toAiTaskQueryFilter(
  value: AiTaskFilterValue,
): AiTaskFilter {
  return {
    search:
      value.search.trim().length > 0
        ? value.search
        : undefined,
    categories:
      value.category === "all"
        ? undefined
        : [value.category],
    priorities:
      value.priority === "all"
        ? undefined
        : [value.priority],
    statuses:
      value.status === "all"
        ? undefined
        : [value.status],
    approvalPendingOnly:
      value.approvalPendingOnly || undefined,
    overdueOnly:
      value.overdueOnly || undefined,
  };
}

export function mergeAiTaskFilterValues(
  current: AiTaskFilterValue,
  updates: Partial<AiTaskFilterValue>,
): AiTaskFilterValue {
  return {
    ...current,
    ...updates,
  };
}

export function applyAiTaskFilterPreset(
  presetId: AiTaskFilterPresetId,
): AiTaskFilterValue {
  return createAiTaskFilterPresetValue(presetId);
}

export function isAiTaskFilterPresetValue(
  value: AiTaskFilterValue,
  presetId: AiTaskFilterPresetId,
): boolean {
  const preset =
    AI_TASK_FILTER_PRESET_VALUES[presetId];

  return (
    value.search === preset.search &&
    value.category === preset.category &&
    value.priority === preset.priority &&
    value.status === preset.status &&
    value.approvalPendingOnly ===
      preset.approvalPendingOnly &&
    value.overdueOnly === preset.overdueOnly
  );
}