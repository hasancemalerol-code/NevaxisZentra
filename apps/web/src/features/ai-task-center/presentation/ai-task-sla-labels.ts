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
    return "Üst Seviyeye Aktarım 1";
  }

  if (level === 2) {
    return "Üst Seviyeye Aktarım 2";
  }

  if (level === 3) {
    return "Yönetim Üst Seviyeye Aktarımı";
  }

  return "Kritik Üst Seviyeye Aktarım";
}

export const AI_TASK_SLA_BREACH_TARGET_LABELS: Readonly<Record<string, string>> = {
  acknowledgment: "onay hedefi aşıldı",
  response: "yanıt hedefi aşıldı",
  resolution: "çözüm hedefi aşıldı",
};

/**
 * Names WHICH SLA target was missed. A breach is not automatically a missed resolution/due date: the engine
 * reports the first missed target (acknowledgement, response, then resolution).
 */
export function getAiTaskSlaBreachLabel(
  slaState: AiTaskSlaState,
  breachType?: string | null,
): string {
  const base = getAiTaskSlaStateLabel(slaState);
  const target = breachType ? AI_TASK_SLA_BREACH_TARGET_LABELS[breachType] : undefined;
  if (slaState !== "breached" || !target) {
    return base;
  }

  // Only a resolution breach is a generic "SLA ihlali"; an acknowledgement/response breach names the missed target
  // so it can never read as a contradiction next to a due date that is still ahead.
  return breachType === "resolution"
    ? `${base} · ${target}`
    : `${target.charAt(0).toLocaleUpperCase("tr-TR")}${target.slice(1)}`;
}

export function getAiTaskSlaDisplayText(
  slaState: AiTaskSlaState | null,
  slaDueAt: string | null | undefined,
  now: Date = new Date(),
  breachType?: string | null,
): string | null {
  if (!slaState || slaState === "not-applicable") {
    return null;
  }

  switch (slaState) {
    case "on-track":
      return formatAiTaskSlaCountdown(slaDueAt ?? undefined, now) ?? "SLA içinde";
    case "due-soon":
      return formatAiTaskSlaCountdown(slaDueAt ?? undefined, now) ?? "SLA riski";
    case "overdue":
      return formatAiTaskSlaCountdown(slaDueAt ?? undefined, now) ?? getAiTaskSlaStateLabel(slaState);
    case "breached":
      // Never a countdown: the remaining time to the resolution deadline is shown separately and must not
      // read as a delay while that deadline is still in the future.
      return getAiTaskSlaBreachLabel(slaState, breachType);
    default:
      return getAiTaskSlaStateLabel(slaState);
  }
}

export type AiTaskResolutionDeadlineSummary = Readonly<{
  label: string;
  value: string;
  /** True only when the resolution deadline has actually passed. */
  passed: boolean;
}>;

/** Shared "remaining until the resolution deadline" row: wording is a delay only after the deadline passed. */
export function getAiTaskResolutionDeadlineSummary(
  slaState: AiTaskSlaState | null,
  resolutionDueAt: string | null | undefined,
  now: Date = new Date(),
): AiTaskResolutionDeadlineSummary | null {
  if (!slaState || slaState === "not-applicable") {
    return null;
  }

  if (slaState === "completed-within-sla" || slaState === "completed-after-sla") {
    return { label: "Çözüm hedefi", value: getAiTaskSlaStateLabel(slaState), passed: slaState === "completed-after-sla" };
  }

  const target = resolutionDueAt ? new Date(resolutionDueAt) : null;
  if (!target || Number.isNaN(target.getTime())) {
    return null;
  }

  const diffMs = target.getTime() - now.getTime();
  const duration = formatDuration(Math.abs(diffMs));
  return diffMs >= 0
    ? { label: "Çözüm hedefi", value: duration, passed: false }
    : { label: "Çözüm hedefi", value: duration, passed: true };
}

function formatDuration(absMs: number): string {
  const absMinutes = Math.max(1, Math.round(absMs / 60_000));
  if (absMinutes < 60) {
    return `${absMinutes} dk`;
  }

  const hours = Math.floor(absMinutes / 60);
  const minutes = absMinutes % 60;
  return minutes > 0 ? `${hours} sa ${minutes} dk` : `${hours} sa`;
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
  return `${formatDuration(Math.abs(diffMs))} ${diffMs >= 0 ? "kaldı" : "gecikti"}`;
}
