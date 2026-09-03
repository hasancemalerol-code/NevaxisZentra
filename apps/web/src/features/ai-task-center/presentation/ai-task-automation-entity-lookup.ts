/**
 * Görünüm katmanı varlık çözümlemesi — yalnızca pilot/dev Teams kanalları.
 * Teknik UUID'ler kullanıcı arayüzünde gösterilmez.
 */

export const AI_TASK_AUTOMATION_TEAM_CHANNELS: ReadonlyArray<Readonly<{ id: string; label: string }>> = [
  { id: "bb000001-03d3-4000-8000-0000000003d3", label: "Bakım Ekibi" },
  { id: "bb000001-03d2-4000-8000-0000000003d2", label: "Kalite Ekibi" },
  { id: "bb000001-03d0-4000-8000-0000000003d0", label: "Üretim Ekibi" },
  { id: "bb000001-03d4-4000-8000-0000000003d4", label: "Stok ve Depo Ekibi" },
  { id: "bb000001-03d5-4000-8000-0000000003d5", label: "Satın Alma Ekibi" },
  { id: "bb000001-03d6-4000-8000-0000000003d6", label: "Yönetim Ekibi" },
];

const TEAM_CHANNEL_LABEL_BY_ID = new Map(
  AI_TASK_AUTOMATION_TEAM_CHANNELS.map((entry) => [entry.id.toLowerCase(), entry.label]),
);

const UUID_PATTERN = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;

export function resolveAutomationTeamChannelLabel(teamChannelId: string | null | undefined): string {
  if (!teamChannelId) return "Atanmamış";
  const resolved = TEAM_CHANNEL_LABEL_BY_ID.get(teamChannelId.toLowerCase());
  return resolved ?? "Bilinmeyen ekip";
}

export function containsRawUuid(value: string | null | undefined): boolean {
  if (!value) return false;
  return UUID_PATTERN.test(value.trim());
}

export function stripUuidFromText(value: string): string {
  return value.replace(
    /[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}/gi,
    "",
  ).replace(/\s{2,}/g, " ").trim();
}

export function formatAutomationAssignmentValue(value: string | null | undefined): string {
  if (!value) return "—";
  if (value.startsWith("team:")) {
    return resolveAutomationTeamChannelLabel(value.slice("team:".length));
  }
  if (value.startsWith("membership:")) {
    return "Atanan üye";
  }
  if (containsRawUuid(value)) {
    return resolveAutomationTeamChannelLabel(value);
  }
  return value;
}
