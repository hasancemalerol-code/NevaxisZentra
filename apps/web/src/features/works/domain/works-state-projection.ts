import type { WorksListItem } from "./works-types";

export type WorksDisplayTone =
  | "neutral"
  | "info"
  | "warning"
  | "active"
  | "success"
  | "critical"
  | "muted";

const DISPLAY_STATE_TONES: Record<string, WorksDisplayTone> = {
  "YENİ": "neutral",
  "İNCELENİYOR": "info",
  "PLANLANIYOR": "info",
  "ONAY BEKLİYOR": "warning",
  "UYGULANIYOR": "active",
  "GÖZLEMLENİYOR": "info",
  "DOĞRULANIYOR": "warning",
  "TAMAMLANDI": "success",
  "ENGELLENDİ": "critical",
  "İPTAL": "muted",
};

const PRIORITY_TONES: Record<string, WorksDisplayTone> = {
  Kritik: "critical",
  Yüksek: "warning",
  Orta: "info",
  Düşük: "neutral",
};

export function resolveWorksDisplayTone(displayState: string): WorksDisplayTone {
  return DISPLAY_STATE_TONES[displayState] ?? "neutral";
}

export function resolveWorksPriorityTone(priorityLabel: string): WorksDisplayTone {
  return PRIORITY_TONES[priorityLabel] ?? "neutral";
}

export function resolveWorksDisplayBadgeClass(displayState: string): string {
  const tone = resolveWorksDisplayTone(displayState);
  return `works-badge works-badge--${tone}`;
}

export function resolveWorksPriorityBadgeClass(priorityLabel: string): string {
  const tone = resolveWorksPriorityTone(priorityLabel);
  return `works-badge works-badge--${tone}`;
}

export function resolveWorksRowToneClass(item: WorksListItem): string {
  if (item.isBlocked) {
    return "works-row works-row--blocked";
  }
  if (item.hasPendingApproval) {
    return "works-row works-row--approval";
  }
  if (item.severity === "critical") {
    return "works-row works-row--critical";
  }
  return "works-row";
}

export function formatWorksApprovalStatus(hasPendingApproval: boolean): string {
  return hasPendingApproval ? "Onay bekliyor" : "—";
}

export function formatWorksBlockerStatus(item: WorksListItem): string {
  if (item.isBlocked) {
    return item.blockerLabel ?? "Engelli";
  }
  return "—";
}
