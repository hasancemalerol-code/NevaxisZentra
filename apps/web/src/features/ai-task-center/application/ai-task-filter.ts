import type {
  AiTaskCategory,
  AiTaskPriority,
  AiTaskStatus,
} from "../domain/ai-task";

export type AiTaskFilterValue = Readonly<{
  search: string;
  category: AiTaskCategory | "all";
  priority: AiTaskPriority | "all";
  status: AiTaskStatus | "all";
  approvalPendingOnly: boolean;
  overdueOnly: boolean;
}>;

export type AiTaskFilterPresetId =
  | "all"
  | "critical"
  | "awaiting-approval"
  | "overdue"
  | "failed";

export const INITIAL_AI_TASK_FILTER_VALUE:
  AiTaskFilterValue = {
    search: "",
    category: "all",
    priority: "all",
    status: "all",
    approvalPendingOnly: false,
    overdueOnly: false,
  };

export const AI_TASK_FILTER_PRESET_VALUES: Readonly<
  Record<AiTaskFilterPresetId, AiTaskFilterValue>
> = {
  all: {
    ...INITIAL_AI_TASK_FILTER_VALUE,
  },
  critical: {
    ...INITIAL_AI_TASK_FILTER_VALUE,
    priority: "critical",
  },
  "awaiting-approval": {
    ...INITIAL_AI_TASK_FILTER_VALUE,
    status: "awaiting-approval",
    approvalPendingOnly: true,
  },
  overdue: {
    ...INITIAL_AI_TASK_FILTER_VALUE,
    overdueOnly: true,
  },
  failed: {
    ...INITIAL_AI_TASK_FILTER_VALUE,
    status: "failed",
  },
};

export function createInitialAiTaskFilterValue():
  AiTaskFilterValue {
  return {
    ...INITIAL_AI_TASK_FILTER_VALUE,
  };
}

export function createAiTaskFilterPresetValue(
  presetId: AiTaskFilterPresetId,
): AiTaskFilterValue {
  return {
    ...AI_TASK_FILTER_PRESET_VALUES[presetId],
  };
}