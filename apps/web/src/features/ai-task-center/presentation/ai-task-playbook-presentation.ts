import { formatAiTaskShortDateTime } from "./ai-task-formatters";

const FIXTURE_PLAYBOOK_KEY_PREFIX = "AI-TASK-PLAYBOOK1-LIVE";

const FIXTURE_PLAYBOOK_NAME_LABELS: Readonly<Record<string, string>> = {
  "Critical Machine Incident Live Flow": "Kritik Makine Olayı — Canlı Akış",
};

const UUID_PATTERN =
  /^[0-9a-f]{8}-[0-9a-f]{4}-[1-5][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i;

export function isAiTaskPlaybookFixtureKey(playbookKey: string | null | undefined): boolean {
  if (!playbookKey) return false;
  return playbookKey.trim().toUpperCase().startsWith(FIXTURE_PLAYBOOK_KEY_PREFIX);
}

export function localizeAiTaskPlaybookDisplayName(
  name: string,
  playbookKey?: string | null,
): string {
  const trimmed = name.trim();
  if (!trimmed) return "Adsız playbook";

  if (FIXTURE_PLAYBOOK_NAME_LABELS[trimmed]) {
    return FIXTURE_PLAYBOOK_NAME_LABELS[trimmed];
  }

  if (playbookKey && isAiTaskPlaybookFixtureKey(playbookKey)) {
    return FIXTURE_PLAYBOOK_NAME_LABELS["Critical Machine Incident Live Flow"];
  }

  return trimmed;
}

export function formatAiTaskPlaybookScopeLabel(facilityId: string | null): string {
  return facilityId ? "Tesis kapsamı" : "Genel kapsam";
}

export function formatAiTaskPlaybookVersionLabel(version: number): string {
  if (!Number.isFinite(version) || version <= 0) return "v1";
  return `v${version}`;
}

export function formatAiTaskPlaybookStepCountLabel(stepCount: number): string {
  const count = Number.isFinite(stepCount) ? Math.max(0, stepCount) : 0;
  return `${count} Adım`;
}

export function formatAiTaskPlaybookLastRunLabel(lastRunAtUtc: string | null): string {
  if (!lastRunAtUtc) return "Henüz çalıştırılmadı";
  return formatAiTaskShortDateTime(lastRunAtUtc);
}

export function formatAiTaskPlaybookUpdatedAtLabel(updatedAtUtc: string | null | undefined): string {
  if (!updatedAtUtc) return "—";
  return formatAiTaskShortDateTime(updatedAtUtc);
}

export function isAiTaskPlaybookTechnicalIdentifier(value: string | null | undefined): boolean {
  if (!value) return false;
  const trimmed = value.trim();
  if (!trimmed) return false;
  if (isAiTaskPlaybookFixtureKey(trimmed)) return true;
  if (UUID_PATTERN.test(trimmed)) return true;
  if (/^AI-TASK-[A-Z0-9-]+$/i.test(trimmed)) return true;
  return false;
}

export function shouldExposePlaybookTechnicalKeyInLibrary(): boolean {
  return false;
}
