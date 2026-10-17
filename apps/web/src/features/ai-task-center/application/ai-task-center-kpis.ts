import type { AiTaskCenterItem } from "../domain/ai-task-center-item";
import type { AiTaskKpiSummary } from "./ai-task-query";
import { filterAiTaskCenterItems } from "./ai-task-center-composition";

/**
 * Derives the summary cards from the SAME item list and the SAME view filters the tab strip counts,
 * so a card and the tab it links to can never disagree (single definition of open / overdue / risk).
 * `criticalRisk` carries the service-level-risk view count and `awaitingApproval` the approval and
 * handover view count, matching the views those cards open.
 */
export function deriveAiTaskCenterKpis(
  items: readonly AiTaskCenterItem[],
  fallback: AiTaskKpiSummary,
  now: Date = new Date(),
): AiTaskKpiSummary {
  const none: ReadonlySet<string> = new Set();
  const count = (view: Parameters<typeof filterAiTaskCenterItems>[1]) =>
    filterAiTaskCenterItems(items, view, none, "all", now).length;

  return {
    ...fallback,
    total: items.filter((item) => item.kind === "task").length,
    open: count("all-tasks"),
    awaitingApproval: count("approvals-handovers"),
    criticalRisk: count("sla-risk"),
    overdue: count("overdue"),
  };
}
