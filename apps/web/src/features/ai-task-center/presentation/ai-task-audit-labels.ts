import type {
  AiTaskAuditEvent,
  AiTaskAuditEventType,
} from "../domain/ai-task";

export const AI_TASK_AUDIT_EVENT_LABELS: Readonly<
  Record<AiTaskAuditEventType, string>
> = {
  created: "Görev oluşturuldu",
  "analysis-updated": "AI analizi güncellendi",
  routed: "Görev yönlendirildi",
  assigned: "Sorumlu atandı",
  reassigned: "Sorumlu yeniden atandı",
  acknowledged: "Teslim alındı",
  "priority-changed": "Öncelik değiştirildi",
  "due-date-changed": "Son tarih değiştirildi",
  "sla-warning": "SLA uyarısı",
  escalated: "Görev eskale edildi",
  "approval-requested": "Onay istendi",
  approved: "Görev onaylandı",
  rejected: "Görev reddedildi",
  deferred: "Görev ertelendi",
  "execution-started": "Uygulama başlatıldı",
  "execution-completed": "Uygulama tamamlandı",
  "execution-failed": "Uygulama başarısız oldu",
  cancelled: "Görev iptal edildi",
  "status-changed": "Durum değiştirildi",
  "teams-notification-emitted": "Teams bildirimi gönderildi",
  "evidence-submitted": "Kanıt eklendi",
  "resolution-updated": "Çözüm güncellendi",
  "closure-requested": "Kapanış talep edildi",
  "closure-approved": "Kapanış onaylandı",
  "closure-rejected": "Kapanış reddedildi",
  "task-reopened": "Görev yeniden açıldı",
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
  return AI_TASK_AUDIT_EVENT_LABELS[eventType] ?? eventType;
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

export function getAiTaskAuditMetadataSummary(
  metadataJson: string | undefined,
): string | undefined {
  if (!metadataJson?.trim()) {
    return undefined;
  }

  try {
    const metadata = JSON.parse(metadataJson) as Record<string, unknown>;
    const parts: string[] = [];

    if (typeof metadata.reason === "string" && metadata.reason.trim()) {
      parts.push(metadata.reason.trim());
    }

    if (typeof metadata.priority === "string") {
      parts.push(`Öncelik: ${metadata.priority}`);
    }

    if (typeof metadata.dueAtUtc === "string") {
      parts.push(`Son tarih: ${metadata.dueAtUtc}`);
    }

    if (typeof metadata.escalationLevel === "number") {
      parts.push(`Eskalasyon seviyesi: ${metadata.escalationLevel}`);
    }

    if (typeof metadata.breachType === "string") {
      parts.push(`İhlal türü: ${metadata.breachType}`);
    }

    if (typeof metadata.source === "string") {
      parts.push(`Kaynak: ${metadata.source}`);
    }

    return parts.length > 0 ? parts.join(" · ") : metadataJson;
  } catch {
    return metadataJson;
  }
}
