import type { LivePlanWorkspaceSelection, LivePlanWorkspaceTab } from "../domain/live-plan-workspace-types";

const VALID_TABS = new Set<LivePlanWorkspaceTab>([
  "summary",
  "live",
  "scenarios",
  "changes",
  "history",
  "risks",
  "timeline",
]);

function readParam(
  searchParams: Readonly<Record<string, string | string[] | undefined>> | undefined,
  key: string,
): string | undefined {
  const value = searchParams?.[key];
  if (Array.isArray(value)) {
    return value[0];
  }
  return typeof value === "string" && value.trim() ? value.trim() : undefined;
}

export function readLivePlanWorkspaceSelection(
  searchParams?: Readonly<Record<string, string | string[] | undefined>>,
): LivePlanWorkspaceSelection {
  const tabParam = readParam(searchParams, "tab");
  const tab = tabParam && VALID_TABS.has(tabParam as LivePlanWorkspaceTab)
    ? (tabParam as LivePlanWorkspaceTab)
    : "live";

  return {
    productionPlanId: readParam(searchParams, "plan"),
    lineId: readParam(searchParams, "hat"),
    tab,
    search: readParam(searchParams, "q"),
  };
}

export function buildLivePlanWorkspaceHref(
  basePath: string,
  selection: LivePlanWorkspaceSelection,
): string {
  const params = new URLSearchParams();
  if (selection.productionPlanId) params.set("plan", selection.productionPlanId);
  if (selection.lineId) params.set("hat", selection.lineId);
  if (selection.tab !== "live") params.set("tab", selection.tab);
  if (selection.search) params.set("q", selection.search);
  const suffix = params.size > 0 ? `?${params.toString()}` : "";
  return `${basePath}${suffix}`;
}
