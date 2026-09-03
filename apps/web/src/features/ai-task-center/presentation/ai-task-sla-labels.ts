import type {
  AiTaskEscalationState,
  AiTaskSlaState,
} from "../domain/ai-task";
import type { AiTaskVisualTone } from "./ai-task-labels";

export const AI_TASK_SLA_STATE_LABELS: Readonly<
  Record<AiTaskSlaState, string>
> = {
  "on-track": "Zamanında",
  "due-soon": "Yakında son tarih",
  overdue: "Gecikmiş",
  breached: "SLA ihlali",
  "completed-within-sla": "SLA içinde tamamlandı",
  "completed-after-sla": "SLA sonrası tamamlandı",
  "not-applicable": "Uygulanamaz",
};

export const AI_TASK_ESCALATION_STATE_LABELS: Readonly<
  Record<AiTaskEscalationState, string>
> = {
  none: "Yok",
  active: "Aktif",
  resolved: "Çözüldü",
};

export const AI_TASK_SLA_STATE_TONES: Readonly<
  Record<AiTaskSlaState, AiTaskVisualTone>
> = {
  "on-track": "success",
  "due-soon": "warning",
  overdue: "danger",
  breached: "danger",
  "completed-within-sla": "success",
  "completed-after-sla": "warning",
  "not-applicable": "neutral",
};

export const AI_TASK_ESCALATION_STATE_TONES: Readonly<
  Record<AiTaskEscalationState, AiTaskVisualTone>
> = {
  none: "neutral",
  active: "danger",
  resolved: "success",
};

export function getAiTaskSlaStateLabel(state: AiTaskSlaState): string {
  return AI_TASK_SLA_STATE_LABELS[state];
}

export function getAiTaskEscalationStateLabel(
  state: AiTaskEscalationState,
): string {
  return AI_TASK_ESCALATION_STATE_LABELS[state];
}

export function getAiTaskEscalationLevelLabel(level: number): string {
  if (level <= 0) {
    return "Eskale değil";
  }

  return `Seviye ${level}`;
}

export function getAiTaskEscalationBadgeLabel(level: number): string | null {
  if (level <= 0) {
    return null;
  }

  if (level === 1) {
    return "Eskalasyon 1";
  }

  if (level === 2) {
    return "Eskalasyon 2";
  }

  if (level === 3) {
    return "Yönetim Eskalasyonu";
  }

  return "Kritik Eskalasyon";
}

export function getAiTaskSlaDisplayText(
  slaState: AiTaskSlaState | null,
  slaDueAt: string | null | undefined,
  now: Date = new Date(),
): string | null {
  if (!slaState || slaState === "not-applicable") {
    return null;
  }

  const countdown = slaDueAt ? formatAiTaskSlaCountdown(slaDueAt, now) : null;

  switch (slaState) {
    case "on-track":
      return countdown ?? "SLA içinde";
    case "due-soon":
      return countdown ?? "SLA riski";
    case "overdue":
      return countdown ?? getAiTaskSlaStateLabel(slaState);
    case "breached":
      return countdown ?? "SLA ihlali";
    case "completed-within-sla":
      return getAiTaskSlaStateLabel(slaState);
    case "completed-after-sla":
      return getAiTaskSlaStateLabel(slaState);
    default:
      return getAiTaskSlaStateLabel(slaState);
  }
}

export function formatAiTaskSlaCountdown(
  dueAt: string | undefined,
  now: Date = new Date(),
): string | null {
  if (!dueAt) {
    return null;
  }

  const target = new Date(dueAt);
  if (Number.isNaN(target.getTime())) {
    return null;
  }

  const diffMs = target.getTime() - now.getTime();
  const absMinutes = Math.max(1, Math.round(Math.abs(diffMs) / 60_000));

  if (diffMs >= 0) {
    if (absMinutes < 60) {
      return `${absMinutes} dk kaldı`;
    }

    const hours = Math.floor(absMinutes / 60);
    const minutes = absMinutes % 60;
    return minutes > 0 ? `${hours} sa ${minutes} dk kaldı` : `${hours} sa kaldı`;
  }

  if (absMinutes < 60) {
    return `${absMinutes} dk gecikti`;
  }

  const hours = Math.floor(absMinutes / 60);
  const minutes = absMinutes % 60;
  return minutes > 0 ? `${hours} sa ${minutes} dk gecikti` : `${hours} sa gecikti`;
}
