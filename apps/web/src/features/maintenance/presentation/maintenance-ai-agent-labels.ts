import type { MaintenanceAiDraftStatus, MaintenanceAiQualitativeConfidence } from "../domain/maintenance-ai-agent";

export const MAINTENANCE_AI_AGENT_PROMPT_VERSION = "MAINT-AI-AGENT-V1";

export const MAINTENANCE_AI_SAFETY_COPY = [
  "AI çıktısı karar desteğidir.",
  "Bakım talebi yalnızca yetkili kullanıcı onayıyla oluşturulur.",
  "AI bakım emri oluşturmaz.",
  "AI makineye komut göndermez.",
  "Güncelliğini yitiren taslak yeniden hazırlanmalıdır.",
] as const;

export const MAINTENANCE_AI_LOCAL_PROVIDER_DISCLAIMER =
  "Yerel deterministic provider gerçek model doğrulaması değildir.";

const STATUS_LABELS: Record<MaintenanceAiDraftStatus, string> = {
  draft: "Taslak",
  "under-review": "İncelemede",
  approved: "Onaylandı",
  rejected: "Reddedildi",
  stale: "Güncelliğini Yitirdi",
  executed: "Uygulandı",
  "execution-failed": "Uygulama Başarısız",
};

const CONFIDENCE_LABELS: Record<MaintenanceAiQualitativeConfidence, string> = {
  low: "Düşük",
  medium: "Orta",
  high: "Yüksek",
};

export function getMaintenanceAiDraftStatusLabel(status: MaintenanceAiDraftStatus): string {
  return STATUS_LABELS[status] ?? "Bilinmiyor";
}

export function getMaintenanceAiConfidenceLabel(confidence: MaintenanceAiQualitativeConfidence): string {
  return CONFIDENCE_LABELS[confidence] ?? "Bilinmiyor";
}

export function getMaintenanceAiRequestTypeLabel(type: string): string {
  switch (type) {
    case "fault":
      return "Arıza";
    case "corrective":
      return "Düzeltici";
    case "preventive":
      return "Önleyici";
    case "inspection":
      return "Kontrol";
    default:
      return "Diğer";
  }
}

export function getMaintenanceAiPriorityLabel(priority: string): string {
  switch (priority) {
    case "urgent":
      return "Acil";
    case "high":
      return "Yüksek";
    case "medium":
      return "Orta";
    case "low":
      return "Düşük";
    default:
      return priority;
  }
}
