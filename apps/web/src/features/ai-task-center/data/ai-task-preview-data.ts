// Isolated test fixture data for dashboard deep-link catalog entries only.

export type AiTaskPreviewRecord = Readonly<{
  id: string;
}>;

export const AI_TASK_PREVIEW_DATA: readonly AiTaskPreviewRecord[] = [];

export function getAiTaskPreviewById(
  taskId: string,
): AiTaskPreviewRecord | null {
  return AI_TASK_PREVIEW_DATA.find((task) => task.id === taskId) ?? null;
}
