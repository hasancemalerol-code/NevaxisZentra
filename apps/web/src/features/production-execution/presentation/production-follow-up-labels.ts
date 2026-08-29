export const PRODUCTION_FOLLOW_UP_TASK_DISCLAIMER =
  "Otomatik üretim emri, makine, stok veya kalite kararı uygulanmaz.";

const FOLLOW_UP_STATUS_LABELS: Record<string, string> = {
  "requires-review": "İnceleme gerekli",
  acknowledged: "Görüldü",
  "task-created": "Görev oluşturuldu",
  resolved: "Çözüldü",
  dismissed: "Reddedildi",
};

const EXCEPTION_TYPE_LABELS: Record<string, string> = {
  "stale-downtime": "Süresi dolmuş duruş",
  "stale-setup": "Süresi dolmuş kurulum",
  "quantity-anomaly": "Miktar anomalisi",
  "cycle-anomaly": "Çevrim anomalisi",
  "schedule-overrun": "Plan aşımı",
  "handoff-inconsistency": "Devir tutarsızlığı",
  "operator-attention": "Operatör dikkati",
  other: "Diğer",
};

const SEVERITY_LABELS: Record<string, string> = {
  info: "Bilgi",
  warning: "Uyarı",
  critical: "Kritik",
};

export function getProductionFollowUpStatusLabel(status: string): string {
  return FOLLOW_UP_STATUS_LABELS[status] ?? status;
}

export function getProductionExceptionTypeLabel(type: string): string {
  return EXCEPTION_TYPE_LABELS[type] ?? type;
}

export function getProductionExceptionSeverityLabel(severity: string): string {
  return SEVERITY_LABELS[severity] ?? severity;
}

export function getProductionFollowUpTaskButtonLabel(hasInsufficientEvidence: boolean): string {
  return hasInsufficientEvidence
    ? "Veri inceleme görev taslağı oluştur"
    : "Üretim inceleme görev taslağı oluştur";
}
