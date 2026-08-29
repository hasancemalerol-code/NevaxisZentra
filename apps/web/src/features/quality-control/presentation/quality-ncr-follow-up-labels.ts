export const QUALITY_NCR_FOLLOW_UP_TASK_DISCLAIMER =
  "Otomatik üretim, stok veya kalite kararı uygulanmaz.";

const FOLLOW_UP_STATUS_LABELS: Record<string, string> = {
  "requires-review": "İnceleme gerekli",
  acknowledged: "Görüldü",
  "task-created": "Görev oluşturuldu",
  resolved: "Çözüldü",
  dismissed: "Reddedildi",
};

const TASK_PURPOSE_LABELS: Record<string, string> = {
  "human-review-draft": "Kalite inceleme görev taslağı",
  "data-review-draft": "Veri inceleme görev taslağı",
};

export function getQualityNcrFollowUpStatusLabel(status: string): string {
  return FOLLOW_UP_STATUS_LABELS[status] ?? status;
}

export function getQualityNcrFollowUpTaskPurposeLabel(purpose?: string): string {
  if (!purpose) return "—";
  return TASK_PURPOSE_LABELS[purpose] ?? purpose;
}

export function getQualityNcrFollowUpSeverityTone(
  severity: string,
): "danger" | "warning" | "neutral" {
  if (severity === "critical") return "danger";
  if (severity === "major") return "warning";
  return "neutral";
}
