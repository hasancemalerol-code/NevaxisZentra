import { Badge } from "@/components/ui/badge";

import { AI_TASK_CENTER_SOURCE_LABELS } from "../domain/ai-task-center-item";

/** A task's origin. A missing origin is a data gap, so it reads as a quiet dash (full text kept for assistive tech), not as a button-like chip. */
export function AiTaskSourceLabel({ label }: Readonly<{ label: string }>) {
  if (label === AI_TASK_CENTER_SOURCE_LABELS.unspecified) {
    return (
      <span className="ai-task-source-none">
        <span aria-hidden="true">—</span>
        <span className="visually-hidden">{label}</span>
      </span>
    );
  }
  return <Badge tone="neutral">{label}</Badge>;
}
