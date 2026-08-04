import type {
  AiTaskAuditEvent,
  AiTaskAuditEventType,
} from "../domain/ai-task";

export const AI_TASK_AUDIT_EVENT_LABELS: Readonly<
  Record<AiTaskAuditEventType, string>
> = {
  created: "Görev oluşturuldu",
  "analysis-updated": "AI analizi güncellendi",
  assigned: "Sorumlu atandı",
  "approval-requested": "Onay istendi",
  approved: "Görev onaylandı",
  rejected: "Görev reddedildi",
  deferred: "Görev ertelendi",
  "execution-started": "Uygulama başlatıldı",
  "execution-completed": "Uygulama tamamlandı",
  "execution-failed": "Uygulama başarısız oldu",
  cancelled: "Görev iptal edildi",
};

export const AI_TASK_AUDIT_ACTOR_TYPE_LABELS: Readonly<
  Record<AiTaskAuditEvent["actorType"], string>
> = {
  ai: "AI",
  user: "Kullanıcı",
  system: "Sistem",
};

export function getAiTaskAuditEventLabel(
  eventType: AiTaskAuditEventType,
): string {
  return AI_TASK_AUDIT_EVENT_LABELS[eventType];
}

export function getAiTaskAuditActorTypeLabel(
  actorType: AiTaskAuditEvent["actorType"],
): string {
  return AI_TASK_AUDIT_ACTOR_TYPE_LABELS[actorType];
}

export function getAiTaskAuditTransitionLabel(
  event: Pick<
    AiTaskAuditEvent,
    "previousStatus" | "nextStatus"
  >,
  getStatusLabel: (status: NonNullable<
    AiTaskAuditEvent["previousStatus"]
  >) => string,
): string | undefined {
  if (
    !event.previousStatus &&
    !event.nextStatus
  ) {
    return undefined;
  }

  const previousLabel = event.previousStatus
    ? getStatusLabel(event.previousStatus)
    : "Başlangıç";

  const nextLabel = event.nextStatus
    ? getStatusLabel(event.nextStatus)
    : "Değişmedi";

  return `${previousLabel} → ${nextLabel}`;
}