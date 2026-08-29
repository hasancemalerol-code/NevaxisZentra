export const MOLD_FOLLOW_UP_TASK_DISCLAIMER =
  "Otomatik bakım emri, kalıp yaşam döngüsü veya üretim kararı uygulanmaz.";

const FOLLOW_UP_STATUS_LABELS: Record<string, string> = {
  "requires-review": "İnceleme gerekli",
  acknowledged: "Görüldü",
  "task-created": "Görev oluşturuldu",
  resolved: "Çözüldü",
  dismissed: "Reddedildi",
};

const TASK_PURPOSE_LABELS: Record<string, string> = {
  "human-review-draft": "Kalıp teknik inceleme görev taslağı",
  "data-review-draft": "Kalıp veri inceleme görev taslağı",
};

export function getMoldFollowUpStatusLabel(status: string): string {
  return FOLLOW_UP_STATUS_LABELS[status] ?? status;
}

export function getMoldFollowUpTaskPurposeLabel(purpose?: string): string {
  if (!purpose) return "—";
  return TASK_PURPOSE_LABELS[purpose] ?? purpose;
}

export function getMoldFollowUpTaskButtonLabel(hasInsufficientEvidence: boolean): string {
  return hasInsufficientEvidence
    ? "Kalıp veri inceleme görev taslağı oluştur"
    : "Kalıp teknik inceleme görev taslağı oluştur";
}
