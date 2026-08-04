import {
  AI_TASK_FILTER_PRESET_VALUES,
  type AiTaskFilterPresetId,
  type AiTaskFilterValue,
} from "../application/ai-task-filter";
import { areAiTaskFilterValuesEqual } from "./ai-task-filter-url";

const AI_TASK_FILTER_PRESET_IDS:
  readonly AiTaskFilterPresetId[] = [
    "all",
    "critical",
    "awaiting-approval",
    "overdue",
    "failed",
  ];

export function resolveAiTaskFilterPresetId(
  value: AiTaskFilterValue,
): AiTaskFilterPresetId | undefined {
  return AI_TASK_FILTER_PRESET_IDS.find(
    (presetId) =>
      areAiTaskFilterValuesEqual(
        value,
        AI_TASK_FILTER_PRESET_VALUES[presetId],
      ),
  );
}

export function isAiTaskFilterPresetActive(
  value: AiTaskFilterValue,
  presetId: AiTaskFilterPresetId,
): boolean {
  return areAiTaskFilterValuesEqual(
    value,
    AI_TASK_FILTER_PRESET_VALUES[presetId],
  );
}