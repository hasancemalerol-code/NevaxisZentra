export type AiTaskPlaybookLibraryView = "playbooks" | "active" | "history";

export const AI_TASK_PLAYBOOK_LIBRARY_PATH = "/ai-gorevleri/playbooklar";

export function parseAiTaskPlaybookLibraryView(
  value: string | null | undefined,
): AiTaskPlaybookLibraryView {
  if (!value) return "playbooks";
  if (value === "active" || value === "calisan" || value === "calisma") return "active";
  if (value === "history" || value === "gecmis") return "history";
  if (value === "playbooks" || value === "playbooklar") return "playbooks";
  return "playbooks";
}

export function toAiTaskPlaybookLibraryViewParam(view: AiTaskPlaybookLibraryView): string | null {
  if (view === "active") return "calisan";
  if (view === "history") return "gecmis";
  return null;
}

export function buildAiTaskPlaybookLibraryHref(
  view: AiTaskPlaybookLibraryView = "playbooks",
  extras?: Readonly<Record<string, string | null | undefined>>,
): string {
  const params = new URLSearchParams();
  const viewParam = toAiTaskPlaybookLibraryViewParam(view);
  if (viewParam) params.set("gorunum", viewParam);
  if (extras) {
    for (const [key, value] of Object.entries(extras)) {
      if (value) params.set(key, value);
    }
  }
  const query = params.toString();
  return query ? `${AI_TASK_PLAYBOOK_LIBRARY_PATH}?${query}` : AI_TASK_PLAYBOOK_LIBRARY_PATH;
}

export function buildAiTaskPlaybookRunDetailHref(runId: string): string {
  return `/ai-gorevleri/playbooklar/calisma/${runId}`;
}
